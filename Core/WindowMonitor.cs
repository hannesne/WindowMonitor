using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    /// <summary>
    /// This class raises events whenever the current foreground window, or it's title changes. It uses Win32 hooks to be sent events asynchronously, similar to how a screen reader would work.
    /// Refer here to the relevant win32 system APIs:
    /// SetWinEventHook function: https://msdn.microsoft.com/en-us/library/windows/desktop/dd373640(v=vs.85).aspx
    /// Event Constants: https://msdn.microsoft.com/en-us/library/windows/desktop/dd318066(v=vs.85).aspx
    /// WinEventProc callback function: https://msdn.microsoft.com/en-us/library/windows/desktop/dd373885(v=vs.85).aspx
    /// GetForegroundWindow function: https://msdn.microsoft.com/en-us/library/ms633505
    /// GetWindowText function: https://msdn.microsoft.com/en-us/library/windows/desktop/ms633520(v=vs.85).aspx
    /// The code was inspired from this Stack Overflow question: http://stackoverflow.com/questions/8840926/asynchronously-getforegroundwindow-via-sendmessage-or-something/11943387#11943387
    /// Also this one here: http://stackoverflow.com/questions/115868/how-do-i-get-the-title-of-the-current-active-window-using-c
    /// </summary>
    public class WindowMonitor : IDisposable
    {
        private readonly List<GCHandle> callbackHandles = new List<GCHandle>();
        private string activeWindowTitle = "";
        private const int MaximumTitleLength = 256;

        //create RX observables for the event stream rather than classic .NET events
        private Subject<string> windowTitlesSubject = new Subject<string>();
        public IObservable<string> WindowTitles { get { return windowTitlesSubject.AsObservable(); } }

        public WindowMonitor()
        {
            //create hooks for name changes and switching the active window.
            CreateHook(EventConstants.EVENT_SYSTEM_FOREGROUND);
            CreateHook(EventConstants.EVENT_OBJECT_NAMECHANGE);
        }

        private void CreateHook(EventConstants eventConstant)
        {
            //we create a GCHandle to prevent the callback from being garbage collected. 
            //save it so that we can free it up when this class is disposed.
            WindowsEventDelegate eventCallback = WindowsEventCallback;
            GCHandle callbackHandle = GCHandle.Alloc(eventCallback, GCHandleType.Normal);
            callbackHandles.Add(callbackHandle);
            //this wil register a hook outside the context of the tareted process, and won't listen to events for our own process.
            SetWinEventHook((uint)eventConstant, (uint)eventConstant, IntPtr.Zero,
                           eventCallback, 0, 0, (uint)(SetWinEventHookParameter.WINEVENT_OUTOFCONTEXT | SetWinEventHookParameter.WINEVENT_SKIPOWNPROCESS));


        }

        private void WindowsEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            try
            {
                if (IsNotForegroundSwitchEvent(eventType) && IsNotFromWindowObject(idObject))
                    return;

                string currentWindowTitle = GetActiveWindowTitle();
                lock (activeWindowTitle)
                    if (currentWindowTitle != string.Empty && activeWindowTitle != currentWindowTitle)
                    {
                        //bubble window title up to observable
                        windowTitlesSubject.OnNext(currentWindowTitle);
                        activeWindowTitle = currentWindowTitle;
                    }
            }
            catch (Exception ex)
            {
                //bubble exception up to observable
                windowTitlesSubject.OnError(ex);
            }
        }

        private static bool IsNotFromWindowObject(int idObject)
        {
            return idObject != 0x0;
        }

        private static bool IsNotForegroundSwitchEvent(uint eventType)
        {
            return (EventConstants)eventType != EventConstants.EVENT_SYSTEM_FOREGROUND;
        }

        public string GetActiveWindowTitle()
        {
            StringBuilder builder = new StringBuilder(MaximumTitleLength);
            IntPtr handle = GetForegroundWindow();
            GetWindowText(handle, builder, MaximumTitleLength);
            return builder.ToString();
        }

        #region win32 interop definitions
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", EntryPoint = "SetWinEventHook", SetLastError = true)]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WindowsEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        public void Dispose()
        {
            callbackHandles.ForEach(handle => handle.Free());
        }

        // Callback function
        delegate void WindowsEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        // Enums
        public enum EventConstants : uint

        {
            EVENT_AIA_START = 0xA000,
            EVENT_AIA_END = 0xAFFF,
            EVENT_MIN = 0x00000001,
            EVENT_MAX = 0x7FFFFFFF,
            EVENT_OBJECT_ACCELERATORCHANGE = 0x8012,
            EVENT_OBJECT_CONTENTSCROLLED = 0x8015,
            EVENT_OBJECT_CREATE = 0x8000,
            EVENT_OBJECT_DEFACTIONCHANGE = 0x8011,
            EVENT_OBJECT_DESCRIPTIONCHANGE = 0x800D,
            EVENT_OBJECT_DESTROY = 0x8001,
            EVENT_OBJECT_DRAGSTART = 0x8021,
            EVENT_OBJECT_DRAGCANCEL = 0x8022,
            EVENT_OBJECT_DRAGCOMPLETE = 0x8023,
            EVENT_OBJECT_DRAGENTER = 0x8024,
            EVENT_OBJECT_DRAGLEAVE = 0x8025,
            EVENT_OBJECT_DRAGDROPPED = 0x8026,
            EVENT_OBJECT_END = 0x80FF,
            EVENT_OBJECT_FOCUS = 0x8005,
            EVENT_OBJECT_HELPCHANGE = 0x8010,
            EVENT_OBJECT_HIDE = 0x8003,
            EVENT_OBJECT_HOSTEDOBJECTSINVALIDATED = 0x8020,
            EVENT_OBJECT_IME_HIDE = 0x8028,
            EVENT_OBJECT_IME_SHOW = 0x8027,
            EVENT_OBJECT_IME_CHANGE = 0x8029,
            EVENT_OBJECT_INVOKED = 0x8013,
            EVENT_OBJECT_LIVEREGIONCHANGED = 0x8019,
            EVENT_OBJECT_LOCATIONCHANGE = 0x800B,
            EVENT_OBJECT_NAMECHANGE = 0x800C,
            EVENT_OBJECT_PARENTCHANGE = 0x800F,
            EVENT_OBJECT_REORDER = 0x8004,
            EVENT_OBJECT_SELECTION = 0x8006,
            EVENT_OBJECT_SELECTIONADD = 0x8007,
            EVENT_OBJECT_SELECTIONREMOVE = 0x8008,
            EVENT_OBJECT_SELECTIONWITHIN = 0x8009,
            EVENT_OBJECT_SHOW = 0x8002,
            EVENT_OBJECT_STATECHANGE = 0x800A,
            EVENT_OBJECT_TEXTEDIT_CONVERSIONTARGETCHANGED = 0x8030,
            EVENT_OBJECT_TEXTSELECTIONCHANGED = 0x8014,
            EVENT_OBJECT_VALUECHANGE = 0x800E,
            EVENT_OEM_DEFINED_START = 0x0101, EVENT_OEM_DEFINED_END = 0x01FF,
            EVENT_SYSTEM_ALERT = 0x0002,
            EVENT_SYSTEM_ARRANGMENTPREVIEW = 0x8016,
            EVENT_SYSTEM_CAPTUREEND = 0x0009,
            EVENT_SYSTEM_CAPTURESTART = 0x0008,
            EVENT_SYSTEM_CONTEXTHELPEND = 0x000D,
            EVENT_SYSTEM_CONTEXTHELPSTART = 0x000C,
            EVENT_SYSTEM_DESKTOPSWITCH = 0x0020,
            EVENT_SYSTEM_DIALOGEND = 0x0011,
            EVENT_SYSTEM_DIALOGSTART = 0x0010,
            EVENT_SYSTEM_DRAGDROPEND = 0x000F,
            EVENT_SYSTEM_DRAGDROPSTART = 0x000E,
            EVENT_SYSTEM_END = 0x00FF,
            EVENT_SYSTEM_FOREGROUND = 0x0003,
            EVENT_SYSTEM_MENUPOPUPEND = 0x0007,
            EVENT_SYSTEM_MENUPOPUPSTART = 0x0006,
            EVENT_SYSTEM_MENUEND = 0x0005,
            EVENT_SYSTEM_MENUSTART = 0x0004,
            EVENT_SYSTEM_MINIMIZEEND = 0x0017,
            EVENT_SYSTEM_MINIMIZESTART = 0x0016,
            EVENT_SYSTEM_MOVESIZEEND = 0x000B,
            EVENT_SYSTEM_MOVESIZESTART = 0x000A,
            EVENT_SYSTEM_SCROLLINGEND = 0x0013,
            EVENT_SYSTEM_SCROLLINGSTART = 0x0012,
            EVENT_SYSTEM_SOUND = 0x0001,
            EVENT_SYSTEM_SWITCHEND = 0x0015,
            EVENT_SYSTEM_SWITCHSTART = 0x0014,
            EVENT_UIA_EVENTID_START = 0x4E00,
            EVENT_UIA_EVENTID_END = 0x4EFF,
            EVENT_UIA_PROPID_START = 0x7500,
            EVENT_UIA_PROPID_END = 0x75FF


        }



        [Flags]
        internal enum SetWinEventHookParameter
        {
            WINEVENT_OUTOFCONTEXT = 0,
            WINEVENT_SKIPOWNTHREAD = 1,
            WINEVENT_SKIPOWNPROCESS = 2,
            WINEVENT_INCONTEXT = 4
        }

        #endregion
    }
}
