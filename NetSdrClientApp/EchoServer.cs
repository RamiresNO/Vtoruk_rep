using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp // <--- ВАЖЛИВО: Простір імен як у основного проекту
{
    public class EchoServer
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

        // Цей метод ми будемо тестувати
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
            _cancellationTokenSource.Cancel();
            _listener.Stop();
        }
    }
}