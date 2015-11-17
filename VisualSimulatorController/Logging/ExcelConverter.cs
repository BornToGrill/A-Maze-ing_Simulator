﻿using System;
using System.Drawing;
using System.IO;
using System.Threading;
using OfficeOpenXml;

namespace VisualSimulatorController.Logging {
    internal static class ExcelConverter {


        
        internal static void Convert(string CsvPath, string ExcelPath, int FileLength, ManualResetEvent DoneEvent) {
            Console.Title = "A-Maze-ing simulator - Converting";
            //try {
            string SheetName = "Simulator Results";

            var format = new ExcelTextFormat();

            format.Delimiter = ';';
            format.EOL = "\r";

            using (ExcelPackage pack = new ExcelPackage(new FileInfo(ExcelPath))) {
                using (ExcelWorksheet Sheet = pack.Workbook.Worksheets.Add(SheetName)) {
                    // Setting Column widths
                    Sheet.Column(1).Width = GetTrueColumnWidth(6.4);
                    Sheet.Column(2).Width = GetTrueColumnWidth(7.2);
                    Sheet.Column(3).Width = GetTrueColumnWidth(12);
                    Sheet.Column(4).Width = GetTrueColumnWidth(13);
                    Sheet.Column(5).Width = GetTrueColumnWidth(12);
                    Sheet.Column(6).Width = GetTrueColumnWidth(10);
                    Sheet.Column(7).Width = GetTrueColumnWidth(16.5);

                    // Merge cells [IMPORTANT] - Cells have to be merged before import or the file will be "corrupted / damage".
                    Merge(new[] { 7 }, new[] { 1, 2, 5, 8, 11, 18 }, Sheet);
                    Merge(new[] { 2, 2, 2 }, new[] { 3, 4, 6, 7 }, Sheet);
                    Merge(new[] { 2 }, new[] { 9, 10 }, Sheet);
                    Merge(new[] { 1, 2, 2, 2 }, new[] { 12, 13, 14, 15, 16 }, Sheet);

                    using (StreamReader reader = new StreamReader(CsvPath)) {
                        int Row = 1;
                        string Line;
                        while ((Line = reader.ReadLine()) != null) {
                            Sheet.Cells[Row, 1].LoadFromText(Line, format);
                            float perc = ((float)Row / FileLength) * 100;
                            string Title = string.Format("A-Maze-ing simulator - Converting - {0:#}%", perc);
                            if (Console.Title != Title)
                                Console.Title = Title;
                            Row++;
                        }
                    }
                    Sheet.DeleteRow(1);
                    CreateExcelTemplate(Sheet);
                    pack.Save();
                }
            }
            File.SetAttributes(CsvPath, FileAttributes.Normal);
            //}
            //catch {
            //    throw;
            //}
            //finally {
            //    File.SetAttributes(CsvPath, FileAttributes.Normal); // UNSAFE , not sure if this will cause an exception
            Console.Title = "A-Maze-ing simulator";
            DoneEvent.Set();
            HandleInput.PrintColor("Excel converter finished styling the CSV Sheet.", ConsoleColor.Green);
            //    // Try to unhide file.
            //}
        }

        private static void CreateExcelTemplate(ExcelWorksheet Sheet) {

            // Set background & Fonts
            int[] GlobalHeaders = { 1, 18 };
            int[] HeavyHeader = { 2, 5, 8, 11 };
            int[] Header = { 3, 6, 9, 12, 19 };
            int[] Constants = { 4, 10, 13, 14, 15, 16 };


            for (int i = 0; i < GlobalHeaders.Length; i++)
                StyleRow(Sheet, GlobalHeaders[i], 128, 128, 128, true);
            for (int i = 0; i < HeavyHeader.Length; i++)
                StyleRow(Sheet, HeavyHeader[i], 166, 166, 166, true);
            for (int i = 0; i < Header.Length; i++)
                if (i < Header.Length - 1)
                    StyleRow(Sheet, Header[i], 217, 217, 217, true);
                else
                    StyleRow(Sheet, Header[i], 217, 217, 217, false);
            for (int i = 0; i < Constants.Length; i++)
                StyleRow(Sheet, Constants[i], 146, 208, 80, false);

            // Editing variable data fields.
            StyleRow(Sheet, 7, 237, 125, 49, false);
            int Height = Sheet.Dimension.End.Row;
            string VariableDataAddress = "A19:G" + Height.ToString();
            var style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            var color = Color.FromArgb(128, 128, 128);

            Sheet.Cells[VariableDataAddress].Style.Border.Left.Style = style;
            Sheet.Cells[VariableDataAddress].Style.Border.Right.Style = style;
            Sheet.Cells[VariableDataAddress].Style.Border.Top.Style = style;
            Sheet.Cells[VariableDataAddress].Style.Border.Bottom.Style = style;

            Sheet.Cells[VariableDataAddress].Style.Border.Left.Color.SetColor(color);
            Sheet.Cells[VariableDataAddress].Style.Border.Right.Color.SetColor(color);
            Sheet.Cells[VariableDataAddress].Style.Border.Top.Color.SetColor(color);
            Sheet.Cells[VariableDataAddress].Style.Border.Bottom.Color.SetColor(color);

            VariableDataAddress = "A20:G" + Height.ToString();
            Sheet.Cells[VariableDataAddress].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            Sheet.Cells[VariableDataAddress].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(237, 125, 49));
            Sheet.Cells[VariableDataAddress].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            Sheet.Cells[VariableDataAddress].Style.Numberformat.Format = "0";

