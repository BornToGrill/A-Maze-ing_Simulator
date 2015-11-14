using System;
using System.Collections.Generic;
using System.Linq;
using SimulatorDelegate;
using SimulatorDelegate.Entities;
using Microsoft.Xna.Framework;

namespace VisualSimulatorController.Game_Logic {

    internal class Labyrinth {

        private IDataCollector Phase;
        internal LabyrinthBlock[,] Board;
        internal LabyrinthBlock[] Reserves;

        internal Player[] Players;

        /// <summary>
        /// Creates a Labyrinth board with given dimensions.
        /// </summary>
        /// <param name="BoardSize">An integer defining the board dimensions. This number should be uneven! and greater than 4.</param>
        /// <param name="StraightBlocks">An integer defining the amount of Straight type blocks on both the board and reserves combined.</param>
        /// <param name="CornerBlocks">An integer defining the amount of Corner type blocks on both the board and reserves combined.</param>
        /// <param name="TSplitBlocks">An integer defining the amount of T-Split type blocks on both the board and reserves combined.</param>
        /// <param name="ReserveBlocks">An integer defining the amount of reserve blocks available. This value has a default of 5 and should be greater than 0.</param>
        internal Labyrinth(IDataCollector sender, Player[] Players, int BoardSize, int StraightBlocks, int CornerBlocks, int TSplitBlocks, int ReserveBlocks) {
            this.Players = Players;
            this.Phase = sender;


            Board = new LabyrinthBlock[BoardSize, BoardSize];
            Reserves = new LabyrinthBlock[ReserveBlocks];

            int TotalBlocks = BoardSize * BoardSize + ReserveBlocks - 5; // 5 = Corners + chest

            List<int> Blocks = new List<int>(){ StraightBlocks, CornerBlocks, TSplitBlocks };

            // Start randomizer
            RandomizeBoard(Blocks);
        }

