using System;
using Unmanaged.Collections;

namespace Unmanaged.XML
{
    public struct XMLReader : IDisposable
    {
        private BinaryReader reader;
        private bool inside;

        public readonly bool IsDisposed => reader.IsDisposed;
        public uint Position
        {
            readonly get => reader.Position;
            set => reader.Position = value;
        }

        public XMLReader(UnmanagedList<byte> data)
        {
            reader = new(data);
        }

        public XMLReader(ReadOnlySpan<byte> data)
        {
            reader = new(data);
        }

        public unsafe XMLReader(ReadOnlySpan<char> data)
        {
            fixed (char* ptr = data)
            {
                Span<byte> bytes = new(ptr, data.Length * sizeof(char));
                reader = new(bytes);
            }
        }

        public XMLReader(BinaryReader reader)
        {
            this.reader = reader;
        }

        public void Dispose()
        {
            reader.Dispose();
        }

        public readonly ReadOnlySpan<byte> AsSpan()
        {
            return reader.AsSpan();
        }

        public readonly bool PeekToken(uint position, out Token token)
        {
            token = default;
            while (position < reader.Length)
            {
                char c = reader.PeekValue<char>(position);
                if (c == '<')
                {
                    token = new Token(position, sizeof(char), Token.Type.Open);
                    return true;
                }
                else if (c == '>')
                {
                    token = new Token(position, sizeof(char), Token.Type.Close);
                    return true;
                }
                else if (c == '/')
                {
                    token = new Token(position, sizeof(char), Token.Type.Slash);
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

                    throw new Exception($"Invalid XML, was reading text starting with '\"' but matching one to close was not found.");
                }
                else if (char.IsLetterOrDigit(c))
                {
                    uint start = position;
                    position += sizeof(char);
                    while (position < reader.Length)
                    {
                        c = reader.PeekValue<char>(position);
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

                        position += sizeof(char);
                    }

                    throw new Exception($"Invalid XML, was reading content inside a node but no more XML tokens have been found to end.");
                }
                else
                {
                    //skip
                    position += sizeof(char);
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

        public ReadOnlySpan<char> ReadAttribute(out ReadOnlySpan<char> name)
        {
            Token token = ReadToken();
            name = GetText(token);
            token = ReadToken();
            ReadOnlySpan<char> quotedText = GetText(token);
            return quotedText[1..^1];
        }

        public readonly ReadOnlySpan<char> GetText(Token token)
        {
            return reader.PeekSpan<char>(token.position, token.length / sizeof(char));
        }
    }
}