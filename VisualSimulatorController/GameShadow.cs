using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using SimulatorDelegate;
using SimulatorDelegate.Entities;
using VisualSimulator;
using System.Linq;

namespace VisualSimulatorController {

    // To-Do:
    // Boolean : Visually represent the simulation?
    // If true: How many games should be visually simulated?

    public class GameShadow {
        // Console Color Guide :
        // Green :      Controller related output.
        // Yellow :     Game back end related output.
        // Magenta :    Variable information ( Non critical )
        // Red :        Errors ( Critical ) && Critical information

        public static Random rnd = new Random();    // Global randomizer.

        public static void Main(string[] args) {
            //var log = new Logging.CsvLogger("CSVTest");
            //log.CreateTemplate(new[] { 7, 20, 20, 9, 5 }, 500, 15, 50, 15, new[] { "Purple", "Blue", "Red", "Orange" }, new[] { "Daniel", "Hannah", "Bert", "Thom" });

            //for (int i = 0; i < 15; i++)
                //log.LogGameData(50, 15, 20, 22, 15, 51);
            
            //return;

            Console.Title = "A-Maze-ing simulator";
            HandleInput.PrintColor("Controller started", ConsoleColor.Green);

            Commands Coms = new Commands();

            HandleInput.PrintColor("Board created.", ConsoleColor.Yellow);

            var BoardData = Coms.CreateBoardData();
            var PlayerData = Coms.CreatePlayerData();
            var SimulationData = Coms.CreateSimulationData();
            
            var Phase = new GamePhase(Coms, BoardData[0], BoardData[1], BoardData[2], BoardData[3], BoardData[4], PlayerData.Item1, PlayerData.Item2, PlayerData.Item3, 
                SimulationData[2], SimulationData[0], SimulationData[1], SimulationData[3]);

            Phase.RunPhase();

            HandleInput.PrintColor("Log file has been created. Simulation run completed.", ConsoleColor.Green);
            Console.ReadLine();
        }
    }

    public class Commands :IRelayCommand {
        public Game1 Game;
        public bool GameRunning = false;

        public void StartGame() {
            Thread thread = new Thread(
                new ThreadStart(
                    delegate {
                        using (Game = new Game1(this))
                            Game.Run();
                    }));
            thread.Start();
            while (!GameRunning) {
                Thread.Sleep(250);
            }
            CommandHandler Handler = new CommandHandler(this);
            Handler.WaitForCommand();
        }

