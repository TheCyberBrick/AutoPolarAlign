using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
using System.Windows.Automation;
using System.Threading;
using System.IO;
using System.Globalization;

namespace AutoPolarAlign
{
    public class OptronIPolarSetup
    {
        public class Settings
        {
            public bool CenterXFound = false;

            public float CenterX;

            public bool CenterYFound = false;

            public float CenterY;
        }


        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SendMessageTimeoutA(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam, int fuFlags, int uTimeout, out IntPtr lpdwResult);

        private static readonly int SMTO_NORMAL = 0x0000;
        private static readonly int SMTO_ABORTIFHUNG = 0x0002;

        private static readonly int BM_CLICK = 0xF5;

        private static readonly int WM_CLOSE = 0x0010;

        private static readonly int ERROR_TIMEOUT = 0x5B4;


        public double Timeout { get; set; } = 10;

        public bool AutoLoadDark { get; set; } = true;

        public string ProcessName { get; set; } = "iOptron iPolar";

        public string ApplicationPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "iOptron iPolar", "iOptron iPolar.exe");


        public bool Run(out Settings settings)
        {
            StartIPolar();
            return ConnectIPolar(out settings);
        }

        public void StopIPolar()
        {
            foreach (var process in Process.GetProcessesByName(ProcessName))
            {
                process.Kill();
            }
        }

        public void StartIPolar()
        {
            // Once iPolar has connected UI automation doesn't work anymore
            // for some reason, so we just restart iPolar to run the setup
            StopIPolar();

            var startInfo = new ProcessStartInfo
            {
                FileName = ApplicationPath,
                WorkingDirectory = Path.GetDirectoryName(ApplicationPath)
            };

            Process.Start(startInfo);
        }

        public bool ConnectIPolar(out Settings settings)
        {
            settings = new Settings();

            DateTime startTime = DateTime.Now;

            bool CheckTimeout()
            {
                return (DateTime.Now - startTime).TotalSeconds > Timeout;
            }

            var window = FindAndCheckWindow(ProcessName, "MainPanel", Timeout);

            if (CheckTimeout())
            {
                return false;
            }

            if (!FindAndClickButton(window, "buttonSettings", Timeout))
            {
                return false;
            }

            if (CheckTimeout())
            {
                return false;
            }

            var settingsWindow = FindAndCheckWindow(ProcessName, "Settings", Timeout);

            if (CheckTimeout())
            {
                return false;
            }

            if (FindFloatElement(settingsWindow, "maskedTextBoxX", out float centerX) && centerX > float.Epsilon)
            {
                settings.CenterX = centerX;
                settings.CenterXFound = true;
            }

            if (FindFloatElement(settingsWindow, "maskedTextBoxY", out float centerY) && centerY > float.Epsilon)
            {
                settings.CenterY = centerY;
                settings.CenterYFound = true;
            }

            if (AutoLoadDark)
            {
                if (!FindAndClickButton(settingsWindow, "buttonLoadLastDarkFrame", Timeout))
                {
                    return false;
                }

                if (CheckTimeout())
                {
                    return false;
                }
            }

            SendMessageTimeoutA(new IntPtr(settingsWindow.Current.NativeWindowHandle), WM_CLOSE, IntPtr.Zero, IntPtr.Zero, SMTO_NORMAL | SMTO_ABORTIFHUNG, (int)Math.Ceiling(Timeout * 1000), out var _);

            if (CheckTimeout())
            {
                return false;
            }

            // blocking: true would be better, but seems to hang once iPolar has connected
            if (!FindAndClickButton(window, "buttonConnect", Timeout, blocking: false))
            {
                return false;
            }

            return true;
        }

        private AutomationElement FindAndCheckWindow(string processName, string automationId, double timeout)
        {
            var window = FindWindow(processName, automationId, timeout);
            if (window == null)
            {
                throw new Exception("iPolar application is not running");
            }
            return window;
        }

        private AutomationElement FindWindow(string processName, string automationId, double timeout)
        {
            DateTime startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalSeconds <= timeout)
            {
                var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, automationId != null ? new PropertyCondition(AutomationElement.AutomationIdProperty, automationId) : Condition.TrueCondition);

                foreach (AutomationElement window in windows)
                {
                    try
                    {
                        var process = Process.GetProcessById(window.Current.ProcessId);
                        if (process.ProcessName == processName)
                        {
                            return window;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Process not running
                    }
                }

                Thread.Sleep(100);
            }

            return null;
        }

        private bool FindAndClickButton(AutomationElement parent, string automationId, double timeout, bool blocking = true)
        {
            DateTime startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalSeconds <= timeout)
            {
                var handle = FindElementHandle(parent, automationId);

                if (handle != IntPtr.Zero)
                {
                    int result = SendMessageTimeoutA(handle, BM_CLICK, IntPtr.Zero, IntPtr.Zero, SMTO_NORMAL | SMTO_ABORTIFHUNG, !blocking ? 1 : (int)Math.Ceiling(timeout * 1000), out var _);

                    if (result != 0)
                    {
                        return true;
                    }
                    else
                    {
                        var lastError = Marshal.GetLastWin32Error();
                        if (!blocking && lastError == ERROR_TIMEOUT)
                        {
                            return true;
                        }
                        return false;
                    }
                }

                Thread.Sleep(100);
            }

            return false;
        }

        private AutomationElement FindElement(AutomationElement parent, string automationId)
        {
            return parent.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
        }

        private IntPtr FindElementHandle(AutomationElement parent, string automationId)
        {
            AutomationElement element = FindElement(parent, automationId);

            if (element != null)
            {
                return new IntPtr(element.Current.NativeWindowHandle);
            }

            return IntPtr.Zero;
        }

        private bool FindFloatElement(AutomationElement parent, string automationId, out float value)
        {
            var element = FindElement(parent, automationId);

            if (element != null)
            {
                string valueStr = element.Current.Name;

                if (float.TryParse(valueStr.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                {
                    return true;
                }
            }

            value = 0;
            return false;
        }

        private void InspectWindow(AutomationElement window)
        {
            if (window != null)
            {
                foreach (AutomationElement element in window.FindAll(TreeScope.Descendants, Condition.TrueCondition))
                {
                    Console.WriteLine("> " + element.Current.ControlType.ProgrammaticName);
                    Console.WriteLine("  " + element.Current.Name);
                    Console.WriteLine("  " + element.Current.AutomationId);
                    Console.WriteLine();
                }
            }
        }
    }
}
