using NetSdrClientApp.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetSdrClientAppTests
{
    internal class UdpClientWrapperTests
    {
        private UdpClientWrapper _udpClientWrapper;

        [SetUp]
        public void Setup()
        {
            _udpClientWrapper = new UdpClientWrapper(5000);
        }

        [Test]
        public void Constructor_ShouldInitializeWithoutError()
        {
            // act
            var wrapper = new UdpClientWrapper(5001);

            // assert
            Assert.That(wrapper, Is.Not.Null);
        }

        [Test]
        public async Task StartAndStopListening_ShouldNotThrow()
        {
            // act
            var listenTask = _udpClientWrapper.StartListeningAsync();

            // невелика пауза, щоб метод стартував
            await Task.Delay(100);

            // act
            _udpClientWrapper.StopListening();

            // assert
            Assert.That(listenTask.IsCompleted || !listenTask.IsFaulted);
        }

        [Test]
        public void StopListening_ShouldNotThrow()
        {
            // act & assert
            Assert.DoesNotThrow(() => _udpClientWrapper.StopListening());
        }

        [Test]
        public void Exit_ShouldNotThrow()
        {
            // act & assert
            Assert.DoesNotThrow(() => _udpClientWrapper.Exit());
        }

        [Test]
        public void Equals_ShouldReturnTrueForSamePort()
        {
            // arrange
            var a = new UdpClientWrapper(6000);
            var b = new UdpClientWrapper(6000);

            // act & assert
            Assert.That(a.Equals(b), Is.True);
        }

        [Test]
        public void Equals_ShouldReturnFalseForDifferentPort()
        {
            // arrange
            var a = new UdpClientWrapper(6000);
            var b = new UdpClientWrapper(6001);

            // act & assert
            Assert.That(a.Equals(b), Is.False);
        }

        [Test]
        public void GetHashCode_ShouldBeConsistent()
        {
            // arrange
            var wrapper = new UdpClientWrapper(7000);

            // act
            var hash1 = wrapper.GetHashCode();
            var hash2 = wrapper.GetHashCode();

            // assert
            Assert.That(hash1, Is.EqualTo(hash2));
        }
               // 1. Цей тест додасть найбільше покриття (покриє receive loop та event invoke)
        [Test]
        public async Task StartListeningAsync_ShouldReceiveMessage_AndFireEvent()
        {
            // Arrange
            int testPort = 15000; // Використовуємо унікальний порт
            var wrapper = new UdpClientWrapper(testPort);
            bool eventFired = false;
            byte[] receivedData = null;

            // Підписуємось на подію
            wrapper.MessageReceived += (sender, data) =>
            {
                eventFired = true;
                receivedData = data;
            };

            // Act
            // Запускаємо слухача
            var listenTask = wrapper.StartListeningAsync();
            
            // Чекаємо трохи, щоб сокет відкрився
            await Task.Delay(100);

            // Відправляємо реальний пакет через звичайний UdpClient
            using (var senderClient = new System.Net.Sockets.UdpClient())
            {
                var bytesToSend = Encoding.UTF8.GetBytes("TestPayload");
                await senderClient.SendAsync(bytesToSend, bytesToSend.Length, "127.0.0.1", testPort);
            }

            // Чекаємо обробки (loop має спрацювати)
            await Task.Delay(500);

            // Зупиняємо
            wrapper.StopListening();
            
            // Чекаємо безпечного завершення
            try { await listenTask; } catch { /* ігноруємо помилки скасування */ }

            // Assert
            Assert.That(eventFired, Is.True, "Подія MessageReceived не спрацювала");
            Assert.That(receivedData, Is.Not.Null);
            Assert.That(Encoding.UTF8.GetString(receivedData), Is.EqualTo("TestPayload"));
        }

        // 2. Цей тест покриє гілки if (ReferenceEquals) та if (obj is not...) в методі Equals
        [Test]
        public void Equals_EdgeCases_ShouldWork()
        {
            var wrapper = new UdpClientWrapper(5555);

            // Перевірка порівняння з самим собою (ReferenceEquals)
            Assert.That(wrapper.Equals(wrapper), Is.True);

            // Перевірка на null
            Assert.That(wrapper.Equals(null), Is.False);

            // Перевірка на інший тип об'єкта
            Assert.That(wrapper.Equals(new object()), Is.False);
        }
    }
    
}