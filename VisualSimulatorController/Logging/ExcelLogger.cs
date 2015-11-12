using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.Drawing;

namespace VisualSimulatorController.Logging {
    internal class ExcelLogger {

        private string FilePath;
        private Application ExcelFile;
        private Workbook Book;
        private Worksheet Sheet;
        private Range SheetRange;

        private ColorConverter Converter;

        private int CurrentRow = 20;    // Starting row for game data is 20.

        public ExcelLogger(string FilePath) {
            Converter = new ColorConverter();
            this.FilePath = FilePath;
            try {
                ExcelFile = new Application();
                ExcelFile.Visible = false;
                Book = ExcelFile.Workbooks.Add(1);
                Sheet = Book.Sheets[1] as Worksheet;
                Sheet.Name = "Simulation results";
            }
            catch {
                throw;
                // Exception handling.
                // Excel is not installed.
            }
        }
        public void SaveAs(string filename) {
            try {
                AddData(7, 3, "=AVERAGE(B20:B1048576)", "C7", "D7", 2, "#,#", 237, 125, 49, "Center");
                AddData(7, 5, "=AVERAGE(G20:G1048576)", "E7", "F7", 2, "0", 237, 125, 49, "Center");
                // Difference between expected answer % and actual is more than 2.5%
                if (Math.Abs((double)(Sheet.Cells[7, 5] as Range).Value - (double)(Sheet.Cells[4, 5] as Range).Value) > 2.5)    
                    SetBackground("E7", "F7", 229, 59, 59);

                AddData(7, 7, "=AVERAGE(F20:F1048576", "G7", "G7", 0, "0", 237, 125, 49, "Center");

                string SavePath = Environment.CurrentDirectory + "\\Game Logs\\" + FilePath;
                if (!Directory.Exists(Path.GetDirectoryName(SavePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                Book.SaveAs(SavePath);
            }
            catch {
                throw;
                // Exceptions. IO Access denied?
            }
        }
        public void Dispose() {
            try {
                Book.Close();
            }
            catch {
                throw;
            }
            finally {
                Marshal.ReleaseComObject(Book);
            }
        }

        public void LogGameData(int Turns, int Shifted, int Rotated, int Moved, int TurnTime, int Chance) {
            Sheet.Cells[CurrentRow, 1] = (CurrentRow - 19).ToString();
            Sheet.Cells[CurrentRow, 2] = Turns.ToString();
            Sheet.Cells[CurrentRow, 3] = Shifted.ToString();
            Sheet.Cells[CurrentRow, 4] = Rotated.ToString();
            Sheet.Cells[CurrentRow, 5] = Moved.ToString();
            Sheet.Cells[CurrentRow, 6] = (Turns * TurnTime).ToString();
            Sheet.Cells[CurrentRow, 7] = Chance.ToString();
            SheetRange = Sheet.get_Range(GetCell("A", CurrentRow), GetCell("G", CurrentRow));
            SheetRange.Interior.Color = 3243501;    // RGB Values : r: 237 g: 125 b: 49
            SheetRange.Borders.Color = 8421504;     // RGB Values : r: 128 g: 128 b: 128
            CurrentRow++;

        }
        public void CreateTemplate(int[] GameData, int Runs, int VisualRuns, int Chance, int TurnTime, string[] PlayerColors, string[] PlayerNames) {
            // To-Do get correct ARGB Color values.

            // Setting Column sizes.
            SetSize(1, 1, 6.29f);
            SetSize(1, 2, 7f);
            SetSize(1, 3, 12f);
            SetSize(1, 4, 12.57f);
            SetSize(1, 5, 12f);
            SetSize(1, 6, 10f);
            SetSize(1, 7, 16.43f);

            // Creating headers
            CreateHeader(1, 1, "Simulation constants", "A1", "G1", 7, 128,128,128, true, "Black", "Center");
            CreateHeader(2, 1, "Simulation data", "A2", "G2", 7, 166,166,166, true, "Black", "Center");
            CreateHeader(5, 1, "Simulation results", "A5", "G5", 7, 166,166,166, true, "Black", "Center");
            CreateHeader(8, 1, "Game board data", "A8", "G8", 7, 166, 166, 166, true, "Black", "Center");
            CreateHeader(11, 1, "Player data", "A11", "G11", 7, 166, 166, 166, true, "Black", "Center");
            CreateHeader(18, 1, "Game results", "A18", "G18", 7, 128, 128, 128, true, "Black", "Center");
            
            // Simulation data headers
            CreateHeader(3, 1, "Simulation runs", "A3", "B3", 2, 217, 217, 217, true, "Black", "Center");
            CreateHeader(3, 3, "Visual simulations", "C3", "D3", 2, 217, 217, 217, true, "Black", "Center");
            CreateHeader(3, 5, "Correct answer %", "E3", "F3", 2, 217, 217, 217, true, "Black", "Center");
            CreateHeader(3, 7, "Average turn time", "G3", "G3", 0, 217, 217, 217, true, "Black", "Center");
            // Simulation results headers
            CreateHeader(6, 1, "Simulation runs", "A6", "B6", 2, 217, 217, 217, true, "Black", "Center");
            CreateHeader(6, 3, "Average turns", "C6", "D6", 2, 217, 217, 217, true, "Black", "Center");
            CreateHeader(6, 5, "Correct answer %", "E6", "F6", 2, 217, 217, 217, true, "Black", "Center");
            CreateHeader(6, 7, "Average game time", "G6", "G6", 0, 217, 217, 217, true, "Black", "Center");
            // Game board data headers
            CreateHeader(9, 1, "Dimensions", "A9", "B9", 2, 217, 217, 217, true, "Black", "Center");
            CreateHeader(9, 3, "Straight", "C9", "C9", 0, 217, 217, 217, true, "Black", "Center");
            CreateHeader(9, 4, "Corner", "D9", "D9", 0, 217, 217, 217, true, "Black", "Center");
            CreateHeader(9, 5, "T-Split", "E9", "E9", 0, 217, 217, 217, true, "Black", "Center");
            CreateHeader(9, 6, "Reserves", "F9", "F9", 0, 217, 217, 217, true, "Black", "Center");
            CreateHeader(9, 7, "Total blocks", "G9", "G9", 0, 217, 217, 217, true, "Black", "Center");
            // Player data headers
            CreateHeader(12, 1, "#", "A12", "A12", 0, 217, 217, 217, true, "Black", "Center");
            CreateHeader(12, 2, "Name", "B12", "C12", 2, 217, 217, 217, true, "Black", "Center");
            CreateHeader(12, 4, "Color", "D12", "E12", 2, 217, 217, 217, true, "Black", "Center");
            CreateHeader(12, 6, "Starting corner", "F12", "G12", 2, 217, 217, 217, true, "Black", "Center");
            // Game results data headers
            CreateHeader(19, 1, "Game", "A19", "A19", 0, 217, 217, 217, false, "Black", "Center");
            CreateHeader(19, 2, "Turns", "B19", "B19", 0, 217, 217, 217, false, "Black", "Center");
            CreateHeader(19, 3, "Rows shifted", "C19", "C19", 0, 217, 217, 217, false, "Black", "Center");
            CreateHeader(19, 4, "Blocks rotated", "D19", "D19", 0, 217, 217, 217, false, "Black", "Center");
            CreateHeader(19, 5, "Pawns moved", "E19", "E19", 0, 217, 217, 217, false, "Black", "Center");
            CreateHeader(19, 6, "Game time", "F19", "F19", 0, 217, 217, 217, false, "Black", "Center");
            CreateHeader(19, 7, "Average answer %", "G19", "G19", 0, 217, 217, 217, false, "Black", "Center");
            SetBorder("A19", "G19", 128, 128, 128);


            // Adding data
            // Simulation data constants
            AddData(4, 1, Runs.ToString(), "A4", "B4", 2, "#,#", 146, 208, 80, "Center");
            AddData(4, 3, VisualRuns.ToString(), "C4", "D4", 2, "0", 146, 208, 80, "Center");
            AddData(4, 5, Chance.ToString(), "E4", "F4", 2, "#,#", 146, 208, 80, "Center");
            AddData(4, 7, TurnTime.ToString(), "G4", "G4", 0, "#,#", 146, 208, 80, "Center");
            // Simulation results backgrounds
            AddData(7, 1, Runs.ToString(), "A7", "B7", 2, "#,#", 237, 125, 49, "Center");
            SetBackground("E7", "G7", 229, 59, 59);
            // Game board data constants
            AddData(10, 1, string.Format("{0}x{0}", GameData[0]), "A10", "B10", 2, "General", 146, 208, 80, "Center");
            AddData(10, 3, GameData[1].ToString(), "C10", "C10", 0, "#,#", 146, 208, 80, "Center");
            AddData(10, 4, GameData[2].ToString(), "D10", "D10", 0, "#,#", 146, 208, 80, "Center");
            AddData(10, 5, GameData[3].ToString(), "E10", "E10", 0, "#,#", 146, 208, 80, "Center");
            AddData(10, 6, GameData[4].ToString(), "F10", "F10", 0, "#,#", 146, 208, 80, "Center");
            AddData(10, 7, (GameData[0] * GameData[0] + 5).ToString(), "G10", "G10", 0, "#,#", 146, 208, 80, "Center");
            // Player data constants
            for (int i = 0; i < PlayerColors.Length; i++) {
                int Row = 13 + i;
                AddData(Row, 1, (i + 1).ToString(), GetCell("A", Row), GetCell("A", Row), 0, "0", 146, 208, 80, "Center");
                AddData(Row, 2, PlayerNames[i], GetCell("B", Row), GetCell("C", Row), 2, "General", 146, 208, 80, "Center");
                AddData(Row, 4, PlayerColors[i], GetCell("D", Row), GetCell("E", Row), 2, "General", 146, 208, 80, "Center");
                AddData(Row, 6, IndexToCornerString(i), GetCell("F", Row), GetCell("G", Row), 2, "General", 146, 208, 80, "Center");
            }

        }

        public void CreateHeader(int Row, int Column, string HeaderText, string Cell1, string Cell2, int MergeColumns, int BackRed, int BackGreen, int BackBlue, bool BoldFont, string Foreground, string Alignment) {
            Sheet.Cells[Row, Column] = HeaderText;
            Sheet.Cells[Row, Column].Style.HorizontalAlignment = GetAlignment(Alignment);
            SheetRange = Sheet.get_Range(Cell1, Cell2);
            SheetRange.Merge(MergeColumns);

            SheetRange.Interior.Color = ConvertColor(BackRed, BackGreen,BackBlue);
            SheetRange.Font.Bold = BoldFont;
            SheetRange.Font.Color = ConvertColor(Foreground);
        }
        private void SetSize(int Row, int Column, float Width) {
            Sheet.Cells[Row, Column].ColumnWidth = Width;
        }
        private void SetBackground(string StartCell, string EndCell, int Red, int Green, int Blue) {
            SheetRange = Sheet.get_Range(StartCell, EndCell);
            SheetRange.Interior.Color = ConvertColor(Blue, Green, Red);
        }
        private void SetBorder(string StartCell, string EndCell, int Red, int Green, int Blue) {
            SheetRange = Sheet.get_Range(StartCell, EndCell);
            SheetRange.Borders.Color = ConvertColor(Red, Green, Blue);
        }
        public void AddData(int Row, int Column, string Data, string Cell1, string Cell2, int MergeColumns, string Format, int BackRed, int BackGreen, int BackBlue, string Alignment, string BorderColor = "Transparent") {
            Sheet.Cells[Row, Column] = Data;
            Sheet.Cells[Row, Column].Style.HorizontalAlignment = GetAlignment(Alignment);
            SheetRange = Sheet.get_Range(Cell1, Cell2);
            SheetRange.Merge(MergeColumns);
            SheetRange.Interior.Color = ConvertColor(BackBlue, BackGreen, BackRed);
            if (BorderColor != "Transparent")
                SheetRange.Borders.Color = ConvertColor(BorderColor);
            SheetRange.NumberFormat = Format;
        }

        #region Data Conversion
        private int ConvertColor(string color) {
            var RawColor = (Color)Converter.ConvertFromString(color);
            return RawColor.ToArgb();
        }
        private int ConvertColor(int Red, int Green, int Blue) {
            return Color.FromArgb(255, Red, Green, Blue).ToArgb();
        }
        private XlHAlign GetAlignment(string alignment) {
            switch (alignment.ToLower()) {
                case "left":
                    return XlHAlign.xlHAlignLeft;
                case "right":
                    return XlHAlign.xlHAlignRight;
                case "center":
                    return XlHAlign.xlHAlignCenter;
                default:
                    return XlHAlign.xlHAlignLeft;
            }
        }
        private string GetCell(string Collumn, int Row) {
            return string.Format("{0}{1}", Collumn, Row);
        }
        private string IndexToCornerString(int index) {
            switch (index) {
                case 0: return "Top Left";
                case 1: return "Top Right";
                case 2: return "Bottom Right";
                case 3: return "Bottom Left";
                default:
                    throw new ArgumentOutOfRangeException("There can't be more than 4 players!");
            }
        }
        #endregion
    }
}
