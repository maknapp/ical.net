using Ical.Net.DataTypes;
using NUnit.Framework;

namespace Ical.Net.FrameworkUnitTests
{
    [TestFixture]
    [Category("DataType")]
    public class DataTypeTests
    {
        [Test]
        public void OrganizerConstructorMustAcceptNull()
        {
            Assert.DoesNotThrow(() => { _ = new Organizer(null); });
        }

        [Test]
        public void AttachmentConstructorMustAcceptNull()
        {
            Assert.DoesNotThrow(() => { _ = new Attachment((byte[])null); });
            Assert.DoesNotThrow(() => { _ = new Attachment((string)null); });
        }
    }
}