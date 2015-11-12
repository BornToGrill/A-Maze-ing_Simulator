using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Xna.Framework;
using SimulatorDelegate;
using SimulatorDelegate.Entities;
using VisualSimulatorController.AI_Logic;
using VisualSimulatorController.Game_Logic;
using VisualSimulatorController.Logging;

namespace VisualSimulatorController {
    internal class GamePhase : IDataCollector {

        // Game data
        internal Player[] Players;
        internal Labyrinth GameBoard;
        internal StateController AIController;

        // Game constants
        private readonly int[] GameData;
        private readonly Color[] PlayerData;
        private readonly int Chance;
        private readonly int GameRuns;
        private readonly int VisualRuns;
        private readonly int TurnTime;

        // Logging variables
        internal int CurrentRun;
        internal int RowsShifted;
        internal int BlocksRotated;
        internal int PawnsMoved;
        internal int Turns;
        internal int RightAnswers;
        internal int WrongAnswers;

        // Logger
        private CsvLogger Logger;

        // Visual simulation 
        private string[] PlayerNameHistory;
        private Queue<int> TurnCountHistory;
        private Queue<LabyrinthBlock[,]> BoardHistory;
        private Queue<LabyrinthBlock[]> ReserveHistory;
        private string[] PlayerHistory;
        private Commands Coms;
        private bool AnimationStarted;
        // The move history contains tuples with the following format:
        // MoveType ( MovePawn, ShiftRow, RotateBlock )
        // TargetObject ( Player #, Row index, Block index )
        // Move direction ( Direction, Direction, Rotation )
        private Queue<List<Tuple<string, string, string>>> MoveHistory;
        internal List<Tuple<string, string, string>> TemporaryMoves;

        /// <summary>
        /// Initializes a new game phase. The phase contains controls the simulation and subsequently logs the data gathered.
        /// </summary>
        /// <param name="BoardSize">Integer representing the board width and height. The integer squared will be the board size.</param>
        /// <param name="Straight">Amount of straight type labyrinth pieces.</param>
        /// <param name="Corner">Amount of corner type labyrinth pieces.</param>
        /// <param name="TSplit">Amount of T-Split type labyrinth pieces.</param>
        /// <param name="Reserves">Amount of reserve blocks.</param>
        /// <param name="PlayerColors">An array of player colours. The length should be between 1 and 4.</param>
        /// <param name="Chance">Percentage chance that a player will answer the question correctly.</param>
        /// <param name="Runs">Amount of games the simulator will run.</param>
        /// <param name="TurnTime">The amount of time a turn would take given in seconds.</param>
        internal GamePhase(Commands coms, int BoardSize, int Straight, int Corner, int TSplit, int Reserves, Color[] PlayerColors, string[] Colors, string[] PlayerNames, int Chance, int Runs, int VisualRuns, int TurnTime) {
            // Set all constants
            this.GameData = new[] { BoardSize, Straight, Corner, TSplit, Reserves };
            this.PlayerData = PlayerColors;
            this.Chance = Chance;
            this.GameRuns = Runs;
            this.VisualRuns = VisualRuns;
            this.TurnTime = TurnTime;
            this.Coms = coms;
            this.PlayerNameHistory = PlayerNames;

            // Setup logger and template
            Logger = new CsvLogger(DateTime.Now.ToString("dd-MM-yy, hh`mm`ss"));
            Logger.CreateTemplate(GameData, GameRuns, VisualRuns, Chance, TurnTime, Colors, PlayerNames);
            // If visual simulation is enabled, initialize queues.
            if(VisualRuns > 0) {
                TurnCountHistory = new Queue<int>();
                BoardHistory = new Queue<LabyrinthBlock[,]>();
                ReserveHistory = new Queue<LabyrinthBlock[]>();
                PlayerHistory = DeepCopy(Colors);
                MoveHistory = new Queue<List<Tuple<string, string, string>>>();
            }
            // Load constants into new phase.
            NextPhase();
        }

