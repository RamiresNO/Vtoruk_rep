﻿using NetSdrClientApp;
using NetSdrClientApp.Networking;
using System;
using System.Threading.Tasks;

namespace NetSdrClientApp
{

    else if (key == ConsoleKey.F)
    public static class Program
    {

        public static async Task Main(string[] args)
        {
            Console.WriteLine(@"Usage: C-connect, D-disconnect, F-freq, S-Start/Stop, Q-quit");

            // Налаштування залежностей
            var tcpClient = new TcpClientWrapper("127.0.0.1", 5000);
            var udpClient = new UdpClientWrapper(60000);
            var netSdr = new NetSdrClient(tcpClient, udpClient);
            
            // Використовуємо наш новий процесор
            var processor = new CommandProcessor(netSdr);

            bool isRunning = true;
            while (isRunning)
            {
                // Цей рядок неможливо протестувати, але це єдине, що залишилось не покритим!
                var key = Console.ReadKey(intercept: true).Key;
                
                // Вся логіка тепер тут, і вона покрита тестами:
                isRunning = await processor.HandleKeyAsync(key);
            }
        }
    }
}