using System;
using System.Collections.Generic;
using System.Xml;

namespace Com.MeraBills.StringResourceReaderWriter
{
    public sealed class StringResources
    {
        public StringResources(string language, bool isSourceLanguage)
        {
            this.Language = string.IsNullOrEmpty(language) ? throw new ArgumentNullException(nameof(language)) : language;
            this.IsSourceLanguage = isSourceLanguage;
            this.Strings = new Dictionary<string, StringResource>(StringComparer.Ordinal);
        }

        public void Read(string fileName, XmlReader reader)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            // Skip to the first element
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    reader.Skip();
                    continue;
                }

                if (string.CompareOrdinal(reader.LocalName, ResourcesElementName) != 0)
                    return; // Not a resources file
                else
                    break;
            }

            // This is a resources file - read the resources
            List<string> commentLines = null;
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Comment)
                {
                    if (commentLines == null)
                        commentLines = new List<string>();

                    commentLines.Add(reader.Value);
                    continue;
                }

                ResourceType resourceType = ResourceType.Other;
                if (reader.NodeType != XmlNodeType.Element)
                    resourceType = StringResource.GetResourceType(reader.LocalName);

                if (resourceType == ResourceType.Other)
                {
                    // We don't care about this XML - skip it
                    if (commentLines != null)
                        commentLines.Clear(); // We don't care about comments that are not before strings

                    reader.Skip();
                    continue;
                }

                // This is a string resource
                StringResource stringResource = new StringResource(resourceType, reader);
                stringResource.FileName = fileName;
                if ((commentLines != null) && (commentLines.Count > 0))
                {
                    if (this.IsSourceLanguage)
                    {
                        // If this is a string resource in the source language, save the comments
                        stringResource.CommentLines = new List<string>(commentLines);
                    }
                    else
                    {
                        // This is a string resource in a translated language
                        // See if we can get the source string for this string from the last comment
                        stringResource.TrySetSourceFromComment(commentLines[commentLines.Count - 1]);
                    }
                    commentLines.Clear();
                }

                this.Strings.Add(stringResource.Name, stringResource);
            }

            reader.ReadEndElement();
        }

        public const string ResourcesElementName = "resources";

        public readonly string Language;
        public readonly bool IsSourceLanguage;
        public readonly Dictionary<string, StringResource> Strings;
    }
}