        private void NextPhase() {
            this.Players = GetPlayers(this.PlayerData, GameData[0]);
            this.GameBoard = new Labyrinth(this, this.Players, GameData[0], GameData[1], GameData[2], GameData[3], GameData[4]);
            this.AIController = new StateController(this, Chance, this.GameBoard, this.Players);

            if (VisualRuns > 0 && !AnimationStarted) {
                if (BoardHistory.Count < VisualRuns) {
                    BoardHistory.Enqueue(DeepCopy(this.GameBoard.Board));
                    ReserveHistory.Enqueue(DeepCopy(this.GameBoard.Reserves));
                    TemporaryMoves = new List<Tuple<string, string, string>>();
                }
                else if (BoardHistory.Count == VisualRuns) {
                    Coms.StartGame();
                    Coms.UpdateSimulationData(BoardHistory, ReserveHistory, MoveHistory, TurnCountHistory, PlayerHistory, PlayerNameHistory);
                    TemporaryMoves = null;
                    AnimationStarted = true;
                }
            }

            RowsShifted = 0;
            BlocksRotated = 0;
            PawnsMoved = 0;
            Turns = 0;
            RightAnswers = 0;
            WrongAnswers = 0;
        }
        internal void RunPhase() {
            CurrentRun++;

            int CurrentPlayer = 0;
            while (!AIController.ExecuteNextMove(Players[CurrentPlayer])) {
                Turns++;
                // Add turn end to move history
                if (TemporaryMoves != null)
                    TemporaryMoves.Add(null);   // Null indicates turn end.

                CurrentPlayer++;
                if (CurrentPlayer >= Players.Length)
                    CurrentPlayer = 0;
            }

            // Phase ends.
            // CurrentPlayer wins the game.
            // Send game data to logger.
            int percRight = (int)Math.Round(((float)RightAnswers / (WrongAnswers + RightAnswers)) * 100, MidpointRounding.AwayFromZero);

            Logger.LogGameData(Turns, RowsShifted, BlocksRotated, PawnsMoved, TurnTime, percRight);

            // Check if visual simulation still requires game data.
            if (VisualRuns > 0 && MoveHistory.Count < VisualRuns) {
                MoveHistory.Enqueue(TemporaryMoves);
                TurnCountHistory.Enqueue(Turns);
            }

            if (CurrentRun < GameRuns) {
                float perc = ((float)CurrentRun / GameRuns) * 100;
                Console.Title = string.Format("A-Maze-ing simulator - Simulating - {0:#.00}%", perc);
                NextPhase();
                RunPhase();
            }
            else {
                Console.Title = "A-Maze-ing simulator";
                // Convert Csv to excel
                //Logger.SaveAs("LOGDATA");
                //Logger.Dispose();
                // Simulation ended.
            }
        }

        private Player[] GetPlayers(Color[] PawnColor, int BoardSize) {
            var temp = new Player[PawnColor.Length];
            for (int i = 0; i < PawnColor.Length; i++) {
                Vector2 Position;
                switch (i) {
                    case 0:
                        Position = new Vector2(0, 0);
                        break;
                    case 1:
                        Position = new Vector2(BoardSize - 1, 0);
                        break;
                    case 2:
                        Position = new Vector2(BoardSize - 1, BoardSize - 1);
                        break;
                    case 3:
                        Position = new Vector2(0, BoardSize - 1);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Starting corner can only be in range 0-3");
                }
                temp[i] = new Player(this, PawnColor[i], Position, i);
            }
            return temp;
        }

        /// <summary>
        /// Makes a deep copy of a serializable object.
        /// </summary>
        /// <typeparam name="T">A serializable object.</typeparam>
        /// <param name="Target">Target object to copy.</param>
        /// <returns>Returns a deepcopy of the target object.</returns>
        private T DeepCopy<T>(T Target) {
            using (var Stream = new MemoryStream()) {
                var Formatter = new BinaryFormatter();
                Formatter.Serialize(Stream, Target);
                Stream.Position = 0;
                return (T)Formatter.Deserialize(Stream);
            }
        }

        #region IDataCollector implementation members
        public void SendMoveData(String MoveType, String TargetObject, String MoveDirection) {
            if(TemporaryMoves != null)
                TemporaryMoves.Add(Tuple.Create(MoveType, TargetObject, MoveDirection));
        }

        public void IncrementMoveData(String DataType) {
            switch (DataType) {
                case "PawnMoved":
                    PawnsMoved++;
                    break;
                case "CorrectAnswer":
                    RightAnswers++;
                    break;
                case "IncorrectAnswer":
                    WrongAnswers++;
                    break;
                case "HallShifted":
                    RowsShifted++;
                    break;
                case "BlockRotated":
                    BlocksRotated++;
                    break;
                case "BlockRotationUndone":
                    BlocksRotated--;
                    break;
                default: break;
            }
        }
        public void RemoveLastMove() {
            if(TemporaryMoves != null)
                if (TemporaryMoves.Count > 0)
                    TemporaryMoves.RemoveAt(TemporaryMoves.Count - 1);
        }
        #endregion
    }
}