        #region Block Manipulation
        internal void ShiftRow(int RowIndex, Direction dir) {
            ShiftRow(RowIndex, dir, GameShadow.rnd.Next(0, Reserves.Length), GameShadow.rnd.Next(0, 4));
        }
        internal void ShiftRow(int RowIndex, Direction dir, int ReserveIndex, int Orientation) {
            Phase.IncrementMoveData("HallShifted");
            List<int> PlayersMoved = new List<int>();

            LabyrinthBlock Reserve = Reserves[ReserveIndex];
            Reserve.Orientation = Orientation;

            if(dir == Direction.Right) {
                Reserves[ReserveIndex] = Board[RowIndex, Board.GetLength(1) - 1];
                Reserves[ReserveIndex].Orientation = 0;
                for (int i = Board.GetLength(1) - 1; i >= 0; i--) {  // -0 == -1
                    // Check if Block contains player
                    if (i != Board.GetLength(1) - 1)
                        for (int x = 0; x < Players.Length; x++) {
                            var PlayerLocation = Players[x].Position;
                            if (PlayerLocation.X == i && PlayerLocation.Y == RowIndex) {
                                Players[x].MovePlayer(Direction.Right, true);
                                PlayersMoved.Add(x);
                            }
                        }
                    if (i != 0)  // All blocks except for far left one. Which will be replaced by the Reserve.
                        Board[RowIndex, i] = Board[RowIndex, i - 1];
                }

                Board[RowIndex, 0] = Reserve;
            }
            else if (dir == Direction.Left) {
                Reserves[ReserveIndex] = Board[RowIndex, 0];
                Reserves[ReserveIndex].Orientation = 0;
                for (int i = 0; i < Board.GetLength(1) ; i++) {
                    // Check if Block contains player
                    if (i != 0)
                        for (int x = 0; x < Players.Length; x++) {
                            var PlayerLocation = Players[x].Position;
                            if (PlayerLocation.X == i && PlayerLocation.Y == RowIndex) {
                                Players[x].MovePlayer(Direction.Left, true);
                                PlayersMoved.Add(x);
                            }
                        }

                    if (i != Board.GetLength(1) - 1) // All blocks except for far right one. Which will be replaced by the Reserve.
                        Board[RowIndex, i] = Board[RowIndex, i + 1];
                }
                Board[RowIndex, Board.GetLength(1) - 1] = Reserve;
            }
            else if (dir == Direction.Up) {
                Reserves[ReserveIndex] = Board[0, RowIndex];
                Reserves[ReserveIndex].Orientation = 0;
                for (int i = 0; i < Board.GetLength(0); i++) {
                    // Check if Block contains player
                    if (i != 0)
                        for (int x = 0; x < Players.Length; x++) {
                            var PlayerLocation = Players[x].Position;
                            if (PlayerLocation.X == RowIndex && PlayerLocation.Y == i) {
                                Players[x].MovePlayer(Direction.Up, true);
                                PlayersMoved.Add(x);
                            }
                        }
                    if (i != Board.GetLength(0) - 1) // All blocks except for the bottom one. Which will be replaced by the Reserve.
                        Board[i, RowIndex] = Board[i + 1, RowIndex];
                }
                Board[Board.GetLength(1) - 1, RowIndex] = Reserve;
            }
            else if (dir == Direction.Down) {
                Reserves[ReserveIndex] = Board[Board.GetLength(0) - 1, RowIndex];
                Reserves[ReserveIndex].Orientation = 0;
                for (int i = Board.GetLength(0) - 1; i >= 0; i--) {
                    // Check if Block contains player
                    if (i != Board.GetLength(0) - 1)
                        for (int x = 0; x < Players.Length; x++) {
                            var PlayerLocation = Players[x].Position;
                            if (PlayerLocation.X == RowIndex && PlayerLocation.Y == i) {
                                Players[x].MovePlayer(Direction.Down, true);
                                PlayersMoved.Add(x);
                            }
                        }
                    if (i != 0) // All blocks except for the top one. Which will be replaced by the Reserve.
                        Board[i, RowIndex] = Board[i - 1, RowIndex];
                }
                Board[0, RowIndex] = Reserve;
            }
            if (Phase.IsReceivingData) {
                string MoveData = string.Format("{0},{1},{2}", RowIndex, ReserveIndex, Orientation);
                if (PlayersMoved.Count > 0)
                    MoveData += "-" + string.Join(":", PlayersMoved);
                Phase.SendMoveData("HallShifted", MoveData, dir.ToString());
            }
        }

        internal void Rotate(int Xindex, int Yindex, Rotate Direction, int Count = 1, bool undo = false) {
            if (Board[Yindex, Xindex].Type == BlockType.Chest)
                return;
            if (!(Xindex == 0 && Yindex == 0) && !(Xindex == 0 && Yindex == Board.GetLength(0) - 1) &&
                !(Xindex == Board.GetLength(1) - 1 && Yindex == 0) && !(Xindex == Board.GetLength(1) - 1 && Yindex == Board.GetLength(0) - 1)) { // Prevent corner pieces from being moved.
                Board[Yindex, Xindex].RotateBlock(Direction, Count);
                if (Count == 1 && !undo) {
                    Phase.IncrementMoveData("BlockRotated");
                    if (Phase.IsReceivingData)
                        Phase.SendMoveData("BlockRotated", string.Format("{0},{1}", Xindex, Yindex), Direction.ToString());
                }
                else if (Count == 2 && Phase.IsReceivingData) {
                    Phase.RemoveLastMove();
                    Phase.SendMoveData("BlockRotated", string.Format("{0},{1}", Xindex, Yindex), Direction.ToString());
                }
                else if (undo) {
                    if (Phase.IsReceivingData)
                        Phase.RemoveLastMove();
                    Phase.IncrementMoveData("BlockRotationUndone");
                }

            }
        }
        #endregion
 
        #region Board Creation

