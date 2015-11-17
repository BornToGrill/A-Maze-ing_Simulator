using System;

namespace SimulatorDelegate.Entities {

    [Serializable]
    public class LabyrinthBlock {

        public BlockType Type;
        private int _orientation;

        public int Orientation {    // Clamped orientation value with range (0 - 3)
            get { return _orientation; }
            set { _orientation = value;
                while (_orientation > 3)
                    _orientation -= 4;
                while (_orientation < 0)
                    _orientation += 4;
            }
        }

        public LabyrinthBlock(BlockType Type, int Orientation) {
            this.Type = Type;
            this.Orientation = Orientation;
        }
        public void RotateBlock(Rotate rotate, int count = 1) {
            switch (rotate) {
                case Rotate.Left:
                    Orientation -= count;
                    break;
                case Rotate.Right:
                    Orientation += count;
                    break;
            }
        }

        public bool CanMove(Direction direction) {
            switch (Type) {
                case BlockType.Straight:
                    return (int)direction == FixOrientation(1, Orientation) || (int)direction == FixOrientation(3, Orientation);
                case BlockType.Corner:
                    return (int)direction == FixOrientation(1, Orientation) || (int)direction == FixOrientation(2, Orientation);
                case BlockType.TSplit:
                    return (int)direction == FixOrientation(0, Orientation) || (int)direction == FixOrientation(2, Orientation) || (int)direction == FixOrientation(3, Orientation);
                case BlockType.Chest:
                    return true;
                default:
                    return false;


            }
        }
        private int FixOrientation(int MainDirection, int AddedDirection) {
            int Direction = MainDirection + AddedDirection;
            while (Direction > 3)
                Direction -= 4;
            while (Direction < 0)
                Direction += 4;
            return Direction;
        }
    }
}
