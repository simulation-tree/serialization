namespace Unmanaged.JSON
{
    public interface IJSONObject
    {
        void Read(ref JSONReader reader);
        void Write(JSONWriter writer);
    }
}