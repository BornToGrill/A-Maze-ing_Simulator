using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using SimulatorDelegate;
using SimulatorDelegate.Entities;
using VisualSimulatorController.Game_Logic;

namespace VisualSimulatorController.AI_Logic {
    internal class StateController {


        private Labyrinth Current;
        private Vector2 Chest;
        private IDataCollector Phase;

        /// <summary>
        /// Array of players excluding the player who executed the method.
        /// Array is preferred over List since the cost of conversion is lower than the
        /// performance cost of itterating over a list compared to an array. 
        /// </summary>
        private Player[] Players;

        private int Chance;

        internal StateController(IDataCollector sender, int Chance, Labyrinth GameState, Player[] Players) {
            this.Phase = sender;
            this.Chance = Chance;
            this.Current = GameState;
            this.Players = Players;
            this.Chest = new Vector2((int)GameState.Board.GetLength(1) / 2, (int)GameState.Board.GetLength(0) / 2);
        }
        /// <summary>
        /// Moves a Player object on the board according to specific AI Rules.
        /// If the player won the game a true value is returned.
        /// </summary>
        /// <returns>Returns true if the player calling the method has won.</returns>
        internal bool ExecuteNextMove(Player sender) {
            bool Answer = AnswerQuestion();
            Phase.IncrementAnswer(Answer);
            
            CalculateMove(sender, Answer);

            if (sender.Position == Chest)
                return true;
            return false;
            // Check for win state.
            // Call next player.
        }

        private bool AnswerQuestion() {
            return (GlobalMethods.NextRandom(0, 100) <= Chance);
        }

        #region AI Moves

        private void CalculateMove(Player sender, bool Correct) {
            var Distance = new Vector2(Chest.X - sender.Position.X, Chest.Y - sender.Position.Y);
            if (!Correct) { // Incorrect answer. Only rotate an adjacent block.
                RotateAdjacent(sender, Distance);
                return;
            }
            for (int i = 0; i < Players.Length; i++) {
                if (Players[i] != sender && GetTotalDistance(Players[i]) <= 2) { // Player is in proximity of chest.
                    if (Players[i].Position != sender.Position && ExecuteCounter(sender, Players[i])) {
                        OnlyMovePlayer(sender, Distance);
                        return;
                    }
                }
                else if (Players[i] != sender && Players[i].Position == sender.Position) {   // Player shares a block with another player.
                    if (HandleSameBlock(sender, Distance))
                        return;
                    else
                        ExecuteBestMove(sender, Distance);
                }
            }
            ExecuteBestMove(sender, Distance);
        }

