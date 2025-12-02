using NetArchTest.Rules;
using NetSdrClientApp; // Підключення основного проекту
using NUnit.Framework;
using System.Reflection;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        [Test]
        public void Interfaces_Should_Start_With_I()
        {
            // 1. Вказуємо, яку збірку (проект) перевіряємо
            var assembly = typeof(NetSdrClient).Assembly;

            // 2. Формулюємо правило:
            // "Всі типи, які є інтерфейсами, повинні мати назву, що починається з 'I'"
            var result = Types.InAssembly(assembly)
                .That().AreInterfaces()
                .Should().HaveNameStartingWith("I")
                .GetResult();

            // 3. Якщо правило порушено - тест впаде і покаже список "поганих" класів
            Assert.That(result.IsSuccessful, Is.True, "All interfaces must start with 'I'");
        }
    }
}