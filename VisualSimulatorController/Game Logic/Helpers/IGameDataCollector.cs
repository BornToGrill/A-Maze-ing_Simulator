using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimulatorDelegate.Entities;

namespace VisualSimulatorController.Game_Logic.Helpers {
    interface IGameDataCollector {
        void AddGameHistoryData(int TurnCount, object GameBoard, object Reserves, object Moves);
        void AddGameLogData(GameData Data, int WinnerIndex);
    }
}
