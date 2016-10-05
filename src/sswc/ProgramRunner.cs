using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Ssw.Cli
{
    internal class ProgramRunner
    {
        private ProgramArgs _args;
        private AppDomain _appHostDomain;
        private FileSystemWatcher _watcher;
        private ServerHostProxy _proxy;
        private Timer _timer;
        private string _binDirectoryPath = null;

        public ProgramRunner Start(ProgramArgs args)
        {
            if (!NormalizeArgs(args))
            {
                return null;
            }

            _args = args;

            var binDir = new DirectoryInfo(args.BinDirectory);
            _binDirectoryPath = binDir.FullName;
            _appHostDomain = AppDomain.CreateDomain("ServerHostDomain", null, binDir.FullName, null, true);
            _proxy = (ServerHostProxy)_appHostDomain.CreateInstanceFromAndUnwrap(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath, typeof(ServerHostProxy).FullName);

            try
            {
                var errorMessage = _proxy.Start(_args.AppHostAssembly, _args.AppHostType, _args.Port);
                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    throw new Exception(errorMessage);
                }

                if (_args.Port > 0)
                {
                    Console.WriteLine("Server listening at http://localhost:" + _args.Port);
                }

                if (_args.Poll <= 0)
                {
                    _watcher = new FileSystemWatcher(_binDirectoryPath)
                    {
                        Filter = "*.*",
                        IncludeSubdirectories = false
                    };
                    _watcher.Changed += Watcher_Changed;
                    _watcher.Created += Watcher_Changed;
                    _watcher.Deleted += Watcher_Changed;
                    _watcher.Renamed += Watcher_Changed;
                    // start the watcher
                    _watcher.EnableRaisingEvents = true;
                }
                else
                {
                    _timerPaused = false;
                    if (_timer == null) _timer = new Timer(Timer_Fired, null, _args.Poll, _args.Poll);
                }

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("Watching directory: " + binDir.FullName + (_timer != null ? " (polling every " + _args.Poll + " ms)": ""));
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("A fatal error occurred!");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                Console.WriteLine("Please fix the error and press enter to continue ...");
                Console.ResetColor();

                if (!Console.IsInputRedirected)
                {
                    Console.ReadLine();
                    Start(_args);
                }
            }

            return this;
        }

        public void Stop()
        {
            try
            {
                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Dispose();
                }
                _proxy?.Stop();
            }
            catch { /* No big deal, we're quitting anyway */ }

            if (!Console.IsInputRedirected)
            {
                _timer?.Dispose();
                Environment.Exit(0);
            }
        }

        private void Restart()
        {
            if (_appHostDomain != null)
            {
                AppDomain.Unload(_appHostDomain);
                _appHostDomain = null;
                Thread.Sleep(2000);
                Start(_args);
            }
            else
            {
                Stop();
            }
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (!Regex.IsMatch(e.FullPath, _args.Watch))
                return;

            // if we've already turned off events, don't do anything
            if (!_watcher.EnableRaisingEvents) return;

            // stop watching to avoid multiple triggers
            _watcher.EnableRaisingEvents = false;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(e.ChangeType + " detected to " + e.Name + " (" + e.FullPath + ")");
            Console.ResetColor();

            Restart();
        }

        private void Timer_Fired(object state)
        {
            Console.WriteLine("Timer_Fired: " + _timerPaused);

            if (_timerPaused) return;

            _timerPaused = true;

            Console.WriteLine("Checking dir " + _binDirectoryPath + " for changes");
            var changesDetected = DirectoryHasChanged(_binDirectoryPath);
            if (changesDetected)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("Changes detected in " + _binDirectoryPath);
                Console.ResetColor();

                Restart();
            }

            _timerPaused = false;
        }

        private bool _timerPaused = false;

        #region Private Methods

        private static bool NormalizeArgs(ProgramArgs pArgs)
        {
            try
            {
                if (!pArgs.AppHostAssembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                    !pArgs.AppHostAssembly.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    pArgs.AppHostAssembly += ".dll";
                }

                var bin = pArgs.BinDirectory;
                if (!string.IsNullOrWhiteSpace(bin) &&
                    !Directory.Exists(bin))
                {
                    throw new DirectoryNotFoundException("Could not find bin directory: " + pArgs.BinDirectory);
                }

                if (!string.IsNullOrWhiteSpace(bin))
                {
                    pArgs.AppHostAssembly = Path.Combine(bin, pArgs.AppHostAssembly);
                }

                if (!File.Exists(pArgs.AppHostAssembly))
                {
                    throw new FileNotFoundException("Could not find assembly file: " + pArgs.AppHostAssembly);
                }

                pArgs.BinDirectory = Path.GetDirectoryName(pArgs.AppHostAssembly);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.GetType().Name + ": " + ex.Message);
                Console.ResetColor();
                return false;
            }
            return true;
        }

        #endregion Private Methods

        #region Config File Methods
        //internal static string GetConfigFile()
        //{
        //    var configFile = GetConfigFilePath();
        //    if (configFile == null)
        //        return null;

        //    return File.Exists(configFile) ? configFile : null;
        //}

        //private static void SaveConfigFile(ProgramArgs pArgs)
        //{
        //    var configFile = GetConfigFilePath();
        //    if (configFile == null)
        //        return;

        //    File.WriteAllText(configFile, pArgs.ToXml());
        //}

        //private static string GetConfigFilePath()
        //{
        //    //var exe = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
        //    var exe = Assembly.GetExecutingAssembly().Location;
        //    var configFileName = Path.GetFileNameWithoutExtension(exe) + ".config";
        //    var dirName = Path.GetDirectoryName(exe);
        //    return dirName == null ? null : Path.Combine(dirName, configFileName);
        //}

        #endregion

        #region FileCompare

        private DateTime? _lastAccessTime = null;
        private string _directoryHash = null;
        private bool DirectoryHasChanged(string directory)
        {
            var dir = new DirectoryInfo(directory);
            var lastAccessTime = dir.LastAccessTime;
            var directoryHasChanged = false;

            var rebuildingDirHash = _directoryHash == null ||
                                    _lastAccessTime.GetValueOrDefault(DateTime.MinValue) != lastAccessTime;

            if (rebuildingDirHash)
            {
                var sb = new StringBuilder();
                foreach (var fi in dir.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    if (!Regex.IsMatch(fi.FullName, _args.Watch))
                        continue;
                    sb.Append("0;" + 
                        string.Format("{0}{1}{2}{3}", fi.Name, fi.Length, fi.CreationTime, fi.LastWriteTime)
                            .GetHashCode());
                }
                var directoryHash = sb.ToString();
                directoryHasChanged = _directoryHash != null && _directoryHash != directoryHash;

                _lastAccessTime = lastAccessTime;
                _directoryHash = directoryHash;
            }

            return directoryHasChanged;
        }
        #endregion
    }
}