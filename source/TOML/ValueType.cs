namespace Serialization.TOML
{
    public enum ValueType : byte
    {
        Unknown,
        Text,
        Number,
        Boolean,
        DateTime,
        Array,
        Table
    }
}