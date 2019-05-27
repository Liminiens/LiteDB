using System;
using System.Globalization;
using Utf8Json;

namespace LiteDB
{
    public class BsonValueFormatter : IJsonFormatter<BsonValue>
    {
        /// <inheritdoc />
        public void Serialize(ref Utf8Json.JsonWriter writer, BsonValue value, IJsonFormatterResolver formatterResolver)
        {
            writer.
        }

        /// <inheritdoc />
        public BsonValue Deserialize(ref Utf8Json.JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            switch (reader.GetCurrentJsonToken())
            {
                case Utf8Json.JsonToken.String:
                    var str = reader.ReadString();
                    switch (str)
                    {
                        case "null":
                            return BsonValue.Null;
                        case "true":
                            return true;
                        case "false":
                            return false;
                        default:
                            throw LiteException.UnexpectedToken(str);
                    }

                case Utf8Json.JsonToken.BeginObject:
                    return this.ReadObject(ref reader);
                case Utf8Json.JsonToken.BeginArray:
                    return this.ReadArray(ref reader);
                case Utf8Json.JsonToken.Number:
                    return reader.ReadString().Contains(".")
                        ? new BsonValue(reader.ReadDouble())
                        : new BsonValue(reader.ReadInt32());
            }

            throw LiteException.UnexpectedToken("");
        }
        
        private BsonValue ReadObject(ref Utf8Json.JsonReader reader)
        {
            var obj = new BsonDocument();
            reader.ReadNext();
            var token = reader.GetCurrentJsonToken(); // read "<key>"

            while (token != Utf8Json.JsonToken.None)
            {
                var key = reader.ReadString();

                reader.ReadNext();
                token = reader.GetCurrentJsonToken(); // read ":"

                reader.ReadNext();
                token = reader.GetCurrentJsonToken(); // read "<value>"

                // check if not a special data type - only if is first attribute
                if (key[0] == '$' && obj.Count == 0)
                {
                    var val = this.ReadExtendedDataType(key, ref reader);

                    // if val is null then it's not a extended data type - it's just a object with $ attribute
                    if (!val.IsNull) return val;
                }

                obj[key] = this.Deserialize(ref reader, null); // read "," or "}"

                reader.ReadNext();
                token = reader.GetCurrentJsonToken();

                if (token == Utf8Json.JsonToken.NameSeparator || token == Utf8Json.JsonToken.ValueSeparator)
                {
                    token = reader.GetCurrentJsonToken(); // read "<key>"
                }
            }

            return obj;
        }

        private BsonArray ReadArray(ref Utf8Json.JsonReader reader)
        {
            var arr = new BsonArray();

            var token = reader.GetCurrentJsonToken();

            while (token != Utf8Json.JsonToken.EndArray)
            {
                var value = this.Deserialize(ref reader, null);

                arr.Add(value);

                token = reader.GetCurrentJsonToken();

                if (token == Utf8Json.JsonToken.None)
                {
                    reader.ReadNext();
                    token = reader.GetCurrentJsonToken();
                }
            }

            return arr;
        }

        private BsonValue ReadExtendedDataType(string key, ref Utf8Json.JsonReader reader)
        {
            switch (key)
            {
                case "$binary":
                    return new BsonValue(Convert.FromBase64String(reader.ReadString()));
                case "$oid":
                    return new BsonValue(new ObjectId(reader.ReadString()));
                case "$guid":
                    return new BsonValue(new Guid(reader.ReadString()));
                case "$date":
                    return new BsonValue(DateTime.Parse(reader.ReadString()).ToLocalTime());
                case "$numberLong":
                    return new BsonValue(reader.ReadInt64());
                case "$numberDecimal":
                    return new BsonValue(Convert.ToDecimal(reader.ReadString()));
                case "$minValue":
                    return BsonValue.MinValue;
                case "$maxValue":
                    return BsonValue.MaxValue;
                default:
                    return BsonValue.Null; // is not a special data type
            }
        }
    }
}