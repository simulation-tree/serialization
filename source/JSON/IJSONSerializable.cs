namespace Serialization.JSON
{
    public interface IJSONSerializable
    {
        void Read(JSONReader byteReader);
        void Write(ref JSONWriter byteWriter);
    }
}