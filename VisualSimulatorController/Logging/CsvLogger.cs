using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OfficeOpenXml;
using System.IO;
using System.Xml;
using Microsoft.Office.Interop.Excel;

namespace VisualSimulatorController.Logging {
    class CsvLogger {
        private Queue<string> LogQueue = new Queue<string>();
        private string CsvPath;
        private string ExcelPath;
        private bool IsWriting;

        private int ExpectedGames;
        private int GameNumber = 0;


        public CsvLogger(string Path) {
            this.CsvPath = "Game Logs\\" + Path + ".csv";
            this.ExcelPath = "Game Logs\\" + Path + ".xlsx";
        }
        public void CreateTemplate(int[] GameData, int Runs, int VisualRuns, int Chance, int TurnTime, string[] PlayerColors, string[] PlayerNames) {
            this.ExpectedGames = Runs;
            using (StreamWriter writer = new StreamWriter(CsvPath)) {
                writer.WriteLine("sep=;");
                writer.WriteLine("Simulation constants");
                writer.WriteLine("Simulation data");
                writer.WriteLine("Simulation runs;;Visual simulations;;Correct answer %;;Average turn time");
                writer.WriteLine(string.Format("{0};{1};{2};{3}", Runs, VisualRuns, Chance, TurnTime));
                writer.WriteLine("Simulation results");
                writer.WriteLine("Simulation runs;;Average turns;;Correct answer %;;Average game time");
                writer.WriteLine();
                writer.WriteLine("Game board data");
                writer.WriteLine("Dimensions;;Straight;Corner;T-Split;Reserves;Total blocks");
                writer.WriteLine(string.Format("{0};{1}", string.Join(";", GameData), GameData.Sum() - GameData[0]));
                writer.WriteLine("Player data");
                writer.WriteLine("#;Name;Color;Starting corner");
                for(int i = 0; i < PlayerNames.Length; i++)
                    writer.WriteLine(string.Format("{0};{1};{2};{3}", i + 1, PlayerNames[i], PlayerColors[i], IndexToCornerString(i)));
                writer.WriteLine();
                writer.WriteLine("Game results");
                writer.WriteLine("Game;Turns;Rows shifted;Blocks rotated;Pawns moved;Game time;Average answer %");
            }

        }
        public void LogGameData(int Turns, int Shifted, int Rotated, int Moved, int TurnTime, int Chance) {
            LogQueue.Enqueue(string.Format("{0};{1};{2};{3};{4};{5};{6}", ++GameNumber, Turns, Shifted, Rotated, Moved, TurnTime * Turns, Chance));
            if (!IsWriting)
                WriteData();
        }

        public void WriteData() {
            if (IsWriting)
                return;

            IsWriting = true;
            Thread thrd = new Thread(new ThreadStart( delegate {
                using (var writer = new StreamWriter(CsvPath, true))   // Appending
                    while (LogQueue.Count > 0) {
                        writer.WriteLine(LogQueue.Peek());
                        LogQueue.Dequeue();
                    }
                IsWriting = false;
                if (GameNumber >= ExpectedGames)
                    Convert();
            }));
            thrd.Start();
        }

        public void Convert() {
            while (IsWriting) {
                Thread.Sleep(250);
            }

            string worksheet = "Simulator results";

            var format = new ExcelTextFormat();

            format.Delimiter = ';';
            format.EOL = "\r";
            int Rows = 0;

            using (ExcelPackage package = new ExcelPackage(new FileInfo(ExcelPath))) {
                using (ExcelWorksheet sheet = package.Workbook.Worksheets.Add(worksheet)) {
                    sheet.Cells["A1"].LoadFromText(new FileInfo(CsvPath), format, OfficeOpenXml.Table.TableStyles.Custom, false);
                    sheet.DeleteRow(1);
                    Rows = sheet.Dimension.End.Row;
                    package.Save();
                }
            }
            Application App = new Application();
            Workbook Book = App.Workbooks.Open(ExcelPath);
            Worksheet Sheet = Book.Sheets[1] as Worksheet;
            StyleExcelFile(Sheet, Rows);
            Book.Save();
        }

        private void StyleExcelFile(Worksheet Sheet, int TotalRows) {
            Range range;
            SetSize(1, 1, 6.29f, Sheet);
            SetSize(1, 2, 7f, Sheet);
            SetSize(1, 3, 12f, Sheet);
            SetSize(1, 4, 12.57f, Sheet);
            SetSize(1, 5, 12f, Sheet);
            SetSize(1, 6, 10f, Sheet);
            SetSize(1, 7, 16.43f, Sheet);
            SetBackground("A1", "G1", 128, 128, 128, true, Sheet);
            SetBackground("A2", "G2", 166, 166, 166, true, Sheet);
            SetBackground("A3", "G3", 217, 217, 217, true, Sheet);

            SetBackground("A4", "G4", 146, 208, 80, false, Sheet);
            SetBackground("A5", "G5", 166, 166, 166, true, Sheet);
            SetBackground("A6", "G6", 217, 217, 217, true, Sheet);
            SetBackground("A7", "G7", 237, 125, 49, false, Sheet);
            SetBackground("A8", "G8", 166, 166, 166, true, Sheet);
            SetBackground("A9", "G9", 217, 217, 217, true, Sheet);
            SetBackground("A10", "G10", 146, 208, 80, false, Sheet);
            SetBackground("A11", "G11", 166, 166, 166, true, Sheet);
            SetBackground("A12", "G12", 217, 217, 217, true, Sheet);
            SetBackground("A13", "G16", 146, 208, 80, false, Sheet);
            SetBackground("A18", "G18", 128, 128, 128, true, Sheet);
            SetBackground("A19", "G19", 217, 217, 217, false, Sheet);
            SetBackground("A20", "G" + TotalRows.ToString(), 237, 125, 49, false, Sheet);

            Merge("A3", "B3", 2, Sheet);
            Merge("C3", "D3", 2, Sheet);
            Merge("E3", "F3", 2, Sheet);

        }
        private void SetBackground(string StartCell, string EndCell, int Red, int Green, int Blue, bool Bold, Worksheet Sheet) {
            Range SheetRange;
            SheetRange = Sheet.get_Range(StartCell, EndCell);
            SheetRange.Interior.Color = ConvertColor(Blue, Green, Red);
            SheetRange.Font.Bold = Bold;
        }
        private void Merge(string Start, string End, int Collumns, Worksheet Sheet) {
            Range SheetRange;
            SheetRange = Sheet.get_Range(Start, End);
            SheetRange.Merge(Collumns);
        }
        private void SetSize(int Row, int Column, float Width, Worksheet Sheet) {
            Sheet.Cells[Row, Column].ColumnWidth = Width;
        }
        private int ConvertColor(int Red, int Green, int Blue) {
            return System.Drawing.Color.FromArgb(255, Red, Green, Blue).ToArgb();
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
    }
}
