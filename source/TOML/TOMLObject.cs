using System;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Serialization.TOML
{
    [SkipLocalsInit]
    public readonly struct TOMLObject : IDisposable, ISerializable
    {
        public readonly void Dispose()
        {
        }

        void ISerializable.Read(ByteReader byteReader)
        {

        }

        readonly void ISerializable.Write(ByteWriter byteWriter)
        {
        }
    }

    public readonly struct TOMLKeyValue
    {

    }
}