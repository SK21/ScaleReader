using System;
using System.Windows.Forms;

namespace ScaleDisplay
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // SerialPort's internal event-loop thread throws ObjectDisposedException
            // ("Safe handle has been closed") when the port is closed on shutdown.
            // Suppress it — everything else is re-thrown.
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                if (ex.ExceptionObject is ObjectDisposedException)
                    return;
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
