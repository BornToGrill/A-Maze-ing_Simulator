using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VisualSimulatorController.Game_Logic.Helpers;
using VisualSimulatorController.Logging;
using VisualSimulatorController.Logging.Helpers;
using Microsoft.Xna.Framework;
using SimulatorDelegate.Entities;
using System.Threading;

namespace VisualSimulatorController.Game_Logic {

    internal class PhaseController : IGameDataCollector {

        // Game constants
        readonly int[] GameData;
        readonly Color[] PlayerData;
        readonly int[] PlayerChances;
        readonly int GameRuns;
        readonly int VisualRuns;
        readonly int TurnTime;
        readonly int TimeOut;

        // Logger
        AsyncLoggerBase Logger;
        ManualResetEvent DoneSimulating;

        // Visual simulation 
        string[] PlayerNameHistory;
        Queue<int> TurnCountHistory;
        Queue<LabyrinthBlock[,]> BoardHistory;
        Queue<LabyrinthBlock[]> ReserveHistory;
        string[] PlayerHistory;
        Commands Coms;

        int CurrentSimulation;

        // The move history contains tuples with the following format:
        // MoveType ( MovePawn, ShiftRow, RotateBlock )
        // TargetObject ( Player #, Row index, Block index )
        // Move direction ( Direction, Direction, Rotation )
        Queue<List<Tuple<string, string, string>>> MoveHistory;
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
        internal PhaseController(Commands coms, int[] BoardData, Color[] PlayerColors, string[] Colors, string[] PlayerNames, int[] PlayerChances, int Runs, int VisualRuns, int TurnTime, int TimeOut ) {
            // Set all constants
            DoneSimulating = new ManualResetEvent(false);

            this.GameData = BoardData;
            this.PlayerData = PlayerColors;
            this.PlayerChances = PlayerChances;
            this.GameRuns = Runs;
            this.VisualRuns = VisualRuns;
            this.TurnTime = TurnTime;
            this.Coms = coms;
            this.PlayerNameHistory = PlayerNames;
            this.TimeOut = TimeOut;

            // Setup logger and template
            Logger = new CsvLogger(DateTime.Now.ToString("dd-MM-yyyy, HH;mm;ss"));
            Logger.CreateTemplate(BoardData, GameRuns, VisualRuns, TurnTime, Colors, PlayerNames,PlayerChances, DoneSimulating);
            // If visual simulation is enabled, initialize queues.
            if(VisualRuns > 0) {
                TurnCountHistory = new Queue<int>();
                BoardHistory = new Queue<LabyrinthBlock[,]>();
                ReserveHistory = new Queue<LabyrinthBlock[]>();
                PlayerHistory = Colors;
                MoveHistory = new Queue<List<Tuple<string, string, string>>>();
            }
        }

        internal void RunSimulation() {
            Parallel.For(0, VisualRuns, VisualSim);
            Parallel.For(VisualRuns, GameRuns, StartSim);
            HandleInput.PrintColor(string.Format("Simulation completed, '{0:n0}' simulations were run.", GameRuns), ConsoleColor.Green);
            Console.Title = "A-Maze-ing simulator";
            Logger.IsMainProcess = true;
            DoneSimulating.WaitOne();   // Wait for excel converter to finish.
        }

        private void VisualSim(int obj) {
            CurrentSimulation++;
            var Phase = new GamePhase(this, GameData, PlayerData, PlayerChances, true, TimeOut);
            Phase.RunSimulation();
        }
        private void StartSim(int obj) {
            CurrentSimulation++;
            var Phase = new GamePhase(this, GameData, PlayerData, PlayerChances, false, TimeOut);
            Phase.RunSimulation();
        }


        #region IGameDataCollector implementation members
        public void AddGameHistoryData(Int32 TurnCount, object GameBoard, object Reserves, object Moves) {
            lock (TurnCountHistory)
                lock (BoardHistory)
                    lock (ReserveHistory)
                        lock (MoveHistory) {
                            TurnCountHistory.Enqueue(TurnCount);
                            BoardHistory.Enqueue((LabyrinthBlock[,])GameBoard);
                            ReserveHistory.Enqueue((LabyrinthBlock[])Reserves);
                            MoveHistory.Enqueue((List<Tuple<string, string, string>>)Moves);
                            if (MoveHistory.Count >= VisualRuns) {
                                Coms.StartGame();
                                Coms.UpdateSimulationData(BoardHistory, ReserveHistory, MoveHistory, TurnCountHistory, PlayerHistory, PlayerNameHistory);
                            }
                        }
        }

        public void AddGameLogData(GameData Data, int WinnerIndex) {
            Logger.LogData(Data, PlayerNameHistory[WinnerIndex]);

            float perc = 0;
            perc = ((float)CurrentSimulation/ GameRuns) * 100;
            string Title = string.Format("A-Maze-ing simulator - Simulating - {0:0.0}%", perc);
            if (Console.Title != Title)
                Console.Title = Title;
        }
        #endregion
    }
}