            // Set average formula calculations.
            Sheet.Cells["A7"].Value = Sheet.Cells["A4"].Value;
            Sheet.Cells["C7"].Formula = "AVERAGE(B20:B" + Height.ToString() + ")";
            Sheet.Cells["E7"].Formula = "AVERAGE(G20:G" + Height.ToString() + ")";
            Sheet.Cells["G7"].Formula = "AVERAGE(F20:F" + Height.ToString() + ")";
            Sheet.Cells["C7:G7"].Calculate();

            // Set data type for cells
            Sheet.Cells["A4"].Style.Numberformat.Format = Sheet.Cells["A7"].Style.Numberformat.Format = "#,#";
            string[] Numbers = { "C4", "E4", "E7", "C7", "G7" };
            for (int i = 0; i < Numbers.Length; i++)
                Sheet.Cells[Numbers[i]].Style.Numberformat.Format = "0";

            try {
                if (Math.Abs((double)Sheet.Cells[7,5].Value - (double)Sheet.Cells["E4"].Value) > 2)
                    Sheet.Cells["E7"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(229, 59, 59));
            }
            catch { }
        }
        private static void Merge(int[] Widths, int[] Rows, ExcelWorksheet Sheet) {
            int currentIndex = 0;
            for (int i = 0; i < Widths.Length; i++) {
                for (int x = 0; x < Rows.Length; x++)
                    Sheet.Cells[Rows[x] + 1, currentIndex + 1, Rows[x] + 1, currentIndex + Widths[i]].Merge = true;
                currentIndex += Widths[i];
            }
        }
        private static void StyleRow(ExcelWorksheet Sheet,int Row, int Red, int Green, int Blue, bool Bold) {
            int RowWidth = 7;
            Sheet.Cells[Row, 1, Row, RowWidth].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            Sheet.Cells[Row, 1, Row, RowWidth].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            Sheet.Cells[Row, 1, Row, RowWidth].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(Red, Green, Blue));
            Sheet.Cells[Row, 1, Row, RowWidth].Style.Font.Bold = Bold;
        }

        private static double GetTrueColumnWidth(double width) {
            // Calculating what the actual Excel column width will be.
            double z = 1d;
            if (width >= (1 + 2 / 3))
                z = Math.Round((Math.Round(7 * (width - 1 / 256), 0) - 5) / 7, 2);
            else
                z = Math.Round((Math.Round(12 * (width - 1 / 256), 0) - Math.Round(5 * width, 0)) / 12, 2);

            //How far are we off? (Will be less than 1)
            double errorAmt = width - z;

            //CALCULATE WHAT AMOUNT TO TACK ONTO THE ORIGINAL AMOUNT TO RESULT IN THE CLOSEST POSSIBLE SETTING 
            double adj = 0d;
            if (width >= (1 + 2 / 3))
                adj = (Math.Round(7 * errorAmt - 7 / 256, 0)) / 7;
            else
                adj = ((Math.Round(12 * errorAmt - 12 / 256, 0)) / 12) + (2 / 12);

            //RETURN A SCALED-VALUE THAT SHOULD RESULT IN THE NEAREST POSSIBLE VALUE TO THE TRUE DESIRED SETTING
            if (z > 0)
                return width + adj;

            return 0d;
        }

    }
}