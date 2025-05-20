using Collections.Generic;
using Serialization.TOML;
using Unmanaged;
using Unmanaged.Tests;

namespace Serialization.Tests
{
    public class TOMLTests : UnmanagedTests
    {
        [Test]
        public void CreateAndDisposeObject()
        {
            TOMLObject tomlObject = new();
            tomlObject.Dispose();
        }

        [Test]
        public void ReadTokens()
        {
            const string Source = @"# This is a TOML document

title = ""TOML Example""

[owner]
name = ""Tom Preston-Werner""
dob = 1979-05-27T07:32:00-08:00

[database]
enabled = true
ports = [ 8000, 8001, 8002 ]
data = [ [""delta"", ""phi""], [3.14] ]
temp_targets = { cpu = 79.5, case = 72.0 }

[servers]

[servers.alpha]
ip = ""10.0.0.1""
role = ""frontend""

[servers.beta]
ip = ""10.0.0.2""
role = ""backend""";

            using ByteWriter writer = new();
            writer.WriteUTF8(Source);
            using ByteReader reader = new(writer.AsSpan());
            TOMLReader tomlReader = new(reader);
            using List<Token> tokens = new();
            while (tomlReader.ReadToken(out Token token))
            {
                tokens.Add(token);
            }

            foreach (Token token in tokens)
            {
                System.Console.WriteLine($"{token.type} = {token.ToString(tomlReader)}");
            }
        }

        [Test]
        public void ReadSimpleSource()
        {
            const string Source = 
@"# This is a TOML document

title = ""TOML Example""
amount = -3213.777
enabled = true

[table]
name = ""Yes""

[two]
name = ""No""";

            using ByteWriter writer = new();
            writer.WriteUTF8(Source);
            using ByteReader reader = new(writer.AsSpan());
            TOMLReader tomlReader = new(reader);
            using TOMLObject tomlObject = reader.ReadObject<TOMLObject>();
            Assert.That(tomlObject.ContainsValue("title"), Is.True);
            Assert.That(tomlObject.ContainsValue("amount"), Is.True);
            Assert.That(tomlObject.ContainsValue("enabled"), Is.True);
            Assert.That(tomlObject.ContainsValue("unknown"), Is.False);
            Assert.That(tomlObject.GetValue("title").Text.ToString(), Is.EqualTo("TOML Example"));
            Assert.That(tomlObject.GetValue("amount").Number, Is.EqualTo(-3213.777).Within(0.01));
            Assert.That(tomlObject.GetValue("enabled").Boolean, Is.True);
            Assert.That(tomlObject.ContainsTable("table"), Is.True);
            Assert.That(tomlObject.ContainsTable("two"), Is.True);
            Assert.That(tomlObject.GetTable("table").ContainsValue("name"), Is.True);
            Assert.That(tomlObject.GetTable("table").GetValue("name").Text.ToString(), Is.EqualTo("Yes"));
            Assert.That(tomlObject.GetTable("two").ContainsValue("name"), Is.True);
            Assert.That(tomlObject.GetTable("two").GetValue("name").Text.ToString(), Is.EqualTo("No"));
        }
    }
}