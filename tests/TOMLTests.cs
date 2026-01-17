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
            while (tomlReader.ReadToken(out Token token))
            {
                Console.WriteLine($"{token.type}:{token.ToString(tomlReader)}");
            }
        }

        [Test]
        public void ReadArraysWithInlineTables()
        {
            const string Source =
@"integers = [ 1, 2, 3 ]
colors = [ ""red"", ""yellow"", ""green"" ]
nested_arrays_of_ints = [ [ 1, 2 ], [3, 4, 5] ]
nested_mixed_array = [ [ 1, 2 ], [""a"", ""b"", ""c""] ]
]";

            using ByteWriter byteWriter = new();
            byteWriter.WriteUTF8(Source);
            using ByteReader byteReader = new(byteWriter.AsSpan());
            TOMLReader tomlReader = new(byteReader);

            using TOMLDocument document = byteReader.ReadObject<TOMLDocument>();
            Assert.That(document.ContainsValue("integers"), Is.True);
            Assert.That(document.ContainsValue("colors"), Is.True);
            Assert.That(document.ContainsValue("nested_arrays_of_ints"), Is.True);
            Assert.That(document.ContainsValue("nested_mixed_array"), Is.True);

            TOMLArray integers = document.GetValue("integers").Array;
            Assert.That(integers.Length, Is.EqualTo(3));
            Assert.That(integers[0].Number, Is.EqualTo(1));
            Assert.That(integers[1].Number, Is.EqualTo(2));
            Assert.That(integers[2].Number, Is.EqualTo(3));

            TOMLArray colors = document.GetValue("colors").Array;
            Assert.That(colors.Length, Is.EqualTo(3));
            Assert.That(colors[0].Text.ToString(), Is.EqualTo("red"));
            Assert.That(colors[1].Text.ToString(), Is.EqualTo("yellow"));
            Assert.That(colors[2].Text.ToString(), Is.EqualTo("green"));

            TOMLArray nestedArraysOfInts = document.GetValue("nested_arrays_of_ints").Array;
            Assert.That(nestedArraysOfInts.Length, Is.EqualTo(2));

            TOMLArray first = nestedArraysOfInts[0].Array;
            Assert.That(first.Length, Is.EqualTo(2));
            Assert.That(first[0].Number, Is.EqualTo(1));
            Assert.That(first[1].Number, Is.EqualTo(2));

            TOMLArray second = nestedArraysOfInts[1].Array;
            Assert.That(second.Length, Is.EqualTo(3));
            Assert.That(second[0].Number, Is.EqualTo(3));
            Assert.That(second[1].Number, Is.EqualTo(4));
            Assert.That(second[2].Number, Is.EqualTo(5));

            TOMLArray nestedMixedArray = document.GetValue("nested_mixed_array").Array;
            Assert.That(nestedMixedArray.Length, Is.EqualTo(2));

            first = nestedMixedArray[0].Array;
            Assert.That(first.Length, Is.EqualTo(2));
            Assert.That(first[0].Number, Is.EqualTo(1));
            Assert.That(first[1].Number, Is.EqualTo(2));

            second = nestedMixedArray[1].Array;
            Assert.That(second.Length, Is.EqualTo(3));
            Assert.That(second[0].Text.ToString(), Is.EqualTo("a"));
            Assert.That(second[1].Text.ToString(), Is.EqualTo("b"));
            Assert.That(second[2].Text.ToString(), Is.EqualTo("c"));
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

        [Test]
        public void WriteSimpleSource()
        {
            using TOMLDocument document = new();
            document.Add("title", "TOML Example");
            document.Add("amount", -3213.777);
            document.Add("enabled", true);

            using ByteWriter byteWriter = new();
            byteWriter.WriteObject(document);

            using ByteReader byteReader = new(byteWriter.AsSpan());
            TOMLReader tomlReader = new(byteReader);
            using TOMLDocument readDocument = byteReader.ReadObject<TOMLDocument>();
            Assert.That(readDocument.ContainsValue("title"), Is.True);
            Assert.That(readDocument.ContainsValue("amount"), Is.True);
            Assert.That(readDocument.ContainsValue("enabled"), Is.True);
            Assert.That(readDocument.GetValue("title").Text.ToString(), Is.EqualTo("TOML Example"));
            Assert.That(readDocument.GetValue("amount").Number, Is.EqualTo(-3213.777).Within(0.01));
            Assert.That(readDocument.GetValue("enabled").Boolean, Is.True);
        }

        [Test]
        public void WriteWithArrays()
        {
            using TOMLDocument document = new();
            TOMLArray integers = new([1, 2, 3]);
            document.Add("integers", integers);

            TOMLArray colors = new();
            colors.Add("red");
            colors.Add("yellow");
            colors.Add("green");
            document.Add("colors", colors);

            TOMLArray nestedArraysOfInts = new();
            TOMLArray first = new([1, 2]);
            TOMLArray second = new([3, 4, 5]);
            nestedArraysOfInts.Add(first);
            nestedArraysOfInts.Add(second);
            document.Add("nested_arrays_of_ints", nestedArraysOfInts);

            TOMLArray nestedMixedArray = new();
            first = new([1, 2]);
            second = new();
            second.Add("a");
            second.Add("b");
            second.Add("c");
            nestedMixedArray.Add(first);
            nestedMixedArray.Add(second);
            document.Add("nested_mixed_array", nestedMixedArray);

            using ByteWriter byteWriter = new();
            byteWriter.WriteObject(document);

            using ByteReader byteReader = new(byteWriter.AsSpan());
            TOMLReader tomlReader = new(byteReader);
            using TOMLDocument readDocument = byteReader.ReadObject<TOMLDocument>();

            Assert.That(readDocument.ContainsValue("integers"), Is.True);
            Assert.That(readDocument.ContainsValue("colors"), Is.True);
            Assert.That(readDocument.ContainsValue("nested_arrays_of_ints"), Is.True);
            Assert.That(readDocument.ContainsValue("nested_mixed_array"), Is.True);

            TOMLArray readIntegers = readDocument.GetValue("integers").Array;
            Assert.That(readIntegers.Length, Is.EqualTo(3));
            Assert.That(readIntegers[0].Number, Is.EqualTo(1));
            Assert.That(readIntegers[1].Number, Is.EqualTo(2));
            Assert.That(readIntegers[2].Number, Is.EqualTo(3));

            TOMLArray readColors = readDocument.GetValue("colors").Array;
            Assert.That(readColors.Length, Is.EqualTo(3));
            Assert.That(readColors[0].Text.ToString(), Is.EqualTo("red"));
            Assert.That(readColors[1].Text.ToString(), Is.EqualTo("yellow"));
            Assert.That(readColors[2].Text.ToString(), Is.EqualTo("green"));

            TOMLArray readNestedArraysOfInts = readDocument.GetValue("nested_arrays_of_ints").Array;
            Assert.That(readNestedArraysOfInts.Length, Is.EqualTo(2));

            TOMLArray readFirst = readNestedArraysOfInts[0].Array;
            Assert.That(readFirst.Length, Is.EqualTo(2));
            Assert.That(readFirst[0].Number, Is.EqualTo(1));
            Assert.That(readFirst[1].Number, Is.EqualTo(2));

            TOMLArray readSecond = readNestedArraysOfInts[1].Array;
            Assert.That(readSecond.Length, Is.EqualTo(3));
            Assert.That(readSecond[0].Number, Is.EqualTo(3));
            Assert.That(readSecond[1].Number, Is.EqualTo(4));
            Assert.That(readSecond[2].Number, Is.EqualTo(5));

            TOMLArray readNestedMixedArray = readDocument.GetValue("nested_mixed_array").Array;
            Assert.That(readNestedMixedArray.Length, Is.EqualTo(2));

            readFirst = readNestedMixedArray[0].Array;
            Assert.That(readFirst.Length, Is.EqualTo(2));
            Assert.That(readFirst[0].Number, Is.EqualTo(1));
            Assert.That(readFirst[1].Number, Is.EqualTo(2));

            readSecond = readNestedMixedArray[1].Array;
            Assert.That(readSecond.Length, Is.EqualTo(3));
            Assert.That(readSecond[0].Text.ToString(), Is.EqualTo("a"));
            Assert.That(readSecond[1].Text.ToString(), Is.EqualTo("b"));
            Assert.That(readSecond[2].Text.ToString(), Is.EqualTo("c"));
        }
    }
}