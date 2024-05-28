namespace Unmanaged.JSON
{
    public interface IJSONSerializable
    {
        void Read(JSONReader reader);
        void Write(JSONWriter writer);
    }
}