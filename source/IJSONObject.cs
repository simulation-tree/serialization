namespace Unmanaged.JSON
{
    public interface IJSONObject
    {
        void Deserialize(ref JSONReader reader);
        void Serialize(JSONWriter writer);
    }
}