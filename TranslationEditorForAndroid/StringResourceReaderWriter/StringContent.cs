using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace Com.MeraBills.StringResourceReaderWriter
{
    public sealed class StringContent : ResourceContent, IEquatable<StringContent>
    {
        public StringContent() : base()
        {
        }

        public StringContent(XmlReader reader) : this()
        {
            Read(reader);
        }

        public override void Read(XmlReader reader)
        {
            this.Value = ReadStringValue(reader);
        }

        public override void Write(XmlWriter writer)
        {
            WriteStringValue(writer, this.Value);
        }

        public static string ReadStringValue(XmlReader reader)
        {
            if (reader.IsEmptyElement)
            {
                // Read past the empty element
                reader.Skip();

                return null;
            }

            // Read past the start element
            reader.Read();

            string result = null;
            if (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Text)
                    throw new ArgumentException("Text expected");

                result = reader.Value;

                // Read past the text node
                reader.Read();
            }

            // Read past the end element
            reader.ReadEndElement();

            return result;
        }

        public static void WriteStringValue(XmlWriter writer, string value)
        {
            if (!string.IsNullOrEmpty(value))
                writer.WriteValue(value);
        }

        public override bool HasNonEmptyContent
        {
            get => IsValueNonEmpty(this.Value);
        }

        public static bool IsValueNonEmpty(string value)
        {
            return !string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(value.Trim());
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

        public string Value { get; set; }
    }
}
