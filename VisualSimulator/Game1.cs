using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulatorDelegate;
using SimulatorDelegate.Entities;

namespace VisualSimulator {

    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private bool _pause = false;
        internal IRelayCommand MainProcess;

        private Dictionary<BlockType, Texture2D> Textures;
        private Dictionary<Color, Texture2D> PlayerTextures;
        private Texture2D BaseTexture;
        private Texture2D CastleBase;

        internal static readonly Vector2 TextureSize = new Vector2(200, 200);     // Global TextureSize constant
        private readonly Rectangle Source = new Rectangle(0, 0, 200, 200);
        private Vector2 Origin = new Vector2(100, 100);     // Origin is the center of the texture. (Half the size)
        private Vector2 PlayerOrigin = new Vector2(24f, 24f);
        private float Scale = .5f;

        // History Queues
        private Queue<int> TurnCountHistory;
        private Queue<LabyrinthBlock[,]> BoardHistory;
        private Queue<LabyrinthBlock[]> ReserveHistory;
        private Queue<List<Tuple<string, string, string>>> MoveHistory;
        private string[] PlayerHistory;
        private string[] PlayerNameHistory;

        // Current game data
        private LabyrinthBlock[,] CurrentBoard;
        private LabyrinthBlock[] CurrentReserves;
        private static Player[] CurrentPlayers;
        private bool GameIsRunning;
        private static int _playerIndex;

        public static int CurrentPlayerIndex {
            get { return _playerIndex; }
            set {if (CurrentPlayers == null)
                    _playerIndex = value;
                else if (value >= CurrentPlayers.Length)
                    _playerIndex = 0;
                else
                    _playerIndex = value;
            }
        }
        
        // Current game log data
        SpriteFont Font;
        internal static int PawnsMoved;
        internal static int RowsShifted;
        internal static int BlocksRotated;
        internal static int CurrentTurn;

        // Next game data
        private int GameInterval = 3;   // Set in controller
        private double LastGameEnd;
        private int NextGameIn;

