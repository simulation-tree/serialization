using Collections.Generic;
using Serialization.XML;
using System;
using Unmanaged;

namespace Serialization.Tests
{
    public class XMLTests
    {
        private const string XMLDummy = "<Project Sdk=\"Microsoft.NET.Sdk\">\r\n\t<PropertyGroup>\r\n\t\t<OutputType>Exe</OutputType>\r\n\t\t<TargetFramework>net9.0</TargetFramework>\r\n\t\t<ImplicitUsings>disable</ImplicitUsings>\r\n\t\t<Nullable>enable</Nullable>\r\n\t\t<AllowUnsafeBlocks>true</AllowUnsafeBlocks>\r\n\t\t<PublishAoT>true</PublishAoT>\r\n\t\t<IsAotCompatible>true</IsAotCompatible>\r\n\t</PropertyGroup>\r\n\r\n\t<PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Release|AnyCPU'\">\r\n\t\t<Optimize>False</Optimize>\r\n\t\t<WarningLevel>7</WarningLevel>\r\n\t</PropertyGroup>\r\n\r\n\t<PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Debug|AnyCPU'\">\r\n\t\t<Optimize>False</Optimize>\r\n\t\t<WarningLevel>7</WarningLevel>\r\n\t</PropertyGroup>\r\n\r\n\t<ItemGroup>\r\n\t\t<None Remove=\"Assets\\page.html\" />\r\n\t\t<None Remove=\"Assets\\spritesheet.json\" />\r\n\t\t<None Remove=\"Assets\\spritesheet.png\" />\r\n\t\t<None Remove=\"Assets\\test - Copy.frag\" />\r\n\t\t<None Remove=\"Assets\\test.frag\" />\r\n\t\t<None Remove=\"Assets\\testModel.fbx\" />\r\n\t\t<None Remove=\"Assets\\texture.jpg\" />\r\n\t\t<None Remove=\"Assets\\triangle - Copy.vert\" />\r\n\t\t<None Remove=\"Assets\\triangle.frag\" />\r\n\t\t<None Remove=\"Assets\\triangle.vert\" />\r\n\t</ItemGroup>\r\n\r\n\t<ItemGroup>\r\n\t\t<EmbeddedResource Include=\"Assets\\page.html\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\spritesheet.json\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\spritesheet.png\">\r\n\t\t\t<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>\r\n\t\t</EmbeddedResource>\r\n\t\t<EmbeddedResource Include=\"Assets\\test - Copy.frag\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\testModel.fbx\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\texture.jpg\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\test.frag\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\triangle - Copy.vert\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\triangle.frag\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\triangle.vert\" />\r\n\t</ItemGroup>\r\n\r\n\t<ItemGroup>\r\n\t\t<ProjectReference Include=\"..\\chair\\source\\Chair.csproj\" />\r\n\t\t<ProjectReference Include=\"..\\expression-machine\\source\\Expression Machine.csproj\" />\r\n\t\t<ProjectReference Include=\"..\\game-objects\\source\\Game Objects.csproj\" />\r\n\t\t<ProjectReference Include=\"..\\game\\source\\Game.csproj\" />\r\n\t</ItemGroup>\r\n\r\n</Project>\r\n";

        [Test]
        public void ReadXMLTokens()
        {
            using ByteReader reader = ByteReader.CreateFromUTF8(XMLDummy);
            XMLReader xmlReader = new(reader);
            using List<Text> tokens = new();
            while (xmlReader.ReadToken(out Token token))
            {
                Text tokenText = new(0);
                token.ToString(xmlReader, tokenText);
                tokens.Add(tokenText);
            }

            Assert.That(tokens[0], Is.EqualTo("<"));
            Assert.That(tokens[1], Is.EqualTo("Project"));
            Assert.That(tokens[2], Is.EqualTo("Sdk"));
            Assert.That(tokens[3], Is.EqualTo("Microsoft.NET.Sdk"));
            Assert.That(tokens[4], Is.EqualTo(">"));
            Assert.That(tokens[5], Is.EqualTo("<"));

            foreach (Text token in tokens)
            {
                token.Dispose();
            }
        }

