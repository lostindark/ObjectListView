using System;
using System.Collections.Generic;
using System.Windows.Forms;
using BrightIdeasSoftware;

namespace ListViewPrinterDemo
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
#if !NETFRAMEWORK
            if (!Application.SetHighDpiMode(HighDpiMode.PerMonitorV2))
            {
                throw new InvalidOperationException("The demo must configure DPI awareness before initializing Windows Forms.");
            }

            Application.SetColorMode(SystemColorMode.System);
#endif
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            FormTheme.SetColorMode(ObjectListViewColorMode.System);
            Application.Run(new Form1());
        }
    }
}