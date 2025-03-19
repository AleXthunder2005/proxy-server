using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ProxyServer.views;
using System.Threading;
using System.Net;
using static ProxyServer.Settings;
using System.IO;
using System.Security.Policy;
using ProxyServer.controllers;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices.ComTypes;

namespace ProxyServer.models
{
    class ProxyService
    {
        private MainForm _view;
        private ProxyController _controller;

        private Task _tcpListenerTask;
        private TcpListener _tcpListener;
        private CancellationTokenSource _cancellationTokenSource;
        private Blocker _blocker;
        private CacheService _cacheService;
        private bool _isRunning = false;

        private const string END_LINE = "\r\n";

        public ProxyService(MainForm view, ProxyController controller) 
        { 
            _view = view;
            _controller = controller;
        }

        public void StartProxy(int port)
        {
            try
            {
                _isRunning = true;
                _blocker = new Blocker(_view);
                _cacheService = new CacheService(_view);
                _tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
                _tcpListener.Start();
                _cancellationTokenSource = new CancellationTokenSource();
                _tcpListenerTask = Task.Run(() => ListenTCPConnection(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                _view.SafeUpdateLog($"{DateTime.Now}: Error - {ex.Message}");
            }
        }

        public void StopProxy()
        {
            _isRunning = false;
            _cancellationTokenSource.Cancel();
        }

        private async Task ListenTCPConnection(CancellationToken cancelationToken)
        {
            cancelationToken.Register(() => { _tcpListener.Stop(); });

            while (_isRunning)
            {
                try
                {
                    TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(tcpClient);
                }
                catch (ObjectDisposedException ex)
                {
                    break;
                }
                catch (Exception ex)
                {
                    //_view.SafeUpdateLog($"{DateTime.Now}: Error - {ex.Message}");
                    break ;
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            NetworkStream clientStream = client.GetStream();
            string[] requestLines = await ReadLinesFromNetworkStream(clientStream);

            if (requestLines.Length < 1) return;
            string method, requestUrl, httpVersion;
            ParseHTTPStartLine(requestLines[0], out method, out requestUrl, out httpVersion);
            
            if (!Uri.TryCreate(requestUrl, UriKind.Absolute, out Uri url)) return;

            string host = url.Host;
            int port = url.Port;
            string path = url.PathAndQuery;
            if (string.IsNullOrEmpty(host)) return;

            //Черный список
            if (_blocker.IsBlocked(host))
            {
                await SendBlockMessageAsync(clientStream);
                _view.SafeUpdateLog($"{DateTime.Now}: {method} {host}{path}:{port} - 403 Forbidden\r\n");
                return;
            }

            if (_cacheService.ContainsInCache(requestUrl)) 
            {
                byte[] data = _cacheService.GetFromCache(requestUrl);
                await clientStream.WriteAsync(data, 0, data.Length);
                _view.SafeUpdateLog($"{DateTime.Now}: {method} {host}{path}:{port} - 200 OK (from cache)\r\n");
                return;
            }

            TcpClient server = new TcpClient();
            try
            {
                await server.ConnectAsync(host, port);
                NetworkStream serverStream = server.GetStream();
                serverStream.ReadTimeout = 1000;

                requestLines[0] = $"{method} {path} {httpVersion}";// производим замену длинного url на короткий

                await SendRequestAsync(serverStream, requestLines);

                byte[] statusLineBytes = await ReceiveStatusLineAsync(serverStream);
                string statusLine = Encoding.UTF8.GetString(statusLineBytes);
                
                _view.SafeUpdateLog($"{DateTime.Now}: {method} {host}{path}:{port} - {statusLine}");
                await clientStream.WriteAsync(statusLineBytes, 0, statusLineBytes.Length); //отправили строку статуса

                //if (bytesRead == DEFAULT_HTTP_BUFFER_SIZE)
                    await TransmitRequestAsync(serverStream, clientStream, method, requestUrl, statusLineBytes); //передаем возвращаем оставшуюсю часть клиенту
            }
            catch (Exception ex)
            {
                   // _view.SafeUpdateLog($"{DateTime.Now}: Error - {ex.Message}");
            }
            finally
            {
                server.Client.Shutdown(SocketShutdown.Both);
                server.Close();
                server.Dispose();

                client.Close();
                client.Dispose();
            }
        }

        private async Task TransmitRequestAsync(NetworkStream serverStream, NetworkStream clientStream, string method, string url, byte[] statusLineBytes)
        {
            string statusLine = Encoding.UTF8.GetString(statusLineBytes);
            bool needCaching = (!_cacheService.ContainsInCache(url) && (method == "GET") && Int32.Parse(statusLine.Split(' ')[1]) == 200);

            MemoryStream responseStream = new MemoryStream();
            responseStream.Write(statusLineBytes, 0, statusLineBytes.Length);
            
            
            byte[] buffer = new byte[DEFAULT_HTTP_BUFFER_SIZE];
            int bytesRead = await serverStream.ReadAsync(buffer, 0, buffer.Length);
            if (needCaching && bytesRead != DEFAULT_HTTP_BUFFER_SIZE)
            {
                responseStream.Write(buffer, 0, bytesRead);
                _cacheService.AddToCache(url, responseStream.ToArray());
            }

            do
            {
                await clientStream.WriteAsync(buffer, 0, bytesRead);
            } while ((bytesRead = await serverStream.ReadAsync(buffer, 0, buffer.Length)) > 0);
        }

        private void ParseHTTPStartLine(string startLine, out string method, out string url, out string httpVersion) 
        {
            string[] requestParams = startLine.Split(' ');
            method = requestParams[0];
            url = requestParams[1];
            httpVersion = requestParams[2];
        }

        private async Task<string[]> ReadLinesFromNetworkStream(NetworkStream clientStream)
        {
            byte[] buffer = new byte[DEFAULT_HTTP_BUFFER_SIZE];
            int bytesRead = 0;

            using (MemoryStream stream = new MemoryStream())
            {
                //читаем запрос клиента
                while ((bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, bytesRead);

                    if (bytesRead < DEFAULT_HTTP_BUFFER_SIZE) break;  //больше фрагментов нет
                }
                buffer = stream.ToArray();
            }

            // парсим заголовок на куски

            string requestText = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            string[] requestLines = Regex.Split(requestText, @"\r\n")
                                         .ToArray();
            return requestLines;
        }

        private async Task SendRequestAsync(NetworkStream stream, string[] requestLines)
        {
            foreach (var requestLine in requestLines)
            {
                if (string.IsNullOrEmpty(requestLine)) break;

                byte[] lineBytes = Encoding.UTF8.GetBytes(requestLine + (requestLine == "\r\n" ? "" : "\r\n"));
                await stream.WriteAsync(lineBytes, 0, lineBytes.Length);
            }
            await stream.WriteAsync(Encoding.UTF8.GetBytes("\r\n"), 0, 2);
        }

        private async Task SendResponseAsync(NetworkStream stream, string[] responseLines)
        {
            foreach (var responseLine in responseLines)
            {
                byte[] lineBytes = Encoding.UTF8.GetBytes(responseLine + "\r\n");
                await stream.WriteAsync(lineBytes, 0, lineBytes.Length);
            }
            // Добавляем пустую строку, отделяя заголовки от тела ответа
            await stream.WriteAsync(Encoding.UTF8.GetBytes("\r\n"), 0, 2);
        }



        private string ParseResponseStatus(string responseStatus)
        {
            string[] responseStatusWords = responseStatus.Split(' ');
            StringBuilder sb = new StringBuilder();
            sb.Append($"{responseStatusWords[1]} ");
            for (int i = 2; i < responseStatusWords.Length; i++)
            {
                 sb.Append(responseStatusWords[i]);
            }

            return sb.ToString();
        }

        private async Task<byte[]> ReceiveStatusLineAsync(NetworkStream serverStream)
        {
            byte[] buffer = new byte[2];

            List<byte> responseLineBytes = new List<byte>(); // Сюда собираем первую строку
            bool isFirstLineRead = false;

            // Читаем первую строку вручную (до \r\n)
            while (!isFirstLineRead && await serverStream.ReadAsync(buffer, 0, 1) > 0)
            {
                responseLineBytes.Add(buffer[0]);

                // Проверяем конец строки (\r\n)
                if (responseLineBytes.Count >= 2 &&
                    responseLineBytes[responseLineBytes.Count - 2] == '\r' &&
                    responseLineBytes[responseLineBytes.Count - 1] == '\n')
                {
                    isFirstLineRead = true;
                }
            }

            // Преобразуем байты первой строки ответа в строку
            return responseLineBytes.ToArray();
        }
        private async Task SendBlockMessageAsync(NetworkStream stream)
        {
            // получаем длину тела в байтах
            int contentLength = Encoding.UTF8.GetByteCount(_blocker.ResponseBody);
            // формируем ответ
            string response = "HTTP/1.1 403 Forbidden\r\n" +
                              "Content-Type: text/html; charset=utf-8\r\n" +
                              $"Content-Length: {contentLength}\r\n" +
                              "\r\n" +
                              _blocker.ResponseBody;

            // кодируем в байты ответ
            byte[] buffer = Encoding.UTF8.GetBytes(response);

            // отправляем ответ клиенту
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

    }
}
