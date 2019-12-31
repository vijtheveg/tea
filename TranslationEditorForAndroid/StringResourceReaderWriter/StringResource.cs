﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;

namespace Com.MeraBills.StringResourceReaderWriter
{
    public sealed class StringResource : IEquatable<StringResource>
    {
        public StringResource(ResourceType resouceType, XmlReader reader)
        {
            if (resouceType == ResourceType.Other)
                throw new ArgumentException("A string resource type must be specified");
            this.ResourceType = resouceType;

            if (reader.NodeType != XmlNodeType.Element)
                throw new ArgumentException("Reader is not positioned on an element");

            string attributeValue = reader.GetAttribute(NameAttributeName);
            this.Name = string.IsNullOrEmpty(attributeValue) ? throw new ArgumentException("The name attribute is required") : attributeValue;

            attributeValue = reader.GetAttribute(TranslatableAttributeName);
            this.IsTranslatable = attributeValue == null ? IsTranslatableDefault : (string.CompareOrdinal(attributeValue, FalseValue) != 0);

            attributeValue = reader.GetAttribute(FormattedAttributeName);
            this.HasFormatSpecifiers = attributeValue == null ? HasFormatSpecifiersDefault : (string.CompareOrdinal(attributeValue, FalseValue) != 0);

            if (!reader.Read())
                throw new ArgumentException("Reader ended unexpectedly");

            if (resouceType == ResourceType.String)
                this.Content = new StringContent(reader);
            else if (this.ResourceType == ResourceType.StringArray)
                this.Content = new StringArrayContent(reader);
            else
                this.Content = new PluralsContent(reader);

            reader.ReadEndElement();
        }

        public static ResourceType GetResourceType(string elementName)
        {
            if (string.CompareOrdinal(elementName, StringElementName) == 0)
                return ResourceType.String;

            if (string.CompareOrdinal(elementName, StringArrayElementName) == 0)
                return ResourceType.StringArray;

            if (string.CompareOrdinal(elementName, PluralsElementName) == 0)
                return ResourceType.Plurals;

            return ResourceType.Other;
        }

        public void Write(XmlWriter writer)
        {
            string startElementName;
            if (this.ResourceType == ResourceType.String)
                startElementName = StringElementName;
            else if (this.ResourceType == ResourceType.StringArray)
                startElementName = StringArrayElementName;
            else
                startElementName = PluralsElementName;
            writer.WriteStartElement(startElementName);

            writer.WriteAttributeString(NameAttributeName, this.Name);

            if (this.IsTranslatable != IsTranslatableDefault)
                writer.WriteAttributeString(TranslatableAttributeName, this.IsTranslatable ? TrueValue : FalseValue);

            if (this.HasFormatSpecifiers != HasFormatSpecifiersDefault)
                writer.WriteAttributeString(FormattedAttributeName, this.HasFormatSpecifiers ? TrueValue : FalseValue);

            this.Content.Write(writer);

            writer.WriteEndElement();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StringResource);
        }

        public bool Equals([AllowNull] StringResource other)
        {
            return other != null &&
                   ResourceType == other.ResourceType &&
                   string.CompareOrdinal(Name, other.Name) == 0 &&
                   EqualityComparer<ResourceContent>.Default.Equals(Content, other.Content);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ResourceType, Name, Content);
        }

        public override string ToString()
        {
            var writerSettings = new XmlWriterSettings();
            writerSettings.Async = false;
            writerSettings.Indent = false;
            writerSettings.NewLineChars = "";
            writerSettings.OmitXmlDeclaration = true;

            var result = new StringBuilder();
            using var writer = XmlWriter.Create(result, writerSettings);
            this.Write(writer);
            return result.ToString();
        }

        public readonly ResourceType ResourceType;

        public readonly string Name;

        public readonly bool HasFormatSpecifiers;

        public readonly bool IsTranslatable;

        public ResourceContent Content { get; set; }

        public String FileName { get; set; }

        public List<String> CommentLines { get; set; }

        public const string StringElementName = "string";
        public const string StringArrayElementName = "string-array";
        public const string PluralsElementName = "plurals";
        public const string NameAttributeName = "name";
        public const string TranslatableAttributeName = "translatable";
        public const string FormattedAttributeName = "formatted";
        public const string TrueValue = "true";
        public const string FalseValue = "false";

        public const bool IsTranslatableDefault = true;
        public const bool HasFormatSpecifiersDefault = true;
    }
}