namespace VisualSimulatorController.Game_Logic.Helpers {

    using System;
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using SimulatorDelegate;
    using SimulatorDelegate.Entities;
    using AI_Logic;
    using PrivateExtensions;

    internal class GamePhase : IDataCollector {

        private StateController AIController;
        private IGameDataCollector Main;
        private int TimeOut;

        // Regular game results
        private Player[] Players;
        private Labyrinth GameBoard;
        private bool VisualSim;
        private GameData Data;
        private int CurrentPlayer;
        private int[] PlayerChances;

        // Visual simulator results
        private LabyrinthBlock[,] OriginalBoard;
        private LabyrinthBlock[] OriginalReserves;
        private List<Tuple<string, string, string>> Moves;



        internal GamePhase(IGameDataCollector sender, int[] GameData, Color[] PlayerData, int[] PlayerChances, bool VisualSim, int TimeOut) {
            this.PlayerChances = PlayerChances;
            this.VisualSim = VisualSim;
            this.TimeOut = TimeOut;
            this.Main = sender;
            this.Players = GetPlayers(PlayerData, GameData[0]);
            this.GameBoard = new Labyrinth(this, this.Players, GameData[0], GameData[1], GameData[2], GameData[3], GameData[4]);
            AIController = new StateController(this, GameBoard, Players);
            Data = new GameData(Players.Length);

            if (VisualSim) {
                OriginalBoard = GameBoard.Board.DeepCopy();
                OriginalReserves = GameBoard.Reserves.DeepCopy();
                Moves = new List<Tuple<string, string, string>>();
            }
        }

        internal void RunSimulation() {
            CurrentPlayer = 0;
            while (!AIController.ExecuteNextMove(Players[CurrentPlayer], PlayerChances[CurrentPlayer])) {
                Data.Turns++;

                if(TimeOut != 0 && Data.Turns >= TimeOut) {
                    if (VisualSim)
                        Main.AddGameHistoryData(Data.Turns, OriginalBoard, OriginalReserves, Moves);
                    Main.AddGameLogData(null, 0);
                    return;
                }


                if (VisualSim)
                    Moves.Add(null);    // Null indicates end of turn.

                CurrentPlayer++;
                if (CurrentPlayer >= Players.Length)
                    CurrentPlayer = 0;
            }
            if (VisualSim)
                Main.AddGameHistoryData(Data.Turns, OriginalBoard, OriginalReserves, Moves);
            Main.AddGameLogData(Data, CurrentPlayer);

        }

        #region Player creation
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
        #endregion

        public Boolean IsReceivingData {
            get {
                return VisualSim;
            }
        }

        public void IncrementMoveData(String DataType) {
            switch (DataType) {
                case "PawnMoved":
                    Data.PawnsMoved++;
                    break;
                case "HallShifted":
                    Data.RowsShifted++;
                    break;
                case "BlockRotated":
                    Data.BlocksRotated++;
                    break;
                case "BlockRotationUndone":
                    Data.BlocksRotated--;
                    break;
                default: break;
            }
        }

        public void RemoveLastMove() {
            if (Moves.Count > 0)
                Moves.RemoveAt(Moves.Count - 1);
        }

        public void SendMoveData(String MoveType, String TargetObject, String MoveDirection) {
            Moves.Add(Tuple.Create(MoveType, TargetObject, MoveDirection));
        }

        public void IncrementAnswer(Boolean Correct) {
            if (Correct)
                Data.RightAnswers[CurrentPlayer]++;
            else
                Data.WrongAnswers[CurrentPlayer]++;
        }
    }
    namespace PrivateExtensions {

        using System.IO;
        using System.Runtime.Serialization.Formatters.Binary;

        public static class Extensions {

            /// <summary>
            /// Makes a deep copy of a serializable object.
            /// </summary>
            /// <typeparam name="T">A serializable object.</typeparam>
            /// <param name="Target">Target object to copy.</param>
            /// <returns>Returns a deepcopy of the target object.</returns>
            public static T DeepCopy<T>(this T Target) {
                using (var Stream = new MemoryStream()) {
                    var Formatter = new BinaryFormatter();
                    Formatter.Serialize(Stream, Target);
                    Stream.Position = 0;
                    return (T)Formatter.Deserialize(Stream);
                }
            }
        }
    }
}
