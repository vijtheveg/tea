using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace Com.MeraBills.StringResourceReaderWriter
{
    public sealed class PluralsContent : ResourceContent, IEquatable<PluralsContent>
    {
        public PluralsContent(XmlReader reader) : base(reader)
        {
            if (reader.NodeType == XmlNodeType.EndElement)
                return;

            while(reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                    break;

                if (reader.NodeType != XmlNodeType.Element)
                    continue;   // Ignore everything but elements

                if (string.CompareOrdinal(reader.LocalName, ItemElementName) != 0)
                    throw new ArgumentException("<item> expected");

                string quantity = reader.GetAttribute(QuantityAttributeName);
                if (string.IsNullOrEmpty(quantity))
                    throw new ArgumentException("quantity attriute is required");

                if (!reader.Read())
                    throw new ArgumentException("Reader ended unexpectedly");

                string value = StringContent.ReadStringValue(reader);
                if (this.Values == null)
                    this.Values = new Dictionary<string, string>(StringComparer.Ordinal);
                this.Values.Add(quantity, value);

                reader.ReadEndElement();
            }
        }

        public override void Write(XmlWriter writer)
        {
            if ((this.Values == null) || (this.Values.Count <= 0))
                return;

            foreach(var pair in this.Values)
            {
                writer.WriteStartElement(ItemElementName);
                writer.WriteAttributeString(QuantityAttributeName, pair.Key);
                StringContent.WriteStringValue(writer, pair.Value);
                writer.WriteEndElement();
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PluralsContent);
        }

        public bool Equals([AllowNull] PluralsContent other)
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

            foreach (var pair in this.Values)
            {
                string otherValue = other.Values[pair.Key];
                if (string.CompareOrdinal(pair.Value, otherValue) != 0)
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();

            if (this.Values != null)
                foreach (var pair in this.Values)
                {
                    hashCode.Add(pair.Key);
                    hashCode.Add(pair.Value);
                }

            return hashCode.ToHashCode();
        }

        public const string ItemElementName = "item";
        public const string QuantityAttributeName = "quantity";

        public readonly Dictionary<string, string> Values;
    }
}
