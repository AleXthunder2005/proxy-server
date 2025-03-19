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
        private Dictionary<string, byte[]> _cache = new Dictionary<string, byte[]>();
        private MainForm _view;
        private int _cacheSize = 0;
        private const int MAX_CACHE_SIZE = 8092;


        public CacheService(MainForm view) 
        { 
            _view = view;
        }

        public byte[] GetFromCache(string url)
        {
            if (_cache.ContainsKey(url))
                return _cache[url];
            return null;
        }

        public bool ContainsInCache(string url) 
        {
            return _cache.ContainsKey(url);
        }

        public void AddToCache(string url, byte[] data)
        {
            _cache[url] = data;
            _cacheSize += data.Length;
            
            if (_cacheSize > MAX_CACHE_SIZE) _cache.Clear();
        }
    }
}
