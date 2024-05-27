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

        public JSONReader(ReadOnlySpan<char> data)
        {
            BinaryWriter writer = new();
            writer.WriteSpan(data);
            reader = new(writer.AsSpan());
            writer.Dispose();
        }

        public void Dispose()
        {
            reader.Dispose();
        }

        public readonly bool PeekToken(out JSONToken token)
        {
            token = default;
            uint position = reader.Position;
            while (position < reader.Length)
            {
                char c = reader.PeekValue<char>(position);
                if (c == '{')
                {
                    token = new JSONToken(position, sizeof(char), JSONToken.Type.StartObject);
                    return true;
                }
                else if (c == '}')
                {
                    token = new JSONToken(position, sizeof(char), JSONToken.Type.EndObject);
                    return true;
                }
                else if (c == '[')
                {
                    token = new JSONToken(position, sizeof(char), JSONToken.Type.StartArray);
                    return true;
                }
                else if (c == ']')
                {
                    token = new JSONToken(position, sizeof(char), JSONToken.Type.EndArray);
                    return true;
                }
                else if (c == '"')
                {
                    uint start = position;
                    position += sizeof(char);
                    while (start < reader.Length)
                    {
                        c = reader.PeekValue<char>(position);
                        position += sizeof(char);
                        if (c == '"')
                        {
                            token = new JSONToken(start, position - start, JSONToken.Type.Text);
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
                            token = new JSONToken(position, 4 * sizeof(char), JSONToken.Type.True);
                            return true;
                        }
                    }
                     
                    if (position + (5 * sizeof(char)) < reader.Length)
                    {
                        if (reader.PeekValue<uint>(position) == 6357094 && reader.PeekValue<char>(position + (4 * sizeof(char))) == 'e')
                        {
                            token = new JSONToken(position, 5 * sizeof(char), JSONToken.Type.False);
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
                            token = new JSONToken(start, position - start, JSONToken.Type.Number);
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

        public JSONToken ReadToken()
        {
            PeekToken(out JSONToken token);
            reader.Position = token.position + token.length;
            return token;
        }

        public bool ReadToken(out JSONToken token)
        {
            bool read = PeekToken(out token);
            uint end = token.position + token.length;
            reader.Position = end;
            return read;
        }

        public ReadOnlySpan<char> ReadText(out ReadOnlySpan<char> name)
        {
            while (ReadToken(out JSONToken token))
            {
                if (token.type == JSONToken.Type.EndObject || token.type == JSONToken.Type.EndArray)
                {
                    //skip
                }
                else if (token.type == JSONToken.Type.Text)
                {
                    name = GetText(token);
                    if (ReadToken(out JSONToken value) && value.type == JSONToken.Type.Text)
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
            while (ReadToken(out JSONToken token))
            {
                if (token.type == JSONToken.Type.EndObject || token.type == JSONToken.Type.EndArray)
                {
                    //skip
                }
                else if (token.type == JSONToken.Type.Text)
                {
                    name = GetText(token);
                    if (ReadToken(out JSONToken value) && value.type == JSONToken.Type.Number)
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
            while (ReadToken(out JSONToken token))
            {
                if (token.type == JSONToken.Type.EndObject || token.type == JSONToken.Type.EndArray)
                {
                    //skip
                }
                else if (token.type == JSONToken.Type.Text)
                {
                    name = GetText(token);
                    if (ReadToken(out JSONToken value) && (value.type == JSONToken.Type.True || value.type == JSONToken.Type.False))
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
            while (ReadToken(out JSONToken token))
            {
                if (token.type == JSONToken.Type.EndObject || token.type == JSONToken.Type.EndArray || token.type == JSONToken.Type.Text)
                {
                    //skip
                }
                else if (token.type == JSONToken.Type.StartObject)
                {
                    T obj = default;
                    obj.Deserialize(ref this);
                    return obj;
                }
                else
                {
                    break;
                }
            }

            throw new InvalidOperationException("Expected start object token.");
        }

        public readonly ReadOnlySpan<char> GetText(JSONToken token)
        {
            ReadOnlySpan<char> span = reader.PeekSpan<char>(token.position, token.length / sizeof(char));
            if (token.type == JSONToken.Type.Text)
            {
                return span[1..^1];
            }
            else
            {
                return span;
            }
        }

        public readonly double GetNumber(JSONToken token)
        {
            ReadOnlySpan<char> span = GetText(token);
            return double.Parse(span);
        }

        public readonly bool GetBoolean(JSONToken token)
        {
            ReadOnlySpan<char> span = GetText(token);
            return span[0] == 't' || span[0] == '0';
        }
    }
}