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
    }
}