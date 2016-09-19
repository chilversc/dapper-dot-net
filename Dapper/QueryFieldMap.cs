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
            var fieldsToType = new int[reader.FieldCount];
            for (var index = 0; index < fieldsToType.Length; index++)
            {
                var fieldName = reader.GetName(index);
                var typeIndex = Array.FindIndex(prefixes, prefix => fieldName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                fieldsToType[index] = typeIndex;
            }
            return fieldsToType;
        }


        public FieldMap this[int index] => maps[index];
        public int Length => maps.Length;


        public IEnumerator<FieldMap> GetEnumerator() => ((IEnumerable<FieldMap>)maps).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
