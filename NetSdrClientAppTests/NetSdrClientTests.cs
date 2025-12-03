using Moq;
using NUnit.Framework;
using NetSdrClientApp;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class NetSdrClientTests
    {
        private NetSdrClient _client;
        private Mock<ITcpClient> _tcpMock;
        private Mock<IUdpClient> _updMock;

        [SetUp]
        public void Setup()
        {

            _tcpMock = new Mock<ITcpClient>();
            
            // Налаштування поведінки Connect/Disconnect для властивості Connected
            _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
            {
                _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
            });

            _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
            {
                _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
            });

            // Налаштування SendMessageAsync, щоб імітувати подію MessageReceived
            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
            {
                // Тут можна імітувати відповідь сервера, якщо потрібно
                // _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
            });

            _updMock = new Mock<IUdpClient>();

            _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
        }

        [Test]
        public async Task ConnectAsyncTest()
        {

            // Act
            await _client.ConnectAsync();

            // Assert
            _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
            // Перевіряємо, що відправились 3 конфігураційні повідомлення
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
        }

        [Test]
        public void DisconnectWithNoConnectionTest()
        {
            // Act
            _client.Disconect();

            // Assert
            // Має викликати Disconnect навіть якщо ми не підключені (безпечне відключення)
            _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
        }

        [Test]
        public async Task DisconnectTest()
        {
            // Arrange 
            await ConnectAsyncTest();

            // Act
            _client.Disconect();

            // Assert
            _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
        }

        [Test]
        public async Task StartIQNoConnectionTest()
        {
            // Ensure not connected
            _tcpMock.Setup(t => t.Connected).Returns(false);

            // Act
            await _client.StartIQAsync();

            // Assert
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
            _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
        }

        [Test]
        public async Task StartIQTest()
        {
            // Arrange 
            await ConnectAsyncTest(); // Стаємо Connected

            // Act
            await _client.StartIQAsync();

        // --- НОВИЙ ТЕСТ 1: Покриває if (!Connected) return; у StopIQAsync ---
        [Test]
        public async Task StopIQ_ShouldReturnEarly_WhenNotConnected()
        {
            // Arrange
            _tcpMock.Setup(t => t.Connected).Returns(false); // Явно вказуємо, що немає з'єднання

            // Act
            await _client.StopIQAsync();

            // Assert
            // Переконуємось, що не намагалися зупинити UDP або слати команди
            _updMock.Verify(u => u.StopListening(), Times.Never);
            _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
            // Перевіряємо, що прапорець не змінився помилково (або лишився false)
            Assert.That(_client.IQStarted, Is.False); 
        }

        [Test]
        public async Task StopIQTest()
        {
            // Arrange 
            await ConnectAsyncTest(); // Стаємо Connected

            // Act
            await _client.StopIQAsync();

            // Assert
            _updMock.Verify(udp => udp.StopListening(), Times.Once);
            Assert.That(_client.IQStarted, Is.False);
        }

        // --- НОВИЙ ТЕСТ 2: Покриває метод ChangeFrequencyAsync ---
        [Test]
        public async Task ChangeFrequencyAsync_ShouldSendRequest()
        {
            // Arrange
            await ConnectAsyncTest(); // Потрібне підключення, щоб відправити запит

            // Act
            long frequency = 100000;
            await _client.ChangeFrequencyAsync(frequency, 1);

            // Assert
            // Перевіряємо, що викликався метод відправки (ConnectAsync шле 3 рази + 1 раз наш ChangeFrequency = 4)
            _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(4));
        }

        // --- Тести для хелперів ---

        [Test]
        public void GetHeader_ShouldThrow_WhenLengthTooLarge()
        {
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var ex = Assert.Throws<TargetInvocationException>(() =>
            {
                var method = typeof(NetSdrMessageHelper)
                    .GetMethod("GetHeader", BindingFlags.NonPublic | BindingFlags.Static);
                method!.Invoke(null, new object[] { type, 9000 });
            });
            Assert.That(ex!.InnerException, Is.TypeOf<ArgumentException>());
        }

        [Test]
        public void GetSamples_ShouldThrow_WhenSampleSizeTooBig()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                NetSdrMessageHelper.GetSamples(64, new byte[] { 1, 2, 3 }).ToList());
        }

        [Test]
        public void GetSamples_ShouldReturnCorrectValues()
        {
            var data = new byte[] { 1, 0, 2, 0 }; // 16-bit little endian
            var samples = NetSdrMessageHelper.GetSamples(16, data).ToArray();
            Assert.That(samples.Length, Is.EqualTo(2));
            Assert.That(samples[0], Is.EqualTo(1));
            Assert.That(samples[1], Is.EqualTo(2));
        }

        [Test]
        public void TranslateHeader_ShouldHandleDataItemWithZeroLength()
        {
            var type = NetSdrMessageHelper.MsgTypes.DataItem0;
            ushort headerValue = (ushort)(((int)type << 13) + 0);
            byte[] header = BitConverter.GetBytes(headerValue);

            var method = typeof(NetSdrMessageHelper)
                .GetMethod("TranslateHeader", BindingFlags.NonPublic | BindingFlags.Static);
            object[] args = new object[] { header, null!, 0 };
            method!.Invoke(null, args);
        }
    }
}