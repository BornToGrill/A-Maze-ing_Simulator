using System;
using System.Linq;
using Microsoft.Xna.Framework;

namespace VisualSimulatorController.Game_Logic {

    static class GameCreation {

        public static int[] CreateBoardData() {
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

            int MaxBlocks = Size * Size + Reserves - 5;
            if (amounts.Sum() != MaxBlocks)
                amounts = FixBlockCount(amounts, MaxBlocks);

            return new[] { Size, amounts[0], amounts[1], amounts[2], Reserves };
        }
        public static Tuple<Color[], string[], string[]> CreatePlayerData() {
            int Amount = HandleInput.ReadLine<int>((x => char.IsDigit(x)), "How many players will be playing the game (default '4') : ",
                (x => x > 0 && x < 5), "The amount of players is limited to 1 to 4 players.", true, true, 4);
            HandleInput.PrintColor(string.Format("The amount of players has been set to {0}.", Amount), ConsoleColor.Yellow);

            Color[] PlayerColors = new Color[Amount];
            string[] PlayerColorNames = new string[Amount];
            string[] PlayerNames = new string[Amount];

            string[] Defaults = new[] { "Purple", "Blue", "Red", "Orange" };

            for (int i = 0; i < Amount; i++) {
                HandleInput.PrintColor("Creating player #" + (i + 1).ToString(), ConsoleColor.Magenta);
                PlayerNames[i] = HandleInput.ReadLine<string>((x => char.IsLetter(x)), string.Format("What is the player's name (default 'Player {0}') : ", i + 1),
                    (c => c.Length > 0 && c.Length <= 12), "Player name length should be between 1 and 12 characters.", true, true, "Player " + (i + 1));
                PlayerColorNames[i] = HandleInput.ReadLine<string>((x => char.IsLetter(x)), string.Format("What should the player's colour be (default '{0}') ? : ", Defaults[i]),
                    (c => GlobalMethods.GetColor(c) != null), "An invalid color value has been giving.", false, true, Defaults[i]);
                PlayerColors[i] = (Color)GlobalMethods.GetColor(PlayerColorNames[i]);
            }
            return Tuple.Create(PlayerColors, PlayerColorNames, PlayerNames);
        }
        public static int[] CreateSimulationData() {
            int Runs = HandleInput.ReadLine<int>((x => char.IsDigit(x)), "How many game should be run in the simulation (default '50.000') : ",
                (c => c > 0), "Atleast 1 simulation should be run.", true, true, 50000);
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

        #region Data Validation and converters

        private static int[] FixBlockCount(int[] Blocks, int TotalBlocks) {
            // Fixing block count mismatch.

            HandleInput.PrintColor("\n" + "Block count mismatch, automatically fixing block occurrence.", ConsoleColor.Red);

            float[] TempBlocks = Array.ConvertAll(Blocks, (x => (float)x));

            float max = TotalBlocks;

            float total = TempBlocks.Sum();

            if(total == 0) {
                total = Blocks.Length;
                for (int i = 0; i < TempBlocks.Length; i++)
                    TempBlocks[i] = 1;
            }

            // Calculate percentage of block occurrence
            for (int i = 0; i < TempBlocks.Length; i++)
                TempBlocks[i] /= total;

            // Set block counts to percentage of maximum
            for (int i = 0; i < TempBlocks.Length; i++)
                TempBlocks[i] *= max;

            // Calculate remainders and add it to TSplit blocks.
            int modul = (int)Math.Round((TempBlocks[0] % 1) + (TempBlocks[1] % 1) + (TempBlocks[2] % 1), MidpointRounding.AwayFromZero);
            TempBlocks[2] += modul;

            // Round all block counts to integers
            for (int i = 0; i < TempBlocks.Length; i++)
                TempBlocks[i] = (int)Math.Floor(TempBlocks[i]);

            // Replace the original list with the fix values.
            HandleInput.PrintColor(string.Format("Block count has been adjusted new count is as follows :\nStraight Blocks : {0}\nCorner Blocks : {1}\nT-Split Blocks : {2}", TempBlocks.Select(x => (object)x).ToArray()), ConsoleColor.Yellow);
            return Array.ConvertAll(TempBlocks, (x => (int)x));
        }
        #endregion
    }
}
