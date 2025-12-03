﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

// Виправлено простір імен
namespace NetSdrClientApp.Networking;

public class UdpClientWrapper : IUdpClient
{
    private readonly IPEndPoint _localEndPoint;
    private CancellationTokenSource? _cts;
    private UdpClient? _udpClient;

    public event EventHandler<byte[]>? MessageReceived;

    public UdpClientWrapper(int port)
    {
        // Слухаємо на всіх інтерфейсах на вказаному порту
        _localEndPoint = new IPEndPoint(IPAddress.Any, port);
    }

    public Task StartListeningAsync()
    {
        if (_udpClient != null) return Task.CompletedTask;

        _cts = new CancellationTokenSource();
        _udpClient = new UdpClient(_localEndPoint);

        // Запускаємо прослуховування в окремому потоці/завданні
        return Task.Run(async () =>
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested && _udpClient != null)
                {
                    // ReceiveAsync у UdpClient не приймає токен напряму в старих версіях,
                    // тому використовуємо таку конструкцію або новіші методи .NET
                    var result = await _udpClient.ReceiveAsync();
                    MessageReceived?.Invoke(this, result.Buffer);
                }
            }
        }
            catch (ObjectDisposedException) 
            {
                // Ігноруємо помилку при закритті сокета
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UDP Error: {ex.Message}");
            }
        }, _cts.Token);
    }

    public void StopListening()
    {
        _cts?.Cancel();
    }

    // Реалізуємо повне очищення, щоб не було помилок "Dispose _cts"
    public void Exit()
    {

        StopListening();

        _udpClient?.Close();
        _udpClient?.Dispose();
        _udpClient = null;

        _cts?.Dispose();
        _cts = null;
    }
}