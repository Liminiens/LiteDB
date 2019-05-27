using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace LiteDB
{
    internal class JsonBsonValueWriter
    {
        private Utf8Json.JsonWriter _writer;

        public bool Pretty { get; set; }
        public bool WriteBinary { get; set; }

        public JsonBsonValueWriter(ref Utf8Json.JsonWriter writer)
        {
            _writer = writer;
        }

        public void Serialize(BsonValue value)
        {
            this.WriteValue(value ?? BsonValue.Null);
        }

        private void WriteValue(BsonValue value)
        {
            // use direct cast to better performance
            switch (value.Type)
            {
                case BsonType.Null:
                    _writer.WriteNull();
                    break;

                case BsonType.Array:
                    this.WriteArray(new BsonArray((List<BsonValue>)value.RawValue));
                    break;

                case BsonType.Document:
                    this.WriteObject(new BsonDocument((Dictionary<string, BsonValue>)value.RawValue));
                    break;

                case BsonType.Boolean:
                    _writer.WriteBoolean((bool)value.RawValue);
                    break;

                case BsonType.String:
                    this.WriteString((string)value.RawValue);
                    break;

                case BsonType.Int32:
                    _writer.WriteInt32((Int32)value.RawValue);
                    break;

                case BsonType.Double:
                    _writer.WriteDouble(Convert.ToDouble(value.RawValue, NumberFormatInfo.InvariantInfo));
                    break;

                case BsonType.Binary:
                    var bytes = (byte[])value.RawValue;
                    this.WriteExtendDataType("$binary", this.WriteBinary ? Convert.ToBase64String(bytes, 0, bytes.Length) : "-- " + bytes.Length + " bytes --");
                    break;

                case BsonType.ObjectId:
                    this.WriteExtendDataType("$oid", ((ObjectId)value.RawValue).ToString());
                    break;

                case BsonType.Guid:
                    this.WriteExtendDataType("$guid", ((Guid)value.RawValue).ToString());
                    break;

                case BsonType.DateTime:
                    this.WriteExtendDataType("$date", ((DateTime)value.RawValue).ToUniversalTime().ToString("o"));
                    break;

                case BsonType.Int64:
                    this.WriteExtendDataType("$numberLong", ((Int64)value.RawValue).ToString());
                    break;

                case BsonType.Decimal:
                    this.WriteExtendDataType("$numberDecimal", ((Decimal)value.RawValue).ToString());
                    break;

                case BsonType.MinValue:
                    this.WriteExtendDataType("$minValue", "1");
                    break;

                case BsonType.MaxValue:
                    this.WriteExtendDataType("$maxValue", "1");
                    break;
            }
        }

        private void WriteObject(BsonDocument obj)
        {
            _writer.WriteBeginObject();
            foreach (var key in obj.Keys)
            {
                this.WriteKeyValue(key, obj[key], index++ < length - 1);
            }
            _writer.WriteEndObject();
        }

        private void WriteArray(BsonArray arr)
        {
            var hasData = arr.Count > 0;

            _writer.WriteBeginArray();
            this.WriteStartBlock("[", hasData);

            for (var i = 0; i < arr.Count; i++)
            {
                var item = arr[i];

                // do not do this tests if is not pretty format - to better performance
                if (this.Pretty)
                {
                    if (!((item.IsDocument && item.AsDocument.Keys.Any()) || (item.IsArray && item.AsArray.Count > 0)))
                    {
                        _writer.WriteBeginArray();
                        this.WriteIndent();
                    }
                }

                this.WriteValue(item ?? BsonValue.Null);

                if (i < arr.Count - 1)
                {
                    _writer.WriteValueSeparator();
                }
                this.WriteNewLine();
            }

            _writer.WriteEndArray();
        }

        private void WriteString(string s)
        {
            _writer.WriteQuotation();
            int l = s.Length;
            for (var index = 0; index < l; index++)
            {
                var c = s[index];
                switch (c)
                {
                    case '\"':
                        _writer.Write("\\\"");
                        break;

                    case '\\':
                        _writer.Write("\\\\");
                        break;

                    case '\b':
                        _writer.Write("\\b");
                        break;

                    case '\f':
                        _writer.Write("\\f");
                        break;

                    case '\n':
                        _writer.Write("\\n");
                        break;

                    case '\r':
                        _writer.Write("\\r");
                        break;

                    case '\t':
                        _writer.Write("\\t");
                        break;

                    default:
                        int i = (int)c;
                        if (i < 32 || i > 127)
                        {
                            _writer.Write("\\u");
                            _writer.Write(i.ToString("x04"));
                        }
                        else
                        {
                            _writer.Write(c);
                        }
                        break;
                }
            }
            _writer.Write('\"');
        }

        private void WriteExtendDataType(string type, string value)
        {
            // format: { "$type": "string-value" }
            // no string.Format to better performance
            _writer.WriteBeginObject();
            _writer.WritePropertyName(type);
            _writer.WriteNameSeparator();
            _writer.WriteString(value);
            _writer.WriteEndObject();
        }

        private void WriteKeyValue(string key, BsonValue value)
        {
            this.WriteIndent();

            _writer.Write('\"');
            _writer.Write(key);
            _writer.Write("\":");

            // do not do this tests if is not pretty format - to better performance
            if (this.Pretty)
            {
                _writer.Write(' ');

                if ((value.IsDocument && value.AsDocument.Keys.Any()) || (value.IsArray && value.AsArray.Count > 0))
                {
                    this.WriteNewLine();
                }
            }

            this.WriteValue(value ?? BsonValue.Null);

            if (comma)
            {
                _writer.Write(',');
            }

            this.WriteNewLine();
        }
    }
}