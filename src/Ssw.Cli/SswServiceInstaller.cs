using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Ssw.Cli
{
    [RunInstaller(true)]
    public class SswServiceInstaller : ServiceInstallerBase
    {
        public SswServiceInstaller() :
            base(
            ProgramArgs.DefaultServiceName,
            null,
            null,
            true,
            ServiceAccount.LocalSystem,
            ServiceStartMode.Automatic)
        {
        }
    }
}
