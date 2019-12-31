using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace Com.MeraBills.StringResourceReaderWriter
{
    public sealed class StringContent : ResourceContent, IEquatable<StringContent>
    {
        public StringContent(XmlReader reader) : base(reader)
        {
            this.Value = ReadStringValue(reader);
        }

        public override void Write(XmlWriter writer)
        {
            WriteStringValue(writer, this.Value);
        }

        public static string ReadStringValue(XmlReader reader)
        {
            if (reader.NodeType == XmlNodeType.EndElement)
                return null;

            if (reader.NodeType != XmlNodeType.Text)
                throw new ArgumentException("Text expected");

            string result = reader.Value;
            reader.Read();

            return result;
        }

        public static void WriteStringValue(XmlWriter writer, string value)
        {
            if (!string.IsNullOrEmpty(value))
                writer.WriteValue(value);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StringContent);
        }

        public bool Equals([AllowNull] StringContent other)
        {
            return other != null &&
                   string.CompareOrdinal(Value, other.Value) == 0;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }

        public readonly string Value;
    }
}
