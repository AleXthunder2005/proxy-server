using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProxyServer.models;
using ProxyServer.views;
using static ProxyServer.models.LogService;
using static ProxyServer.Settings;

namespace ProxyServer.controllers
{
    class ProxyController
    {
        private MainForm _view;
          
        private ProxyService _proxyService;
        private LogService _logService;
        private CacheService _cacheService;

        public ProxyController(MainForm view)
        {
            _view = view;
            _proxyService = new ProxyService(view, this);
            _logService = new LogService(view);
            _cacheService = new CacheService(view);
        }
        
        public void StartProxy()
        {
            _proxyService.StartProxy(Port);
            _logService.Log(LogMessageType.ServerStarted);
        }

        public void StopProxy()
        {
            _proxyService.StopProxy();
            _logService.Log(LogMessageType.ServerStopped);
        }
    }
}
