using System;
using Unmanaged;

namespace Serialization.XML
{
    public struct SerializationSettings
    {
        public const int DefaultIndentation = 2;
        public static readonly SerializationSettings Default = new(default, 0);
        public static readonly SerializationSettings PrettyPrinted = new(Flags.CarriageReturn | Flags.LineFeed, DefaultIndentation);

        public Flags flags;
        public int indent;

        public SerializationSettings(Flags flags, int indent)
        {
            this.flags = flags;
            this.indent = indent;
        }

        public readonly void Indent(Text text)
        {
            text.Append(' ', indent);
        }

        public readonly void Indent(ByteWriter writer)
        {
            for (int i = 0; i < indent; i++)
            {
                writer.WriteUTF8(' ');
            }
        }

        public readonly void NewLine(Text text)
        {
            if ((flags & Flags.CarriageReturn) == Flags.CarriageReturn)
            {
                text.Append('\r');
            }

            if ((flags & Flags.LineFeed) == Flags.LineFeed)
            {
                text.Append('\n');
            }
        }

        [Flags]
        public enum Flags : byte
        {
            None = 0,
            CarriageReturn = 1,
            LineFeed = 2,
            RootSpacing = 4,
            SkipEmptyNodes = 8
        }
    }
}