﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

// 1. ВИПРАВЛЕННЯ: Додано простір імен (прибирає помилку з фото 3)
namespace NetSdrClientApp.Networking; 

public class UdpClientWrapper : IUdpClient
{
    private readonly IPEndPoint _localEndPoint;
    private CancellationTokenSource? _cts;
    private UdpClient? _udpClient;

    public event EventHandler<byte[]>? MessageReceived;

    public UdpClientWrapper(int port)
    {
        _localEndPoint = new IPEndPoint(IPAddress.Any, port);
    }

    public async Task StartListeningAsync()
    {
        // Запобігаємо повторному запуску
        if (_udpClient != null) return;

        try
        {
            _cts = new CancellationTokenSource();
            _udpClient = new UdpClient(_localEndPoint);

            Console.WriteLine("Start listening for UDP messages...");

            while (_cts != null && !_cts.Token.IsCancellationRequested)
            {
                // Увага: ReceiveAsync(CancellationToken) доступний у .NET 6+. 
                // Якщо у вас старіша версія, цей рядок треба змінити.
                var result = await _udpClient.ReceiveAsync(_cts.Token);
                
                MessageReceived?.Invoke(this, result.Buffer);
                
                // Для дебагу (можна прибрати)
                // Console.WriteLine($"Received from {result.RemoteEndPoint}");
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальна зупинка
        }
        catch (ObjectDisposedException)
        {
            // Сокет був закритий
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UDP Error: {ex.Message}");
        }
        finally
        {
             Console.WriteLine("UDP Listener stopped.");
        }
    }

    public void StopListening()
    {
        _cts?.Cancel();
    }

    // 2. ВИПРАВЛЕННЯ: Метод для повного очищення (прибирає помилку з фото 1 та 4)
    public void Exit()
    {
        StopListening();

        if (_udpClient != null)
        {
            _udpClient.Close();
            _udpClient.Dispose();
            _udpClient = null;
        }

        if (_cts != null)
        {
            _cts.Dispose(); // Саме це виправляє "Dispose '_cts' when it is no longer needed"
            _cts = null;
        }
    }

    // 3. ВАЖЛИВО: Перевірте низ файлу. Якщо там є методи 
    // "override Equals" або "override GetHashCode" — ВИДАЛІТЬ ЇХ.
    // Вони викликали помилку з фото 2.
}