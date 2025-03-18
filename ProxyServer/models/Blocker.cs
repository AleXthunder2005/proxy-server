using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ProxyServer.views;

namespace ProxyServer.models
{
    public class Blocker
    {
        private string _responseBody = @"
            <!DOCTYPE html>
            <html lang=""ru"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>403 Forbidden</title>
                <style>
                    body {
                        font-family: Arial, sans-serif;
                        text-align: center;
                        margin: 50px;
                    }
                    h1 {
                        font-size: 2em;
                        color: #d9534f;
                    }
                    p {
                        font-size: 1.2em;
                    }
                </style>
            </head>
            <body>
                <h1>403 Forbidden</h1>
                <p>У вас нет доступа к этому ресурсу.</p>
            </body>
            </html>";

        private static HashSet<string> _blockedSites;
        private MainForm _view;

        public Blocker(MainForm view, string path = "blacklist.txt")
        {
            _view = view;
            _blockedSites = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            LoadFromFile(path); // Автоматическая загрузка черного списка
        }

        public bool Add(string site)
        {
            return _blockedSites.Add(site);
        }

        public bool IsBlocked(string site)
        {
            return _blockedSites.Contains(site);
        }

        public string ResponseBody => _responseBody;

        public static void UpdateBlackList(string[] blacklist) 
        {
            _blockedSites = new HashSet<string>();
            foreach (var line in blacklist)
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed)) // Игнорируем пустые строки
                {
                    _blockedSites.Add(trimmed);
                }
            }
        }

        private void LoadFromFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    foreach (var line in File.ReadLines(path))
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#")) // Игнорируем пустые строки и комментарии
                        {
                            _blockedSites.Add(trimmed);
                        }
                    }
                }
                else
                {
                    _view.SafeUpdateLog($"{DateTime.Now}: Файл с чёрным списком {path} не найден, список блокировок пуст");
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[Blocker] Ошибка загрузки файла {path}: {ex.Message}");
            }
        }
    }
}
