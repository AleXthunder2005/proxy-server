using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProxyServer.models;

namespace ProxyServer.controllers
{
    class LogController
    {
        private LogService _logService;

        public LogController(LogService logService)
        {
            _logService = logService;
        }

        public void Log(string message)
        {
            _logService.Log(message);
        }
    }
}
