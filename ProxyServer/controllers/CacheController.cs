using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProxyServer.models;

namespace ProxyServer.controllers
{
    class CacheController
    {
        private CacheService _cacheService;

        public CacheController(CacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public ResponseModel GetFromCache(string url)
        {
            return _cacheService.GetFromCache(url);
        }

        public void AddToCache(string url, ResponseModel response)
        {
            _cacheService.AddToCache(url, response);
        }
    }
}