        [Test]
        public void DeserializeXML()
        {
            using ByteReader reader = ByteReader.CreateFromUTF8(XMLDummy);
            XMLReader xmlReader = new(reader);
            using List<Text> tokens = new();
            while (xmlReader.ReadToken(out Token token))
            {
                Text tokenText = new(0);
                token.ToString(xmlReader, tokenText);
                tokens.Add(tokenText);
            }

            foreach (Text token in tokens)
            {
                Console.WriteLine(token);
                token.Dispose();
            }

            reader.Position = 0;
            using XMLNode projectXml = reader.ReadObject<XMLNode>();
            string str = projectXml.ToString();
            Console.WriteLine(str);
        }

        [Test]
        public void EmptyNode()
        {
            const string Sample = "<Apple><PackageId/></Apple>";

            using ByteReader reader = ByteReader.CreateFromUTF8(Sample);
            XMLReader xmlReader = new(reader);
            using List<Text> tokens = new();
            while (xmlReader.ReadToken(out Token token))
            {
                Text tokenText = new(0);
                token.ToString(xmlReader, tokenText);
                tokens.Add(tokenText);
            }

            Assert.That(tokens[0], Is.EqualTo("<"));
            Assert.That(tokens[1], Is.EqualTo("Apple"));
            Assert.That(tokens[2], Is.EqualTo(">"));
            Assert.That(tokens[3], Is.EqualTo("<"));
            Assert.That(tokens[4], Is.EqualTo("PackageId"));
            Assert.That(tokens[5], Is.EqualTo("/"));
            Assert.That(tokens[6], Is.EqualTo(">"));
            Assert.That(tokens[7], Is.EqualTo("<"));
            Assert.That(tokens[8], Is.EqualTo("/"));
            Assert.That(tokens[9], Is.EqualTo("Apple"));
            Assert.That(tokens[10], Is.EqualTo(">"));

            reader.Position = 0;
            using XMLNode node = reader.ReadObject<XMLNode>();
            string str = node.ToString("");
            Assert.That(str, Is.EqualTo(Sample));
        }

        [Test]
        public void TryReadJSONAsXML()
        {
            using ByteReader reader = ByteReader.CreateFromUTF8("{\"some\":true,\"kind\":7,\"of\":\"json\"}");
            XMLReader xmlReader = new(reader);
            while (xmlReader.ReadToken(out Token token))
            {
                Console.WriteLine(token.ToString(xmlReader));
            }
        }

        [Test]
        public void ModifyXML()
        {
            using ByteReader reader = ByteReader.CreateFromUTF8(XMLDummy);
            using XMLNode projectXml = reader.ReadObject<XMLNode>();
            projectXml.TryGetFirst("PropertyGroup", out XMLNode propertyGroup);
            propertyGroup.TryGetFirst("TargetFramework", out XMLNode tfm);
            tfm.Content.CopyFrom("net10.0");
            string str = projectXml.ToString();
            Console.WriteLine(str);
        }

        [Test]
        public void DeserializeFromBinary()
        {
            using ByteReader reader = ByteReader.CreateFromUTF8(XMLDummy);
            ReadOnlySpan<byte> byteStream = reader.GetBytes();
            using XMLNode projectXml = reader.ReadObject<XMLNode>();
            string str = projectXml.ToString();
            Console.WriteLine(str);
        }

        [Test]
        public void ReadEmptyNodes()
        {
            using System.IO.Stream xmlFileStream = GetType().Assembly.GetManifestResourceStream("Serialization.Tests.XMLFile1.xml") ?? throw new Exception();
            using ByteReader reader = new(xmlFileStream);
            XMLReader xmlReader = new(reader);
            using XMLNode node = xmlReader.ReadNode();
            Assert.That(node.Name.ToString(), Is.EqualTo("None"));
            Assert.That(node.Attributes.Length, Is.EqualTo(1));
            Assert.That(node.Attributes[0].Name.ToString(), Is.EqualTo("Include"));
            Assert.That(node.Attributes[0].Value.ToString(), Is.EqualTo("..\\README.md"));
            Assert.That(node.Children.Length, Is.EqualTo(3));
        }

        [Test]
        public void ReadWithPrologue()
        {
            using System.IO.Stream xmlFileStream = GetType().Assembly.GetManifestResourceStream("Serialization.Tests.XMLFile2.xml") ?? throw new Exception();
            using ByteReader reader = new(xmlFileStream);
            XMLReader xmlReader = new(reader);
            using XMLNode node = xmlReader.ReadNode();
            Assert.That(node.IsPrologue, Is.True);
        }
    }
}
