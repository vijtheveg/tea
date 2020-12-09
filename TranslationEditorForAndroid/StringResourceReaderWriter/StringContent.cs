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

        public override ResourceContent CreateTargetContent(ResourceContent oldSourceContent, ResourceContent oldTargetContent)
        {
            return oldTargetContent; // This is the best we can do
        }

        public static string ReadStringValue(XmlReader reader)
        {
            if (reader.IsEmptyElement)
            {
                // Read past the empty element
                reader.Skip();

                return null;
            }

            return reader.ReadInnerXml();
        }

        public static void WriteStringValue(XmlWriter writer, string value)
        {
            if (!string.IsNullOrEmpty(value))
                writer.WriteValue(value);
        }

        public override bool HasTranslatableContent
        {
            get => ValueNeedsTranslation(this.Value);
        }

        public static bool ValueNeedsTranslation(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            value = value.Trim();
            if (string.IsNullOrEmpty(value))
                return false;

            // If the value starts with '@string/', the value just refers to another string
            // It therefore doesn't need translation
            if (value.StartsWith("@string/", StringComparison.Ordinal))
                return false;

            return true;
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
