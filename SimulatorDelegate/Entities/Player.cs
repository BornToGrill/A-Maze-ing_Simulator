using System;
using Microsoft.Xna.Framework;

namespace SimulatorDelegate.Entities {

    [Serializable]
    public class Player {
        private bool Human;
        private IDataCollector Phase;
        private int PlayerIndex;
        public string Name;
        public Color PawnColor;
        public Vector2 Position;    // Index representation of board location
        public int TimesMoved;

        public Player(IDataCollector sender, Color PawnColor, Vector2 StartingPosition, int Index, bool HumanControlled = false) {
            this.Human = HumanControlled;
            this.Position = StartingPosition;
            this.PawnColor = PawnColor;
            this.PlayerIndex = Index;
            this.Phase = sender;
        }
        /// <summary>
        /// Initializer only to be called by the Game instance.
        /// </summary>
        public Player(Color PawnColor, Vector2 StartingPosition) {
            this.PawnColor = PawnColor;
            this.Position = StartingPosition;
        }

        #region Entity Manipulation

        public void MovePlayer(Direction dir, bool MovedByHallway = false) {
            if (!MovedByHallway) {
                Phase.IncrementMoveData("PawnMoved");
                Phase.SendMoveData("PawnMoved", PlayerIndex.ToString(), dir.ToString());
            }

            if (dir == Direction.Left)
                this.Position.X--;
            else if (dir == Direction.Right)
                this.Position.X++;
            else if (dir == Direction.Up)
                this.Position.Y--;
            else if (dir == Direction.Down)
                this.Position.Y++;
        }
        #endregion

    }
}
