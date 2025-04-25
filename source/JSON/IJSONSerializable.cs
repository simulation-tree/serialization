namespace Serialization.JSON
{
    public interface IJSONSerializable
    {
        void Read(JSONReader reader);
        void Write(ref JSONWriter writer);
    }
}