        private void RandomizeBoard(List<int> Blocks) {
            List<int> BlockNumbers = new List<int>() { 0, 1, 2 };
            // Remove any indexes if 0 pieces have been selected
            for (int i = Blocks.Count - 1; i >= 0; i--)
                if (Blocks[i] <= 0)
                    BlockNumbers.RemoveAt(i);

            for(int i = 0; i < Board.GetLength(0); i++)
                for(int x = 0; x < Board.GetLength(1); x++) {
                    LabyrinthBlock LockedPiece;
                    if((LockedPiece = FindLocked(i, x, Board.GetLength(0))) != null) {
                        Board[i, x] = LockedPiece;
                        if (LockedPiece.Type == BlockType.Straight)
                            Board[i, x] = null;
                        continue;
                    }
                    int Index = GameShadow.rnd.Next(BlockNumbers.Count);
                    int Type = BlockNumbers[Index];
                    Board[i, x] = new LabyrinthBlock((BlockType)Type, GameShadow.rnd);
                    Blocks[Type]--;
                    if (Blocks[Type] <= 0)
                        BlockNumbers.RemoveAt(Index);
                }
            int index = 0;
            for (int i = 0; i < 3; i++) {
                while(Blocks[i] > 0) {
                    Reserves[index] = new LabyrinthBlock((BlockType)i, 0);
                    Blocks[i]--;
                    index++;
                }
            }

        }
        private LabyrinthBlock FindLocked(int y, int x, int Dimensions) {
            Dimensions--;   // Correcting for 0-based
            if (x == 0 && y == 0)                                   // Top-left
                return new LabyrinthBlock(BlockType.Corner, 1);
            else if (x == Dimensions && y == 0)                     // Top-right
                return new LabyrinthBlock(BlockType.Corner, 2);
            else if (x == Dimensions && y == Dimensions)            // Bottom-right
                return new LabyrinthBlock(BlockType.Corner, 3);
            else if (x == 0 && y == Dimensions)                     // Bottom-left
                return new LabyrinthBlock(BlockType.Corner, 0);
            else if (x == Dimensions / 2 && y == Dimensions / 2)    // Center-Piece
                return new LabyrinthBlock(BlockType.Chest, 0);
            else
                return null;
        }
        #endregion

        internal bool PlayerCanMove(Player player, Direction dir) {
            return PlayerCanMove(player.Position, dir);
        }
        internal bool PlayerCanMove(Vector2 player, Direction dir) {
            int MoveToX = (int)player.X;
            int MoveToY = (int)player.Y;
            Direction Opposite;

            if (dir == Direction.Left) {
                MoveToX--;
                Opposite = Direction.Right;
            }
            else if (dir == Direction.Right) {
                MoveToX++;
                Opposite = Direction.Left;
            }
            else if (dir == Direction.Down) {
                MoveToY++;
                Opposite = Direction.Up;
            }
            else {
                MoveToY--;
                Opposite = Direction.Down;
            }
            // Check if the location to move to is inside the board. If not return false.
            if (MoveToX < 0 || MoveToX > Board.GetLength(1) - 1)
                return false;
            else if (MoveToY < 0 || MoveToY > Board.GetLength(0) - 1)
                return false;

            if (Board[(int)player.Y, (int)player.X].CanMove(dir))
                if (Board[MoveToY, MoveToX].CanMove(Opposite))
                    return true;
            return false;
        }
        internal bool PlayerCanMoveOut(Player player, Direction dir) {
            if (Board[(int)player.Position.Y, (int)player.Position.X].CanMove(dir))
                return true;
            return false;
        }
        internal bool PlayerCanMoveOut(Vector2 player, Direction dir) {
            if (Board[(int)player.Y, (int)player.X].CanMove(dir))
                return true;
            return false;
        }
        internal bool CanShift(int index) {
            return (index != 0 && index != Board.GetLength(0) - 1 && index != Board.GetLength(0) / 2);  // Top/Bottom and center rows.
        }
    }
    
}
