using System;
using System.IO;
using System.Reflection;
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

        public ProgramRunner Start(ProgramArgs args)
        {
            if (!NormalizeArgs(args))
                return null;

            _args = args;

            var binDir = new DirectoryInfo(args.BinDirectory);
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

                _watcher = new FileSystemWatcher(binDir.FullName)
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

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("Watching directory: " + binDir.FullName);
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
                Environment.Exit(0);
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
    }
}