using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace Com.MeraBills.StringResourceReaderWriter
{
    public static class ExcelReaderWriter
    {
        public static void Write(StringResources sourceStrings, StringResources targetStrings, FileInfo outputFile)
        {
            using var package = new ExcelPackage();

            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(string.Format("{0:s} to {1:s}", sourceStrings.Language, targetStrings.Language));

            int row = 1;

            // Add headers
            worksheet.Cells[row, 1].Value = NameHeader;             // The name / id of the string resource
            worksheet.Cells[row, 2].Value = IndexHeader;            // The index of the string in a string-array or the quantity in plurals
            worksheet.Cells[row, 3].Value = sourceStrings.Language; // The string in the source language
            worksheet.Cells[row, 4].Value = targetStrings.Language; // The string in the target language
            worksheet.Cells[row, 5].Value = FinalHeader;            // Is the translation final (i.e., checked and accepted by a human)?

            ++row;

            // Write the strings
            foreach(var sourceString in sourceStrings.Strings.Values)
            {
                if (!sourceString.IsTranslatable || !sourceString.HasNonEmptyContent)
                    continue;

                // This is a string resource that requires translation
                // Find existing translation, if any
                if (targetStrings.Strings.TryGetValue(sourceString.Name, out StringResource targetString))
                {
                    // If the translation is final and the source has not changed since the target was finalized,
                    // then we don't need to translate this string again
                    if (sourceString.Equals(targetString.Source))
                        continue;
                }

                // Translation is required - write the source and target content
                if (sourceString.ResourceType == ResourceType.String)
                {
                    worksheet.Cells[row, 1].Value = sourceString.Name;
                    worksheet.Cells[row, 3].Value = ((StringContent)sourceString.Content).Value;
                    if (targetString != null)
                        worksheet.Cells[row, 4].Value = ((StringContent)targetString.Content).Value;
                    worksheet.Row(row).Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    ++row;
                }
                else if (sourceString.ResourceType == ResourceType.StringArray)
                {
                    var sourceContent = (StringArrayContent)sourceString.Content;
                    int count = sourceContent.Values.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        worksheet.Cells[row, 1].Value = sourceString.Name;
                        worksheet.Cells[row, 2].Value = i;
                        worksheet.Cells[row, 3].Value = sourceContent.Values[i];
                        if ((targetString != null) && (targetString.Content is StringArrayContent targetContent) && (i < targetContent.Values.Count))
                        {
                            worksheet.Cells[row, 4].Value = targetContent.Values[i];
                        }
                        ++row;
                    }
                    worksheet.Row(row).Style.Border.Top.Style = ExcelBorderStyle.Thin;
                }
                else
                {
                    var sourceContent = (PluralsContent)sourceString.Content;
                    foreach (var pair in sourceContent.Values)
                    {
                        worksheet.Cells[row, 1].Value = sourceString.Name;
                        worksheet.Cells[row, 2].Value = pair.Key;
                        worksheet.Cells[row, 3].Value = pair.Value;
                        if ((targetString != null) && (targetString.Content is PluralsContent targetContent))
                        {
                            if (targetContent.Values.TryGetValue(pair.Key, out string value))
                                worksheet.Cells[row, 4].Value = value;
                        }
                        ++row;
                    }
                    worksheet.Row(row).Style.Border.Top.Style = ExcelBorderStyle.Thin;
                }
            }

            // Set column styles
            worksheet.Column(2).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Column(5).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            FormatColumn(worksheet.Column(1), 60, locked: true);
            FormatColumn(worksheet.Column(2), 10, locked: true);
            FormatColumn(worksheet.Column(3), 75, locked: true, wrapText: true);
            FormatColumn(worksheet.Column(4), 75, locked: false, wrapText: true);
            FormatColumn(worksheet.Column(5), 20, locked: false);

            // Set header style (do this after setting column styles, otherwise this will get overwritten by the column styles)
            var rowStyle = worksheet.Row(1).Style;
            rowStyle.Locked = true;
            rowStyle.Fill.PatternType = ExcelFillStyle.Solid;
            rowStyle.Fill.BackgroundColor.SetColor(LockedCellBackgroundColor);
            rowStyle.Border.BorderAround(ExcelBorderStyle.Thin);
            var font = rowStyle.Font;
            font.Bold = true;
            font.Size = 14;

            // Protect worksheet to prevent unwanted editing
            var protection = worksheet.Protection;
            protection.AllowDeleteRows = false;
            protection.AllowDeleteColumns = false;
            protection.AllowInsertRows = false;
            protection.AllowInsertColumns = false;
            protection.AllowAutoFilter = true;
            protection.AllowSort = true;
            protection.IsProtected = true;

            package.SaveAs(outputFile);
        }

        public static StringResources Read(string sourceLanguage, string targetLanguage, FileInfo inputFile)
        {
            using var package = new ExcelPackage(inputFile);
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

            int row = 1;
            // Read the header, to make sure this file contains translated strings
            if ((string.CompareOrdinal(NameHeader, worksheet.Cells[row, 1].Value as string) != 0) ||
                (string.CompareOrdinal(IndexHeader, worksheet.Cells[row, 2].Value as string) != 0) ||
                (string.CompareOrdinal(sourceLanguage, worksheet.Cells[row, 3].Value as string) != 0) ||
                (string.CompareOrdinal(targetLanguage, worksheet.Cells[row, 4].Value as string) != 0) ||
                (string.CompareOrdinal(FinalHeader, worksheet.Cells[row, 5].Value as string) != 0))
            {
                // This file does not contain translations
                throw new ArgumentException(nameof(inputFile));
            }
            ++row;  // This file contains translations

            StringResources targetStrings = new StringResources(targetLanguage, isSourceLanguage: false);
            var name = worksheet.Cells[row, 1].Value as string;
            while(!string.IsNullOrEmpty(name))
            {
                ResourceType resourceType = ResourceType.String;
                string indexString = null;
                ushort index = 0;
                {
                    var indexObject = worksheet.Cells[row, 2].Value;
                    if (indexObject != null)
                    {
                        indexString = indexObject as string;
                        if (indexString != null)
                        {
                            indexString = indexString.Trim();
                            if (indexString.Length > 0)
                                resourceType = ResourceType.Plurals;
                        }
                        else
                        {
                            index = (ushort)((double)indexObject);
                            resourceType = ResourceType.StringArray;
                        }
                    }
                }
                var sourceString = worksheet.Cells[row, 3].Value as string;
                var targetString = worksheet.Cells[row, 4].Value as string;
                bool final = worksheet.Cells[row, 5].Value is string finalString ? finalString.StartsWith("y", ignoreCase: true, CultureInfo.InvariantCulture) : false;

                StringResource stringResource;
                if (resourceType == ResourceType.String)
                {
                    if (targetStrings.Strings.TryGetValue(name, out stringResource))
                        throw new InvalidDataException(string.Format("A string resource with name {0:s} appears more than once", name));

                    stringResource = new StringResource(ResourceType.String)
                    {
                        Name = name
                    };
                    ((StringContent)stringResource.Content).Value = targetString;
                    if (final)
                    {
                        var source = new StringResource(ResourceType.String)
                        {
                            Name = name
                        };
                        ((StringContent)source.Content).Value = sourceString;
                        stringResource.Source = source;
                    }

                    targetStrings.Strings.Add(name, stringResource);
                }
                else
                {
                    if (resourceType == ResourceType.StringArray)
                    {
                        // This is a string array resource
                        bool indexOrderError = false;
                        if (targetStrings.Strings.TryGetValue(name, out stringResource))
                        {
                            // Not the first item in the string array
                            var content = (StringArrayContent)stringResource.Content;
                            if (content.Values.Count == index)
                            {
                                content.Values.Add(targetString);
                                if (stringResource.Source != null)
                                {
                                    // Previous items were all marked final
                                    if (final)
                                        ((StringArrayContent)stringResource.Source.Content).Values.Add(sourceString);
                                    else
                                        stringResource.Source = null; // This is not final, so the array is not final
                                }
                                // else: previous was not final, so the array is not final
                            }
                            else
                                indexOrderError = true;
                        }
                        else
                        {
                            // This is the first of the items in the string array
                            if (index == 0)
                            {
                                stringResource = new StringResource(ResourceType.StringArray)
                                {
                                    Name = name
                                };
                                ((StringArrayContent)stringResource.Content).Values.Add(targetString);

                                if (final)
                                {
                                    var source = new StringResource(ResourceType.StringArray)
                                    {
                                        Name = name
                                    };
                                    ((StringArrayContent)source.Content).Values.Add(sourceString);
                                    stringResource.Source = source;
                                }

                                targetStrings.Strings.Add(name, stringResource);
                            }
                            else
                                indexOrderError = true; // The first line of the string array resourcer is not at index 0
                        }

                        if (indexOrderError)
                            throw new InvalidDataException(string.Format("The items in the string-array resource {0:s} are not arranged in increasing order of index", name));
                    }
                    else
                    {
                        // This is a plurals string resource
                        if (targetStrings.Strings.TryGetValue(name, out stringResource))
                        {
                            // Not the first item in the plurals array
                            var content = (PluralsContent)stringResource.Content;
                            if (content.Values.ContainsKey(indexString))
                                throw new InvalidDataException(string.Format("An item {0:s} of the plurals resource {1:s} has been duplicated", indexString, name));
                            else
                            {
                                content.Values.Add(indexString, targetString);
                                if (stringResource.Source != null)
                                {
                                    // Previous items were all marked final
                                    if (final)
                                        ((PluralsContent)stringResource.Source.Content).Values.Add(indexString, sourceString);
                                    else
                                        stringResource.Source = null; // This is not final, so the array is not final
                                }
                                // else: previous was not final, so the array is not final
                            }
                        }
                        else
                        {
                            // This is the first item we have seen in this plurals array
                            stringResource = new StringResource(ResourceType.Plurals)
                            {
                                Name = name
                            };
                            ((PluralsContent)stringResource.Content).Values.Add(indexString, targetString);

                            if (final)
                            {
                                var source = new StringResource(ResourceType.Plurals)
                                {
                                    Name = name
                                };
                                ((PluralsContent)source.Content).Values.Add(indexString, sourceString);
                                stringResource.Source = source;
                            }

                            targetStrings.Strings.Add(name, stringResource);
                        }
                    }
                }

                ++row;
                name = worksheet.Cells[row, 1].Value as string;
            }

            return targetStrings;
        }

        private static void FormatColumn(ExcelColumn column, int width, bool locked, bool wrapText = false)
        {
            column.Width = width;

            var columnStyle = column.Style;
            columnStyle.Border.BorderAround(ExcelBorderStyle.Medium);
            columnStyle.Locked = locked;
            if (locked)
            {
                columnStyle.Fill.PatternType = ExcelFillStyle.Solid;
                columnStyle.Fill.BackgroundColor.SetColor(LockedCellBackgroundColor);
            }
            columnStyle.WrapText = wrapText;
        }

        private const string NameHeader = "Name";
        private const string IndexHeader = "Index";
        private const string FinalHeader = "Final (Y/N)?";
        private static readonly Color LockedCellBackgroundColor = Color.FloralWhite;
    }
}