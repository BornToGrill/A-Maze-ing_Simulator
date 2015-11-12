namespace SimulatorDelegate {
    /// <summary>
    /// Straight : A block with an opening at the top and bottom sides.
    /// Corner : A block with an opening at the top and right sides.
    /// TSplit : A block with an opening at the bottom, left, and right sides.
    /// </summary>
    public enum BlockType { Straight, Corner, TSplit, Chest };
    public enum Direction { Left, Up, Right, Down };
    public enum Rotate { Left, Right };

    public interface IRelayCommand
    {
        void Quit();
        void NotifyGameRunning();
    }
}
