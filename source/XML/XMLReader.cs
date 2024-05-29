using System;

namespace Unmanaged.XML
{
    public ref struct XMLReader(BinaryReader reader)
    {
        private BinaryReader reader = reader;
        private bool inside;

        public uint Position
        {
            readonly get => reader.Position;
            set => reader.Position = value;
        }

        public readonly uint Length => reader.Length;

        public readonly ReadOnlySpan<byte> AsSpan()
        {
            return reader.AsSpan();
        }

        public readonly bool PeekToken(uint position, out Token token)
        {
            token = default;
            while (position < reader.Length)
            {
                uint cLength = reader.PeekUTF8(position, out char c, out char high);
                if (c == '<')
                {
                    token = new Token(position, cLength, Token.Type.Open);
                    return true;
                }
                else if (c == '>')
                {
                    token = new Token(position, cLength, Token.Type.Close);
                    return true;
                }
                else if (c == '/')
                {
                    token = new Token(position, cLength, Token.Type.Slash);
                    return true;
                }
                else if (c == '"')
                {
                    uint start = position;
                    position += cLength;
                    while (position < reader.Length)
                    {
                        cLength = reader.PeekUTF8(position, out c, out _);
                        position += cLength;
                        if (c == '"')
                        {
                            token = new Token(start, position - start, Token.Type.Text);
                            return true;
                        }
                    }

                    throw new Exception($"Invalid XML, was reading text starting with '\"' but matching one to close was not found.");
                }
                else if (char.IsLetterOrDigit(c))
                {
                    uint start = position;
                    position += cLength;
                    while (position < reader.Length)
                    {
                        cLength = reader.PeekUTF8(position, out c, out _);
                        if (c == '<')
                        {
                            token = new Token(start, position - start, Token.Type.Text);
                            return true;
                        }
                        else if (inside)
                        {
                            if (c == ' ' || c == '=' || c == '>')
                            {
                                token = new Token(start, position - start, Token.Type.Text);
                                return true;
                            }
                            else if (!char.IsLetterOrDigit(c))
                            {
                                throw new Exception($"Invalid XML, unknown symbol '{c}' inside node.");
                            }
                        }

                        position += cLength;
                    }

                    return false;
                }
                else
                {
                    //skip
                    position += cLength;
                }
            }

            return false;
        }

        public readonly bool PeekToken(out Token token)
        {
            return PeekToken(reader.Position, out token);
        }

        public Token ReadToken()
        {
            ReadToken(out Token token);
            return token;
        }

        public bool ReadToken(out Token token)
        {
            bool read = PeekToken(out token);
            uint end = token.position + token.length;
            reader.Position = end;
            if (token.type == Token.Type.Open)
            {
                inside = true;
            }
            else if (token.type == Token.Type.Close)
            {
                inside = false;
            }

            return read;
        }

        /// <summary>
        /// Reads and creates a new <see cref="XMLNode"/> instance.
        /// </summary>
        public readonly XMLNode ReadNode()
        {
            return reader.ReadObject<XMLNode>();
        }

        public unsafe readonly int GetText(Token token, Span<char> buffer)
        {
            int length = reader.PeekUTF8Span(token.position, token.length, buffer);
            if (buffer[0] == '"')
            {
                fixed (char* ptr = buffer)
                {
                    for (int i = 0; i < length; i++)
                    {
                        ptr[i] = ptr[i + 1];
                    }
                }

                return length - 2;
            }
            else return length;
        }
    }
}