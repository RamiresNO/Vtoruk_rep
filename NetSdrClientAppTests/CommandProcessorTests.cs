using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class CommandProcessorTests
    {
        private Mock<ITcpClient> _tcpMock;
        private Mock<IUdpClient> _udpMock;
        private NetSdrClient _client;
        private CommandProcessor _processor;

        [SetUp]
        public void Setup()
        {
            _tcpMock = new Mock<ITcpClient>();
            _udpMock = new Mock<IUdpClient>();
            
            // Налаштування моків, щоб методи не падали
            _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>())).Returns(Task.CompletedTask);
            _tcpMock.Setup(t => t.Connect()).Callback(() => _tcpMock.Setup(x => x.Connected).Returns(true));
            _udpMock.Setup(u => u.StartListeningAsync()).Returns(Task.CompletedTask);

            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
            _processor = new CommandProcessor(_client);
        }

        [Test]
        public async Task HandleKey_C_ShouldConnect()
        {
            _tcpMock.Setup(t => t.Connected).Returns(false);
            
            var result = await _processor.HandleKeyAsync(ConsoleKey.C);
            
            _tcpMock.Verify(t => t.Connect(), Times.Once);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task HandleKey_D_ShouldDisconnect()
        {
            var result = await _processor.HandleKeyAsync(ConsoleKey.D);
            _tcpMock.Verify(t => t.Disconnect(), Times.Once);
            Assert.IsTrue(result);
        }

        [Test]
        public async Task HandleKey_Q_ShouldReturnFalse()
        {
            var result = await _processor.HandleKeyAsync(ConsoleKey.Q);
            Assert.IsFalse(result, "Q key should return false to stop the loop");
        }

        [Test]
        public async Task HandleKey_S_ShouldToggleIQ()
        {
            // 1. Спочатку не запущено -> має запустити
            _tcpMock.Setup(t => t.Connected).Returns(true);
            await _processor.HandleKeyAsync(ConsoleKey.S);
            Assert.IsTrue(_client.IQStarted);

            // 2. Тепер запущено -> має зупинити
            await _processor.HandleKeyAsync(ConsoleKey.S);
            Assert.IsFalse(_client.IQStarted);
        }
        
        [Test]
        public async Task HandleKey_F_ShouldChangeFrequency()
        {
            _tcpMock.Setup(t => t.Connected).Returns(true);
            await _processor.HandleKeyAsync(ConsoleKey.F);
            _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
        }
    }
}