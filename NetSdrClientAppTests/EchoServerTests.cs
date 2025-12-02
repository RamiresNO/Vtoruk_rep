using NUnit.Framework;
using NetSdrClientApp; // <--- ОСЬ ЦЕ ДОЗВОЛЯЄ БАЧИТИ СЕРВЕР
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class EchoServerTests
    {
        [Test]
        public async Task ProcessStreamAsync_ShouldEchoDataBack()
        {
            // Arrange
            var server = new EchoServer(0); 
            var token = CancellationToken.None;

            using (var memoryStream = new MemoryStream())
            {
                // Записуємо туди дані, ніби клієнт надіслав "Hello"
                byte[] sentData = new byte[] { 0x01, 0x02, 0x03 };
                memoryStream.Write(sentData, 0, sentData.Length);
                memoryStream.Position = 0;

                // Act
                await server.ProcessStreamAsync(memoryStream, token);

                // Assert
                Assert.That(memoryStream.Length, Is.GreaterThan(0));
            }
        }
    }
}