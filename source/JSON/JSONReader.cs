using System;

namespace Unmanaged.JSON
{
    public struct JSONReader
    {
        private BinaryReader reader;

        public readonly bool IsDisposed => reader.IsDisposed;

#if NET5_0_OR_GREATER
        [Obsolete("Use Create() or other constructor", true)]
        public JSONReader()
        {
            throw new NotImplementedException();
        }
#endif

        public JSONReader(BinaryReader reader)
        {
            this.reader = reader;
        }

        public readonly bool PeekToken(out Token token)
        {
            Span<char> buffer = stackalloc char[8];
            token = default;
            uint position = reader.Position;
            while (position < reader.Length)
            {
                byte cLength = reader.PeekUTF8(position, out char c, out _);
                if (c == '{')
                {
                    token = new Token(position, cLength, Token.Type.StartObject);
                    return true;
                }
                else if (c == '}')
                {
                    token = new Token(position, cLength, Token.Type.EndObject);
                    return true;
                }
                else if (c == '[')
                {
                    token = new Token(position, cLength, Token.Type.StartArray);
                    return true;
                }
                else if (c == ']')
                {
                    token = new Token(position, cLength, Token.Type.EndArray);
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

                    throw new InvalidOperationException($"Invalid JSON token at position {position}, expected '\"'.");
                }
                else if (c == 't' || c == 'f')
                {
                    int peekLength = reader.PeekUTF8Span(position, 5, buffer);
                    if (buffer[..peekLength].SequenceEqual("false"))
                    {
                        token = new Token(position, (uint)peekLength, Token.Type.False);
                        return true;
                    }

                    Span<char> smallerBuffer = buffer[..(peekLength - 1)];
                    if (smallerBuffer.SequenceEqual("true"))
                    {
                        token = new Token(position, (uint)peekLength - 1, Token.Type.True);
                        return true;
                    }

                    throw new InvalidOperationException($"Unexpected token {c} at {position}.");
                }
                else if (char.IsDigit(c) || c == '.' || c == '-')
                {
                    uint start = position;
                    position += cLength;
                    while (position < reader.Length)
                    {
                        cLength = reader.PeekUTF8(position, out c, out _);
                        if (!char.IsDigit(c) && c != '.' && c != '-')
                        {
                            token = new Token(start, position - start, Token.Type.Number);
                            return true;
                        }

                        position += cLength;
                    }

                    throw new InvalidOperationException($"Invalid JSON token at position {position}, expected number.");
                }
                else
                {
                    //skip
                    position += cLength;
                }
            }

            return false;
        }

        public Token ReadToken()
        {
            PeekToken(out Token token);
            reader.Position = token.position + token.length;
            return token;
        }

        public bool ReadToken(out Token token)
        {
            bool read = PeekToken(out token);
            uint end = token.position + token.length;
            reader.Position = end;
            return read;
        }

        public int ReadText(Span<char> buffer)
        {
            while (ReadToken(out Token token))
            {
                if (token.type == Token.Type.EndObject || token.type == Token.Type.EndArray)
                {
                    //skip
                }
                else if (token.type == Token.Type.Text)
                {
                    return GetText(token, buffer);
                }
                else
                {
                    break;
                }
            }

            throw new InvalidOperationException("Expected token for text but none found");
        }

        public double ReadNumber()
        {
            while (ReadToken(out Token token))
            {
                if (token.type == Token.Type.EndObject || token.type == Token.Type.EndArray)
                {
                    //skip
                }
                else if (token.type == Token.Type.Number)
                {
                    return GetNumber(token);
                }
                else
                {
                    break;
                }
            }

            throw new InvalidOperationException("Expected token for number but none found");
        }

        public bool ReadBoolean()
        {
            while (ReadToken(out Token token))
            {
                if (token.type == Token.Type.EndObject || token.type == Token.Type.EndArray)
                {
                    //skip
                }
                else if (token.type == Token.Type.True)
                {
                    return true;
                }
                else if (token.type == Token.Type.False)
                {
                    return false;
                }
                else
                {
                    throw new InvalidOperationException($"Expected token for property name but found {token.type}");
                }
            }

            throw new InvalidOperationException("Expected token for boolean but none more found");
        }

        public T ReadObject<T>() where T : unmanaged, IJSONSerializable
        {
            while (ReadToken(out Token token))
            {
                if (token.type == Token.Type.EndObject || token.type == Token.Type.EndArray || token.type == Token.Type.Text)
                {
                    //skip
                }
                else if (token.type == Token.Type.StartObject)
                {
                    T obj = default;
                    obj.Read(this);
                    if (PeekToken(out var peek) && peek.type == Token.Type.EndObject)
                    {
                        ReadToken();
                    }

                    return obj;
                }
                else
                {
                    break;
                }
            }

            throw new InvalidOperationException("Expected start object token.");
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

        public readonly double GetNumber(Token token)
        {
            Span<char> buffer = stackalloc char[(int)token.length];
            int length = GetText(token, buffer);
            return double.Parse(buffer[..length]);
        }

        public readonly bool GetBoolean(Token token)
        {
            Span<char> buffer = stackalloc char[(int)token.length];
            int length = GetText(token, buffer);
            return buffer[..length].SequenceEqual("true");
        }
    }
}