using System;
using NUnit.Framework;

namespace Ical.Net.FrameworkUnitTests
{
    public class NamedServiceProviderTests
    {
        [Test]
        public void GetService_WhenNameIsNull_ShouldThrow()
        {
            // Arrange
            var namedServices = CreateServiceProvider();

            // Act + Assert
            Assert.Throws<ArgumentNullException>(() => namedServices.GetService(null));
        }

        [Test]
        public void GetService_WhenNameDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var namedServices = CreateServiceProvider();

            // Act
            var actual = namedServices.GetService("ABC");

            // Assert
            Assert.AreEqual(null, actual);
        }

        [Test]
        public void GetService_WhenReturnTypeDoesNotMatchEntryType_ShouldReturnDefaultValue()
        {
            // Arrange
            var namedServices = CreateServiceProvider();

            // Act
            namedServices.SetService("ABC", "STRING_OBJECT");
            var actual = namedServices.GetService<int>("ABC");

            // Assert
            Assert.AreEqual(0, actual);
            Assert.IsInstanceOf<int>(actual);
        }

        [Test]
        public void SetService_WhenNameDoesNotExist_ShouldAddNewEntry()
        {
            // Arrange
            var namedServices = CreateServiceProvider();

            // Act
            namedServices.SetService("ABC", "STRING_OBJECT");
            var actual = namedServices.GetService("ABC");

            // Assert
            Assert.AreEqual("STRING_OBJECT", actual);
        }

        [Test]
        public void SetService_WhenNameAlreadyExists_ShouldReplaceExisting()
        {
            // Arrange
            var namedServices = CreateServiceProvider();

            // Act
            namedServices.SetService("ABC", "VALUE_1");
            namedServices.SetService("ABC", "VALUE_2");
            var actual = namedServices.GetService("ABC");

            // Assert
            Assert.AreEqual("VALUE_2", actual);
        }

        [Test]
        public void SetService_WhenNameIsNull_ShouldNotThrow()
        {
            // Arrange
            var namedServices = CreateServiceProvider();

            // Act + Assert
            Assert.DoesNotThrow(() => namedServices.SetService(null, "VALUE"));
        }

        [Test]
        public void SetService_WhenNameIsEmpty_ShouldDoNothing()
        {
            // Arrange
            var namedServices = CreateServiceProvider();

            // Act
            namedServices.SetService("", "VALUE");
            var actual = namedServices.GetService("");

            // Assert
            Assert.AreEqual(null, actual);
        }

        [Test]
        public void SetService_WhenValueIsNull_ShouldDoNothing()
        {
            // Arrange
            var namedServices = CreateServiceProvider();

            // Act
            namedServices.SetService("ABC", null);
            var actual = namedServices.GetService("ABC");

            // Assert
            Assert.AreEqual(null, actual);
        }

        [Test]
        public void RemoveService_WhenNameIsNull_ShouldThrow()
        {
            // Arrange
            var namedServices = CreateServiceProvider();

            // Act + Assert
            Assert.Throws<ArgumentNullException>(() => namedServices.RemoveService(null));
        }

        [Test]
        public void RemoveService_WhenNameDoesNotExist_ShouldDoNothing()
        {
            // Arrange
            var namedServices = CreateServiceProvider();

            // Act + Assert
            Assert.DoesNotThrow(() => namedServices.RemoveService("ABC"));
        }

        [Test]
        public void RemoveService_WhenNameExists_ShouldRemoveEntry()
        {
            // Arrange
            var namedServices = CreateServiceProvider();

            // Act
            namedServices.SetService("ABC", "VALUE");
            namedServices.RemoveService("ABC");
            var actual = namedServices.GetService("ABC");

            // Assert
            Assert.AreEqual(null, actual);
        }

        private NamedServiceProvider CreateServiceProvider()
        {
            return new NamedServiceProvider();
        }
    }
}