        #region Self-Centered Moves
        private bool OnlyMovePlayer(Player sender, Vector2 Distance) {
            if (Distance.X == 0) {
                if (Distance.Y > 0) {
                    if (Current.PlayerCanMove(sender, Direction.Down)) {
                        sender.MovePlayer(Direction.Down);
                        return true;
                    }
                }
                else {
                    if (Current.PlayerCanMove(sender, Direction.Up)) {
                        sender.MovePlayer(Direction.Up);
                        return true;
                    }
                }
                return false;
            }
            else if (Distance.Y == 0) {
                if (Distance.X > 0) {
                    if (Current.PlayerCanMove(sender, Direction.Right)) {
                        sender.MovePlayer(Direction.Right);
                        return true;
                    }
                }
                else {
                    if (Current.PlayerCanMove(sender, Direction.Left)) {
                        sender.MovePlayer(Direction.Left);
                        return true;
                    }
                }
                return false;
            }

            if (Distance.X > 0) {
                if (Current.PlayerCanMove(sender, Direction.Right)) {
                    sender.MovePlayer(Direction.Right);
                    return true;
                }
            }
            else if (Distance.X < 0) {
                if (Current.PlayerCanMove(sender, Direction.Left)) {
                    sender.MovePlayer(Direction.Left);
                    return true;
                }
            }
            if (Distance.Y > 0) {
                if (Current.PlayerCanMove(sender, Direction.Down)) {
                    sender.MovePlayer(Direction.Down);
                    return true;
                }
            }
            else if (Distance.Y < 0) {
                if (Current.PlayerCanMove(sender, Direction.Up)) {
                    sender.MovePlayer(Direction.Up);
                    return true;
                }
            }
            return false;
        }
        private bool HandleSameBlock(Player sender, Vector2 Distance) {
            if (OnlyMovePlayer(sender, Distance))
                return true;
            if (Distance.X > 0) {
                if (TryRotateSelf(sender, Direction.Right) || TryRotate(sender, Direction.Right)) {
                    sender.MovePlayer(Direction.Right);
                    return true;
                }
            }
            else if (Distance.X < 0) {
                if (TryRotateSelf(sender, Direction.Left) || TryRotate(sender, Direction.Left)) {
                    sender.MovePlayer(Direction.Left);
                    return true;
                }
            }
            if (Distance.Y > 0) {
                if (TryRotateSelf(sender, Direction.Down) || TryRotate(sender, Direction.Down)) {
                    sender.MovePlayer(Direction.Down);
                    return true;
                }
            }
            else if (Distance.Y < 0) {
                if (TryRotateSelf(sender, Direction.Up) || TryRotate(sender, Direction.Up)) {
                    sender.MovePlayer(Direction.Up);
                    return true;
                }
            }
            return false;
        }
        private void ExecuteBestMove(Player sender, Vector2 Distance, bool SecondBest = false) {
            // Handle player being inline with the Chest.
            #region New Method : HandleZeroDistance     // <<<<<
            if (SecondBest)
                if (HandleSameBlock(sender, Distance))
                    return;

            if (Distance.X == 0) {
                if (Distance.Y > 0)    // Player has to move Down.
                    HandleZeroDistance(sender, Distance, Direction.Down);
                else                   // Player has to move Up.
                    HandleZeroDistance(sender, Distance, Direction.Up);
                return;
            }
            else if (Distance.Y == 0) {
                if (Distance.X > 0)    // Player has to move Right.
                    HandleZeroDistance(sender, Distance, Direction.Right);
                else                   // Player has to move Left.
                    HandleZeroDistance(sender, Distance, Direction.Left);
                return;
            }
            if (Distance.X == 1 && Distance.Y == 1) {
                HandleLastMove(sender, Distance);
                return;
            }
            #endregion

            if (Distance.X > 0) {
                if (Distance.X != 1) {
                    if (TryShiftRowAndMove(sender, Direction.Right))
                        return;
                    if (SecondBest) {
                        if (Current.PlayerCanMove(sender, Direction.Right)) {
                            ExecuteCounter(sender, Direction.Right);
                            sender.MovePlayer(Direction.Right);
                            return;
                        }
                        else if (Current.CanShift((int)sender.Position.Y)) {  // If second best, only move a row. Player can't move.
                            Current.ShiftRow((int)sender.Position.Y, Direction.Right);
                            return;
                        }
                    }
                }
            }
            else if (Distance.X < 0) {
                if (Distance.X != -1) {
                    if (TryShiftRowAndMove(sender, Direction.Left))
                        return;
                    if (SecondBest) {
                        if (Current.PlayerCanMove(sender, Direction.Left)) {
                            ExecuteCounter(sender, Direction.Left);
                            sender.MovePlayer(Direction.Left);
                            return;
                        }
                        else if (Current.CanShift((int)sender.Position.Y)) {
                            Current.ShiftRow((int)sender.Position.Y, Direction.Left);
                            return;
                        }

                    }

                }
            }
            if (Distance.Y > 0) {
                if (Distance.Y != 1) {
                    if (TryShiftRowAndMove(sender, Direction.Down))
                        return;
                    if (SecondBest)
                        if (Current.PlayerCanMove(sender, Direction.Down)) {
                            ExecuteCounter(sender, Direction.Down);
                            sender.MovePlayer(Direction.Down);
                            return;
                        }
                        else if (Current.CanShift((int)sender.Position.X)) {
                            Current.ShiftRow((int)sender.Position.X, Direction.Down);
                            return;
                        }
                }
            }
            else if (Distance.Y < 0) {
                if (Distance.Y != 1) {
                    if (TryShiftRowAndMove(sender, Direction.Up))
                        return;
                    if (SecondBest)
                        if (Current.PlayerCanMove(sender, Direction.Up)) {
                            ExecuteCounter(sender, Direction.Up);
                            sender.MovePlayer(Direction.Up);
                            return;
                        }
                        else if (Current.CanShift((int)sender.Position.X)) {
                            Current.ShiftRow((int)sender.Position.X, Direction.Up);
                            return;
                        }
                }
            }

            if (SecondBest) {
                // Worst possible move. Only obstruct other player.
                if (!RotateAdjacent(sender, Distance))
                    ExecuteCounter(sender);
                return;
                // Later implementation ? :
                // Annoy other player if you can move.
                // else rotate nearby and move.
                // else only rotate.
            }
            ExecuteBestMove(sender, Distance, true);
        }

