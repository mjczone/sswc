using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Ssw.Cli
{
    public class SswService : ServiceBase
    {
        private ProgramRunner _programRunner;
        private readonly ProgramArgs _programArgs;

        public SswService() { }

        public SswService(ProgramArgs args)
        {
            _programArgs = args;
        }

        protected override void OnStart(string[] args)
        {
            if (_programArgs != null)
            {
                Start(_programArgs);
            }
        }

        protected override void OnShutdown()
        {
            Kill();
        }

        public void Start(ProgramArgs args)
        {
            _programRunner = new ProgramRunner().Start(args);
        }

        public void Kill()
        {
            try
            {
                _programRunner?.Stop();
            }
            catch
            {
                // ignore
            }
        }
    }
}
