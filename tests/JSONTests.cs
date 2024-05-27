using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unmanaged;
using Unmanaged.Collections;
using Unmanaged.JSON;

namespace Serialization.Tests
{
    public class JSONTests
    {
        [TearDown]
        public void CleanUp()
        {
            Allocations.ThrowIfAnyAllocation();
        }

        [Test]
        public void ReadSampleJSON()
        {
            JsonObject json = new()
            {
                { "name", "John Doe" },
                { "age", 42 },
                { "isAlive", true },
                {
                    "address", new JsonObject
                    {
                        { "streetAddress", "21 2nd Street" },
                        { "city", "New York" },
                        { "state", "NY" },
                        { "postalCode", "10021-3100" }
                    }
                }
            };

            string jsonString = json.ToString();
            JSONObject obj = new(jsonString.AsSpan());
            Assert.That(obj.GetText("name").ToString(), Is.EqualTo("John Doe"));
            Assert.That(obj.GetNumber("age"), Is.EqualTo(42));
            Assert.That(obj.GetBoolean("isAlive"), Is.True);
            Assert.That(obj.Contains("address"), Is.True);

            JSONObject address = obj.GetObject("address");
            Assert.That(address.GetText("streetAddress").ToString(), Is.EqualTo("21 2nd Street"));
            Assert.That(address.GetText("city").ToString(), Is.EqualTo("New York"));
            Assert.That(address.GetText("state").ToString(), Is.EqualTo("NY"));
            Assert.That(address.GetText("postalCode").ToString(), Is.EqualTo("10021-3100"));
            obj.Dispose();
        }

        [Test]
        public void CreateJSON()
        {
            DateTime now = DateTime.Now;
            double seconds = (now - new DateTime(1970, 1, 1)).TotalSeconds;

            JSONObject obj = new();
            obj.Add("name", "John Doe");
            obj.Add("age", 42);
            obj.Add("isAlive", true);

            JSONObject address = new();
            address.Add("streetAddress", "21 2nd Street");
            address.Add("city", "New York");
            address.Add("state", "NY");
            address.Add("seconds", seconds);

            JSONArray inventory = new();
            inventory.Add("apples");
            inventory.Add("oranges");
            inventory.Add("pears");

            obj.Add("address", address);
            obj.Add("inventory", inventory);

            string jsonString = obj.ToString();
            JsonNode json = JsonNode.Parse(jsonString) ?? throw new Exception();
            Assert.That(json["name"].GetValue<string>(), Is.EqualTo("John Doe"));
            Assert.That(json["age"].GetValue<int>(), Is.EqualTo(42));
            Assert.That(json["isAlive"].GetValue<bool>(), Is.True);

            Assert.That(json["address"], Is.Not.Null);
            Assert.That(json["address"]["streetAddress"].GetValue<string>(), Is.EqualTo("21 2nd Street"));
            Assert.That(json["address"]["city"].GetValue<string>(), Is.EqualTo("New York"));
            Assert.That(json["address"]["state"].GetValue<string>(), Is.EqualTo("NY"));
            Assert.That(json["address"]["seconds"].GetValue<double>(), Is.EqualTo(seconds));

            Assert.That(json["inventory"], Is.Not.Null);
            Assert.That(json["inventory"][0].GetValue<string>(), Is.EqualTo("apples"));
            Assert.That(json["inventory"][1].GetValue<string>(), Is.EqualTo("oranges"));
            Assert.That(json["inventory"][2].GetValue<string>(), Is.EqualTo("pears"));
            obj.Dispose();
        }

        [Test]
        public void ExampleUsage()
        {
            JSONObject fruit = new();
            fruit.Add("name", "cherry");
            fruit.Add("color", "red");

            JSONArray inventory = new();
            inventory.Add("apples");
            inventory.Add("oranges");
            inventory.Add(fruit);

            using JSONObject jsonObject = new();
            jsonObject.Add("name", "John Doe");
            jsonObject.Add("age", 42);
            jsonObject.Add("alive", true);
            jsonObject.Add("inventory", inventory);

            jsonObject["age"].Number++;

            using UnmanagedList<char> buffer = new();
            jsonObject.ToString(buffer, "    ", true, true);
            ReadOnlySpan<char> jsonText = buffer.AsSpan();
            Console.WriteLine(jsonText.ToString());
        }

