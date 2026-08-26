using System.Runtime.InteropServices;
using System.Threading;

namespace PersonalFinanceApp
{
    internal static class Program
    {
        private const string MutexName = "PersonalFinanceApp_TekOrnek_Mutex";

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(nint hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(nint hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(nint hWnd);

        private const int SW_RESTORE = 9;

        [STAThread]
        static void Main()
        {
            using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);

            if (!createdNew)
            {
                using var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                foreach (var process in System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName))
                {
                    if (process.Id == currentProcess.Id)
                        continue;

                    nint handle = process.MainWindowHandle;
                    if (handle != nint.Zero)
                    {
                        if (IsIconic(handle))
                            ShowWindow(handle, SW_RESTORE);

                        SetForegroundWindow(handle);
                    }
                }

                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new LoginForm());
        }
    }
}
