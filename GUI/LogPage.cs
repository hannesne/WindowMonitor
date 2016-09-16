using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Core;
using System.Diagnostics;

namespace GUI
{
    public partial class LogPage : Form
    {
        private readonly WindowMonitor monitor;
        private IDisposable windowTitles;

        /// <summary>
        /// Generates the message format for the log textbox
        /// </summary>
        private string LogMessage(TraceLevel level, string message) => $"{DateTime.Now.ToString("hh:MM:ss.fff")} [{level}]  {message}\r\n{LogTextBox.Text}";

        public LogPage()
        {
            InitializeComponent();
            FormClosed += LogPage_FormClosed;

            //instantiates window hooks, subscribes to observable stream of data
            monitor = new WindowMonitor();
            windowTitles = monitor.WindowTitles.Subscribe(
               onNext: (title) => LogTextBox.Text = LogMessage(TraceLevel.Info, title),
               onError: (ex) => LogTextBox.Text = LogMessage(TraceLevel.Error, ex.Message)
            );
        }



        private void LogPage_FormClosed(object sender, FormClosedEventArgs e)
        {
            windowTitles?.Dispose();
            monitor?.Dispose();
        }
    }
}