        /// <summary>
        /// If possible moves a row and then the player in a direction.
        /// </summary>
        /// <returns>Returns true if player was able to move.</returns>
        private bool TryShiftRowAndMove(Player sender, Direction dir) {
            if (dir == Direction.Left || dir == Direction.Right) {
                if (Current.CanShift((int)sender.Position.Y) && Current.PlayerCanMove(sender, dir)) {
                    Current.ShiftRow((int)sender.Position.Y, dir);
                    sender.MovePlayer(dir);
                    return true;
                }
            }
            else {
                if (Current.CanShift((int)sender.Position.X) && Current.PlayerCanMove(sender, dir)) {
                    Current.ShiftRow((int)sender.Position.X, dir);
                    sender.MovePlayer(dir);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Execute when player is within a 1 block radius of chest.
        /// </summary>
        private void HandleLastMove(Player sender, Vector2 Distance) {  // State where player is in proximity of chest
            if (Distance.X == 0) {
                if (Distance.Y > 0)
                    LastMove(sender, Direction.Down);
                else
                    LastMove(sender, Direction.Up);
                return;
            }
            else if (Distance.Y == 0) {
                if (Distance.X > 0)
                    LastMove(sender, Direction.Right);
                else
                    LastMove(sender, Direction.Left);
                return;
            }

            if (Distance.X == 1 && Distance.Y == 1) {    // Player is Left and above of chest.
                if (Current.PlayerCanMoveOut(sender, Direction.Right)) {
                    Current.ShiftRow((int)sender.Position.X, Direction.Down);
                    sender.MovePlayer(Direction.Right);
                }
                else if (Current.PlayerCanMoveOut(sender, Direction.Down)) {
                    Current.ShiftRow((int)sender.Position.Y, Direction.Right);
                    sender.MovePlayer(Direction.Down);
                }
                else
                    Current.ShiftRow((int)sender.Position.X, Direction.Down);
            }
            else if (Distance.X == -1 && Distance.Y == 1) {  // Player is right and above chest.
                if (Current.PlayerCanMoveOut(sender, Direction.Left)) {
                    Current.ShiftRow((int)sender.Position.X, Direction.Down);
                    sender.MovePlayer(Direction.Left);
                }
                else if (Current.PlayerCanMoveOut(sender, Direction.Down)) {
                    Current.ShiftRow((int)sender.Position.Y, Direction.Left);
                    sender.MovePlayer(Direction.Down);
                }
                else
                    Current.ShiftRow((int)sender.Position.X, Direction.Down);
            }
            else if (Distance.X == 1 && Distance.Y == -1) {  // Player is left and below the chest.
                if (Current.PlayerCanMoveOut(sender, Direction.Right)) {
                    Current.ShiftRow((int)sender.Position.X, Direction.Up);
                    sender.MovePlayer(Direction.Right);
                }
                else if (Current.PlayerCanMoveOut(sender, Direction.Up)) {
                    Current.ShiftRow((int)sender.Position.Y, Direction.Right);
                    sender.MovePlayer(Direction.Up);
                }
                else
                    Current.ShiftRow((int)sender.Position.X, Direction.Up);
            }
            else if (Distance.X == -1 && Distance.Y == -1) { // Player is right and below the chest.
                if (Current.PlayerCanMoveOut(sender, Direction.Left)) {
                    Current.ShiftRow((int)sender.Position.X, Direction.Up);
                    sender.MovePlayer(Direction.Left);
                }
                else if (Current.PlayerCanMoveOut(sender, Direction.Up)) {
                    Current.ShiftRow((int)sender.Position.Y, Direction.Left);
                    sender.MovePlayer(Direction.Up);
                }
                else
                    Current.ShiftRow((int)sender.Position.X, Direction.Up);
            }
        }
        private bool LastMove(Player sender, Direction dir) {
            if (Current.PlayerCanMove(sender, dir)) {
                sender.MovePlayer(dir);
                return true;
            }
            else if (TryRotateSelf(sender, dir)) {
                sender.MovePlayer(dir);
                return true;
            }
            else {
                Current.Rotate((int)sender.Position.X, (int)sender.Position.Y, Rotate.Right);
                return true;
            }

        }

        private bool TryRotateSelf(Player sender, Direction dir) {
            Current.Rotate((int)sender.Position.X, (int)sender.Position.Y, Rotate.Left);
            if (Current.PlayerCanMove(sender, dir))
                return true;
            Current.Rotate((int)sender.Position.X, (int)sender.Position.Y, Rotate.Right, 2);
            if (Current.PlayerCanMove(sender, dir))
                return true;
            Current.Rotate((int)sender.Position.X, (int)sender.Position.Y, Rotate.Left, 1, true);
            return false;
        }

        /// <summary>
        /// Execute when players X or Y location is alligned with the Chest.
        /// </summary>
        private void HandleZeroDistance(Player sender, Vector2 Distance, Direction dir) {
            if (Current.PlayerCanMove(sender, dir)) {   // Player can move, try to block another player.
                ExecuteCounter(sender, dir); // Execute counter move.
                sender.MovePlayer(dir); // Move self in direction.
                return;
            }
            // Try rotating self to see if you can move after.
            Current.Rotate((int)sender.Position.X, (int)sender.Position.Y, Rotate.Right);
            if (Current.PlayerCanMove(sender, dir)) {
                sender.MovePlayer(dir);
                return;
            }
            Current.Rotate((int)sender.Position.X, (int)sender.Position.Y, Rotate.Left, 2);
            if (Current.PlayerCanMove(sender, dir)) {
                sender.MovePlayer(dir);
                return;
            }
            Current.Rotate((int)sender.Position.X, (int)sender.Position.Y, Rotate.Right, 1, true);   // Rotating self not usefull, reset rotation.

            // Move not possible. Rotate adjacent and check again.
            RotateAdjacent(sender, Distance);
            // If player can move after rotating , move player.
            if (Current.PlayerCanMove(sender, dir))
                sender.MovePlayer(dir);
        }


        /// <summary>
        /// Rotates and Adjacent block to the player.
        /// </summary>
        private bool RotateAdjacent(Player sender, Vector2 Distance, bool SecondBest = false) {
            if (Distance.X == 0) {   // X-axis is alligned.
                if (Distance.Y > 0) {   // Needs to move down.
                    if (TryRotate(sender, Direction.Down))
                        return true;
                    else if (!Current.PlayerCanMoveOut(sender.Position + new Vector2(0, 1), Direction.Up))
                        Current.Rotate((int)sender.Position.X, (int)sender.Position.Y + 1, Rotate.Right);
                }
                else {  // Needs to move up.
                    if (TryRotate(sender, Direction.Up))
                        return true;
                    else if(!Current.PlayerCanMoveOut(sender.Position + new Vector2(0, -1), Direction.Down))
                        Current.Rotate((int)sender.Position.X, (int)sender.Position.Y - 1, Rotate.Right);
                }
                return false;   // Rotated adjacent but still can't move.
            }
            else if (Distance.Y == 0) {  // Y-axis is alligned.
                if (Distance.X > 0) {    // Needs to move right.
                    if (TryRotate(sender, Direction.Right))
                        return true;
                    else if (!Current.PlayerCanMoveOut(sender.Position + new Vector2(1, 0), Direction.Left))
                        Current.Rotate((int)sender.Position.X + 1, (int)sender.Position.Y, Rotate.Right);
                }
                else {  // Needs to move left.
                    if (TryRotate(sender, Direction.Left))
                        return true;
                    else if (!Current.PlayerCanMoveOut(sender.Position + new Vector2(-1, 0), Direction.Right))
                        Current.Rotate((int)sender.Position.X - 1, (int)sender.Position.Y, Rotate.Right);
                }
                return false;   // rotated adjacent but still can't move.
            }

            if (Distance.X > 0) {    // Rotate right adjacent.
                if (TryRotate(sender, Direction.Right))
                    return true;
                if (SecondBest && !Current.PlayerCanMoveOut(sender.Position + new Vector2(1,0), Direction.Left)) {
                    Current.Rotate((int)sender.Position.X + 1, (int)sender.Position.Y, Rotate.Right);
                    return true;
                }
            }
            else if (Distance.X < 0) {   // Rotate left adjacent.
                if (TryRotate(sender, Direction.Left))
                    return true;
                if (SecondBest && !Current.PlayerCanMoveOut(sender.Position + new Vector2(-1, 0), Direction.Right)) {
                    Current.Rotate((int)sender.Position.X - 1, (int)sender.Position.Y, Rotate.Right);
                    return true;
                }
            }
            if (Distance.Y > 0) {  // Rotate down adjacent.
                if (TryRotate(sender, Direction.Down))
                    return true;
                if (SecondBest && !Current.PlayerCanMoveOut(sender.Position + new Vector2(0, 1), Direction.Up)) {
                    Current.Rotate((int)sender.Position.X, (int)sender.Position.Y + 1, Rotate.Right);
                    return true;
                }
            }
            else if (Distance.Y < 0) {   // Rotate up adjacent.
                if (TryRotate(sender, Direction.Up))
                    return true;
                if (SecondBest && !Current.PlayerCanMoveOut(sender.Position + new Vector2(0, -1), Direction.Down)) {
                    Current.Rotate((int)sender.Position.X, (int)sender.Position.Y - 1, Rotate.Right);
                    return true;
                }
            }

            // No adjacent rotation improves the player's position. Recursive call to rotate an adjacent block anyway.
            if (SecondBest)
                return false;
            return RotateAdjacent(sender, Distance, true);

        }
        /// <summary>
        /// Checks whether or not rotating an adjacent block is beneficial to the player.
        /// </summary>
        /// <returns>Returns true when the move is beneficial.</returns>
        private bool TryRotate(Player sender, Direction dir) {
            int OffsetX = 0;
            int OffsetY = 0;
            if (dir == Direction.Right)
                OffsetX = 1;
            else if (dir == Direction.Left)
                OffsetX = -1;
            else if (dir == Direction.Up)
                OffsetY = -1;
            else
                OffsetY = 1;

            Current.Rotate((int)sender.Position.X + OffsetX, (int)sender.Position.Y + OffsetY, Rotate.Right);
            if (Current.PlayerCanMove(sender, dir))
                return true;
            Current.Rotate((int)sender.Position.X + OffsetX, (int)sender.Position.Y + OffsetY, Rotate.Left, 2);
            if (Current.PlayerCanMove(sender, dir))
                return true;
            Current.Rotate((int)sender.Position.X + OffsetX, (int)sender.Position.Y + OffsetY, Rotate.Right, 1, true);
            return false;
        }

        #endregion

        #region Player counter moves

        /// <summary>
        /// Executes a counter move to block a target player.
        /// </summary>
        /// <returns>Returns true if a counter move was found and executed.</returns>
        private bool ExecuteCounter(Player sender, Player target) {
            var EnemyData = new List<Tuple<Player, int>>();
            for (int i = 0; i < Players.Length; i++)
                if (Players[i] != sender)
                    EnemyData.Add(Tuple.Create(Players[i], GetTotalDistance(Players[i])));
            EnemyData = EnemyData.OrderBy(x => x.Item2).ToList();

            if (TryShiftRowCounter(sender, target, EnemyData, false))
                return true;
            else if (TryRotateCounter(target))
                return true;
            return false;
        }

        /// <summary>
        /// Executes a counter move disregarding player movement after the counter.
        /// Should be used when the player can't move and will only counter others.
        /// </summary>
        private bool ExecuteCounter(Player sender) {
            var EnemyData = new List<Tuple<Player, int>>(); // List containing all enemies with their respective distance to chest.
            for (int i = 0; i < Players.Length; i++)
                if (Players[i] != sender)
                    EnemyData.Add(Tuple.Create(Players[i], GetTotalDistance(Players[i])));
            EnemyData = EnemyData.OrderBy(x => x.Item2).ToList();  // Sort array to get the closest player to the chest at the lowest index.

            for (int i = 0; i < EnemyData.Count; i++) {
                if (TryShiftRowCounter(sender, EnemyData[i].Item1, EnemyData, false))
                    return true;
                else if (TryRotateCounter(EnemyData[i].Item1))
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Executes a counter move without obstructing player movement.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="dir">Direction in which the player is required to be able to move after the counter move.</param>
        private bool ExecuteCounter(Player sender, Direction dir) {
            var EnemyData = new List<Tuple<Player, int>>();
            for (int i = 0; i < Players.Length; i++)
                if (Players[i] != sender)
                    EnemyData.Add(Tuple.Create(Players[i], GetTotalDistance(Players[i])));
            EnemyData = EnemyData.OrderBy(x => x.Item2).ToList();

            for (int i = 0; i < EnemyData.Count; i++) {
                if (TryShiftRowCounter(sender, EnemyData[i].Item1, EnemyData, true, dir))
                    return true;
                else if (TryRotateCounter(EnemyData[i].Item1))
                    return true;
            }
            return false;
        }

        private bool GoodCounter(Player target, List<Tuple<Player, int>> EnemyData, Vector2 Movement) {
            bool GoodMove = true;
            for (int i = 0; i < EnemyData.Count; i++)
                if (EnemyData[i].Item1 == target) {
                    if (GetTotalDistance(EnemyData[i].Item1.Position + Movement) > EnemyData[i].Item2)
                        GoodMove = true;
                    else
                        return false;
                }
                else if (GetTotalDistance(EnemyData[i].Item1.Position + Movement) < EnemyData[i].Item2)
                    return false;
            return GoodMove;

        }

        private bool TryShiftRowCounter(Player sender, Player target, List<Tuple<Player,int>> EnemyData, bool AvoidObstruction, Direction dir = Direction.Down) {
            int Distance = GetTotalDistance(target);
            try {
                if (Current.CanShift((int)target.Position.Y)) {    // Can shift target along it's X axis.
                    if(GoodCounter(target, EnemyData, new Vector2(1,0))) {   // Distance of target is increased. Good counter. (Shift Right)
                        if (!AvoidObstruction) {
                            Current.ShiftRow((int)target.Position.Y, Direction.Right);
                            return true;
                        }
                        if (sender.Position.X != target.Position.X && // If player is not moved.
                            (ExpectedPlayerLocation(sender, dir).Y != target.Position.Y ||     // If the row the player has to move to has not been moved.
                            Current.PlayerCanMoveOut(ExpectedPlayerLocation(sender, dir) + new Vector2(-1, 0), GetOpposite(dir)))) { // If the row has been moved but the player can move anyway.
                            Current.ShiftRow((int)target.Position.Y, Direction.Right);
                            return true;
                        }
                    }
                    else if (GoodCounter(target, EnemyData, new Vector2(-1, 0))) {
                        if (!AvoidObstruction) {
                            Current.ShiftRow((int)target.Position.Y, Direction.Left);
                            return true;
                        }
                        if (sender.Position.X != target.Position.X &&
                            (ExpectedPlayerLocation(sender, dir).Y != target.Position.Y ||
                            Current.PlayerCanMoveOut(ExpectedPlayerLocation(sender, dir) + new Vector2(1, 0), GetOpposite(dir)))) {
                            Current.ShiftRow((int)target.Position.Y, Direction.Left);
                            return true;
                        }
                    }
                }
                if (Current.CanShift((int)target.Position.X)) {
                    if (GoodCounter(target, EnemyData, new Vector2(0, 1))) {
                        if (!AvoidObstruction) {
                            Current.ShiftRow((int)target.Position.X, Direction.Down);
                            return true;
                        }
                        if (sender.Position.Y != target.Position.Y &&
                            (ExpectedPlayerLocation(sender, dir).X != target.Position.X ||
                            Current.PlayerCanMoveOut(ExpectedPlayerLocation(sender, dir) + new Vector2(0, -1), GetOpposite(dir)))) {
                            Current.ShiftRow((int)target.Position.X, Direction.Down);
                            return true;
                        }
                    }
                    else if (GoodCounter(target, EnemyData, new Vector2(0, -1))) {
                        if (!AvoidObstruction) {
                            Current.ShiftRow((int)target.Position.X, Direction.Up);
                            return true;
                        }
                        if (sender.Position.Y != target.Position.Y &&
                            (ExpectedPlayerLocation(sender, dir).X != target.Position.X ||
                            Current.PlayerCanMoveOut(ExpectedPlayerLocation(sender, dir) + new Vector2(0, 1), GetOpposite(dir)))) {
                            Current.ShiftRow((int)target.Position.X, Direction.Up);
                            return true;
                        }
                    }
                }
                return false;
            }
            catch {
                return false;
            }
        }
        private bool TryRotateCounter(Player target) {
            Vector2 Distance = new Vector2(Chest.X - target.Position.X, Chest.Y - target.Position.Y);
            if(Distance.X == 0) {
                if (Distance.Y > 0) {
                    if (!Current.PlayerCanMove(target, Direction.Down))  // Player can't move in the first place.
                        return false;
                    if (AttemptRotate(target, Direction.Down))
                        return true;
                }
                else {
                    if (!Current.PlayerCanMove(target, Direction.Up))
                        return false;
                    if (AttemptRotate(target, Direction.Up))
                        return true;
                }
            }
            else if(Distance.Y == 0) {
                if(Distance.X > 0) {
                    if (!Current.PlayerCanMove(target, Direction.Right))
                        return false;
                    if (AttemptRotate(target, Direction.Right))
                        return true;
                }
                else {
                    if (!Current.PlayerCanMove(target, Direction.Left))
                        return false;
                    if (AttemptRotate(target, Direction.Left))
                        return true;
                }
            }
            return false;
        }

        /// <summary
        /// Check whether rotating a target players block is a good counter.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="dir"></param>
        /// <returns>Returns true </returns>
        private bool AttemptRotate(Player target, Direction dir) {
            Current.Rotate((int)target.Position.X, (int)target.Position.Y, Rotate.Left);
            if (!Current.PlayerCanMove(target, dir))
                return true;
            Current.Rotate((int)target.Position.X, (int)target.Position.Y, Rotate.Right, 2);
            if (!Current.PlayerCanMove(target, dir))
                return true;
            Current.Rotate((int)target.Position.X, (int)target.Position.Y, Rotate.Left, 1, true);
            return false;
        }
        private Vector2 ExpectedPlayerLocation(Player sender, Direction dir) {
            switch (dir) {
                case Direction.Left:
                    return new Vector2(sender.Position.X - 1, sender.Position.Y);
                case Direction.Right:
                    return new Vector2(sender.Position.X + 1, sender.Position.Y);
                case Direction.Up:
                    return new Vector2(sender.Position.X, sender.Position.Y - 1);
                default:
                    return new Vector2(sender.Position.X, sender.Position.Y + 1);
            }
        }
        private int GetTotalDistance(Player target) {
            return (int)(Math.Abs(Chest.X - target.Position.X) + Math.Abs(Chest.Y - target.Position.Y));
        }
        private int GetTotalDistance(Vector2 target) {
            return (int)(Math.Abs(Chest.X - target.X) + Math.Abs(Chest.Y - target.Y));
        }
        private Direction GetOpposite(Direction dir) {
            switch (dir) {
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Left;
                case Direction.Up:
                    return Direction.Down;
                default:    // Direction.Down
                    return Direction.Up;
            }
        }

        #endregion  // Player counter moves
        #endregion  // AI moves
    }
}
