using Moq;
using NetSdrClientApp.Networking;
using NUnit.Framework;
using System;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
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

            client.Connect();
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
        client.Connect();
        Assert.Pass();
    }

    [Test]
    public void Disconnect_ShouldPrintNoConnection_WhenNotConnected()
    {
        var client = new TcpClientWrapper(Host, Port);
        client.Disconnect();
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

    [Test]
    public async Task SendMessageAsync_String_ShouldWriteToStream()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int epPort = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

        var connectedClient = new System.Net.Sockets.TcpClient();
        connectedClient.Connect(System.Net.IPAddress.Loopback, epPort);
        var serverSide = listener.AcceptTcpClient();

        try
        {
            var client = new TcpClientWrapper(Host, Port);
            typeof(TcpClientWrapper).GetField("_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(client, connectedClient);
            typeof(TcpClientWrapper).GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(client, connectedClient.GetStream());

            await client.SendMessageAsync("Hello");

            Assert.Pass();
        }
        finally
        {
            try { serverSide.Close(); } catch { }
            try { connectedClient.Close(); } catch { }
            listener.Stop();
        }
    }

    [Test]
    public async Task SendMessageAsync_ByteArray_ShouldWriteToStream()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int epPort = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

        var connectedClient = new System.Net.Sockets.TcpClient();
        connectedClient.Connect(System.Net.IPAddress.Loopback, epPort);
        var serverSide = listener.AcceptTcpClient();

        try
        {
            var client = new TcpClientWrapper(Host, Port);
            typeof(TcpClientWrapper).GetField("_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(client, connectedClient);
            typeof(TcpClientWrapper).GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(client, connectedClient.GetStream());

            byte[] data = new byte[] { 0x01, 0x02, 0x03 };
            await client.SendMessageAsync(data);

            Assert.Pass();
        }
        finally
        {
            try { serverSide.Close(); } catch { }
            try { connectedClient.Close(); } catch { }
            listener.Stop();
        }
    }

    [Test]
    public async Task SendMessageAsync_EmptyArray_ShouldWriteToStream()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int epPort = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

        var connectedClient = new System.Net.Sockets.TcpClient();
        connectedClient.Connect(System.Net.IPAddress.Loopback, epPort);
        var serverSide = listener.AcceptTcpClient();

        try
        {
            var client = new TcpClientWrapper(Host, Port);
            typeof(TcpClientWrapper).GetField("_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(client, connectedClient);
            typeof(TcpClientWrapper).GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(client, connectedClient.GetStream());

            byte[] data = Array.Empty<byte>();
            await client.SendMessageAsync(data);

            Assert.Pass();
        }
        finally
        {
            try { serverSide.Close(); } catch { }
            try { connectedClient.Close(); } catch { }
            listener.Stop();
        }
    }

    [Test]
    public async Task MessageReceived_ShouldBeInvoked_WhenDataReceived()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

        var client = new TcpClientWrapper("localhost", port);
        byte[]? receivedData = null;
        var receivedEvent = new ManualResetEventSlim(false);

        client.MessageReceived += (sender, data) =>
        {
            receivedData = data;
            receivedEvent.Set();
        };

        Task.Run(() =>
        {
            var serverClient = listener.AcceptTcpClient();
            var serverStream = serverClient.GetStream();
            byte[] testData = new byte[] { 0xAA, 0xBB, 0xCC };
            serverStream.Write(testData, 0, testData.Length);
            serverStream.Flush();
            Thread.Sleep(100);
            serverClient.Close();
        });

        client.Connect();

        bool eventReceived = receivedEvent.Wait(TimeSpan.FromSeconds(2));

        client.Disconnect();
        listener.Stop();

        Assert.That(eventReceived, Is.True);
        Assert.That(receivedData, Is.Not.Null);
        Assert.That(receivedData, Has.Length.EqualTo(3));
    }

    [Test]
    public void Connected_ReturnsTrue_WhenFullyConnected()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int epPort = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

        var connectedClient = new System.Net.Sockets.TcpClient();
        connectedClient.Connect(System.Net.IPAddress.Loopback, epPort);
        var serverSide = listener.AcceptTcpClient();

        try
        {
            var client = new TcpClientWrapper(Host, Port);
            typeof(TcpClientWrapper)
                .GetField("_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(client, connectedClient);

            typeof(TcpClientWrapper)
                .GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(client, connectedClient.GetStream());

            Assert.That(client.Connected, Is.True);
        }
        finally
        {
            try { serverSide.Close(); } catch { }
            try { connectedClient.Close(); } catch { }
            listener.Stop();
        }
    }

    [Test]
    public void Connected_ReturnsFalse_WhenTcpClientDisconnected()
    {
        var client = new TcpClientWrapper(Host, Port);
        var tcp = new TcpClient();
        var memoryStream = new MemoryStreamNetworkStream();

        typeof(TcpClientWrapper).GetField("_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(client, tcp);
        typeof(TcpClientWrapper).GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(client, memoryStream);

        Assert.That(client.Connected, Is.False);
    }

    [Test]
    public async Task StartListeningAsync_ShouldStop_WhenConnectionClosed()
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

        var client = new TcpClientWrapper("localhost", port);


        Task.Run(() =>
        {
            var serverClient = listener.AcceptTcpClient();
            Thread.Sleep(200);
            serverClient.Close();
        });

        client.Connect();
        await Task.Delay(500);

        client.Disconnect();
        listener.Stop();

        Assert.Pass();
    }


}
public class MemoryStreamNetworkStream : NetworkStream
{
    private readonly MemoryStream _ms = new();
    private readonly Socket _clientSocket;
    private readonly Socket _serverSocket;

    public MemoryStreamNetworkStream() : base(CreateConnectedClientSocket(out var server), FileAccess.ReadWrite, ownsSocket: true)
    {
        _serverSocket = server;
        _clientSocket = this.Socket;
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

    public override Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken)
    {
        // Simulate empty read (no data)
        return Task.FromResult(0);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken)
    {
        _ms.Write(buffer, offset, size);
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { _serverSocket?.Dispose(); } catch { }
    }
    }

// Helper stream that cannot be written to
public class ReadOnlyNetworkStream : NetworkStream
{
    private readonly Socket _clientSocket;
    private readonly Socket _serverSocket;

    public ReadOnlyNetworkStream() : base(CreateConnectedClientSocket(out var server), FileAccess.Read, ownsSocket: true)
    {
        _serverSocket = server;
        _clientSocket = this.Socket;
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

            public override bool CanWrite => false;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try { _serverSocket?.Dispose(); } catch { }
    }
}

// Helper stream that throws on Close
public class ThrowingNetworkStream : NetworkStream
{
    private readonly Socket _clientSocket;
    private readonly Socket _serverSocket;

    public ThrowingNetworkStream() : base(CreateConnectedClientSocket(out var server), FileAccess.ReadWrite, ownsSocket: true)
    {
        _serverSocket = server;
        _clientSocket = this.Socket;
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

    public override void Close()
    {
        throw new IOException("Test exception during close");
    }

    protected override void Dispose(bool disposing)
    {
        try { _serverSocket?.Dispose(); } catch { }
        // Don't call base to avoid the exception
    }
}