        public int[] CreateBoardData() {
            int Size = HandleInput.ReadLine<int>((x => char.IsDigit(x)), "Give an integer with the board dimensions (default '7') : ", 
                new Predicate<int>[] {
                    (y => y % 2 != 0),
                    (y => y >= 5) }, 
                new[] {
                "The board dimensions have to be uneven.",
                "The board dimensions should be atleast 5x5."}, true, true, 7);
            HandleInput.PrintColor(string.Format("The board dimensions have been set to {0} by {0}.", Size), ConsoleColor.Yellow);

            int Reserves = HandleInput.ReadLine<int>((x => char.IsDigit(x)), "Give an integer with the amount of reserve blocks (default '5') : ", (y => y > 0), "There should be atleast 1 reserve block!", true, true, 5);
            HandleInput.PrintColor(string.Format("The amount of reserve blocks has been set to {0}.\n", Reserves), ConsoleColor.Yellow);

            int RemainingBlocks = Size * Size + Reserves - 5;   // 5 Locked pieces ( Corners + Chest )
            string[] types = { "straight", "corner", "T-split" };
            int[] amounts = new int[3];
            for (int i = 0; i < 3; i++) {
                HandleInput.PrintColor(RemainingBlocks.ToString() + " Blocks remaining for assignment.", ConsoleColor.Magenta);
                amounts[i] = HandleInput.ReadLine<int>((x => char.IsDigit(x)), string.Format("Give an integer with the amount of {0} blocks : ", types[i]));
                RemainingBlocks -= amounts[i];
            }
            return new[] { Size, amounts[0], amounts[1], amounts[2], Reserves };
        }
        public Tuple<Color[], string[], string[]> CreatePlayerData() {
            int Amount = HandleInput.ReadLine<int>((x => char.IsDigit(x)), "How many players will be playing the game (default '4') : ",
                (x => x > 0 && x < 5), "The amount of players is limited to 1 to 4 players.", true, true, 4);
            HandleInput.PrintColor(string.Format("The amount of players has been set to {0}.", Amount), ConsoleColor.Yellow);

            Color[] PlayerColors = new Color[Amount];
            string[] PlayerColorNames = new string[Amount];
            string[] PlayerNames = new string[Amount];

            string[] Defaults = new[] { "Purple", "Blue", "Red", "Orange" };

            for(int i = 0; i < Amount; i++) {
                HandleInput.PrintColor("Creating player #" + (i + 1).ToString(), ConsoleColor.Magenta);
                PlayerNames[i] = HandleInput.ReadLine<string>((x => char.IsLetter(x)), string.Format("What is the player's name (default 'Player {0}') : ", i + 1),
                    (c => c.Length > 0 && c.Length <= 12), "Player name length should be between 1 and 12 characters.", true, true, "Player " + (i + 1));
                PlayerColorNames[i] = HandleInput.ReadLine<string>((x => char.IsLetter(x)), string.Format("What should the player's colour be (default '{0}') ? : ", Defaults[i]),
                    (c => ConvertColor(c) != null), "An invalid color value has been giving.", false, true, Defaults[i]);
                PlayerColors[i] = (Color)ConvertColor(PlayerColorNames[i]);
            }
            return Tuple.Create(PlayerColors, PlayerColorNames, PlayerNames);
        }
        public int[] CreateSimulationData() {
            int Runs = HandleInput.ReadLine<int>((x => char.IsDigit(x)), "How many game should be run in the simulation (default '500') : ",
                (c => c > 0), "Atleast 1 simulation should be run.", true, true, 500);
            HandleInput.PrintColor(string.Format("The simulator will run '{0}' simulations.", Runs), ConsoleColor.Yellow);
            int VisualRun = HandleInput.ReadLine<int>((x => char.IsDigit(x)), "How many games should be visualized ? : ",
                (c => c <= Runs), "There can't be more visual simulations than actual simulations.", true);
            HandleInput.PrintColor(string.Format("The simulator will visualize {0} simulations.", VisualRun), ConsoleColor.Yellow);
            int Chance = HandleInput.ReadLine<int>(x => char.IsDigit(x), "What is the percentage chance a player answers correctly (default '50') ? : ",
                (c => c > 0 && c <= 100), "The chance should be between 1% and 100%.", true, true, 50);
            HandleInput.PrintColor(string.Format("The chance to answer correctly has been set to {0}.", Chance), ConsoleColor.Yellow);
            int TurnTime = HandleInput.ReadLine<int>((x => char.IsDigit(x)), "How long does a turn take in seconds (default '15') ? : ",
                (c => c > 0), "A turn should take atleast 1 second.", true, true, 15);
            HandleInput.PrintColor(string.Format("The time a turn takes has been set to {0}.", TurnTime), ConsoleColor.Yellow);
            return new[] { Runs, VisualRun, Chance, TurnTime };
        }

        public void UpdateSimulationData(object Boards, object Reserves, object Moves, object Turns, string[] Players, string[] PlayerNames) {
            while (!GameRunning)
                Thread.Sleep(250);

            // Once game  has launched. Update simulation data.
            Game.UpdateSimulationData(Boards, Reserves, Moves, Turns, Players, PlayerNames);
        }
        public void UpdateAnimationSpeed(float Speed) {
            Game.UpdateAnimationSpeed(Speed);
        }
        public void UpdateGameInterval(int Seconds) {
            Game.UpdateIntervalTime(Seconds);
        }
        public void UpdateScale(float Scale) {
            Game.UpdateScale(Scale);
        }
        public void UpdateGameSize(int Width, int Height) {
            Game.UpdateGameSize(Width, Height);
        }
        public void SkipSimulation() {
            Game.SkipSimulation();
        }
        public void Pause() {
            Game.Pause();
        }
        public void UnPause() {
            Game.UnPause();
        }

        private Color? ConvertColor(string ColorName) {
            var conv = typeof(Color).GetProperty(ColorName);
            return (conv == null) ? null : (Color?)conv.GetValue(null, null);
        }

        #region IExecute implementation members
        public void NotifyGameRunning() {
            this.GameRunning = true;
        }

        public void Quit() {
            Environment.Exit(0);
        }
        #endregion
    }

    
}
