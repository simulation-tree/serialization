using System;

namespace Serialization.XML
{
    [Flags]
    public enum ToStringFlags : byte
    {
        None = 0,
        CarriageReturn = 1,
        LineFeed = 2,
        RootSpacing = 4
    }
}