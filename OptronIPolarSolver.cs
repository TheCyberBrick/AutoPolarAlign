using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;
using System.Threading;

namespace AutoPolarAlign
{
    public class OptronIPolarSolver : IPolarAlignmentSolver
    {
        private static readonly Regex LogRegex = new Regex(@"(?<timestamp>[0-9\s\-:.]+)\sPlateSolved:\s(?<solved>true|false),\sPole:\((?<x>[0-9]+(?:[.,][0-9]+)?),(?<y>[0-9]+(?:[.,][0-9]+)?)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public Vec2 AlignmentOffset { get; private set; } = new Vec2();


        public string LogPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "iOptron iPolar", "Logs");

        public int MaxLogAge { get; set; } = 180;

        public float CenterX { get; set; } = 480.0f;

        public float CenterY { get; set; } = 640.0f;

        public bool StartIPolar { get; set; } = true;

        public bool StopIPolar { get; set; } = true;


        public OptronIPolarSetup Setup { get; } = new OptronIPolarSetup();


        private DirectoryInfo dir = null;

        private string lastTimestamp = null;

        public void Connect()
        {
            if (StartIPolar)
            {
                if (!Setup.Run(out var settings))
                {
                    throw new Exception("Could not set up iPolar application");
                }

                if (settings.CenterXFound)
                {
                    CenterX = settings.CenterX;
                }

                if (settings.CenterYFound)
                {
                    CenterY = settings.CenterY;
                }
            }

            dir = new DirectoryInfo(LogPath);

            CheckDirectory();

            try
            {
                FindLatestLogFile();
            }
            catch (Exception ex)
            {
                throw new Exception("Could not connect to iPolar. Ensure iPolar is running and plate solving", ex);
            }
        }

        private void CheckDirectory()
        {
            if (dir == null)
            {
                throw new Exception("iPolar not connected");
            }
            else if (!dir.Exists)
            {
                throw new Exception("iPolar log directory " + LogPath + " not found");
            }
        }

        public void Disconnect()
        {
            if (StopIPolar)
            {
                Setup.StopIPolar();
            }
        }

        public void Dispose()
        {
            Disconnect();
        }

        public void Solve()
        {
            CheckDirectory();

            using (var watcher = new FileSystemWatcher(LogPath))
            using (var waitHandle = new AutoResetEvent(false))
            {
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;

                watcher.Changed += (s, e) => waitHandle.Set();
                watcher.Created += (s, e) => waitHandle.Set();
                watcher.EnableRaisingEvents = true;

                while (true)
                {
                    CheckDirectory();

                    var logFile = FindLatestLogFile();
                    var log = ReadLastLine(logFile);

                    if (TryParseLog(log))
                    {
                        break;
                    }

                    waitHandle.WaitOne(TimeSpan.FromSeconds(0.5 * MaxLogAge));
                }
            }
        }

        private bool TryParseLog(string log)
        {
            var match = LogRegex.Match(log);

            if (match.Success)
            {
                string timestamp = match.Groups["timestamp"].Value;
                if (timestamp == lastTimestamp)
                {
                    return false;
                }

                if (!bool.TryParse(match.Groups["solved"].Value, out var solved) || !solved)
                {
                    return false;
                }

                if (!float.TryParse(match.Groups["x"].Value.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
                {
                    return false;
                }

                if (!float.TryParse(match.Groups["y"].Value.Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                {
                    return false;
                }

                AlignmentOffset = new Vec2(x - CenterX, y - CenterY);

                lastTimestamp = timestamp;

                return true;
            }

            return false;
        }

        private FileInfo FindLatestLogFile()
        {
            var files = dir.GetFiles("*.txt", SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
            {
                throw new Exception("No iPolar log found in " + LogPath);
            }

            var latestFile = files.OrderByDescending(f => f.LastWriteTime).First();

            if ((DateTime.Now - latestFile.LastWriteTime).TotalSeconds > MaxLogAge)
            {
                throw new Exception("Latest iPolar log " + latestFile.FullName + " is too old");
            }

            return latestFile;
        }

        private string ReadLastLine(FileInfo file)
        {
            try
            {
                using (PowerShell ps = PowerShell.Create())
                {
                    return ps
                        .AddCommand("Get-Content")
                        .AddParameter("Path", file.FullName)
                        .AddParameter("Tail", 1)
                        .Invoke()
                        .FirstOrDefault()?.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed reading iPolar log " + file.FullName, ex);
            }
        }
    }
}
