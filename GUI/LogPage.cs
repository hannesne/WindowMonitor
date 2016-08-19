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

namespace GUI
{
    public partial class LogPage : Form
    {
        private readonly WindowMonitor monitor;

        public LogPage()
        {
            InitializeComponent();
            FormClosed += LogPage_FormClosed;
            monitor = new WindowMonitor();
            monitor.WindowChanged += (sender, windowText) => LogTextBox.Text = DateTime.Now.ToString("hh:MM:ss.fff") + " - " + windowText + Environment.NewLine + LogTextBox.Text;
        }

        private void LogPage_FormClosed(object sender, FormClosedEventArgs e)
        {
            monitor?.Dispose();
        }
    }
}
