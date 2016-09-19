using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Dapper
{
    internal struct QueryFieldMap : IEnumerable<FieldMap>
    {
        private readonly FieldMap[] maps;

        public QueryFieldMap(Type[] types, string[] prefixes, IDataReader reader)
        {
            if (reader == null || reader.FieldCount == 0)
                throw new InvalidOperationException("No columns were selected");

            if (types.Length != prefixes.Length)
                throw new ArgumentException("Number of prefixes does not match number of types");

            var fieldsToType = CreateFieldToTypeMap(prefixes, reader);
            maps = new FieldMap[types.Length];

            for (var typeIndex = 0; typeIndex < types.Length; typeIndex++)
            {
                var first = typeIndex == 0;
                var prefix = prefixes[typeIndex];
                maps[typeIndex] = new FieldMap(types[typeIndex], fieldsToType, typeIndex, prefix.Length, !first, reader);

                if (maps[typeIndex].Count == 0)
                {
                    if (prefix.Length == 0)
                    {
                        throw new ArgumentException($"No columns found for empty prefix", "prefixes");
                    }
                    else
                    {
                        throw new ArgumentException($"No columns found for prefix '{prefix}'", "prefixes");
                    }
                }
            }
        }

        private static int[] CreateFieldToTypeMap(string[] prefixes, IDataReader reader)
        {
            return CreateFieldToTypeMap(SortPrefixes(prefixes), reader);
        }

        private static int[] CreateFieldToTypeMap(FieldPrefix[] prefixes, IDataReader reader)
        {
            var fieldsToType = new int[reader.FieldCount];
            for (var index = 0; index < fieldsToType.Length; index++)
            {
                var fieldName = reader.GetName(index);
                var prefixIndex = Array.FindIndex(prefixes, prefix => fieldName.StartsWith(prefix.Prefix, StringComparison.OrdinalIgnoreCase));
                var typeIndex = prefixIndex < 0 ? -1 : prefixes[prefixIndex].TypeIndex;
                fieldsToType[index] = typeIndex;
            }
            return fieldsToType;
        }

        private static FieldPrefix[] SortPrefixes(string[] prefixes)
        {
            var result = new FieldPrefix[prefixes.Length];
            for (var index = 0; index < prefixes.Length; index++)
            {
                result[index] = new FieldPrefix(index, prefixes[index]);
            }
            Array.Sort(result);
            return result;
        }


        public FieldMap this[int index] => maps[index];
        public int Length => maps.Length;


        public IEnumerator<FieldMap> GetEnumerator() => ((IEnumerable<FieldMap>)maps).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        private struct FieldPrefix : IComparable<FieldPrefix>
        {
            public readonly int TypeIndex;
            public readonly string Prefix;

            public FieldPrefix(int index, string prefix)
            {
                TypeIndex = index;
                Prefix = prefix;
            }

            // sort backwards so that longer prefixes come first
            public int CompareTo(FieldPrefix other) => -string.Compare(Prefix, other.Prefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}
