using System;
using Unmanaged;

namespace Serialization.JSON
{
    public struct SerializationSettings
    {
        public const int DefaultIndentation = 4;
        public static readonly SerializationSettings Default = new();
        public static readonly SerializationSettings JSON5 = new(Flags.QuotelessNames | Flags.SingleQuotedText);
        public static readonly SerializationSettings PrettyPrinted = new(Flags.CarrierReturn | Flags.LineFeed | Flags.SpaceAfterColon, DefaultIndentation);
        public static readonly SerializationSettings JSON5PrettyPrinted = new(Flags.CarrierReturn | Flags.LineFeed | Flags.QuotelessNames | Flags.SingleQuotedText | Flags.SpaceAfterColon, DefaultIndentation);

        public Flags flags;
        public int indent;

        public SerializationSettings(Flags flags, int indent = 0)
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
            if ((flags & Flags.CarrierReturn) != 0)
            {
                text.Append('\r');
            }

            if ((flags & Flags.LineFeed) != 0)
            {
                text.Append('\n');
            }
        }

        public readonly void NewLine(ByteWriter writer)
        {
            if ((flags & Flags.CarrierReturn) != 0)
            {
                writer.WriteUTF8('\r');
            }

            if ((flags & Flags.LineFeed) != 0)
            {
                writer.WriteUTF8('\n');
            }
        }

        public readonly void WriteTextQuoteCharacter(ByteWriter writer)
        {
            if ((flags & Flags.SingleQuotedText) != 0)
            {
                writer.WriteUTF8('\'');
            }
            else
            {
                writer.WriteUTF8('"');
            }
        }

        public readonly void WriteNameQuoteCharacter(ByteWriter writer)
        {
            if ((flags & Flags.QuotelessNames) == 0)
            {
                writer.WriteUTF8('"');
            }
        }

        public readonly void SpaceAfterColon(ByteWriter writer)
        {
            if ((flags & Flags.SpaceAfterColon) != 0)
            {
                writer.WriteUTF8(' ');
            }
        }

        [Flags]
        public enum Flags : byte
        {
            None = 0,
            CarrierReturn = 1,
            LineFeed = 2,
            QuotelessNames = 4,
            SingleQuotedText = 8,
            SpaceAfterColon = 16,
        }
    }
}