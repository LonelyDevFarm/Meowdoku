using System.Collections.Generic;
using Meowdoku.Core;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class MiniJsonTests
    {
        [Test]
        public void SerializeAndDeserialize_RoundTripsSaveCompatibleValues()
        {
            var source = new Dictionary<string, object>
            {
                { "level", 12 },
                { "music", true },
                { "locale", "vi\nVN" },
                { "tools", new List<object> { 5, 4, 3 } },
                {
                    "nested",
                    new Dictionary<string, object>
                    {
                        { "ratio", 1.25 },
                        { "none", null }
                    }
                }
            };

            string json = MiniJson.Serialize(source);
            var restored = MiniJson.Deserialize(json) as Dictionary<string, object>;

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored["level"], Is.EqualTo(12L));
            Assert.That(restored["music"], Is.True);
            Assert.That(restored["locale"], Is.EqualTo("vi\nVN"));
            Assert.That(restored["tools"], Is.TypeOf<List<object>>());
            Assert.That(restored["nested"], Is.TypeOf<Dictionary<string, object>>());
        }

        [Test]
        public void Serialize_RejectsNonStringDictionaryKeys()
        {
            var invalid = new Dictionary<int, object> { { 1, "value" } };
            Assert.That(
                () => MiniJson.Serialize(invalid),
                Throws.InvalidOperationException);
        }
    }
}
