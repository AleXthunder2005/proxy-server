using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyServer.models
{
    class CacheService
    {
        private Dictionary<string, ResponseModel> _cache = new Dictionary<string, ResponseModel>();

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
