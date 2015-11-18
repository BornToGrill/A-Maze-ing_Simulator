using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualSimulatorController.Game_Logic.Helpers {
    class GameData {
        internal int RowsShifted { get; set; }
        internal int BlocksRotated { get; set; }
        internal int PawnsMoved { get; set; }
        internal int Turns { get; set; }
        internal int[] RightAnswers { get; set; }
        internal int[] WrongAnswers { get; set; }

        internal GameData(int PlayerAmount) {
            RightAnswers = new int[PlayerAmount];
            WrongAnswers = new int[PlayerAmount];
        }

    }
}
