using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimulatorDelegate;
using SimulatorDelegate.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace VisualSimulator {
    internal static class Animations {
        internal static bool IsAnimating = false;

        // Animation data
        internal static List<Vector2> AnimatedBlocks = new List<Vector2>();
        internal static List<int> AnimatedPlayers = new List<int>();
        private static Tuple<string, string, string> CurrentOperation;

        // Animation targets
        internal static string AnimationTarget;
        private static float TargetRotation;
        private static Vector2 TargetDistance;

        // Current animation values
        internal static Vector2 Distance = Vector2.Zero;
        internal static float Rotation = 0.0f;

        // Animation speed variables
        private const double ConstantSpeed = 1e-5;
        internal static float RotationSpeed = 0.01f;
        internal static float MovementSpeed = 1f;

        // Current game state variables
        private static LabyrinthBlock[,] GameBoard;
        private static LabyrinthBlock[] GameReserves;
        private static Player[] GamePlayers;

        // TextureSize variables
        private static float Scale;
        private static float Offset;

        // Delegates
        private delegate void AnimationAction(double Speed, float Scale);
        private delegate void DataSetter();


        public static void Animate(Tuple<string,string,string> AnimationData, LabyrinthBlock[,] CurrentBoard, LabyrinthBlock[] CurrentReserves, Player[] CurrentPlayers, float scale, float offset) {
            if (IsAnimating)
                return;
            CurrentOperation = AnimationData;

            GameBoard = CurrentBoard;
            GameReserves = CurrentReserves;
            GamePlayers = CurrentPlayers;
            Scale = scale;
            Offset = offset;

            DataSetter set;
            if(CurrentOperation == null) {
                Game1.CurrentTurn++;
                Game1.CurrentPlayerIndex++;
                AnimationDone();
                return;
            }
            switch (CurrentOperation.Item1) {
                case "PawnMoved":
                    set = SetPlayerMovementData;
                    break;
                case "HallShifted":
                    set = SetShiftingData;
                    break;
                case "BlockRotated":
                    set = SetRotationData;
                    break;
                default:
                    AnimationDone();
                    return;
            }
            set.Invoke();
            IsAnimating = true;
        }

        public static void Update(GameTime gameTime) {
            if (!IsAnimating)
                return;
            double Speed = gameTime.ElapsedGameTime.Ticks * ConstantSpeed;

            AnimationAction action;
            switch (CurrentOperation.Item1) {
                case "PawnMoved":
                    action = MovePlayer;
                    break;
                case "HallShifted":
                    action = ShiftHall;
                    break;
                case "BlockRotated":
                    action = RotateBlock;
                    break;
                default:
                    AnimationDone();
                    return;
            }
            action.Invoke(Speed, Scale);
        }
        public static void Draw(SpriteBatch spriteBatch, Texture2D BaseBlock, Dictionary<BlockType, Texture2D> Textures, float Offset, Rectangle Source, Vector2 Origin) {
            if (!IsAnimating || CurrentOperation.Item1 != "HallShifted")
                return;
            string[] ShiftingData = CurrentOperation.Item2.Split('-');
            // Contains : RowIndex, ReserveIndex, Orientation
            int[] LocationData = ShiftingData[0].Split(',').Select(x => int.Parse(x)).ToArray();

            var Block = GameReserves[LocationData[1]];
            var Type = Block.Type;
            var Texture = Textures[Type];
            var Orientation = (float)(LocationData[2] * (Math.PI / 2));
            var Position = Vector2.Zero;
            switch (CurrentOperation.Item3) {
                case "Left":
                    Position = new Vector2(GameBoard.GetLength(1) * Game1.TextureSize.X * Scale + Offset, LocationData[0] * Game1.TextureSize.Y * Scale + Offset);
                    break;
                case "Right":
                    Position = new Vector2(-Game1.TextureSize.X * Scale + Offset, LocationData[0] * Game1.TextureSize.Y * Scale + Offset);
                    break;
                case "Up":
                    Position = new Vector2(LocationData[0] * Game1.TextureSize.X * Scale + Offset, GameBoard.GetLength(0) * Game1.TextureSize.Y * Scale + Offset);
                    break;
                case "Down":
                    Position = new Vector2(LocationData[0] * Game1.TextureSize.X * Scale + Offset, -Game1.TextureSize.Y * Scale + Offset);
                    break;


            }
            spriteBatch.Draw(BaseBlock, Position + Distance, null, Color.White, 0, Origin, Scale, SpriteEffects.None, 0);
            spriteBatch.Draw(Texture, Position + Distance, Source, Color.White, Orientation, Origin, Scale, SpriteEffects.None, 0);

        }
        #region Player movement animation members
        private static void SetPlayerMovementData() {
            Game1.PawnsMoved++;
            AnimationTarget = "Player";
            int TargetIndex = int.Parse(CurrentOperation.Item2);
            AnimatedPlayers.Add(TargetIndex);

            switch (CurrentOperation.Item3) {
                case "Left":
                    TargetDistance = new Vector2(-Game1.TextureSize.X * Scale, 0);
                    break;
                case "Right":
                    TargetDistance = new Vector2(Game1.TextureSize.X * Scale, 0);
                    break;
                case "Up":
                    TargetDistance = new Vector2(0, -Game1.TextureSize.Y * Scale);
                    break;
                case "Down":
                    TargetDistance = new Vector2(0, Game1.TextureSize.Y * Scale);
                    break;
            }


        }
        private static void MovePlayer(double Speed, float Scale) {
            switch (CurrentOperation.Item3) {
                case "Left":
                    if (Distance.X < TargetDistance.X)
                        FinishMovingPlayer();
                    else
                        Distance -= new Vector2((float)(MovementSpeed * Speed), 0);
                    break;
                case "Right":
                    if (Distance.X > TargetDistance.X)
                        FinishMovingPlayer();
                    else
                        Distance += new Vector2((float)(MovementSpeed * Speed), 0);
                    break;
                case "Up":
                    if (Distance.Y < TargetDistance.Y)
                        FinishMovingPlayer();
                    else
                        Distance -= new Vector2(0, (float)(MovementSpeed * Speed));
                    break;
                case "Down":
                    if (Distance.Y > TargetDistance.Y)
                        FinishMovingPlayer();
                    else
                        Distance += new Vector2(0, (float)(MovementSpeed * Speed));
                    break;
            }
        }
        private static void FinishMovingPlayer() {
            for(int i = 0; i < AnimatedPlayers.Count; i++) {
                switch (CurrentOperation.Item3) {
                    case "Left":
                        GamePlayers[AnimatedPlayers[i]].Position += new Vector2(-1, 0);
                        break;
                    case "Right":
                        GamePlayers[AnimatedPlayers[i]].Position += new Vector2(1, 0);
                        break;
                    case "Up":
                        GamePlayers[AnimatedPlayers[i]].Position += new Vector2(0, -1);
                        break;
                    case "Down":
                        GamePlayers[AnimatedPlayers[i]].Position += new Vector2(0, 1);
                        break;
                }
            }
            AnimationDone();
        }
        #endregion

        #region Hallshifting animation members
        private static void SetShiftingData() {
            Game1.RowsShifted++;
            string[] ShiftingData = CurrentOperation.Item2.Split('-');
            if (ShiftingData.Length > 1) {
                AnimationTarget = "Block+Player";
                int[] PlayerTargets = ShiftingData[1].Split(':').Select(x => int.Parse(x)).ToArray();
                for (int i = 0; i < PlayerTargets.Length; i++)
                    AnimatedPlayers.Add(PlayerTargets[i]);
            }
            else
                AnimationTarget = "Block";
            // Contains : RowIndex, ReserveIndex, Orientation
            int[] LocationData = ShiftingData[0].Split(',').Select(x => int.Parse(x)).ToArray();

            switch (CurrentOperation.Item3) {
                case "Left":
                    for (int i = 0; i < GameBoard.GetLength(0); i++)
                        AnimatedBlocks.Add(new Vector2(i, LocationData[0]));
                    TargetDistance = new Vector2(-(Game1.TextureSize.X * Scale), 0);
                    break;
                case "Right":
                    for (int i = 0; i < GameBoard.GetLength(0); i++)
                        AnimatedBlocks.Add(new Vector2(i, LocationData[0]));
                    TargetDistance = new Vector2((Game1.TextureSize.X * Scale), 0);
                    break;
                case "Up":
                    for (int i = 0; i < GameBoard.GetLength(0); i++)
                        AnimatedBlocks.Add(new Vector2(LocationData[0], i));
                    TargetDistance = new Vector2(0, -(Game1.TextureSize.Y * Scale));
                    break;
                case "Down":
                    for (int i = 0; i < GameBoard.GetLength(0); i++)
                        AnimatedBlocks.Add(new Vector2(LocationData[0], i));
                    TargetDistance = new Vector2(0, (Game1.TextureSize.Y * Scale));
                    break;
            }
        }
        private static void ShiftHall(double Speed, float Scale) {
            switch (CurrentOperation.Item3) {
                case "Left":
                    if (Distance.X < TargetDistance.X)
                        FinishShiftingHall();
                    else
                        Distance.X -= (float)(MovementSpeed * Speed);
                    break;
                case "Right":
                    if (Distance.X > TargetDistance.X)
                        FinishShiftingHall();
                    else
                        Distance.X += (float)(MovementSpeed * Speed);
                    break;
                case "Up":
                    if (Distance.Y < TargetDistance.Y)
                        FinishShiftingHall();
                    else
                        Distance.Y -= (float)(MovementSpeed * Speed);
                    break;
                case "Down":
                    if (Distance.Y > TargetDistance.Y)
                        FinishShiftingHall();
                    else
                        Distance.Y += (float)(MovementSpeed * Speed);
                    break;
            }
        }
        private static void FinishShiftingHall() {
            string[] ShiftingData = CurrentOperation.Item2.Split('-');
            // Contains : RowIndex, ReserveIndex, Orientation
            int[] LocationData = ShiftingData[0].Split(',').Select(x => int.Parse(x)).ToArray();

            LabyrinthBlock Reserve = GameReserves[LocationData[1]];
            Reserve.Orientation = LocationData[2];

            switch (CurrentOperation.Item3) {
                case "Left":
                    // Setting Labyrinth block positions
                    GameReserves[LocationData[1]] = GameBoard[LocationData[0], 0];
                    GameReserves[LocationData[1]].Orientation = 0;
                    for (int i = 0; i < GameBoard.GetLength(1) - 1; i++)
                        GameBoard[LocationData[0], i] = GameBoard[LocationData[0], i + 1];
                    GameBoard[LocationData[0], GameBoard.GetLength(1) - 1] = Reserve;
                    // Setting player positions
                    for (int i = 0; i < AnimatedPlayers.Count; i++)
                        GamePlayers[AnimatedPlayers[i]].Position.X--;
                    break;
                case "Right":
                    // Setting Labyrinth block positions
                    GameReserves[LocationData[1]] = GameBoard[LocationData[0], GameBoard.GetLength(1) - 1];
                    GameReserves[LocationData[1]].Orientation = 0;
                    for (int i = GameBoard.GetLength(1) - 1; i > 0; i--)
                        GameBoard[LocationData[0], i] = GameBoard[LocationData[0], i - 1];
                    GameBoard[LocationData[0], 0] = Reserve;
                    // Setting player positions
                    for (int i = 0; i < AnimatedPlayers.Count; i++)
                        GamePlayers[AnimatedPlayers[i]].Position.X++;
                    break;
                case "Up":
                    // Setting Labyrinth block positions
                    GameReserves[LocationData[1]] = GameBoard[0, LocationData[0]];
                    GameReserves[LocationData[1]].Orientation = 0;
                    for (int i =0; i <  GameBoard.GetLength(0) - 1; i++)
                        GameBoard[i, LocationData[0]] = GameBoard[i + 1, LocationData[0]];
                    GameBoard[GameBoard.GetLength(1) - 1, LocationData[0]] = Reserve;
                    // Setting player positions
                    for (int i = 0; i < AnimatedPlayers.Count; i++)
                        GamePlayers[AnimatedPlayers[i]].Position.Y--;
                    break;
                case "Down":
                    // Setting Labyrinth block positions
                    GameReserves[LocationData[1]] = GameBoard[GameBoard.GetLength(0) - 1, LocationData[0]];
                    GameReserves[LocationData[1]].Orientation = 0;
                    for (int i = GameBoard.GetLength(0) - 1; i > 0 ; i--)
                        GameBoard[i, LocationData[0]] = GameBoard[i - 1, LocationData[0]];
                    GameBoard[0, LocationData[0]] = Reserve;
                    // Setting player positions
                    for (int i = 0; i < AnimatedPlayers.Count; i++)
                        GamePlayers[AnimatedPlayers[i]].Position.Y++;
                    break;
            }
            AnimationDone();
        }
        #endregion

        #region Rotation animation members
        private static void SetRotationData() {
            Game1.BlocksRotated++;
            AnimationTarget = "Block";
            string[] TargetBlock = CurrentOperation.Item2.Split(',');
            Vector2 BlockPosition = new Vector2(int.Parse(TargetBlock[0]), int.Parse(TargetBlock[1]));
            LabyrinthBlock Target = GameBoard[(int)BlockPosition.Y, (int)BlockPosition.X];
            AnimatedBlocks.Add(BlockPosition);
            if (CurrentOperation.Item3 == "Right")
                TargetRotation = 1;
            else
                TargetRotation = -1;

        }
        private static void RotateBlock(double Speed, float Scale) {
            switch (CurrentOperation.Item3) {
                case "Right":
                    if (Rotation > TargetRotation)
                        FinishRotation();
                    else
                        Rotation += (float)(RotationSpeed * Speed);
                    break;
                case "Left":
                    if (Rotation < TargetRotation)
                        FinishRotation();
                    else
                        Rotation -= (float)(RotationSpeed * Speed);
                    break;
            }
        }
        private static void FinishRotation() {
            for (int i = 0; i < AnimatedBlocks.Count; i++) {
                var Pos = AnimatedBlocks[i];
                GameBoard[(int)Pos.Y, (int)Pos.X].Orientation += (int)TargetRotation;
            }
            AnimationDone();
        }
        #endregion

        public static void AnimationDone() {
            // Reset all variables.
            Distance = Vector2.Zero;
            Rotation = 0;
            TargetDistance = Vector2.Zero;
            TargetRotation = 0;
            AnimationTarget = string.Empty;
            AnimatedBlocks = new List<Vector2>();
            AnimatedPlayers = new List<int>();

            IsAnimating = false;
        }


    }
}
