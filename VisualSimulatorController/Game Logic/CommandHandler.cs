using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.Xna.Framework;

namespace VisualSimulatorController.Game_Logic {
    internal class CommandHandler {

        private Commands Coms;
        private Dictionary<string, Action<string[]>> AvailableCommands;
        private Dictionary<string, Action<string[]>> CommandSynonyms;

        public CommandHandler(Commands DelegateClass) {
            this.Coms = DelegateClass;
            AvailableCommands = new Dictionary<string, Action<string[]>>(StringComparer.OrdinalIgnoreCase) {
                { "?", ShowCommands },
                { "Resize", Resize },
                { "Scale", Scale },
                { "AnimationSpeed", AnimationSpeed },
                { "Interval", Interval },
                { "SkipSimulation", SkipSimulation },
                { "Pause", Pause },
                { "UnPause", UnPause },
                { "Background", BackgroundColor }
            };
            CommandSynonyms = new Dictionary<string, Action<string[]>>(StringComparer.OrdinalIgnoreCase) {
                { "simulationspeed", AnimationSpeed },
                { "skip", SkipSimulation }
            };
        }
        public void WaitForCommand() {
            Thread thrd = new Thread(new ThreadStart(
                delegate {
                    while (true) {
                        var str = Console.ReadLine().ToLower();
                        if (!string.IsNullOrWhiteSpace(str) && Coms.Game != null) {
                            Action<string[]> command;
                            string[] Parameters = str.Split();
                            if (AvailableCommands.TryGetValue(str, out command) ||
                                CommandSynonyms.TryGetValue(str, out command))
                                command.Invoke(Parameters);
                        }
                        else {
                            HandleInput.SetCursorPosition(0, Console.CursorTop - 1);
                        }
                    }
                }));
            thrd.CurrentCulture = CultureInfo.InvariantCulture;
            thrd.Start();
        }
        internal void ForceOutputCommand(string command, string[] Parameters = null) {
            AvailableCommands[command].Invoke(Parameters);
        }

        #region Console Commands
        private void ShowCommands(string[] Parameters) {
            HandleInput.PrintColor("The following commands are available :", ConsoleColor.Magenta);
            HandleInput.PrintColor(string.Join("\n", AvailableCommands.Keys), ConsoleColor.Green);
        }
        private void Resize(string[] Parameters) {
            int Width = HandleInput.ReadLine<int>((x => char.IsDigit(x)), "What will the width be (default '1280') ? : ", (c => c > 0), "Width has to be more than 0", false, true, 1280);
            int Height = HandleInput.ReadLine<int>((x => char.IsDigit(x)), "What will the height be (default '720') ? : ", (c => c > 0), "Height has to be more than 0", false, true, 720);
            Coms.UpdateGameSize(Width, Height);
        }
        private void Scale(string[] Parameters) {
            try {
                float Scale = HandleInput.ReadLine<float>((x => char.IsDigit(x) || x == '.'), "What will the new scale be (default '0.5') ? : ", (c => c > 0), "Scale should be more than 0.", false, true, 0.5f);
                Coms.UpdateScale(Scale);
            }
            catch {
                HandleInput.PrintColor("Invalid scale value", ConsoleColor.Red);
                return;
            }
        }
        private void AnimationSpeed(string[] Parameters) {
            try {
                float animSpeed = HandleInput.ReadLine<float>((x => char.IsDigit(x) || x == '.'), "What should the new animation speed be (default '1') ? : ", (c => c > 0), "Animation speed can't be 0", true, true, 1);
                Coms.UpdateAnimationSpeed(animSpeed);
            }
            catch {
                return;
            }
        }
        private void Interval(string[] Parameters) {
            int interval = HandleInput.ReadLine<int>((x => char.IsDigit(x)), "What should the new game interval be (default '3') ? : ", (x => true), "", false, true, 3);
            Coms.UpdateGameInterval(interval);
        }
        private void SkipSimulation(string[] Parameters) {
            Coms.SkipSimulation();
        }
        private void Pause(string[] Parameters) {
            Coms.Pause();
        }
        private void UnPause(string[] Parameters) {
            Coms.UnPause();
        }
        private void BackgroundColor(string[] Parameters) {
            string BackgroundColor = HandleInput.ReadLine<string>((x => char.IsLetter(x)), "What should the background color be (default 'BlanchedAlmond') ? : ", (c => GlobalMethods.GetColor(c) != null), "The given color does not exist.", false, true, "BlanchedAlmond");
            Coms.ChangeBackground((Color)GlobalMethods.GetColor(BackgroundColor));
        }
        #endregion

        #region Validation
        private bool TestFloat(string Test) {
            float container;
            return float.TryParse(Test, out container);
        }
        #endregion
    }
}
