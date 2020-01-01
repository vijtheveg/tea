using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.IO;

namespace Com.MeraBills.StringResourceReaderWriter
{
    public static class ExcelWriter
    {
        public static void Write(StringResources sourceStrings, StringResources targetStrings, FileInfo outputFile)
        {
            using var package = new ExcelPackage();

            ExcelWorksheet worksheet = package.Workbook.Worksheets.Add(string.Format("{0:s} to {1:s}", sourceStrings.Language, targetStrings.Language));

            int row = 1;

            // Add headers
            worksheet.Cells[row, 1].Value = "Name";                   // The name / id of the string resource
            worksheet.Cells[row, 2].Value = "Index";                  // The index of the string in a string-array or the quantity in plurals
            worksheet.Cells[row, 3].Value = sourceStrings.Language;   // The string in the source language
            worksheet.Cells[row, 4].Value = targetStrings.Language;   // The string in the target language
            worksheet.Cells[row, 5].Value = "Final (Y/N)?";           // Is the translation final (i.e., checked and accepted by a human)?

            var rowStyle = worksheet.Row(row).Style;
            rowStyle.Border.BorderAround(ExcelBorderStyle.Thin);
            var font = rowStyle.Font;
            font.Bold = true;
            font.Size = 14;
            
            ++row;

            // Write the strings
            foreach(var sourceString in sourceStrings.Strings.Values)
            {
                if (!sourceString.IsTranslatable || !sourceString.IsTranslationRequired)
                    continue;

                // This is a string resource that requires translation
                // Find existing translation, if any
                StringResource targetString;
                if (targetStrings.Strings.TryGetValue(sourceString.Name, out targetString))
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
                    worksheet.Cells[row, 4].Value = ((StringContent)targetString.Content).Value;
                    worksheet.Row(row).Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    ++row;
                }
                else if (sourceString.ResourceType == ResourceType.StringArray)
                {
                    StringArrayContent sourceContent = (StringArrayContent)sourceString.Content;
                    StringArrayContent targetContent = targetString.Content as StringArrayContent;
                    int count = sourceContent.Values.Count;
                    for(int i = 0; i < count; ++i)
                    {
                        worksheet.Cells[row, 1].Value = sourceString.Name;
                        worksheet.Cells[row, 2].Value = i;
                        worksheet.Cells[row, 3].Value = sourceContent.Values[i];
                        if ((targetContent != null) && (i < targetContent.Values.Count))
                        {
                            worksheet.Cells[row, 4].Value = targetContent.Values[i];
                        }
                        ++row;
                    }
                    worksheet.Row(row).Style.Border.Top.Style = ExcelBorderStyle.Thin;
                }
                else
                {
                    PluralsContent sourceContent = (PluralsContent)sourceString.Content;
                    PluralsContent targetContent = targetString.Content as PluralsContent;
                    foreach (var pair in sourceContent.Values)
                    {
                        worksheet.Cells[row, 1].Value = sourceString.Name;
                        worksheet.Cells[row, 2].Value = pair.Key;
                        worksheet.Cells[row, 3].Value = pair.Value;
                        if (targetContent != null)
                        {
                            string value;
                            if (targetContent.Values.TryGetValue(pair.Key, out value))
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

        private static readonly Color LockedCellBackgroundColor = Color.FloralWhite;
    }
}