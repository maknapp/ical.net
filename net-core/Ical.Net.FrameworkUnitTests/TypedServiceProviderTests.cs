using System;
using NUnit.Framework;

namespace Ical.Net.FrameworkUnitTests
{
    public class TypedServiceProviderTests
    {
        [Test]
        public void GetService_WhenTypeIsNull_ShouldThrow()
        {
            // Arrange
            var typedServices = CreateServiceProvider();

            // Act + Assert
            Assert.Throws<ArgumentNullException>(() => typedServices.GetService(null));
        }

        [Test]
        public void GetService_WhenNameDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var typedServices = CreateServiceProvider();

            // Act
            var actual = typedServices.GetService(typeof(string));

            // Assert
            Assert.AreEqual(null, actual);
        }

        [Test]
        public void GetService_WhenReturnTypeDoesNotMatchEntryType_ShouldReturnDefaultValue()
        {
            // Arrange
            var typedServices = CreateServiceProvider();
            typedServices.SetService("ABC");

            // Act
            var actual = typedServices.GetService<int>();

            // Assert
            Assert.AreEqual(0, actual);
            Assert.IsInstanceOf<int>(actual);
        }

        [Test]
        public void SetService_WhenTypeDoesNotExist_ShouldAddNewEntry()
        {
            // Arrange
            var typedServices = CreateServiceProvider();

            // Act
            typedServices.SetService("ABC");
            var actual = typedServices.GetService(typeof(string));

            // Assert
            Assert.AreEqual("ABC", actual);
        }

        [Test]
        public void SetService_WhenTypeAlreadyExists_ShouldReplaceExisting()
        {
            // Arrange
            var typedServices = CreateServiceProvider();

            // Act
            typedServices.SetService("ABC");
            typedServices.SetService("CBA");
            var actual = typedServices.GetService(typeof(string));

            // Assert
            Assert.AreEqual("CBA", actual);
        }

        [Test]
        public void SetService_WhenInputIsNull_ShouldNotThrow()
        {
            // Arrange
            var typedServices = CreateServiceProvider();

            // Act + Assert
            Assert.DoesNotThrow(() => typedServices.SetService(null));
        }

        [Test]
        public void SetService_WhenInputImplementInterfaces_ShouldStoreInterfaceTypes()
        {
            // Arrange
            var typedServices = CreateServiceProvider();
            var dummyClass = new DummyClassA();

            // Act
            typedServices.SetService(dummyClass);
            var ifaceA = typedServices.GetService(typeof(IDummyInterfaceA));
            var ifaceB = typedServices.GetService(typeof(IDummyInterfaceA));

            // Assert
            Assert.AreEqual(dummyClass, ifaceA);
            Assert.AreEqual(dummyClass, ifaceB);
        }

        [Test]
        public void SetService_WhenInterfaceAlreadyExists_ShouldReplaceExisting()
        {
            // Arrange
            var typedServices = CreateServiceProvider();
            var dummyClassA = new DummyClassA();
            var dummyClassB = new DummyClassB();

            // Act
            typedServices.SetService(dummyClassA);
            typedServices.SetService(dummyClassB);

            var ifaceA = typedServices.GetService(typeof(IDummyInterfaceA));
            var ifaceB = typedServices.GetService(typeof(IDummyInterfaceB));

            // Assert
            Assert.AreEqual(dummyClassB, ifaceA);
            Assert.AreEqual(dummyClassA, ifaceB);
        }

        [Test]
        public void RemoveService_WhenTypeIsNull_ShouldDoNothing()
        {
            // Arrange
            var typedServices = CreateServiceProvider();

            // Act + Assert
            Assert.DoesNotThrow(() => typedServices.RemoveService(null));
        }

        [Test]
        public void RemoveService_WhenTypeDoesNotExist_ShouldDoNothing()
        {
            // Arrange
            var typedServices = CreateServiceProvider();

            // Act + Assert
            Assert.DoesNotThrow(() => typedServices.RemoveService(typeof(string)));
        }

        [Test]
        public void RemoveService_WhenTypeImplementInterface_ShouldRemoveInterface()
        {
            // Arrange
            var typedServices = CreateServiceProvider();
            var dummyClass = new DummyClassB();

            // Act
            typedServices.SetService(dummyClass);
            typedServices.RemoveService(typeof(IDummyInterfaceB));
            var actual = typedServices.GetService(typeof(IDummyInterfaceB));

            // Assert
            Assert.AreEqual(null, actual);
        }

        private interface IDummyInterfaceA { }

        private interface IDummyInterfaceB { }

        private class DummyClassA : IDummyInterfaceA, IDummyInterfaceB { }

        private class DummyClassB : IDummyInterfaceA { }

        private TypedServiceProvider CreateServiceProvider()
        {
            return new TypedServiceProvider();
        }
    }
}
