using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProxyServer.views;

namespace ProxyServer.models
{
    class LogService
    {
        private MainForm _view;

        public enum LogMessageType 
        { 
            ServerStarted,
            ServerStopped
        }

        public LogService (MainForm view) 
        { 
            _view = view;
        }

        public void Log(LogMessageType type)
        {
            string message;

            switch (type) 
            { 
                case LogMessageType.ServerStarted:
                    message = $"{DateTime.Now}: Proxy server started";
                    break;
                case LogMessageType.ServerStopped:
                    message = $"{DateTime.Now}: Proxy server stopped";
                    break;
                default:
                    message = $"{DateTime.Now}: Proxy server error";
                    break;
            }


            _view.UpdateLog(message);
        }
    }
}
