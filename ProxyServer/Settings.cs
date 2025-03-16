using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyServer
{
    static class Settings
    {
        public const ushort DEFAULT_PORT = 8080;
        public const int DEFAULT_HTTP_BUFFER_SIZE = 4096;
        public const int DEFAULT_URL_SIZE = 1024;
        public const int DEFAULT_EXPLANATION_SIZE = 64;


        public static ushort Port { get; set; }

        public static void InitializeSettings(ushort port = DEFAULT_PORT) 
        { 
            Port = port;
        }

    }
}
