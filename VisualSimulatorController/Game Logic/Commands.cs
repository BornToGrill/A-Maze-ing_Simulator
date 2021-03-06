﻿using System;
using System.Threading;
using VisualSimulator;
using SimulatorDelegate;
using Microsoft.Xna.Framework;

namespace VisualSimulatorController.Game_Logic {

    public class Commands : IRelayCommand {
        public Game1 Game;
        public bool GameRunning = false;

        public void StartGame() {
            Thread thread = new Thread(
                new ThreadStart(
                    delegate {
                        using (Game = new Game1(this))
                            Game.Run();
                    }));
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
            while (!GameRunning) {
                Thread.Sleep(250);
            }
            CommandHandler Handler = new CommandHandler(this);
            Handler.WaitForCommand();
            HandleInput.PrintColor("The visual simulation has begun. The simulator can be controlled using console commands.", ConsoleColor.Green);
            Handler.ForceOutputCommand("?");
        }



        #region Game manipulation methods
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
        public void ChangeBackground(Color Background) {
            Game.ChangeBackground(Background);
        }
        #endregion

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
