using System;
using System.Collections.Generic;
using Unmanaged;
using Unmanaged.XML;

namespace Serialization.Tests
{
    public class XMLTests
    {
        private const string XMLDummy = "<Project Sdk=\"Microsoft.NET.Sdk\">\r\n\r\n  <PropertyGroup>\r\n\t  <OutputType>WinExe</OutputType>\r\n    <TargetFramework>net9.0</TargetFramework>\r\n    <ImplicitUsings>disable</ImplicitUsings>\r\n    <Nullable>enable</Nullable>\r\n    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>\r\n    <PublishAoT>true</PublishAoT>\r\n    <IsAotCompatible>true</IsAotCompatible>\r\n  </PropertyGroup>\r\n\r\n  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Release|AnyCPU'\">\r\n    <Optimize>False</Optimize>\r\n    <WarningLevel>7</WarningLevel>\r\n  </PropertyGroup>\r\n\r\n  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Debug|AnyCPU'\">\r\n    <Optimize>False</Optimize>\r\n    <WarningLevel>7</WarningLevel>\r\n  </PropertyGroup>\r\n\r\n  <ItemGroup>\r\n    <None Remove=\"Assets\\page.html\" />\r\n    <None Remove=\"Assets\\spritesheet.json\" />\r\n    <None Remove=\"Assets\\spritesheet.png\" />\r\n    <None Remove=\"Assets\\test - Copy.frag\" />\r\n    <None Remove=\"Assets\\test.frag\" />\r\n    <None Remove=\"Assets\\testModel.fbx\" />\r\n    <None Remove=\"Assets\\texture.jpg\" />\r\n    <None Remove=\"Assets\\triangle - Copy.vert\" />\r\n    <None Remove=\"Assets\\triangle.frag\" />\r\n    <None Remove=\"Assets\\triangle.vert\" />\r\n  </ItemGroup>\r\n\r\n  <ItemGroup>\r\n    <EmbeddedResource Include=\"Assets\\page.html\" />\r\n    <EmbeddedResource Include=\"Assets\\spritesheet.json\" />\r\n    <EmbeddedResource Include=\"Assets\\spritesheet.png\" />\r\n    <EmbeddedResource Include=\"Assets\\test - Copy.frag\" />\r\n    <EmbeddedResource Include=\"Assets\\testModel.fbx\" />\r\n    <EmbeddedResource Include=\"Assets\\texture.jpg\" />\r\n    <EmbeddedResource Include=\"Assets\\test.frag\" />\r\n    <EmbeddedResource Include=\"Assets\\triangle - Copy.vert\" />\r\n    <EmbeddedResource Include=\"Assets\\triangle.frag\" />\r\n    <EmbeddedResource Include=\"Assets\\triangle.vert\" />\r\n  </ItemGroup>\r\n\r\n  <ItemGroup>\r\n    <ProjectReference Include=\"..\\chair\\source\\Chair.csproj\" />\r\n    <ProjectReference Include=\"..\\expression-machine\\source\\Expression Machine.csproj\" />\r\n    <ProjectReference Include=\"..\\game-objects\\source\\Game Objects.csproj\" />\r\n    <ProjectReference Include=\"..\\game\\source\\Game.csproj\" />\r\n  </ItemGroup>\r\n\r\n</Project>\r\n";

        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAnyAllocation();
        }

        [Test]
        public void DeserializeXML()
        {
            using BinaryReader reader = new(XMLDummy);
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
        public void ModifyXML()
        {
            using BinaryReader reader = new(XMLDummy);
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
            using BinaryReader reader = new();
            using XMLNode projectXml = reader.ReadObject<XMLNode>();
            string str = projectXml.ToString();
            Console.WriteLine(str);
        }
    }
}
