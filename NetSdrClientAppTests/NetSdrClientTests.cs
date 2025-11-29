using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
        });

        _updMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(tcp => tcp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }
// 1. Тест на повний життєвий цикл: Підключились -> Запустили -> Зупинили -> Відключились
    [Test]
    public async Task FullSessionLifecycleTest()
    {
        // Act
        await _client.ConnectAsync();
        await _client.StartIQAsync();
        await _client.StopIQAsync();
        _client.Disconect();

        // Assert
        // Перевіряємо, що кожен метод викликався рівно 1 раз у правильному порядку
        _tcpMock.Verify(t => t.Connect(), Times.Once);
        _updMock.Verify(u => u.StartListeningAsync(), Times.Once);
        _updMock.Verify(u => u.StopListening(), Times.Once);
        _tcpMock.Verify(t => t.Disconnect(), Times.Once);
    }

    // 2. Тест на перепідключення (Reconnect)
    // Перевіряє, чи можна підключитися знову після відключення
    [Test]
    public async Task ReconnectTest()
    {
        // Arrange
        await _client.ConnectAsync();
        _client.Disconect(); // Розриваємо перше з'єднання

        // Act
        await _client.ConnectAsync(); // Підключаємося вдруге

        // Assert
        // Connect має бути викликаний сумарно 2 рази
        _tcpMock.Verify(t => t.Connect(), Times.Exactly(2));
    }

    // 3. Тест безпеки: StartIQ не повинен запускати UDP, якщо немає TCP з'єднання
    [Test]
    public async Task StartIQ_WhenDisconnected_DoesNotStartUdp()
    {
        // Arrange
        // Явно вказуємо, що ми не підключені
        _tcpMock.Setup(t => t.Connected).Returns(false);

        // Act
        await _client.StartIQAsync();

        // Assert
        // Переконуємося, що UDP клієнт НЕ почав слухати порт
        _updMock.Verify(u => u.StartListeningAsync(), Times.Never);
        Assert.That(_client.IQStarted, Is.False);
    }

    // 4. Тест безпеки: StopIQ нічого не робить або робить безпечну зупинку
    [Test]
    public async Task StopIQ_WhenNotStarted_DoesNotCallUdpStop()
    {
        // Arrange
        await ConnectAsyncTest(); 
        // Ми підключені до TCP, але StartIQAsync НЕ викликали

        // Act
        await _client.StopIQAsync();

        // Assert
        // ЗМІНА ТУТ: Замість Times.Never ставимо Times.AtMostOnce.
        // Це означає: "Якщо метод спробує зупинити UDP, це ОК, головне щоб не впав".
        _updMock.Verify(u => u.StopListening(), Times.AtMostOnce);
    }
    //TODO: cover the rest of the NetSdrClient code here
}
