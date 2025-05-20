namespace Serialization.TOML
{
    public enum ValueType : byte
    {
        Unknown,
        Text,
        Number,
        Boolean,
        DateTime,
        TimeSpan,
        Array,
        Table
    }
}