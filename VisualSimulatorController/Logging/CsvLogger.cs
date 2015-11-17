using System;
using System.IO;
using System.Linq;
using System.Threading;
using VisualSimulatorController.Logging.Helpers;
using VisualSimulatorController.Game_Logic.Helpers;

namespace VisualSimulatorController.Logging {
    internal class CsvLogger : AsyncLoggerBase {

        string CsvPath;
        string ExcelPath;
        
        int ExpectedGames;
        int GameNumber = 1;
        int TurnTime;

        ManualResetEvent DoneEvent;


        public CsvLogger(string Path) {
            this.CsvPath = "Game Logs\\" + Path + "\\CSV-Log.csv";
            this.ExcelPath = "Game Logs\\" + Path + "\\Excel-Log.xlsx";
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

        #region AsnyLoggerBase implementation members
        internal override void AsyncLogData(GameData Data) {
            using (var writer = new StreamWriter(CsvPath, true)) {  // Appending
                float AnswerPercentage = (Data.RightAnswers / (float)(Data.WrongAnswers + Data.RightAnswers)) * 100;
                string Log = string.Format("{0};{1};{2};{3};{4};{5};{6}", GameNumber++, Data.Turns, Data.RowsShifted, Data.BlocksRotated, Data.PawnsMoved, Data.Turns * TurnTime, AnswerPercentage);
                writer.WriteLine(Log);
            }
            if (IsMainProcess) {
                float perc = ((float)GameNumber / ExpectedGames) * 100;
                string Title = string.Format("A-Maze-ing simulator - Logging - {0:#}%", perc);
                if (Console.Title != Title)
                    Console.Title = Title;
            }
            if(GameNumber > ExpectedGames) {
                HandleInput.PrintColor("CSV Logger finished writing a log file.", ConsoleColor.Green);
                ExcelConverter.Convert(CsvPath, ExcelPath, GameNumber + 19, DoneEvent);
                base.Dispose();
            }
        }
        internal override void CreateTemplate(int[] GameData, int Runs, int VisualRuns, int Chance, int TurnTime, string[] PlayerColors, string[] PlayerNames, ManualResetEvent Done) {
            this.ExpectedGames = Runs;
            this.TurnTime = TurnTime;
            this.DoneEvent = Done;
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
        #endregion
    }
}

