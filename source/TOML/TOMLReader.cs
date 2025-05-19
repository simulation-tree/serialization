using System;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Serialization.TOML
{
    [SkipLocalsInit]
    public ref struct TOMLReader
    {
        private ByteReader reader;

        public TOMLReader(ByteReader reader)
        {
            this.reader = reader;
        }

        public readonly bool PeekToken(out Token token)
        {
            return PeekToken(out token, out _);
        }

        public readonly bool PeekToken(out Token token, out int readBytes)
        {
            token = default;
            int position = reader.Position;
            int length = reader.Length;
            while (position < length)
            {
                byte bytesRead = reader.PeekUTF8(position, out char c, out _);
                if (c == '#')
                {
                    token = new Token(position, bytesRead, Token.Type.Hash);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (c == '=')
                {
                    token = new Token(position, bytesRead, Token.Type.Equals);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (c == ',')
                {
                    token = new Token(position, bytesRead, Token.Type.Comma);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (c == '.')
                {
                    token = new Token(position, bytesRead, Token.Type.Period);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (c == '[')
                {
                    token = new Token(position, bytesRead, Token.Type.StartSquareBracket);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (c == ']')
                {
                    token = new Token(position, bytesRead, Token.Type.EndSquareBracket);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (c == '{')
                {
                    token = new Token(position, bytesRead, Token.Type.StartCurlyBrace);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (c == '}')
                {
                    token = new Token(position, bytesRead, Token.Type.EndCurlyBrace);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (SharedFunctions.IsWhitespace(c))
                {
                    position += bytesRead;
                }
                else
                {
                    int start = position;
                    position += bytesRead;
                    while (position < length)
                    {
                        bytesRead = reader.PeekUTF8(position, out c, out _);
                        if (SharedFunctions.IsEndOfLine(c) || Token.Tokens.Contains(c))
                        {
                            token = new Token(start, position - start, Token.Type.Text);
                            readBytes = position - reader.Position;
                            return true;
                        }

                        position += bytesRead;
                    }

                    token = new Token(start, position - start, Token.Type.Text);
                    readBytes = position - reader.Position;
                    return true;
                }
            }

            readBytes = default;
            return false;
        }

        public readonly Token ReadToken()
        {
            PeekToken(out Token token, out int readBytes);
            reader.Advance(readBytes);
            return token;
        }

        public readonly bool ReadToken(out Token token)
        {
            bool read = PeekToken(out token, out int readBytes);
            reader.Advance(readBytes);
            return read;
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