        [Test]
        public void ListOfSettings()
        {
            JsonObject settings = new();
            settings.Add("name", "John Doe");
            settings.Add("age", 42);
            settings.Add("isAlive", true);
            settings.Add("another", "aA");

            List<(string name, object? value)> settingsList = new();
            using JSONReader reader = new(settings.ToString());
            reader.ReadToken(); //{
            while (reader.ReadToken(out JSONToken token))
            {
                if (token.type == JSONToken.Type.Text)
                {
                    string name = reader.GetText(token).ToString();
                    JSONToken next = reader.ReadToken();
                    if (next.type == JSONToken.Type.Text)
                    {
                        string value = reader.GetText(next).ToString();
                        settingsList.Add((name, value));
                    }
                    else if (next.type == JSONToken.Type.Number)
                    {
                        double value = reader.GetNumber(next);
                        settingsList.Add((name, value));
                    }
                    else if (next.type == JSONToken.Type.True)
                    {
                        settingsList.Add((name, true));
                    }
                    else if (next.type == JSONToken.Type.False)
                    {
                        settingsList.Add((name, false));
                    }
                    else if (next.type == JSONToken.Type.Null)
                    {
                        settingsList.Add((name, null));
                    }
                }
                else
                {
                    //}
                    break;
                }
            }

            Assert.That(settingsList.Count, Is.EqualTo(4));
            Assert.That(settingsList[0].name, Is.EqualTo("name"));
            Assert.That(settingsList[0].value, Is.EqualTo("John Doe"));
            Assert.That(settingsList[1].name, Is.EqualTo("age"));
            Assert.That(settingsList[1].value, Is.EqualTo(42));
            Assert.That(settingsList[2].name, Is.EqualTo("isAlive"));
            Assert.That(settingsList[2].value, Is.True);
            Assert.That(settingsList[3].name, Is.EqualTo("another"));
            Assert.That(settingsList[3].value, Is.EqualTo("aA"));
        }

        [Test]
        public void ReadJSONWithArray()
        {
            JsonObject json = new();
            JsonArray inventory = new();
            for (int i = 0; i < 32; i++)
            {
                JsonObject item = new();
                item.Add("name", $"Item {i}");
                item.Add("value", Guid.NewGuid().ToString());
                item.Add("quantity", i * (Guid.NewGuid().GetHashCode() % 7));
                item.Add("isRare", i % 2 == 0);
                inventory.Add(item);
            }

            json.Add("inventory", inventory);
            string jsonString = json.ToJsonString(new JsonSerializerOptions() { WriteIndented = false });
            JSONObject obj = new(jsonString.AsSpan());
            JSONArray array = obj.GetArray("inventory");
            Assert.That(array.Count, Is.EqualTo(32));
            string otherString = obj.ToString();
            Assert.That(jsonString, Is.EqualTo(otherString));
            obj.Dispose();
        }

        [Test]
        public void DeserializeIntoStruct()
        {
            var g = Guid.NewGuid();
            using JSONObject item = new();
            item.Add("name", "Item 25");
            item.Add("value", g.ToString());
            item.Add("quantity", g.GetHashCode() % 7);
            item.Add("isRare", g.GetHashCode() % 2 == 0);

            using JSONReader reader = new(item.ToString());
            using DummyJSONObject dummy = reader.ReadObject<DummyJSONObject>();
            Assert.That(dummy.Name.ToString(), Is.EqualTo("Item 25"));
            Assert.That(dummy.Value.ToString(), Is.EqualTo(g.ToString()));
            Assert.That(dummy.quantity, Is.EqualTo(g.GetHashCode() % 7));
            Assert.That(dummy.isRare, Is.EqualTo(g.GetHashCode() % 2 == 0));
        }

