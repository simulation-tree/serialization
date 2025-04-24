using System;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Serialization.JSON
{
    /// <summary>
    /// A <see cref="ByteWriter"/> wrapper for reading JSON.
    /// </summary>
    [SkipLocalsInit]
    public struct JSONReader
    {
        private ByteReader reader;

        public readonly bool IsDisposed => reader.IsDisposed;

#if NET
        [Obsolete("Default constructor not available", true)]
        public JSONReader()
        {
            throw new NotImplementedException();
        }
#endif

        /// <summary>
        /// Creates a new wrapper around the given binary reader.
        /// </summary>
        public JSONReader(ByteReader reader)
        {
            this.reader = reader;
        }

        public readonly bool PeekToken(out Token token, out int readBytes)
        {
            token = default;
            int position = reader.Position;
            int length = reader.Length;
            while (position < length)
            {
                byte cLength = reader.PeekUTF8(position, out char c, out _);
                if (c == '{')
                {
                    token = new Token(position, cLength, Token.Type.StartObject);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (c == '}')
                {
                    token = new Token(position, cLength, Token.Type.EndObject);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (c == '[')
                {
                    token = new Token(position, cLength, Token.Type.StartArray);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (c == ']')
                {
                    token = new Token(position, cLength, Token.Type.EndArray);
                    readBytes = position - reader.Position + 1;
                    return true;
                }
                else if (c == ',' || c == ':' || SharedFunctions.IsWhitespace(c))
                {
                    position += cLength;
                }
                else if (c == '"')
                {
                    position += cLength;
                    int start = position;
                    while (position < length)
                    {
                        cLength = reader.PeekUTF8(position, out c, out _);
                        if (c == '"')
                        {
                            token = new Token(start, position - start, Token.Type.Text);
                            readBytes = position - reader.Position + 2;
                            return true;
                        }

                        position += cLength;
                    }
                }
                else if (c == '\'')
                {
                    position += cLength;
                    int start = position;
                    while (position < length)
                    {
                        cLength = reader.PeekUTF8(position, out c, out _);
                        if (c == '\'')
                        {
                            token = new Token(start, position - start, Token.Type.Text);
                            readBytes = position - reader.Position + 2;
                            return true;
                        }

                        position += cLength;
                    }
                }
                else
                {
                    int start = position;
                    position += cLength;
                    while (position < length)
                    {
                        cLength = reader.PeekUTF8(position, out c, out _);
                        if (c == '{' || c == '}' || c == '[' || c == ']' || c == ',' || c == ':' || SharedFunctions.IsWhitespace(c))
                        {
                            token = new Token(start, position - start, Token.Type.Text);
                            readBytes = position - reader.Position;
                            return true;
                        }

                        position += cLength;
                    }

                    throw new InvalidOperationException($"Unexpected end of stream while reading token, expected a JSON token to finish the text");
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

        public readonly int ReadText(Span<char> buffer)
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

        public readonly double ReadNumber()
        {
            while (ReadToken(out Token token))
            {
                if (token.type == Token.Type.EndObject || token.type == Token.Type.EndArray)
                {
                    //skip
                }
                else if (token.type == Token.Type.Text)
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

        public readonly bool ReadBoolean()
        {
            Span<char> buffer = stackalloc char[32];
            while (ReadToken(out Token token))
            {
                if (token.type == Token.Type.EndObject || token.type == Token.Type.EndArray)
                {
                    //skip
                }
                else if (token.type == Token.Type.Text)
                {
                    int length = GetText(token, buffer);
                    if (buffer.Slice(0, length).SequenceEqual("true"))
                    {
                        return true;
                    }
                    else if (buffer.Slice(0, length).SequenceEqual("false"))
                    {
                        return false;
                    }

                    throw new InvalidOperationException($"Could not parse {buffer.Slice(0, length).ToString()} as a boolean");
                }
                else
                {
                    throw new InvalidOperationException($"Expected token for property name but found {token.type}");
                }
            }

            throw new InvalidOperationException("Expected token for boolean but none more found");
        }

        public readonly T ReadObject<T>() where T : unmanaged, IJSONSerializable
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
                    if (PeekToken(out Token peek, out int readBytes) && peek.type == Token.Type.EndObject)
                    {
                        reader.Advance(readBytes);
                        //reached end of object
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

        public unsafe readonly int GetText(Token token, Span<char> destination)
        {
            return reader.PeekUTF8(token.position, token.length, destination);
        }

        public readonly double GetNumber(Token token)
        {
            Span<char> buffer = stackalloc char[token.length];
            int length = GetText(token, buffer);
            return double.Parse(buffer.Slice(0, length));
        }

        public readonly bool GetBoolean(Token token)
        {
            Span<char> buffer = stackalloc char[token.length];
            int length = GetText(token, buffer);
            return buffer.Slice(0, length).SequenceEqual("true".AsSpan());
        }
    }
}