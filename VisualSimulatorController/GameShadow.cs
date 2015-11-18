using System;
using VisualSimulatorController.Game_Logic;

namespace VisualSimulatorController {

    internal class GameShadow {
        // Console Color Guide :
        // Green :      Controller related output.
        // Yellow :     Game back end related output.
        // Magenta :    Variable information ( Non critical )
        // Red :        Errors ( Critical ) && Critical information

        internal static void Main(string[] args) {
            Console.Title = "A-Maze-ing simulator";
            HandleInput.PrintColor("Controller started", ConsoleColor.Green);

            // Create new commands object to control the visual simulator if applicable.
            Commands Coms = new Commands();

            // Create all data for the simulation.
            var BoardData = GameCreation.CreateBoardData();
            var PlayerData = GameCreation.CreatePlayerData();
            var SimulationData = GameCreation.CreateSimulationData();
            
            // Create a simulation phase
            var Phase = new PhaseController(Coms, BoardData, PlayerData.Item1, PlayerData.Item2, PlayerData.Item3, PlayerData.Item4,
                SimulationData[0], SimulationData[1], SimulationData[2], SimulationData[3]);

            // Run simulation
            Phase.RunSimulation();

            Console.ReadLine();
        }
    }
}
