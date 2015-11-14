using System;
using VisualSimulatorController.Game_Logic;

namespace VisualSimulatorController {

    public class GameShadow {
        // Console Color Guide :
        // Green :      Controller related output.
        // Yellow :     Game back end related output.
        // Magenta :    Variable information ( Non critical )
        // Red :        Errors ( Critical ) && Critical information

        public static Random rnd = new Random();    // Global randomizer.

        public static void Main(string[] args) {
            Console.Title = "A-Maze-ing simulator";
            HandleInput.PrintColor("Controller started", ConsoleColor.Green);

            Commands Coms = new Commands();

            HandleInput.PrintColor("Board created.", ConsoleColor.Yellow);

            var BoardData = GameCreation.CreateBoardData();
            var PlayerData = GameCreation.CreatePlayerData();
            var SimulationData = GameCreation.CreateSimulationData();
            
            var Phase = new GamePhase(Coms, BoardData[0], BoardData[1], BoardData[2], BoardData[3], BoardData[4], PlayerData.Item1, PlayerData.Item2, PlayerData.Item3, 
                SimulationData[2], SimulationData[0], SimulationData[1], SimulationData[3]);

            Phase.RunSimulation();

            HandleInput.PrintColor("Log file has been created. Simulation run completed.", ConsoleColor.Green);
            Console.ReadLine();
        }
    }
}
