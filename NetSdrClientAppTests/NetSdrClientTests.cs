using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using System.Reflection;

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