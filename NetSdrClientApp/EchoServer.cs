using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp
{
    // 1. Додаємо ": IDisposable" до назви класу
    public class EchoServer : IDisposable
    {
        private readonly int _port;
        private TcpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;

        public EchoServer(int port)
        {
            _port = port;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Console.WriteLine($"Server started on port {_port}.");

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientWrapperAsync(client, _cancellationTokenSource.Token));
                }
                catch (ObjectDisposedException) { break; }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task HandleClientWrapperAsync(TcpClient client, CancellationToken token)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                await ProcessStreamAsync(stream, token);
            }
        }

        public async Task ProcessStreamAsync(Stream stream, CancellationToken token)
        {
            byte[] buffer = new byte[8192];
            int bytesRead;

            try
            {
                while (!token.IsCancellationRequested && 
                       (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, bytesRead, token);
                }
            }
            catch (Exception) { /* ігноруємо помилки */ }
        }

        public void Stop()
        {
            // Викликаємо Dispose, щоб не дублювати код
            Dispose();
        }

        // 2. Реалізуємо метод Dispose, який вимагає Sonar
        public void Dispose()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
            
            // Ось тут ми "звільняємо" токен, як просить помилка
            _cancellationTokenSource?.Dispose();
            _listener?.Stop();
        }
    }
}