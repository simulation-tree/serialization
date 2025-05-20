using Collections.Generic;
using Serialization.TOML;
using System;
using Unmanaged;
using Unmanaged.Tests;

namespace Serialization.Tests
{
    public class TOMLTests : UnmanagedTests
    {
        [Test]
        public void CreateAndDisposeObject()
        {
            TOMLDocument document = new();
            Assert.That(document.IsDisposed, Is.False);
            document.Dispose();
            Assert.That(document.IsDisposed, Is.True);

            document = new();
            Assert.That(document.IsDisposed, Is.False);
            document.Dispose();
            Assert.That(document.IsDisposed, Is.True);
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

            using ByteWriter byteWriter = new();
            byteWriter.WriteUTF8(Source);
            using ByteReader byteReader = new(byteWriter.AsSpan());
            TOMLReader tomlReader = new(byteReader);
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
ld1 = 1979-05-27
lt1 = 07:32:00
lt2 = 00:32:00.999999

[table]
name = ""Yes""
odt1 = 1979-05-27T07:32:00Z
odt2 = 1979-05-27T00:32:00-07:00
odt3 = 1979-05-27T00:32:00.999999-07:00
odt4 = 1979-05-27 07:32:00Z

[another]
name = ""No""
ldt1 = 1979-05-27T07:32:00
ldt2 = 1979-05-27T00:32:00.999999";

            using ByteWriter byteWriter = new();
            byteWriter.WriteUTF8(Source);
            using ByteReader byteReader = new(byteWriter.AsSpan());
            TOMLReader tomlReader = new(byteReader);

            using TOMLDocument document = byteReader.ReadObject<TOMLDocument>();
            Assert.That(document.ContainsValue("title"), Is.True);
            Assert.That(document.ContainsValue("amount"), Is.True);
            Assert.That(document.ContainsValue("enabled"), Is.True);
            Assert.That(document.ContainsValue("ld1"), Is.True);
            Assert.That(document.ContainsValue("lt1"), Is.True);
            Assert.That(document.ContainsValue("lt2"), Is.True);
            Assert.That(document.GetValue("title").Text.ToString(), Is.EqualTo("TOML Example"));
            Assert.That(document.GetValue("amount").Number, Is.EqualTo(-3213.777).Within(0.01));
            Assert.That(document.GetValue("enabled").Boolean, Is.True);
            Assert.That(document.GetValue("ld1").DateTime, Is.EqualTo(new DateTime(1979, 5, 27)));
            Assert.That(document.GetValue("lt1").TimeSpan, Is.EqualTo(new TimeSpan(7, 32, 0)));
            Assert.That(document.GetValue("lt2").TimeSpan, Is.EqualTo(new TimeSpan(0, 0, 32, 0, 999, 999)));

            Assert.That(document.ContainsTable("table"), Is.True);
            TOMLTable table = document.GetTable("table");
            Assert.That(table.ContainsValue("name"), Is.True);
            Assert.That(table.ContainsValue("odt1"), Is.True);
            Assert.That(table.ContainsValue("odt2"), Is.True);
            Assert.That(table.ContainsValue("odt3"), Is.True);
            Assert.That(table.ContainsValue("odt4"), Is.True);
            Assert.That(table.GetValue("name").Text.ToString(), Is.EqualTo("Yes"));
            Assert.That(table.GetValue("odt1").DateTime, Is.EqualTo(new DateTime(1979, 5, 27, 7, 32, 0, DateTimeKind.Utc)));
            Assert.That(table.GetValue("odt2").DateTime, Is.EqualTo(new DateTime(1979, 5, 27, 0, 32, 0, DateTimeKind.Utc).AddHours(7)));
            Assert.That(table.GetValue("odt3").DateTime, Is.EqualTo(new DateTime(1979, 5, 27, 0, 32, 0, 999, 999, DateTimeKind.Utc).AddHours(7)));
            Assert.That(table.GetValue("odt4").DateTime, Is.EqualTo(new DateTime(1979, 5, 27, 7, 32, 0, DateTimeKind.Utc)));

            Assert.That(document.ContainsTable("another"), Is.True);
            TOMLTable another = document.GetTable("another");
            Assert.That(another.ContainsValue("name"), Is.True);
            Assert.That(another.ContainsValue("ldt1"), Is.True);
            Assert.That(another.ContainsValue("ldt2"), Is.True);
            Assert.That(another.GetValue("name").Text.ToString(), Is.EqualTo("No"));
            Assert.That(another.GetValue("ldt1").DateTime, Is.EqualTo(new DateTime(1979, 5, 27, 7, 32, 0)));
            Assert.That(another.GetValue("ldt2").DateTime, Is.EqualTo(new DateTime(1979, 5, 27, 0, 32, 0, 999, 999)));
        }
    }
}