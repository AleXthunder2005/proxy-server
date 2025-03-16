using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProxyServer.views;

namespace ProxyServer.models
{
    class CacheService
    {
        private Dictionary<string, ResponseModel> _cache = new Dictionary<string, ResponseModel>();
        private MainForm _view;


        public CacheService(MainForm view) 
        { 
            _view = view;
        }

        public ResponseModel GetFromCache(string url)
        {
            if (_cache.ContainsKey(url))
                return _cache[url];
            return null;
        }

        public void AddToCache(string url, ResponseModel response)
        {
            _cache[url] = response;
        }
    }
}