        [Test]
        public void SerializeFromStruct()
        {
            using DummyJSONObject dummy = new("abacus", "212-4", 32, false);
            using JSONWriter writer = new();
            writer.WriteObject(dummy);
            string jsonString = writer.ToString();
            Console.WriteLine(jsonString);
            Assert.That(jsonString, Is.EqualTo("{\"name\":\"abacus\",\"value\":\"212-4\",\"quantity\":32,\"isRare\":false}"));
        }

        [Test]
        public void DeserializeArray()
        {
            using JSONObject inventory = new();
            JSONArray array = new();
            for (int i = 0; i < 32; i++)
            {
                array.Add(i);
            }

            inventory.Add("inventory", array);
            using JSONObject obj = new(inventory.ToString());
            JSONArray items = obj.GetArray("inventory");
            Assert.That(items.Count, Is.EqualTo(32));
            for (uint i = 0; i < items.Count; i++)
            {
                Assert.That(items[i].Number, Is.EqualTo(i));
            }
        }

        [Test]
        public void ConvertJSONToStruct()
        {
            JsonObject json = new();
            JsonArray inventory = new();
            List<DummyJSONObject> originals = new();
            for (int i = 0; i < 32; i++)
            {
                var g = Guid.NewGuid();
                DummyJSONObject dummy = new($"Item {i}", g.ToString(), i * (g.GetHashCode() % 7), i % 2 == 0);
                
                JsonObject item = new();
                item.Add("name", dummy.Name.ToString());
                item.Add("value", dummy.Value.ToString());
                item.Add("quantity", dummy.quantity);
                item.Add("isRare", dummy.isRare);
                inventory.Add(item);
                originals.Add(dummy);
            }

            json.Add("inventory", inventory);

            using JSONObject jsonObject = new(json.ToString());
            JSONArray array = jsonObject.GetArray("inventory");
            Assert.That(array.Count, Is.EqualTo(32));
            for (uint i = 0; i < array.Count; i++)
            {
                JSONProperty item = array[i];
                JSONObject itemObj = item.Object;
                using DummyJSONObject dummy = itemObj.As<DummyJSONObject>();
                using DummyJSONObject original = originals[(int)i];
                Assert.That(dummy.Name.ToString(), Is.EqualTo(original.Name.ToString()));
                Assert.That(dummy.Value.ToString(), Is.EqualTo(original.Value.ToString()));
                Assert.That(dummy.quantity, Is.EqualTo(original.quantity));
                Assert.That(dummy.isRare, Is.EqualTo(original.isRare));
            }
        }

        public struct DummyJSONObject : IJSONObject, IDisposable
        {
            public int quantity;
            public bool isRare;

            private UnmanagedArray<char> name;
            private UnmanagedArray<char> value;

            public readonly Span<char> Name => name.AsSpan();
            public readonly Span<char> Value => value.AsSpan();

            public DummyJSONObject(ReadOnlySpan<char> name, ReadOnlySpan<char> value, int quantity, bool isRare)
            {
                this.name = new(name);
                this.value = new(value);
                this.quantity = quantity;
                this.isRare = isRare;
            }

            public void Dispose()
            {
                name.Dispose();
                value.Dispose();
            }

            void IJSONObject.Deserialize(ref JSONReader reader)
            {
                name = new(reader.ReadText(out _));
                value = new(reader.ReadText(out _));
                quantity = (int)reader.ReadNumber(out _);
                isRare = reader.ReadBoolean(out _);
            }

            void IJSONObject.Serialize(JSONWriter writer)
            {
                writer.WriteProperty(nameof(name), name.AsSpan());
                writer.WriteProperty(nameof(value), value.AsSpan());
                writer.WriteProperty(nameof(quantity), quantity);
                writer.WriteProperty(nameof(isRare), isRare);
            }
        }
    }
}
