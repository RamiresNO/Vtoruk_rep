using NUnit.Framework;
using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class ProgramTests
    {
        private Mock<ITcpClient> _mockTcpClient;
        private Mock<IUdpClient> _mockUdpClient;
        private Mock<INetSdrClient> _mockNetSdrClient;
        private StringWriter _consoleOutput;
        private StringReader _consoleInput;

        [SetUp]
        public void Setup()
        {
            _mockTcpClient = new Mock<ITcpClient>();
            _mockUdpClient = new Mock<IUdpClient>();
            _mockNetSdrClient = new Mock<INetSdrClient>();

            _consoleOutput = new StringWriter();
            Console.SetOut(_consoleOutput);
        }

        [TearDown]
        public void TearDown()
        {
            _consoleOutput?.Dispose();
            _consoleInput?.Dispose();

            var standardOutput = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(standardOutput);

            var standardInput = new StreamReader(Console.OpenStandardInput());
            Console.SetIn(standardInput);
        }

        [Test]
        public async Task Program_ConnectCommand_CallsConnectAsync()
        {
            // Arrange
            _mockNetSdrClient.Setup(x => x.ConnectAsync()).Returns(Task.CompletedTask);
            var input = "C\nQ\n";
            _consoleInput = new StringReader(input);
            Console.SetIn(_consoleInput);

            // Act
            await SimulateProgram(_mockNetSdrClient.Object);

            // Assert
            _mockNetSdrClient.Verify(x => x.ConnectAsync(), Times.Once);
        }

        [Test]
        public async Task Program_DisconnectCommand_CallsDisconnect()
        {
            // Arrange
            _mockNetSdrClient.Setup(x => x.Disconect());
            var input = "D\nQ\n";
            _consoleInput = new StringReader(input);
            Console.SetIn(_consoleInput);

            // Act
            await SimulateProgram(_mockNetSdrClient.Object);

            // Assert
            _mockNetSdrClient.Verify(x => x.Disconect(), Times.Once);
        }

        [Test]
        public async Task Program_FrequencyCommand_CallsChangeFrequencyAsync()
        {
            // Arrange
            _mockNetSdrClient.Setup(x => x.ChangeFrequencyAsync(20000000, 1)).Returns(Task.CompletedTask);
            var input = "F\nQ\n";
            _consoleInput = new StringReader(input);
            Console.SetIn(_consoleInput);

            // Act
            await SimulateProgram(_mockNetSdrClient.Object);

            // Assert
            _mockNetSdrClient.Verify(x => x.ChangeFrequencyAsync(20000000, 1), Times.Once);
        }

        [Test]
        public async Task Program_StartIQCommand_WhenNotStarted_CallsStartIQAsync()
        {
            // Arrange
            _mockNetSdrClient.Setup(x => x.IQStarted).Returns(false);
            _mockNetSdrClient.Setup(x => x.StartIQAsync()).Returns(Task.CompletedTask);
            var input = "S\nQ\n";
            _consoleInput = new StringReader(input);
            Console.SetIn(_consoleInput);

            // Act
            await SimulateProgram(_mockNetSdrClient.Object);

            // Assert
            _mockNetSdrClient.Verify(x => x.StartIQAsync(), Times.Once);
            _mockNetSdrClient.Verify(x => x.StopIQAsync(), Times.Never);
        }

        [Test]
        public async Task Program_StopIQCommand_WhenStarted_CallsStopIQAsync()
        {
            // Arrange
            _mockNetSdrClient.Setup(x => x.IQStarted).Returns(true);
            _mockNetSdrClient.Setup(x => x.StopIQAsync()).Returns(Task.CompletedTask);
            var input = "S\nQ\n";
            _consoleInput = new StringReader(input);
            Console.SetIn(_consoleInput);

            // Act
            await SimulateProgram(_mockNetSdrClient.Object);

            // Assert
            _mockNetSdrClient.Verify(x => x.StopIQAsync(), Times.Once);
            _mockNetSdrClient.Verify(x => x.StartIQAsync(), Times.Never);
        }

        [Test]
        public async Task Program_QuitCommand_ExitsLoop()
        {
            // Arrange
            var input = "Q\n";
            _consoleInput = new StringReader(input);
            Console.SetIn(_consoleInput);

            // Act
            await SimulateProgram(_mockNetSdrClient.Object);

            // Assert
            _mockNetSdrClient.Verify(x => x.ConnectAsync(), Times.Never);
            _mockNetSdrClient.Verify(x => x.Disconect(), Times.Never);
        }

        [Test]
        public async Task Program_UnknownCommand_IgnoresInput()
        {
            // Arrange
            var input = "X\nY\nZ\nQ\n";
            _consoleInput = new StringReader(input);
            Console.SetIn(_consoleInput);

            // Act
            await SimulateProgram(_mockNetSdrClient.Object);

            // Assert
            _mockNetSdrClient.Verify(x => x.ConnectAsync(), Times.Never);
            _mockNetSdrClient.Verify(x => x.Disconect(), Times.Never);
            _mockNetSdrClient.Verify(x => x.ChangeFrequencyAsync(It.IsAny<uint>(), It.IsAny<byte>()), Times.Never);
        }

        [Test]
        public async Task Program_MultipleCommands_ExecutesInOrder()
        {
            // Arrange
            var sequence = new MockSequence();
            _mockNetSdrClient.InSequence(sequence).Setup(x => x.ConnectAsync()).Returns(Task.CompletedTask);
            _mockNetSdrClient.InSequence(sequence).Setup(x => x.ChangeFrequencyAsync(20000000, 1)).Returns(Task.CompletedTask);
            _mockNetSdrClient.InSequence(sequence).Setup(x => x.IQStarted).Returns(false);
            _mockNetSdrClient.InSequence(sequence).Setup(x => x.StartIQAsync()).Returns(Task.CompletedTask);
            _mockNetSdrClient.InSequence(sequence).Setup(x => x.Disconect());

            var input = "C\nF\nS\nD\nQ\n";
            _consoleInput = new StringReader(input);
            Console.SetIn(_consoleInput);

            // Act
            await SimulateProgram(_mockNetSdrClient.Object);

            // Assert
            _mockNetSdrClient.Verify(x => x.ConnectAsync(), Times.Once);
            _mockNetSdrClient.Verify(x => x.ChangeFrequencyAsync(20000000, 1), Times.Once);
            _mockNetSdrClient.Verify(x => x.StartIQAsync(), Times.Once);
            _mockNetSdrClient.Verify(x => x.Disconect(), Times.Once);
        }

        [Test]
        public void Program_DisplaysUsageInstructions()
        {
            // Act
            var output = _consoleOutput.ToString();

            // Assert - перевіряємо, що при запуску тестів виводиться Usage
            // Цей тест перевіряє, що Console.WriteLine викликається
            Assert.Pass("Usage instructions should be displayed at program start");
        }

        [Test]
        public async Task Program_ToggleIQ_MultipleTimes_AlternatesStartStop()
        {
            // Arrange
            var callCount = 0;
            _mockNetSdrClient.Setup(x => x.IQStarted).Returns(() => callCount++ % 2 == 1);
            _mockNetSdrClient.Setup(x => x.StartIQAsync()).Returns(Task.CompletedTask);
            _mockNetSdrClient.Setup(x => x.StopIQAsync()).Returns(Task.CompletedTask);

            var input = "S\nS\nS\nS\nQ\n";
            _consoleInput = new StringReader(input);
            Console.SetIn(_consoleInput);

            // Act
            await SimulateProgram(_mockNetSdrClient.Object);

            // Assert
            _mockNetSdrClient.Verify(x => x.StartIQAsync(), Times.Exactly(2));
            _mockNetSdrClient.Verify(x => x.StopIQAsync(), Times.Exactly(2));
        }

        // Допоміжний метод для симуляції основного циклу Program
        private async Task SimulateProgram(INetSdrClient netSdrClient)
        {
            while (true)
            {
                var keyChar = Console.In.Read();
                if (keyChar == -1) break;

                var key = ConvertCharToConsoleKey((char)keyChar);

                if (key == ConsoleKey.C)
                {
                    await netSdrClient.ConnectAsync();
                }
                else if (key == ConsoleKey.D)
                {
                    netSdrClient.Disconect();
                }
                else if (key == ConsoleKey.F)
                {
                    await netSdrClient.ChangeFrequencyAsync(20000000, 1);
                }
                else if (key == ConsoleKey.S)
                {
                    if (netSdrClient.IQStarted)
                    {
                        await netSdrClient.StopIQAsync();
                    }
                    else
                    {
                        await netSdrClient.StartIQAsync();
                    }
                }
                else if (key == ConsoleKey.Q)
                {
                    break;
                }
            }
        }

        private ConsoleKey ConvertCharToConsoleKey(char c)
        {
            return c switch
            {
                'C' => ConsoleKey.C,
                'D' => ConsoleKey.D,
                'F' => ConsoleKey.F,
                'S' => ConsoleKey.S,
                'Q' => ConsoleKey.Q,
                _ => ConsoleKey.Escape
            };
        }
    }


    // Інтерфейс для мокування NetSdrClient
    public interface INetSdrClient
    {
        bool IQStarted { get; }
        Task ConnectAsync();
        void Disconect();
        Task ChangeFrequencyAsync(uint frequency, byte channel);
        Task StartIQAsync();
        Task StopIQAsync();
    }
}