        public Game1(IRelayCommand MainProcess) {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            if (MainProcess == null)
                throw new NullReferenceException("Game was not called by the controller");

            this.MainProcess = MainProcess;
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize() {
            // TODO: Add your initialization logic here
            graphics.SynchronizeWithVerticalRetrace = false;    // Disable V-Sync
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            graphics.ApplyChanges();


            base.Initialize();

        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent() {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            Textures = new Dictionary<BlockType, Texture2D>() {
                { BlockType.Straight, Content.Load<Texture2D>("BlockTextures/Straight") },
                { BlockType.Corner , Content.Load<Texture2D>("BlockTextures/Corner") },
                { BlockType.TSplit , Content.Load<Texture2D>("BlockTextures/TSplit") },
                { BlockType.Chest , Content.Load<Texture2D>("BlockTextures/Castle") }
            };
            PlayerTextures = new Dictionary<Color, Texture2D>();
            BaseTexture = Content.Load<Texture2D>("BlockTextures/Base");
            CastleBase = Content.Load<Texture2D>("BlockTextures/CastleBase");

            this.Font = Content.Load<SpriteFont>("MainFont");
            // All content loaded. Notify that game is running to main thread.
            MainProcess.NotifyGameRunning();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent() {
            // TODO: Unload any non ContentManager content here
            // Quit parent application
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime) {
            if (_pause) 
                return;
            
            if (GraphicsDevice.Viewport.Width != graphics.PreferredBackBufferWidth || GraphicsDevice.Viewport.Height != graphics.PreferredBackBufferHeight)
                graphics.ApplyChanges();
            if (!GameIsRunning && !Animations.IsAnimating) {
                if(CurrentBoard != null) {
                    TurnCountHistory.Dequeue();
                    MoveHistory.Dequeue();
                    BoardHistory.Dequeue();
                    ReserveHistory.Dequeue();
                }
                if (BoardHistory != null && BoardHistory.Count > 0) {
                    GameIsRunning = true;
                    // Reset variables
                    LastGameEnd = 0;
                    PawnsMoved = 0;
                    RowsShifted = 0;
                    BlocksRotated = 0;
                    CurrentPlayerIndex = 0;
                    CurrentTurn = 0;

                    CurrentBoard = BoardHistory.Peek();
                    CurrentReserves = ReserveHistory.Peek();
                    CurrentPlayers = GetPlayers();
                    float Offset = (float)Origin.X * Scale;
                    Animations.Animate(MoveHistory.Peek()[0], CurrentBoard, CurrentReserves, CurrentPlayers, Scale, Offset);
                    
                }
                else if (BoardHistory != null) {
                    this.Exit();
                }
            }
            else if(GameIsRunning && !Animations.IsAnimating) {
                if(CurrentBoard != null) {
                    var CurrentMoves = MoveHistory.Peek();
                    if (CurrentMoves.Count > 0)
                        CurrentMoves.RemoveAt(0);
                    if (CurrentMoves.Count > 0) {
                        float Offset = (float)Origin.X * Scale;
                        Animations.Animate(CurrentMoves[0], CurrentBoard, CurrentReserves, CurrentPlayers, Scale, Offset);
                    }
                    else if (LastGameEnd == 0)  // Game ended. Wait for interval
                        LastGameEnd = gameTime.TotalGameTime.TotalMilliseconds;
                    else if (LastGameEnd + (GameInterval * 1000d) < gameTime.TotalGameTime.TotalMilliseconds) {   // Queue next game
                        if (BoardHistory.Count > 0)
                            GameIsRunning = false;
                        else
                            this.Exit();
                    }
                }

            }
            if (LastGameEnd != 0)
                NextGameIn = (int)Math.Round((LastGameEnd + (GameInterval * 1000d) - gameTime.TotalGameTime.TotalMilliseconds) / 1000, MidpointRounding.AwayFromZero);

            if (CurrentBoard == null || CurrentPlayers == null || CurrentReserves == null)
                SuppressDraw();

            Animations.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) {
            //GraphicsDevice.Clear(Color.Ivory);
            GraphicsDevice.Clear(Color.BlanchedAlmond);
            spriteBatch.Begin();
            float Offset = (float)Origin.X * Scale;

            // Drawing all labyrinth blocks
            for (int y = 0; y < CurrentBoard.GetLength(0); y++)
                for (int x = 0; x < CurrentBoard.GetLength(1); x++) {
                    BlockType Type = CurrentBoard[y, x].Type;
                    float Rotation = (float)(CurrentBoard[y, x].Orientation);
                    Vector2 Position = new Vector2(x * TextureSize.X * Scale + Offset, y * TextureSize.Y * Scale + Offset);
                    if (Animations.AnimationTarget == "Block" || Animations.AnimationTarget == "Block+Player")
                        for (int i = 0; i < Animations.AnimatedBlocks.Count; i++)
                            if (Animations.AnimatedBlocks[i].X == x && Animations.AnimatedBlocks[i].Y == y) {
                                Position += Animations.Distance;
                                Rotation += Animations.Rotation;
                            }
                    Rotation *= (float)(Math.PI / 2);
                    if(Type == BlockType.Chest)
                        spriteBatch.Draw(CastleBase, Position, Source, Color.White, 0f, Origin, Scale, SpriteEffects.None, 0);
                    else
                        spriteBatch.Draw(BaseTexture, Position, Source, Color.White, 0f, Origin, Scale, SpriteEffects.None, 0);
                    spriteBatch.Draw(Textures[Type], Position, Source, Color.White, Rotation, Origin, Scale, SpriteEffects.None, 0);

                }
            // Drawing all reserve blocks
            // To-Do. Limit Y-axis to board length
            for (int y = 0; y < CurrentReserves.Length; y++) {
                BlockType Type = CurrentReserves[y].Type;
                float ReserveOffset = 1.5f * TextureSize.X * Scale;
                var Position = new Vector2(CurrentBoard.GetLength(1) * TextureSize.X * Scale + ReserveOffset + Offset, y * TextureSize.Y * Scale + Offset);
                spriteBatch.Draw(BaseTexture, Position, Source, Color.White, 0f, Origin, Scale, SpriteEffects.None, 0);
                spriteBatch.Draw(Textures[Type], Position, Source, Color.White, 0f, Origin, Scale, SpriteEffects.None, 0);
            }
            // Drawing all players
            for (int i = 0; i < CurrentPlayers.Length; i++) {
                Player current = CurrentPlayers[i];
                Vector2 Position = new Vector2(current.Position.X * TextureSize.X * Scale + Offset, current.Position.Y * TextureSize.Y * Scale + Offset);
                if (Animations.AnimationTarget == "Player" || Animations.AnimationTarget == "Block+Player")
                    for (int x = 0; x < Animations.AnimatedPlayers.Count; x++)
                        if (i == Animations.AnimatedPlayers[x])
                            Position += Animations.Distance;
                var Texture = PlayerTextures[current.PawnColor];
                int samePosition = 0;
                for (int x = 0; x < i; x++)
                    if (CurrentPlayers[x].Position == current.Position)
                        samePosition++;


                switch (samePosition) {
                    case 0:
                        spriteBatch.Draw(Texture, Position, null, Color.White, 0, PlayerOrigin, Scale, SpriteEffects.None, 0);
                        break;
                    case 1:
                        spriteBatch.Draw(Texture, Position, new Rectangle(0, 0, Texture.Width, Texture.Height / 2), Color.White, 0, PlayerOrigin, Scale, SpriteEffects.None, 0);
                        break;
                    case 2:
                        spriteBatch.Draw(Texture, Position, new Rectangle(0, 0, Texture.Width / 2, Texture.Height / 2), Color.White, 0, PlayerOrigin, Scale, SpriteEffects.None, 0);
                        break;
                    case 3:
                        spriteBatch.Draw(Texture, Position, new Rectangle(Texture.Width / 2, 0, Texture.Width / 2, Texture.Height / 2), Color.White, 0, PlayerOrigin, Scale, SpriteEffects.None, 0);
                        break;
                }
            }
            Animations.Draw(spriteBatch,BaseTexture, Textures, Offset, Source, Origin);

            // Game information string formatting
            string length = string.Format("Total turns : {0}", TurnCountHistory.Peek());
            string currTurn = string.Format("Current turn : {0}", CurrentTurn);
            string Pawns = string.Format("Pawns moved : {0}", PawnsMoved);
            string Shifts = string.Format("Halls shifted : {0}", RowsShifted);
            string Rotates = string.Format("Blocks rotated : {0}", BlocksRotated);
            string currPlayer = PlayerNameHistory[CurrentPlayerIndex];
            float textOffset = 25;


            // Game information string drawing
            spriteBatch.DrawString(Font, length, new Vector2(GraphicsDevice.Viewport.Width - Font.MeasureString(length).X - textOffset, 0 + textOffset), Color.Black);
            spriteBatch.DrawString(Font, currTurn, new Vector2(GraphicsDevice.Viewport.Width - Font.MeasureString(currTurn).X - textOffset, 50 + textOffset), Color.Black);
            spriteBatch.DrawString(Font, Pawns, new Vector2(GraphicsDevice.Viewport.Width - Font.MeasureString(Pawns).X - textOffset, 100 + textOffset), Color.Black);
            spriteBatch.DrawString(Font, Shifts, new Vector2(GraphicsDevice.Viewport.Width - Font.MeasureString(Shifts).X - textOffset, 150 + textOffset), Color.Black);
            spriteBatch.DrawString(Font, Rotates, new Vector2(GraphicsDevice.Viewport.Width - Font.MeasureString(Rotates).X - textOffset, 200 + textOffset), Color.Black);

            float FontWidth = Font.MeasureString("Current player").X;
            spriteBatch.DrawString(Font, "Current player", new Vector2(GraphicsDevice.Viewport.Width - FontWidth - textOffset - 25, 250 + textOffset), Color.Black);
            var playerTextPos = new Vector2(GraphicsDevice.Viewport.Width - FontWidth + ((FontWidth / 2) - Font.MeasureString(currPlayer).X), 285 + textOffset);
            spriteBatch.DrawString(Font, currPlayer, playerTextPos, Color.Black);

            var playerTex = PlayerTextures[CurrentPlayers[CurrentPlayerIndex].PawnColor];
            var pos = new Vector2(GraphicsDevice.Viewport.Width - playerTex.Width - (FontWidth / 2), 345 + textOffset);
            spriteBatch.Draw(playerTex, pos, null, Color.White, 0, PlayerOrigin, Scale, SpriteEffects.None, 0);

            if (LastGameEnd != 0) {
                string GameEnd = string.Format("Game has ended the next game will start in {0} seconds", NextGameIn);
                spriteBatch.DrawString(Font, GameEnd, new Vector2((GraphicsDevice.Viewport.Width / 2) - (Font.MeasureString(GameEnd).X / 2), GraphicsDevice.Viewport.Height - Font.MeasureString(GameEnd).Y - textOffset), Color.Black);
            }
            spriteBatch.End();
            base.Draw(gameTime);
        }


        #region RelayCommand Intermediary
        public void Pause() {
            this._pause = true;
        }
        public void UnPause() {
            this._pause = false;
        }
        public void UpdateSimulationData(object Boards, object Reserves, object Moves, object Turns, string[] Players, string[] PlayerNames) {
            this.TurnCountHistory = (Queue<int>)Turns;
            this.BoardHistory = (Queue<LabyrinthBlock[,]>)Boards;
            this.ReserveHistory = (Queue<LabyrinthBlock[]>)Reserves;
            this.MoveHistory = (Queue<List<Tuple<string, string, string>>>)Moves;
            this.PlayerHistory = Players;
            this.PlayerNameHistory = PlayerNames;
            UpdatePlayerTextures();            
        }
        public void UpdateScale(float Scale) {
            this.Scale = Scale;
        }
        public void UpdateAnimationSpeed(float Speed) {
            Animations.RotationSpeed = (0.01f * Speed);
            Animations.MovementSpeed = Speed;
        }
        public void UpdateIntervalTime(int Seconds) {
            GameInterval = Seconds;
        }
        public void UpdateGameSize(int Width, int Height) {
            graphics.PreferredBackBufferWidth = Width;
            graphics.PreferredBackBufferHeight = Height;
        }
        public void SkipSimulation() {
            Animations.AnimationDone();
            GameIsRunning = false;
        }

        #endregion

        #region Player Creation
        private void UpdatePlayerTextures() {
            for(int i = 0; i < PlayerHistory.Length; i++) {
                Color col = ConvertColor(PlayerHistory[i]);
                if (!PlayerTextures.ContainsKey(col))
                    PlayerTextures.Add(col, CreateCircleTexture(col));
            }
        }
        private Player[] GetPlayers() {
            var toReturn = new Player[PlayerHistory.Length];
            for(int i = 0; i < toReturn.Length; i++) {
                Vector2 Position;
                switch (i) {
                    case 0:
                        Position = Vector2.Zero;
                        break;
                    case 1:
                        Position = new Vector2(CurrentBoard.GetLength(0) - 1, 0);
                        break;
                    case 2:
                        Position = new Vector2(CurrentBoard.GetLength(0) - 1);
                        break;
                    case 3:
                        Position = new Vector2(0, CurrentBoard.GetLength(0) - 1);
                        break;
                    default: throw new IndexOutOfRangeException("Not more than 4 players can exist.");
                }
                toReturn[i] = new Player(ConvertColor(PlayerHistory[i]), Position);
            }
            return toReturn;
        }
        private Color ConvertColor(string ColorName) {
            var conv = typeof(Color).GetProperty(ColorName);
            return (Color)conv.GetValue(null, null);
        }
        private Texture2D CreateCircleTexture(Color Pawn) {
            int Radius = 50;

            Color[] Data = new Color[Radius * Radius];

            for (int y = 0; y < Radius; y++)
                for (int x = 0; x < Radius; x++) {
                    int index = y * Radius + x;
                    if (InRange(x, y, Radius))
                        Data[index] = Pawn;
                    else
                        Data[index] = Color.Transparent;
                }
            Texture2D PawnTexture = new Texture2D(GraphicsDevice, Radius, Radius);
            PawnTexture.SetData(Data);
            return PawnTexture;
        }

        private bool InRange(int x, int y, int Radius) {
            x -= Radius / 2;
            y -= Radius / 2;
            float temp = (float)Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));
            return temp < Radius / 2;

        }
        #endregion

    }
}
