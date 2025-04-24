# Serialization

Unmanaged library for working with common human readable formats using readers and
writers with bytes directly. As well as intermediary/high-level types for representing objects
within the supported formats.

### Supported formats

- JSON
- JSON 5
- XML

### JSON reader and writer

The reader and writers are used to iteratively progress over data. How the data
is stored should be known ahead of time (and can be tested).
```cs
using JSONWriter writer = new();
writer.WriteStartObject();
writer.WriteName("name");
writer.WriteText("John Doe");
writer.WriteEndObject();

using JSONReader reader = new(writer.GetBytes());
reader.ReadStartObject();
ReadOnlySpan<char> propertyValue = reader.ReadText(out ReadOnlySpan<char> propertyName);
reader.ReadEndObject();
```

### Generic JSON object

This is an alternative type thats able to represent a JSON object without the need
for interacting with the reader or writer.
```cs
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

using Text jsonText = new();
jsonObject.ToString(jsonText, "    ", true, true);
Console.WriteLine(jsonText);
```

JSON result:
```json
{
    "name": "John Doe",
    "age": 43,
    "alive": true,
    "inventory": [
        "apples",
        "oranges",
        {
            "name": "cherry",
            "color": "red"
        }
    ]
}
```

### JSON to C# and back

The readers and writers have API for serializing/deserializing `IJSONObject` values:
```cs
public struct Player : IJSONObject, IDisposable
{
    public int hp;
    public bool alive;

    private Text name;

    public readonly Span<char> Name
    {
        get => name.AsSpan();
        set => name.CopyFrom(value);
    }

    public Player(int hp, bool alive, ReadOnlySpan<char> name)
    {
        this.hp = hp;
        this.alive = alive;
        this.name = new(name);
    }

    public void Dispose()
    {
        name.Dispose();
    }

    void IJSONObject.Read(ref JSONReader reader)
    {
        //should initialize itself fully
        hp = (int)reader.ReadNumber(out _);
        alive = reader.ReadBoolean(out _);
        name = new(reader.ReadText(out _));
    }

    void IJSONObject.Write(JSONWriter writer)
    {
        writer.WriteNumber(hp);
        writer.WriteBoolean(alive);
        writer.WriteText(name.AsSpan());
    }
}

byte[] jsonBytes = File.ReadAllBytes("player.json");
using ByteReader reader = new(jsonBytes);
JSONReader jsonReader = new(reader);
using Player player = jsonReader.ReadObject<Player>();
ReadOnlySpan<char> name = player.Name;
```

### XML

XML is supported through the `XMLNode` type, which can be created from either a byte or a char array.
Each node has a name, content, and a list of children. Attributes can be read using the indexer.
```cs
byte[] xmlData = File.ReadAllBytes("solution.csproj");
using XMLNode project = new(xmlData);
XMLAttribute sdk = project["Sdk"];
sdk.Value = "Simulation.NET.Sdk";
project.TryGetFirst("PropertyGroup", out XMLNode propertyGroup);
project.TryGetFirst("TargetFramework", out XMLNode tfm);
tfm.Content = "net9.0";
File.WriteAllText("solution.csproj", project.ToString());
```