﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace Com.MeraBills.StringResourceReaderWriter
{
    public sealed class StringArrayContent : ResourceContent, IEquatable<StringArrayContent>
    {
        public StringArrayContent(XmlReader reader) : base(reader)
        {
            if (reader.NodeType == XmlNodeType.EndElement)
                return;

            while(reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement)
                    break;

                if (reader.NodeType != XmlNodeType.Element)
                    continue;   // Ignore everything but elements

                if (string.CompareOrdinal(reader.Name, ItemElementName) != 0)
                    throw new ArgumentException("<item> expected");

                if (!reader.Read())
                    throw new ArgumentException("Reader ended unexpectedly");

                string value = StringContent.ReadStringValue(reader);
                if (this.Values == null)
                    this.Values = new List<string>();
                this.Values.Add(value);

                reader.ReadEndElement();
            }
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