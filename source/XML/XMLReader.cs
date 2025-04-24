using System;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Serialization.XML
{
    public ref struct XMLReader
    {
        private ByteReader reader;
        private bool insideNode;

        public readonly ref int Position => ref reader.Position;
        public readonly int Length => reader.Length;

#if NET
        [Obsolete("Default constructor not available", true)]
        public XMLReader()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Creates a new XML format reader on top of the given <see cref="ByteReader"/>.
        /// </summary>
        public XMLReader(ByteReader reader)
        {
            this.reader = reader;
            insideNode = false;
        }

        public readonly ReadOnlySpan<byte> AsSpan()
        {
            return reader.GetBytes();
        }

        public readonly bool PeekToken(int position, out Token token)
        {
            token = default;
            while (position < reader.Length)
            {
                int cLength = reader.PeekUTF8(position, out char c, out char high);
                if (c == '<')
                {
                    token = new Token(position, cLength, Token.Type.Open);
                    return true;
                }
                else if (c == '?')
                {
                    token = new Token(position, cLength, Token.Type.Prologue);
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
                    int start = position;
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
                else if (c == '=')
                {
                    //skip
                    position += cLength;
                }
                else if (char.IsLetterOrDigit(c) || !SharedFunctions.IsWhitespace(c))
                {
                    int start = position;
                    position += cLength;
                    while (position < reader.Length)
                    {
                        cLength = reader.PeekUTF8(position, out c, out _);
                        if (c == '<')
                        {
                            token = new Token(start, position - start, Token.Type.Text);
                            return true;
                        }
                        else if (insideNode)
                        {
                            if (c == ' ' || c == '=' || c == '>' || c == '/')
                            {
                                token = new Token(start, position - start, Token.Type.Text);
                                return true;
                            }
                            else if (!char.IsLetterOrDigit(c))
                            {
                                throw new Exception($"Invalid XML, unknown symbol '{c}' inside node");
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
            int end = token.position + token.length;
            reader.Position = end;
            if (token.type == Token.Type.Open)
            {
                insideNode = true;
            }
            else if (token.type == Token.Type.Close)
            {
                insideNode = false;
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

        /// <summary>
        /// Copies the underlying text of the given <paramref name="token"/> into
        /// the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/> values copied.</returns>
        public readonly int GetText(Token token, Span<char> destination)
        {
            int length = reader.PeekUTF8(token.position, token.length, destination);
            if (destination[0] == '"')
            {
                for (int i = 0; i < length - 1; i++)
                {
                    destination[i] = destination[i + 1];
                }

                return length - 2;
            }
            else return length;
        }

        [SkipLocalsInit]
        public readonly int GetText(Token token, Text destination)
        {
            Span<char> buffer = stackalloc char[token.length];
            int length = GetText(token, buffer);
            destination.Append(buffer.Slice(0, length));
            return length;
        }
    }
}