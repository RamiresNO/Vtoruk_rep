using System;
using System.Threading.Tasks;

namespace NetSdrClientApp
{
    public class CommandProcessor
    {
        private readonly NetSdrClient _client;

        public CommandProcessor(NetSdrClient client)
        {
            _client = client;
        }

        // Цей метод ми легко протестуємо! Він не чекає консолі, він просто приймає кнопку.
        public async Task<bool> HandleKeyAsync(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.C:
                    await _client.ConnectAsync();
                    return true; // Продовжуємо роботу

                case ConsoleKey.D:
                    _client.Disconect();
                    return true;

                case ConsoleKey.F:
                    await _client.ChangeFrequencyAsync(20000000, 1);
                    return true;

                case ConsoleKey.S:
                    if (_client.IQStarted)
                        await _client.StopIQAsync();
                    else
                        await _client.StartIQAsync();
                    return true;

                case ConsoleKey.Q:
                    return false; // Сигнал для виходу

                default:
                    return true; // Ігноруємо інші кнопки
            }
        }
    }
}