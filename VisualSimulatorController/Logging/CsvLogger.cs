using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using OfficeOpenXml;

namespace VisualSimulatorController.Logging {
    class CsvLogger {
        private Queue<string> LogQueue = new Queue<string>();
        private string CsvPath;
        private string ExcelPath;
        private bool IsWriting;

        private int ExpectedGames;
        private int GameNumber = 0;


        public CsvLogger(string Path) {
            this.CsvPath = "Game Logs\\" + Path + "\\CSV-Log.csv";
            this.ExcelPath = "Game Logs\\" + Path + "\\Excel-Log.xlsx";
        }
        public void CreateTemplate(int[] GameData, int Runs, int VisualRuns, int Chance, int TurnTime, string[] PlayerColors, string[] PlayerNames) {
            this.ExpectedGames = Runs;
            if (!Directory.Exists(Path.GetDirectoryName(CsvPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(CsvPath));
            using (StreamWriter writer = new StreamWriter(CsvPath)) {
                // Setting file to hidden to avoid any access violations.
                // File will be unhidden when converted to Excel.
                File.SetAttributes(CsvPath, File.GetAttributes(CsvPath) | FileAttributes.Hidden);

                writer.WriteLine("sep=;");
                writer.WriteLine("Simulation constants");
                writer.WriteLine("Simulation data");
                writer.WriteLine("Simulation runs;;Visual simulations;;Correct answer %;;Average turn time");
                writer.WriteLine(string.Format("{0};;{1};;{2};;{3}", Runs, VisualRuns, Chance, TurnTime));
                writer.WriteLine("Simulation results");
                writer.WriteLine("Simulation runs;;Average turns;;Correct answer %;;Average game time");
                writer.WriteLine();
                writer.WriteLine("Game board data");
                writer.WriteLine("Dimensions;;Straight;Corner;T-Split;Reserves;Total blocks");
                writer.WriteLine(string.Format("{0}x{0};;{1};{2}", GameData[0], string.Join(";", GameData.Select(c => c.ToString()).ToArray(), 1, GameData.Length - 1), GameData.Sum() - GameData[0]));
                writer.WriteLine("Player data");
                writer.WriteLine("#;Name;;Color;;Starting corner");
                for (int i = 0; i < 4; i++)
                    if (i < PlayerNames.Length)
                        writer.WriteLine(string.Format("{0};{1};;{2};;{3}", i + 1, PlayerNames[i], PlayerColors[i], IndexToCornerString(i)));
                    else
                        writer.WriteLine();

                writer.WriteLine();
                writer.WriteLine("Game results");
                writer.WriteLine("Game;Turns;Rows shifted;Blocks rotated;Pawns moved;Game time;Average answer %");
            }

        }
        public void LogGameData(int Turns, int Shifted, int Rotated, int Moved, int TurnTime, int Chance) {
            lock (LogQueue) {
                LogQueue.Enqueue(string.Format("{0};{1};{2};{3};{4};{5};{6}", ++GameNumber, Turns, Shifted, Rotated, Moved, TurnTime * Turns, Chance));
            }
            if (!IsWriting)
                WriteData();
        }

        public void WriteData() {
            if (IsWriting)
                return;

            IsWriting = true;
            Thread thrd = new Thread(new ThreadStart( delegate {
                using (var writer = new StreamWriter(CsvPath, true)) {  // Appending
                    lock (LogQueue) {
                        while (LogQueue.Count > 0) {
                            writer.WriteLine(LogQueue.Peek());
                            LogQueue.Dequeue();
                        }
                    }
                }

                IsWriting = false;
                lock (LogQueue) {
                    if (GameNumber >= ExpectedGames && LogQueue.Count == 0)
                        ExcelConverter.Convert(CsvPath, ExcelPath, GameNumber + 19);
                    else if (!IsWriting && LogQueue.Count > 0)
                        WriteData();
                }
            }));
            thrd.Start();
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
