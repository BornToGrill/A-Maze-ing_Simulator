using System;
using System.Globalization;
using System.Threading;

namespace VisualSimulatorController {
    class CommandHandler {

        private Commands Coms;
        public CommandHandler(Commands DelegateClass) {
            this.Coms = DelegateClass;
        }
        
        public void WaitForCommand() {
            Thread thrd = new Thread(new ThreadStart(
                delegate {
                    while (true) {
                        var str = Console.ReadLine();
                        HandleCommands(str);
                    }
                }));
            thrd.CurrentCulture = CultureInfo.InvariantCulture;
            thrd.Start();
        }
        private void HandleCommands(string Command) {
            if (Coms.Game == null)
                return;

            switch (Command.ToLower()) {
                case "resize":
                    int Width = HandleInput.ReadLine<int>((x => char.IsDigit(x)), "What will the width be (default '1280') ? : ", (c => c > 0), "Width has to be more than 0", false, true, 1280);
                    int Height = HandleInput.ReadLine<int>((x => char.IsDigit(x)), "What will the height be (default '720') ? : ", (c => c > 0), "Height has to be more than 0", false, true, 720);
                    Coms.UpdateGameSize(Width, Height);
                    break;
                case "scale":
                    try {
                        float Scale = HandleInput.ReadLine<float>((x => char.IsDigit(x) || x == '.'), "What will the new scale be (default '0.5') ? : ", (c => c > 0), "Scale should be more than 0.", false, true, 0.5f);
                        Coms.UpdateScale(Scale);
                    }
                    catch {
                        return;
                    }
                    break;
                case "skipsimulation":
                    Coms.SkipSimulation();
                    break;
                case "pause":
                    Coms.Pause();
                    break;
                case "unpause":
                    Coms.UnPause();
                    break;
                case "animationspeed":
                    try {
                        float animSpeed = HandleInput.ReadLine<float>((x => char.IsDigit(x) || x == '.'), "What should the new animation speed be (default '1') ? : ", (c => c > 0), "Animation speed can't be 0", true, true, 1);
                        Coms.UpdateAnimationSpeed(animSpeed);
                    }
                    catch {
                        return;
                    }
                    break;
                case "interval":
                    int interval = HandleInput.ReadLine<int>((x => char.IsDigit(x)), "What should the new game interval be (default '3') ? : ", (x => true), "", false, true, 3);
                    Coms.UpdateGameInterval(interval);
                    break;
                default:
                    break;
            }
        }

        private bool TestFloat(string Test) {
            float container;
            return float.TryParse(Test, out container);
        }
    }
}
