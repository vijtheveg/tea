using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace Com.MeraBills.StringResourceReaderWriter
{
    public sealed class StringArrayContent : ResourceContent, IEquatable<StringArrayContent>
    {
        public StringArrayContent(XmlReader reader) : base(reader)
        {
            this.Values = new List<string>();

            if (reader.IsEmptyElement)
            {
                // Read past the empty element
                reader.Skip();
                return;
            }

            // Read past the start element
            if (!reader.Read())
                throw new ArgumentException("Reader ended unexpectedly");

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    reader.Skip();
                    continue;   // Ignore everything but elements
                }

                if (string.CompareOrdinal(reader.LocalName, ItemElementName) != 0)
                    throw new ArgumentException("<item> expected");

                string value = StringContent.ReadStringValue(reader);
                this.Values.Add(value);
            }

            // Read past the end element
            reader.ReadEndElement();
        }

        public override void Write(XmlWriter writer)
        {
            if ((this.Values == null) || (this.Values.Count <= 0))
                return;

            foreach(string value in this.Values)
            {
                writer.WriteStartElement(ItemElementName);
                StringContent.WriteStringValue(writer, value);
                writer.WriteEndElement();
            }
        }

        public override bool IsTranslationRequired
        {
            get
            {
                if ((this.Values == null) || (this.Values.Count <= 0))
                    return false;

                foreach (string value in this.Values)
                    if (StringContent.HasNonEmptyContent(value))
                        return true;

                return false;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StringArrayContent);
        }

        public bool Equals([AllowNull] StringArrayContent other)
        {
            if (other == null)
                return false;

            if ((this.Values == null) != (other.Values == null))
                return false;

            if (this.Values == null)
                return true;

            int count = this.Values.Count;
            if (count != other.Values.Count)
                return false;

            for (int i = 0; i < count; ++i)
                if (string.CompareOrdinal(this.Values[i], other.Values[i]) != 0)
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            if (this.Values != null)
                foreach (string value in this.Values)
                    hashCode.Add(value);

            return hashCode.ToHashCode();
        }

        public const string ItemElementName = "item";

        public readonly List<string> Values;
    }
}
