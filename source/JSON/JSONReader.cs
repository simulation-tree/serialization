using System;
using Unmanaged.Collections;

namespace Unmanaged.JSON
{
    public struct JSONReader : IDisposable
    {
        private BinaryReader reader;

        public readonly bool IsDisposed => reader.IsDisposed;

        public JSONReader(ReadOnlySpan<byte> data)
        {
            reader = new(data);
        }

        public JSONReader(UnmanagedList<byte> list)
        {
            reader = new(list);
        }

        public JSONReader(BinaryReader reader)
        {
            this.reader = reader;
        }

        public unsafe JSONReader(ReadOnlySpan<char> data)
        {
            fixed (char* ptr = data)
            {
                Span<byte> bytes = new(ptr, data.Length * sizeof(char));
                reader = new(bytes);
            }
        }

        public void Dispose()
        {
            reader.Dispose();
        }

        public readonly bool PeekToken(out Token token)
        {
            token = default;
            uint position = reader.Position;
            while (position < reader.Length)
            {
                char c = reader.PeekValue<char>(position);
                if (c == '{')
                {
                    token = new Token(position, sizeof(char), Token.Type.StartObject);
                    return true;
                }
                else if (c == '}')
                {
                    token = new Token(position, sizeof(char), Token.Type.EndObject);
                    return true;
                }
                else if (c == '[')
                {
                    token = new Token(position, sizeof(char), Token.Type.StartArray);
                    return true;
                }
                else if (c == ']')
                {
                    token = new Token(position, sizeof(char), Token.Type.EndArray);
                    return true;
                }
                else if (c == '"')
                {
                    uint start = position;
                    position += sizeof(char);
                    while (position < reader.Length)
                    {
                        c = reader.PeekValue<char>(position);
                        position += sizeof(char);
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
                    if (position + (4 * sizeof(char)) < reader.Length)
                    {
                        if (reader.PeekValue<uint>(position) == 7471220)
                        {
                            token = new Token(position, 4 * sizeof(char), Token.Type.True);
                            return true;
                        }
                    }
                     
                    if (position + (5 * sizeof(char)) < reader.Length)
                    {
                        if (reader.PeekValue<uint>(position) == 6357094 && reader.PeekValue<char>(position + (4 * sizeof(char))) == 'e')
                        {
                            token = new Token(position, 5 * sizeof(char), Token.Type.False);
                            return true;
                        }
                    }

                    throw new InvalidOperationException($"Unexpected token {c} at {position}.");
                }
                else if (char.IsDigit(c) || c == '.' || c == '-')
                {
                    uint start = position;
                    position += sizeof(char);
                    while (position < reader.Length)
                    {
                        c = reader.PeekValue<char>(position);
                        if (!char.IsDigit(c) && c != '.' && c != '-')
                        {
                            token = new Token(start, position - start, Token.Type.Number);
                            return true;
                        }

                        position += sizeof(char);
                    }

                    throw new InvalidOperationException($"Invalid JSON token at position {position}, expected number.");
                }
                else
                {
                    //skip
                    position += sizeof(char);
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

        public ReadOnlySpan<char> ReadText(out ReadOnlySpan<char> name)
        {
            while (ReadToken(out Token token))
            {
                if (token.type == Token.Type.EndObject || token.type == Token.Type.EndArray)
                {
                    //skip
                }
                else if (token.type == Token.Type.Text)
                {
                    name = GetText(token);
                    if (ReadToken(out Token value) && value.type == Token.Type.Text)
                    {
                        return GetText(value);
                    }
                }
                else
                {
                    break;
                }
            }

            throw new InvalidOperationException("Expected token for property name");
        }

        public double ReadNumber(out ReadOnlySpan<char> name)
        {
            while (ReadToken(out Token token))
            {
                if (token.type == Token.Type.EndObject || token.type == Token.Type.EndArray)
                {
                    //skip
                }
                else if (token.type == Token.Type.Text)
                {
                    name = GetText(token);
                    if (ReadToken(out Token value) && value.type == Token.Type.Number)
                    {
                        return GetNumber(value);
                    }
                }
                else
                {
                    break;
                }
            }

            throw new InvalidOperationException("Expected token for property name");
        }

        public bool ReadBoolean(out ReadOnlySpan<char> name)
        {
            while (ReadToken(out Token token))
            {
                if (token.type == Token.Type.EndObject || token.type == Token.Type.EndArray)
                {
                    //skip
                }
                else if (token.type == Token.Type.Text)
                {
                    name = GetText(token);
                    if (ReadToken(out Token value) && (value.type == Token.Type.True || value.type == Token.Type.False))
                    {
                        return GetBoolean(value);
                    }
                }
                else
                {
                    break;
                }
            }

            throw new InvalidOperationException("Expected token for property name");
        }

        public T ReadObject<T>() where T : unmanaged, IJSONObject
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
                    obj.Read(ref this);
                    return obj;
                }
                else
                {
                    break;
                }
            }

            throw new InvalidOperationException("Expected start object token.");
        }

        public readonly ReadOnlySpan<char> GetText(Token token)
        {
            ReadOnlySpan<char> span = reader.PeekSpan<char>(token.position, token.length / sizeof(char));
            if (token.type == Token.Type.Text)
            {
                return span[1..^1];
            }
            else
            {
                return span;
            }
        }

        public readonly double GetNumber(Token token)
        {
            ReadOnlySpan<char> span = GetText(token);
            return double.Parse(span);
        }

        public readonly bool GetBoolean(Token token)
        {
            ReadOnlySpan<char> span = GetText(token);
            return span[0] == 't' || span[0] == '0';
        }
    }
}