using Moq;
using NetSdrClientApp.Networking;
using NUnit.Framework;
using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NetSdrClientAppTests;

[TestFixture]
public class TcpClientWrapperTests
{
    private const string Host = "localhost";
    private const int Port = 5000;
    private Mock<NetworkStream> _mockStream;
    private TcpClientWrapper _client;

    [SetUp]
    public void Setup()
    {
        _mockStream = new Mock<NetworkStream>();
        _client = new TcpClientWrapper("localhost", 1234);
    }

    [Test]
    public void Connected_ReturnsFalse_WhenStreamIsNull()
    {
        Assert.IsFalse(_client.Connected);
    }


    [Test]
    public void Constructor_ShouldInitializeHostAndPort()
    {
        var client = new TcpClientWrapper(Host, Port);
        Assert.That(client, Is.Not.Null);
    }

    [Test]
    public void Connect_ShouldPrintAlreadyConnected_WhenAlreadyConnected()
    {
        var client = new TcpClientWrapper(Host, Port);

        // Create a real connected TcpClient to satisfy Connected property
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int epPort = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

        var connectedClient = new System.Net.Sockets.TcpClient();
        connectedClient.Connect(System.Net.IPAddress.Loopback, epPort);
        var serverSide = listener.AcceptTcpClient();
        listener.Stop();

        try
        {
            typeof(TcpClientWrapper)
                .GetField("_tcpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(client, connectedClient);

            typeof(TcpClientWrapper)
                .GetField("_stream", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(client, connectedClient.GetStream());

            Assert.That(client.Connected, Is.True);

            client.Connect(); // should print "Already connected"
        }
        finally
        {
            try { serverSide.Close(); } catch { }
            try { connectedClient.Close(); } catch { }
        }
    }

    [Test]
    public void Connect_ShouldHandleConnectionFailure()
    {
        var client = new TcpClientWrapper("256.256.256.256", 12345);
        client.Connect(); // Повинен спіймати виняток і вивести "Failed to connect"
        Assert.Pass();
    }

    [Test]
    public void Disconnect_ShouldPrintNoConnection_WhenNotConnected()
    {
        var client = new TcpClientWrapper(Host, Port);
        client.Disconnect(); // має вивести "No active connection to disconnect."
    }

    [Test]
    public void Disconnect_ShouldCloseResources_WhenConnected()
    {
        var tcp = new TcpClient();
        var stream = new MemoryStreamNetworkStream();

        var client = new TcpClientWrapper(Host, Port);
        typeof(TcpClientWrapper).GetField("_tcpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(client, tcp);
        typeof(TcpClientWrapper).GetField("_stream", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(client, stream);
        typeof(TcpClientWrapper).GetField("_cts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(client, new System.Threading.CancellationTokenSource());

        client.Disconnect();

        Assert.That(client.Connected, Is.False);
    }

    [Test]
    public async Task SendMessageAsync_ShouldThrow_WhenNotConnected()
    {
        var client = new TcpClientWrapper(Host, Port);
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => client.SendMessageAsync("test"));
        Assert.That(ex.Message, Is.EqualTo("Not connected to a server."));
    }

}

/// <summary>
/// Простий фейковий NetworkStream для тестів без реального сокета.
/// </summary>
public class MemoryStreamNetworkStream : NetworkStream
{
    private readonly MemoryStream _ms = new();
    private readonly Socket _clientSocket;
    private readonly Socket _serverSocket;

    public MemoryStreamNetworkStream() : base(CreateConnectedClientSocket(out var server), FileAccess.ReadWrite, ownsSocket: true)
    {
        _serverSocket = server;
        _clientSocket = this.Socket; // the underlying socket used by NetworkStream
    }

    private static Socket CreateConnectedClientSocket(out Socket serverSocket)
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

        var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        client.Connect(System.Net.IPAddress.Loopback, port);

        serverSocket = listener.AcceptSocket();
        listener.Stop();
        return client;
    }

    public override bool CanRead => true;
    public override bool CanWrite => true;

    public override Task<int> ReadAsync(byte[] buffer, int offset, int size, System.Threading.CancellationToken cancellationToken)
    {
        // Simulate empty read (no data)
        return Task.FromResult(0);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int size, System.Threading.CancellationToken cancellationToken)
    {
        _ms.Write(buffer, offset, size);
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { _serverSocket?.Dispose(); } catch { }
    }

    [TestFixture]
    public class TcpWrapperTests
    {
        private TcpClientWrapper _client;
        private Mock<Stream> _mockStream;
        private CancellationTokenSource _cts;

        [SetUp]
        public void Setup()
        {
            _client = (TcpClientWrapper)Activator.CreateInstance(
                typeof(TcpClientWrapper),
                new object[] { "localhost", 5000 }
            )!;

            _mockStream = new Mock<Stream>();
            _cts = new CancellationTokenSource();

            // Приватні поля встановлюємо через рефлексію
            typeof(TcpClientWrapper).GetField("_stream", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_client, _mockStream.Object);
            typeof(TcpClientWrapper).GetField("_cts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_client, _cts);
        }

        [TearDown]
        public void TearDown()
        {
            _cts?.Dispose();
        }
       
       
    }
}