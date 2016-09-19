using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Dapper
{
    internal struct FieldMap : IEquatable<FieldMap>
    {
        private readonly Type type;
        private readonly Field[] fields;
        private readonly bool returnNullIfFirstMissing;
        private readonly int hashcode;

        public FieldMap(Type type, int[] fieldToTypeMap, int typeIndex, int fieldNamePrefixLength, bool returnNullIfFirstMissing, IDataReader reader)
        {
            this.type = type;
            this.returnNullIfFirstMissing = returnNullIfFirstMissing;
            fields = CreateTypeToFieldMap(fieldToTypeMap, typeIndex, fieldNamePrefixLength, reader);
            hashcode = CreateHashCode(type, fields, returnNullIfFirstMissing);
        }

        public FieldMap(Type type, int start, int length, bool returnNullIfFirstMissing, IDataReader reader)
        {
            if (reader == null || reader.FieldCount == 0)
            {
                throw new InvalidOperationException("No columns were selected");
            }
            if (reader.FieldCount <= start)
            {
                throw new ArgumentException("When using the multi-mapping APIs ensure you set the splitOn param if you have keys other than Id", "splitOn");
            }
            if (length < 0)
            {
                length = reader.FieldCount - start;
            }

            this.type = type;
            this.returnNullIfFirstMissing = returnNullIfFirstMissing;
            fields = new Field[length];
            for (var index = 0; index < length; index++)
            {
                var ordinal = start + index;
                fields[index] = new Field(ordinal, 0, reader);
            }
            hashcode = CreateHashCode(type, fields, returnNullIfFirstMissing);
        }

        private static Field[] CreateTypeToFieldMap(int[] fieldToTypeMap, int typeIndex, int fieldNamePrefixLength, IDataReader reader)
        {
            var count = fieldToTypeMap.Count(x => x == typeIndex);
            var fields = new Field[count];

            int ordinal = 0, mapIndex = 0;
            while (mapIndex < count)
            {
                if (fieldToTypeMap[ordinal] == typeIndex)
                {
                    fields[mapIndex] = new Field(ordinal, fieldNamePrefixLength, reader);
                    mapIndex++;
                }
                ordinal++;
            }

            return fields;
        }

        private static int CreateHashCode(Type type, Field[] fields, bool returnNullIfFirstMissing)
        {
            unchecked
            {
                int hash = 17;
                hash = 31 * hash + (type?.GetHashCode() ?? 0);
                for (int index = 0; index < fields.Length; index++)
                {
                    hash = 31 * hash + fields[index].GetHashCode();
                }
                if (returnNullIfFirstMissing) hash *= -27;
                return hash;
            }
        }


        public Type Type => type;
        public int GetOrdinal(int index) => fields[index].Ordinal;
        public string GetName(int index) => fields[index].Name;
        public Type GetFieldType(int index) => fields[index].Type;
        public int Count => fields.Length;
        public bool ReturnNullIfFirstMissing => returnNullIfFirstMissing;


        public bool Equals(FieldMap other)
        {
            return hashcode == other.hashcode &&
                type == other.type &&
                fields.SequenceEqual(other.fields, EqualityComparer<Field>.Default);
        }

        public override bool Equals(object obj) => obj is FieldMap && Equals((FieldMap)obj);

        public override int GetHashCode() => hashcode;

        // for debugger
        public override string ToString() => string.Join(", ", fields);


        private struct Field : IEquatable<Field>
        {
            public readonly int Ordinal;
            public readonly string Name;
            public readonly Type Type;

            public Field(int ordinal, string name, Type type)
            {
                Ordinal = ordinal;
                Name = name;
                Type = type;
            }

            public Field(int ordinal, int fieldNamePrefixLength, IDataReader reader)
            {
                Ordinal = ordinal;
                if (fieldNamePrefixLength > 0)
                {
                    Name = reader.GetName(ordinal)?.Substring(fieldNamePrefixLength);
                }
                else
                {
                    Name = reader.GetName(ordinal);
                }
                Type = reader.GetFieldType(ordinal);
            }

            public bool Equals(Field other)
            {
                return Ordinal == other.Ordinal &&
                    Type == other.Type &&
                    string.Equals(Name, other.Name, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is Field && Equals((Field)obj);
            }

            public override int GetHashCode()
            {
                int hash = 17;
                hash = 31 * hash + Ordinal;
                hash = 31 * hash + (Name?.GetHashCode() ?? 0);
                hash = 31 * hash + (Type?.GetHashCode() ?? 0);
                return hash;
            }

            public override string ToString() => $"[{Ordinal}] => {Name}: <{Type}>";
        }
    }
}
