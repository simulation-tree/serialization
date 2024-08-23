# Serialization
Unmanaged library for working with JSON and XML using readers and writers with bytes directly.
As well as intermediary types for representing objects within the supported formats.

### JSON Reader and writer
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

### Generic JSON Object
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

using UnmanagedList<char> buffer = new();
jsonObject.ToString(buffer, "    ", true, true);
ReadOnlySpan<char> jsonText = buffer.AsSpan();
Console.WriteLine(jsonText.ToString());
```
```json
{
    "name":"John Doe",
    "age":43,
    "alive":true,
    "inventory":[
        "apples",
        "oranges",
        {
            "name":"cherry",
            "color":"red"
        }
    ]
}
```

### JSON to C# and back
The readers and writers have API for serializing/deserializing `IJSONObject` values.
If the JSON object contains text values, then a `ReadOnlySpan<char>` will be expected
to be stored inside the value type. Using types like `string` and `List<char>` makes the type
not acceptable because of the unmanaged requirement, an `UnmanagedArray<char>` is used instead.
```cs
public struct Player : IJSONObject, IDisposable
{
    public int hp;
    public bool alive;

    private UnmanagedArray<char> name;

    public readonly Span<char> Name
    {
        get => name.AsSpan();
        set
        {
            name.Resize(value.Length);
            value.CopyTo(name.AsSpan());
        }
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
using JSONReader reader = new(jsonBytes);
using Player player = reader.ReadObject<Player>();
ReadOnlySpan<char> name = player.Name;
```

### XML
XML is supported through the `XMLNode` type, which can be created from either a byte or a char array.
Each node has a name, content, and a list of children. Attributes can be read using the indexer.
```csharp
byte[] xmlData = File.ReadAllBytes("solution.csproj");
using XMLNode project = new(xmlData);
XMLAttribute sdk = project["Sdk"];
sdk.Value = "Simulation.NET.Sdk";
project.TryGetFirst("PropertyGroup", out XMLNode propertyGroup);
project.TryGetFirst("TargetFramework", out XMLNode tfm);
tfm.Content = "net9.0";
File.WriteAllText("solution.csproj", project.ToString());
```