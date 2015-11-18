using System;
using System.IO;
using System.Linq;
using System.Threading;
using VisualSimulatorController.Logging.Helpers;
using VisualSimulatorController.Game_Logic.Helpers;
using System.Globalization;

namespace VisualSimulatorController.Logging {
    internal class CsvLogger : AsyncLoggerBase {

        private string CsvPath;
        private string CsvPlayersPath;
        private string ExcelPath;

        private int ExpectedGames;
        private int GameNumber = 1;
        private int TurnTime;

        private ManualResetEvent DoneEvent;

        private StreamWriter GameDataWriter;
        private StreamWriter PlayerDataWriter;


        public CsvLogger(string Path) {
            this.CsvPath = "Game Logs\\" + Path + "\\CSV-Log.csv";
            this.CsvPlayersPath = "Game Logs\\" + Path + "\\CSV-Players-Log.csv";
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
        internal override void AsyncLogData(GameData Data, string WinnerName) {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            if (Data == null) {
                GameDataWriter.WriteLine("N/A;N/A;N/A;N/A;N/A;N/A;N/A;N/A");
                PlayerDataWriter.WriteLine(GameNumber.ToString());
            }
            else {
                // Write game results to result csv
                float AnswerPercentage = (Data.RightAnswers.Sum() / (float)(Data.WrongAnswers.Sum() + Data.RightAnswers.Sum())) * 100;
                string Log = string.Format("{0};{1};{2};{3};{4};{5};{6};{7}", GameNumber, Data.Turns, Data.RowsShifted, Data.BlocksRotated, Data.PawnsMoved, Data.Turns * TurnTime, AnswerPercentage, WinnerName);
                GameDataWriter.WriteLine(Log);

                // Write player chances to player csv
                PlayerDataWriter.Write(GameNumber.ToString());
                for (int i = 0; i < Data.RightAnswers.Length; i++) {
                    float CorrectPercentage = (Data.RightAnswers[i] / (float)(Data.WrongAnswers[i] + Data.RightAnswers[i])) * 100;
                    PlayerDataWriter.Write(string.Format(";{0}", CorrectPercentage));
                }
                PlayerDataWriter.WriteLine();
            }
            GameNumber++;

            if (IsMainProcess) {
                float perc = ((float)GameNumber / ExpectedGames) * 100;
                string Title = string.Format("A-Maze-ing simulator - Logging - {0:#}%", perc);
                if (Console.Title != Title)
                    Console.Title = Title;
            }
            if(GameNumber > ExpectedGames) {
                HandleInput.PrintColor("CSV Logger finished writing a log file.", ConsoleColor.Green);
                GameDataWriter.Dispose();
                PlayerDataWriter.Dispose();
                ExcelConverter.Convert(CsvPath, CsvPlayersPath, ExcelPath, GameNumber + 19, DoneEvent);
                base.Dispose();
            }
        }
        internal override void CreateTemplate(int[] GameData, int Runs, int VisualRuns, int TurnTime, string[] PlayerColors, string[] PlayerNames, int[] PlayerChances, ManualResetEvent Done) {
            this.ExpectedGames = Runs;
            this.TurnTime = TurnTime;
            this.DoneEvent = Done;
            if (!Directory.Exists(Path.GetDirectoryName(CsvPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(CsvPath));
            try {
                using (StreamWriter writer = new StreamWriter(CsvPath)) {
                    // Setting file to hidden to avoid any access violations.
                    // File will be unhidden when converted to Excel.
                    File.SetAttributes(CsvPath, File.GetAttributes(CsvPath) | FileAttributes.Hidden);

                    writer.WriteLine("sep=;");
                    writer.WriteLine("Simulation constants");
                    writer.WriteLine("Simulation data");
                    writer.WriteLine("Simulation runs;;Visual simulations;;Correct answer %;;Average turn time");
                    writer.WriteLine(string.Format("{0};;{1};;;;{2}", Runs, VisualRuns, TurnTime));
                    writer.WriteLine("Simulation results");
                    writer.WriteLine("Simulation runs;;Average turns;;Correct answer %;;Average game time;Best Player");
                    writer.WriteLine();
                    writer.WriteLine("Game board data");
                    writer.WriteLine("Dimensions;;Straight;Corner;T-Split;Reserves;Total blocks");
                    writer.WriteLine(string.Format("{0}x{0};;{1};{2}", GameData[0], string.Join(";", GameData.Select(c => c.ToString()).ToArray(), 1, GameData.Length - 1), GameData.Sum() - GameData[0]));
                    writer.WriteLine("Player data");
                    writer.WriteLine("#;Name;Color;Start;Chance %;Actual chance;Wins;Win %");
                    for (int i = 0; i < 4; i++)
                        if (i < PlayerNames.Length)
                            writer.WriteLine(string.Format("{0};{1};{2};{3};{4}", i + 1, PlayerNames[i], PlayerColors[i], IndexToCornerString(i), PlayerChances[i]));
                        else
                            writer.WriteLine();

                    writer.WriteLine();
                    writer.WriteLine("Game results");
                    writer.WriteLine("Game;Turns;Rows shifted;Blocks rotated;Pawns moved;Game time;Correct %;Winner");
                }
            }
            catch {
                // Unhide created file.
                File.SetAttributes(CsvPath, FileAttributes.Normal);
                throw;
            }
            try {
                using (StreamWriter writer = new StreamWriter(CsvPlayersPath)) {
                    File.SetAttributes(CsvPlayersPath, File.GetAttributes(CsvPlayersPath) | FileAttributes.Hidden);
                    writer.WriteLine("sep=;");
                    writer.Write("Game");
                    for (int i = 0; i < PlayerNames.Length; i++)
                        writer.Write(";" + PlayerNames[i]);
                    writer.WriteLine();
                }
            }
            catch {
                File.SetAttributes(CsvPlayersPath, FileAttributes.Normal);
            }
            // Create the streamwriter instance that will be appending log data.
            GameDataWriter = new StreamWriter(CsvPath, true);
            PlayerDataWriter = new StreamWriter(CsvPlayersPath, true);

        }
        #endregion
    }
}

