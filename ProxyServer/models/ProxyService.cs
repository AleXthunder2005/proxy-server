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

namespace ProxyServer.models
{
    class ProxyService
    {
        private MainForm _view;
        private ProxyController _controller;

        private Task _tcpListenerTask;
        private TcpListener _tcpListener;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning = false;

        public ProxyService(MainForm view, ProxyController controller) 
        { 
            _view = view;
            _controller = controller;
        }

        public void StartProxy(int port)
        {
            _isRunning = true;

            _tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
            _tcpListener.Start();

            _cancellationTokenSource = new CancellationTokenSource();

            _tcpListenerTask = Task.Run(() => ListenTCPConnection(_cancellationTokenSource.Token));
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
                TcpClient tcpClient;
                try
                {
                    tcpClient = await _tcpListener.AcceptTcpClientAsync();
                }
                catch (ObjectDisposedException ex)
                {
                    break;
                }
               await HandleClientAsync(tcpClient);
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            
            using (NetworkStream clientStream = client.GetStream())
            
            {
                // Чтение данных от клиента
                byte[] buffer = new byte[DEFAULT_HTTP_BUFFER_SIZE];
                int bytesRead;

                bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string method, host, path;
                    int port;

                    ParseHTTPRequest(buffer, out method, out host, out path, out port);

                    TcpClient target = new TcpClient(host, port);
                    NetworkStream targetStream = target.GetStream();

                    //перенаправление запроса  (из него надо вырезать имя хоста оставить только путь !!!)
                    int startUrlIndex, endHostIndex;
                    CalculateHostScope(buffer, out startUrlIndex, out endHostIndex);
                    await targetStream.WriteAsync(buffer, 0, startUrlIndex);
                    await targetStream.WriteAsync(buffer, endHostIndex+1, bytesRead - (endHostIndex + 1));  //очень сомнительно!!!

                    while ((bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await targetStream.WriteAsync(buffer, 0, bytesRead);
                    } 


                    //Читаем ответ от сервера
                    int status;
                    string explanation;
                    bytesRead = await targetStream.ReadAsync(buffer, 0, buffer.Length);
                    
                    ParseHTTPResponse(buffer, out status, out explanation);
                    
                    //перенаправление ответа клиенту
                    do
                    {
                        await clientStream.WriteAsync(buffer, 0, bytesRead);
                    } while ((bytesRead = await targetStream.ReadAsync(buffer, 0, buffer.Length)) > 0);

                    _view.UpdateLog($"{method} http:{host}{path}:{port} - {status} ({explanation})");
                }
            }
        }

        private void ParseHTTPRequest(byte[] buffer, out string method, out string host, out string path, out int port) 
        {
            //<Метод> <URI> <Версия HTTP>

            byte[] methodBytes = new byte[10];
            int i = 0;
            int size = 0;
            while (buffer[i] != ' ') 
            {
                methodBytes[i] = (byte)buffer[i];
                i++;
                size++;
            }
            method = Encoding.UTF8.GetString(methodBytes, 0, size);

            //------------------------------------------------------------------

            i++;
            size = 0;
            byte[] URLBytes = new byte[DEFAULT_URL_SIZE];
            while ( buffer[i] != ' ')  //поиск URL
            {
                URLBytes[i] = (byte)buffer[i];
                i++;
                size++;
            }
            string url = Encoding.UTF8.GetString(methodBytes, 0, size);
            Uri targetURL = new Uri(url);
            host = targetURL.Host;
            port = targetURL.Port;
            path = targetURL.AbsolutePath;
        }

        private void ParseHTTPResponse(byte[] buffer, out int status, out string explanation)
        {
            //<Версия HTTP> <Код статуса> <Пояснение>

            int i = 0;
            while (buffer[i] != ' ')
            {
                i++;
            }
            i++;

            //отделение статуса

            byte[] statusBytes = new byte[10];
            int size = 0;
            while (buffer[i] != ' ')
            {
                statusBytes[i] = (byte)buffer[i];
                i++;
                size++;
            }
            status = Int32.Parse(Encoding.UTF8.GetString(statusBytes, 0, size));
            i++;

            //------------------------------------------------------------------

            size = 0;
            byte[] explanationBytes = new byte[DEFAULT_EXPLANATION_SIZE];
            while (buffer[i] != '\r' || buffer[i] != '\n')  //поиск пояснения
            {
                explanationBytes[i] = (byte)buffer[i];
                i++;
                size++;
            }
            explanation = Encoding.UTF8.GetString(explanationBytes, 0, size);
        }

        private void CalculateHostScope(byte[] buffer, out int startUrlIndex, out int endHostIndex)  
        {
            //GET 'http://ds'/path/to/resource HTTP-version

            int i = 0;
            while (buffer[i] != ' ') i++;
            i++;
            startUrlIndex = i;

            int slashCounter = 3;
            while (slashCounter != 0) 
            {
                if (buffer[i] == '/') slashCounter--;
                i++;
            }
            
            endHostIndex = --i;
        }

    }
}
