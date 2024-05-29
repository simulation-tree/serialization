using System;
using System.Collections.Generic;
using Unmanaged;
using Unmanaged.XML;

namespace Serialization.Tests
{
    public class XMLTests
    {
        private const string XMLDummy = "<Project Sdk=\"Microsoft.NET.Sdk\">\r\n\t<PropertyGroup>\r\n\t\t<OutputType>Exe</OutputType>\r\n\t\t<TargetFramework>net9.0</TargetFramework>\r\n\t\t<ImplicitUsings>disable</ImplicitUsings>\r\n\t\t<Nullable>enable</Nullable>\r\n\t\t<AllowUnsafeBlocks>true</AllowUnsafeBlocks>\r\n\t\t<PublishAoT>true</PublishAoT>\r\n\t\t<IsAotCompatible>true</IsAotCompatible>\r\n\t</PropertyGroup>\r\n\r\n\t<PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Release|AnyCPU'\">\r\n\t\t<Optimize>False</Optimize>\r\n\t\t<WarningLevel>7</WarningLevel>\r\n\t</PropertyGroup>\r\n\r\n\t<PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Debug|AnyCPU'\">\r\n\t\t<Optimize>False</Optimize>\r\n\t\t<WarningLevel>7</WarningLevel>\r\n\t</PropertyGroup>\r\n\r\n\t<ItemGroup>\r\n\t\t<None Remove=\"Assets\\page.html\" />\r\n\t\t<None Remove=\"Assets\\spritesheet.json\" />\r\n\t\t<None Remove=\"Assets\\spritesheet.png\" />\r\n\t\t<None Remove=\"Assets\\test - Copy.frag\" />\r\n\t\t<None Remove=\"Assets\\test.frag\" />\r\n\t\t<None Remove=\"Assets\\testModel.fbx\" />\r\n\t\t<None Remove=\"Assets\\texture.jpg\" />\r\n\t\t<None Remove=\"Assets\\triangle - Copy.vert\" />\r\n\t\t<None Remove=\"Assets\\triangle.frag\" />\r\n\t\t<None Remove=\"Assets\\triangle.vert\" />\r\n\t</ItemGroup>\r\n\r\n\t<ItemGroup>\r\n\t\t<EmbeddedResource Include=\"Assets\\page.html\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\spritesheet.json\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\spritesheet.png\">\r\n\t\t\t<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>\r\n\t\t</EmbeddedResource>\r\n\t\t<EmbeddedResource Include=\"Assets\\test - Copy.frag\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\testModel.fbx\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\texture.jpg\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\test.frag\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\triangle - Copy.vert\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\triangle.frag\" />\r\n\t\t<EmbeddedResource Include=\"Assets\\triangle.vert\" />\r\n\t</ItemGroup>\r\n\r\n\t<ItemGroup>\r\n\t\t<ProjectReference Include=\"..\\chair\\source\\Chair.csproj\" />\r\n\t\t<ProjectReference Include=\"..\\expression-machine\\source\\Expression Machine.csproj\" />\r\n\t\t<ProjectReference Include=\"..\\game-objects\\source\\Game Objects.csproj\" />\r\n\t\t<ProjectReference Include=\"..\\game\\source\\Game.csproj\" />\r\n\t</ItemGroup>\r\n\r\n</Project>\r\n";

        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAnyAllocation();
        }

        [Test]
        public void DeserializeXML()
        {
            using BinaryReader reader = BinaryReader.CreateFromUTF8(XMLDummy);
            XMLReader xmlReader = new(reader);
            List<string> tokens = new();
            while (xmlReader.ReadToken(out Token token))
            {
                tokens.Add(token.ToString(xmlReader));
            }

            foreach (string token in tokens)
            {
                Console.WriteLine(token);
            }

            reader.Position = 0;
            using XMLNode projectXml = reader.ReadObject<XMLNode>();
            string str = projectXml.ToString();
            Console.WriteLine(str);
        }

        [Test]
        public void TryReadJSONAsXML()
        {
            using BinaryReader reader = BinaryReader.CreateFromUTF8("{\"some\":true,\"kind\":7,\"of\":\"json\"}");
            XMLReader xmlReader = new(reader);
            while (xmlReader.ReadToken(out Token token))
            {
                Console.WriteLine(token.ToString(xmlReader));
            }
        }

        [Test]
        public void ModifyXML()
        {
            using BinaryReader reader = BinaryReader.CreateFromUTF8(XMLDummy);
            using XMLNode projectXml = reader.ReadObject<XMLNode>();
            projectXml.TryGetFirst("PropertyGroup", out XMLNode propertyGroup);
            propertyGroup.TryGetFirst("TargetFramework", out XMLNode tfm);
            tfm.Content = "net10.0";
            string str = projectXml.ToString();
            Console.WriteLine(str);
        }

        [Test]
        public void DeserializeFromBinary()
        {
            using BinaryReader reader = BinaryReader.CreateFromUTF8(XMLDummy);
            ReadOnlySpan<byte> byteStream = reader.AsSpan();
            using XMLNode projectXml = reader.ReadObject<XMLNode>();
            string str = projectXml.ToString();
            Console.WriteLine(str);
        }
    }
}
