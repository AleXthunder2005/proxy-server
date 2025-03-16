using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProxyServer.models;

namespace ProxyServer.views
{
    class ProxyController
    {
        private MainForm _view;
        private ProxyService _proxyService;
        private LogService _logService;

        public ProxyController(MainForm view)
        {
            _view = view;
            _proxyService = new ProxyService();
            _logService = new LogService();
        }

        public void StartProxy()
        {
            _proxyService.StartProxy(8888);
            _logService.Log("Proxy server started.");
            _view.UpdateLog("Proxy server started.");
        }

        public void StopProxy()
        {
            _proxyService.StopProxy();
            _logService.Log("Proxy server stopped.");
            _view.UpdateLog("Proxy server stopped.");
        }
    }
}
