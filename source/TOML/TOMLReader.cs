using System;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Serialization.TOML
{
    [SkipLocalsInit]
    public readonly ref struct TOMLReader
    {
        private readonly ByteReader byteReader;

        public TOMLReader(ByteReader byteReader)
        {
            this.byteReader = byteReader;
        }

        public readonly bool PeekToken(out Token token)
        {
            return PeekToken(out token, out _);
        }

        public readonly bool PeekToken(out Token token, out int readBytes)
        {
            token = default;
            int position = byteReader.Position;
            int length = byteReader.Length;
            while (position < length)
            {
                byte bytesRead = byteReader.PeekUTF8(position, out char c, out _);
                if (c == '#')
                {
                    token = new Token(position, bytesRead, Token.Type.Hash);
                    readBytes = position - byteReader.Position + 1;
                    return true;
                }
                else if (c == '=')
                {
                    token = new Token(position, bytesRead, Token.Type.Equals);
                    readBytes = position - byteReader.Position + 1;
                    return true;
                }
                else if (c == ',')
                {
                    token = new Token(position, bytesRead, Token.Type.Comma);
                    readBytes = position - byteReader.Position + 1;
                    return true;
                }
                else if (c == '[')
                {
                    token = new Token(position, bytesRead, Token.Type.StartSquareBracket);
                    readBytes = position - byteReader.Position + 1;
                    return true;
                }
                else if (c == ']')
                {
                    token = new Token(position, bytesRead, Token.Type.EndSquareBracket);
                    readBytes = position - byteReader.Position + 1;
                    return true;
                }
                else if (c == '{')
                {
                    token = new Token(position, bytesRead, Token.Type.StartCurlyBrace);
                    readBytes = position - byteReader.Position + 1;
                    return true;
                }
                else if (c == '}')
                {
                    token = new Token(position, bytesRead, Token.Type.EndCurlyBrace);
                    readBytes = position - byteReader.Position + 1;
                    return true;
                }
                else if (c == '"')
                {
                    position += bytesRead;
                    int start = position;
                    while (position < length)
                    {
                        bytesRead = byteReader.PeekUTF8(position, out c, out _);
                        if (c == '"')
                        {
                            token = new Token(start, position - start, Token.Type.Text);
                            readBytes = position - byteReader.Position + 1;
                            return true;
                        }

                        position += bytesRead;
                    }

                    throw new InvalidOperationException("Unterminated string literal");
                }
                else if (c == '\'')
                {
                    position += bytesRead;
                    int start = position;
                    while (position < length)
                    {
                        bytesRead = byteReader.PeekUTF8(position, out c, out _);
                        if (c == '\'')
                        {
                            token = new Token(start, position - start, Token.Type.Text);
                            readBytes = position - byteReader.Position + 1;
                            return true;
                        }

                        position += bytesRead;
                    }

                    throw new InvalidOperationException("Unterminated string literal");
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
                        bytesRead = byteReader.PeekUTF8(position, out c, out _);
                        if (SharedFunctions.IsEndOfLine(c) || Token.Tokens.Contains(c))
                        {
                            if (c == '=')
                            {
                                //trim whitespace
                                int trim = 0;
                                byteReader.PeekUTF8(position - trim - 1, out c, out _);
                                while (SharedFunctions.IsWhitespace(c))
                                {
                                    trim++;
                                    byteReader.PeekUTF8(position - trim - 1, out c, out _);
                                }

                                token = new Token(start, position - start - trim, Token.Type.Text);
                                readBytes = position - byteReader.Position;
                                return true;
                            }
                            else
                            {
                                token = new Token(start, position - start, Token.Type.Text);
                                readBytes = position - byteReader.Position;
                                return true;
                            }
                        }

                        position += bytesRead;
                    }

                    token = new Token(start, position - start, Token.Type.Text);
                    readBytes = position - byteReader.Position;
                    return true;
                }
            }

            readBytes = default;
            return false;
        }

        public readonly Token ReadToken()
        {
            PeekToken(out Token token, out int readBytes);
            byteReader.Advance(readBytes);
            return token;
        }

        public readonly bool ReadToken(out Token token)
        {
            bool read = PeekToken(out token, out int readBytes);
            byteReader.Advance(readBytes);
            return read;
        }

        /// <summary>
        /// Copies the underlying text of the given <paramref name="token"/> into
        /// the <paramref name="destination"/>.
        /// </summary>
        /// <returns>Amount of <see cref="char"/> values copied.</returns>
        public readonly int GetText(Token token, Span<char> destination)
        {
            int length = byteReader.PeekUTF8(token.position, token.length, destination);
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

        public readonly int AppendText(Token token, Text destination)
        {
            Span<char> buffer = stackalloc char[token.length * 4];
            int length = GetText(token, buffer);
            destination.Append(buffer.Slice(0, length));
            return length;
        }
    }
}