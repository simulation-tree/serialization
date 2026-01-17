using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Serialization.TOML
{
    [SkipLocalsInit]
    public unsafe struct TOMLValue : IDisposable
    {
        public readonly ValueType valueType;
        public readonly int length;
        private MemoryAddress data;

        public readonly ReadOnlySpan<char> Text
        {
            get
            {
                MemoryAddress.ThrowIfDefault(data);
                ThrowIfNotTypeOf(ValueType.Text);

                return data.GetSpan<char>(length);
            }
        }

        public readonly ref double Number
        {
            get
            {
                MemoryAddress.ThrowIfDefault(data);
                ThrowIfNotTypeOf(ValueType.Number);

                return ref data.Read<double>();
            }
        }

        public readonly ref bool Boolean
        {
            get
            {
                MemoryAddress.ThrowIfDefault(data);
                ThrowIfNotTypeOf(ValueType.Boolean);

                return ref data.Read<bool>();
            }
        }

        public readonly ref DateTime DateTime
        {
            get
            {
                MemoryAddress.ThrowIfDefault(data);
                ThrowIfNotTypeOf(ValueType.DateTime);

                return ref data.Read<DateTime>();
            }
        }

        public readonly ref TimeSpan TimeSpan
        {
            get
            {
                MemoryAddress.ThrowIfDefault(data);
                ThrowIfNotTypeOf(ValueType.TimeSpan);

                return ref data.Read<TimeSpan>();
            }
        }

        public readonly TOMLArray Array
        {
            get
            {
                MemoryAddress.ThrowIfDefault(data);
                ThrowIfNotTypeOf(ValueType.Array);

                return new(data.Pointer);
            }
        }

        public readonly TOMLTable Table
        {
            get
            {
                MemoryAddress.ThrowIfDefault(data);
                ThrowIfNotTypeOf(ValueType.Table);

                return new(data.Pointer);
            }
        }

        public readonly bool IsDisposed => data == default;

#if NET
        [Obsolete("Default constructor not supported", true)]
        public TOMLValue()
        {
        }
#endif

        public TOMLValue(ReadOnlySpan<char> text)
        {
            valueType = ValueType.Text;
            length = text.Length;
            data = MemoryAddress.Allocate(text);
        }

        public TOMLValue(double number)
        {
            valueType = ValueType.Number;
            length = 1;
            data = MemoryAddress.AllocateValue(number);
        }

        public TOMLValue(bool boolean)
        {
            valueType = ValueType.Boolean;
            length = 1;
            data = MemoryAddress.AllocateValue(boolean);
        }

        public TOMLValue(DateTime dateTime)
        {
            valueType = ValueType.DateTime;
            length = 1;
            data = MemoryAddress.AllocateValue(dateTime);
        }

        public TOMLValue(TimeSpan timeSpan)
        {
            valueType = ValueType.TimeSpan;
            length = 1;
            data = MemoryAddress.AllocateValue(timeSpan);
        }

        public TOMLValue(TOMLArray array)
        {
            valueType = ValueType.Array;
            length = 1;
            data = new(array.array);
        }

        public TOMLValue(TOMLTable table)
        {
            valueType = ValueType.Table;
            length = 1;
            data = new(table.table);
        }

        public readonly override string ToString()
        {
            using Text destination = new(0);
            ToString(destination);
            return destination.ToString();
        }

        public readonly void ToString(Text destination)
        {
            MemoryAddress.ThrowIfDefault(data);

            if (valueType == ValueType.Text)
            {
                Span<char> text = data.GetSpan<char>(length);
                if (text.Contains(' '))
                {
                    destination.Append('"');
                    destination.Append(text);
                    destination.Append('"');
                }
                else
                {
                    destination.Append(text);
                }
            }
            else if (valueType == ValueType.Boolean)
            {
                destination.Append(data.Read<bool>() ? "true" : "false");
            }
            else if (valueType == ValueType.Number)
            {
                destination.Append(data.Read<double>());
            }
            else if (valueType == ValueType.DateTime)
            {
                destination.Append(data.Read<DateTime>());
            }
            else if (valueType == ValueType.TimeSpan)
            {
                destination.Append(data.Read<TimeSpan>());
            }
            else if (valueType == ValueType.Array)
            {
                new TOMLArray(data).ToString(destination);
            }
            else if (valueType == ValueType.Table)
            {
                new TOMLTable(data).ToString(destination);
            }
            else
            {
                throw new NotSupportedException($"Unsupported TOML value type `{valueType}`");
            }
        }

        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(data);

            if (valueType == ValueType.Array)
            {
                TOMLArray array = new(data.Pointer);
                array.Dispose();
            }
            else if (valueType == ValueType.Table)
            {
                TOMLTable table = new(data.Pointer);
                table.Dispose();
            }
            else
            {
                data.Dispose();
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfNotTypeOf(ValueType valueType)
        {
            if (this.valueType != valueType)
            {
                throw new InvalidOperationException($"Array element is not of type `{valueType}`");
            }
        }
    }
}