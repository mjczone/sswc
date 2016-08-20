using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml.Linq;

namespace Ssw.Cli
{
    public class HelpArgs
    {
        [Display(Name = "help")]
        public bool Help { get; set; }
        //public string assembly { get; set; }
        //public int port { get; set; }
        //public string bin { get; set; }
        //public string apphost { get; set; }
    }

    [Description(@"
NOTE: In the following examples, the 'sswc.exe' executable
      is in the PATH environment variable. Otherwise, point to the
      full path of the 'sswc.exe' file.

  sswc.exe .\bin\MyAppHostAssembly.dll
  sswc.exe .\bin\MyAppHostAssembly.dll /port=2020
  sswc.exe /bin=.\bin /assembly=MyAppHostAssembly.dll
  sswc.exe /assembly=.\bin\MyAppHostAssembly.dll
  sswc.exe /bin=.\bin /type=""MyAppHostAssembly.MyAppHost, MyAppHostAssembly""
  sswc.exe /bin=.\bin /assembly=MyAppHostAssembly.dll /type=MyAppHostAssembly.MyAppHost
")]
    public class ProgramArgs
    {
        private const string DefaultPort = "2020";
        private const string DefaultWatchPattern = @"\.*";

        internal const string DefaultServiceName = @"SuperSimpleWeb";

        public ProgramArgs()
        {
            Port = int.Parse(DefaultPort);
            BinDirectory = Environment.CurrentDirectory;
            Watch = DefaultWatchPattern;
            ServiceName = DefaultServiceName;
        }

        [Required, Display(Name = "assembly", Description = "Assembly file with server hosting implementation(s).", Order = 1)]
        public string AppHostAssembly { get; set; }

        [Display(Name = "port", Description = "HTTP port", Order = 10), DefaultValue(typeof(int), DefaultPort)]
        public int Port { get; set; }

        [Display(Name = "bin", Description = "Directory with all the assemblies", Order = 11), DefaultValue(typeof(string), "current directory or assembly file directory")]
        public string BinDirectory { get; set; }

        [Display(Name = "type", Description = "Server hosting type name to use. Useful when there might be multiple server hosting implementations in the assembly.", Order = 12)
            , DefaultValue(typeof(string), "scans the assembly for a ServiceStack AppSelfHostBase implementation")]
        public string AppHostType { get; set; }

        [Display(Name = "watch", Description = "Regex watch pattern for recycling the dev server and detecting changes in the bin directory.", Order = 13), DefaultValue(typeof(string), DefaultWatchPattern)]
        public string Watch { get; set; }

        [Display(Name = "install", Description = "Install windows service.", Order = 30)]
        public bool Install { get; set; }

        [Display(Name = "uninstall", Description = "Uninstall windows service.", Order = 31)]
        public bool Uninstall { get; set; }

        [Display(Name = "serviceName", Description = "Windows service name.", Order = 32), DefaultValue(typeof(string), DefaultServiceName)]
        public string ServiceName { get; set; }

        [Display(Name = "serviceDisplayName", Description = "Windows service display name.", Order = 33)]
        public string ServiceDisplayName { get; set; }

        [Display(Name = "serviceDescription", Description = "Windows service description.", Order = 34)]
        public string ServiceDescription { get; set; }

        //[Display(Name = "config", Description = "Windows service configuration id.", Order = 35)]
        //public string _Config { get; set; }

        [Display(Name = "help", Description = "Display command line usage information.", Order = 99)]
        public bool Help { get; set; }

        internal string[] GetServiceInstallerArgs()
        {
            Console.WriteLine("DEBUG AppHostAssembly: " + AppHostAssembly);
            var args = new List<string>{ $"/assembly=\"{this.AppHostAssembly}\"" };

            if (this.Port != int.Parse(DefaultPort))  args.Add($"/port={this.Port}");
            if (!string.IsNullOrWhiteSpace(this.BinDirectory)) args.Add($"/bin=\"{this.BinDirectory}\"");
            if (!string.IsNullOrWhiteSpace(this.AppHostType)) args.Add($"/type=\"{this.AppHostType}\"");
            if (!string.IsNullOrWhiteSpace(this.Watch)) args.Add($"/watch=\"{this.Watch}\"");

            return args.ToArray();
        }

        //internal static ProgramArgs FromXml(string xml)
        //{
        //    var rootElement = XElement.Parse(xml);
        //    var programArgs = new ProgramArgs();
        //    foreach (var el in rootElement.Elements())
        //    {
        //        switch (el.Name.LocalName)
        //        {
        //            case "AppHostAssembly":
        //                programArgs.AppHostAssembly = el.Value;
        //                break;
        //            case "AppHostType":
        //                programArgs.AppHostType = el.Value;
        //                break;
        //            case "BinDirectory":
        //                programArgs.BinDirectory = el.Value;
        //                break;
        //            case "Port":
        //                int port;
        //                if (int.TryParse(el.Value, out port))
        //                    programArgs.Port = port;
        //                break;
        //            case "Watch":
        //                programArgs.Watch = el.Value;
        //                break;
        //            case "ServiceName":
        //                programArgs.ServiceName = el.Value;
        //                break;
        //            case "ServiceDisplayName":
        //                programArgs.ServiceDisplayName = el.Value;
        //                break;
        //            case "ServiceDescription":
        //                programArgs.ServiceDescription = el.Value;
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //    return programArgs;
        //}

        //internal string ToXml()
        //{
        //    var dict = new Dictionary<string, string>
        //    {
        //        {"AppHostAssembly", this.AppHostAssembly},
        //        {"AppHostType", this.AppHostType},
        //        {"BinDirectory", this.BinDirectory},
        //        {"Port", this.Port.ToString()},
        //        {"Watch", this.Watch},
        //        {"ServiceName", this.ServiceName},
        //        {"ServiceDisplayName", this.ServiceDisplayName},
        //        {"ServiceDescription", this.ServiceDescription}
        //    };
        //    var el = new XElement("server", dict.Select(kv => new XElement(kv.Key, kv.Value)));
        //    return el.ToString(SaveOptions.None);
        //}
    }
}