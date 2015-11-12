namespace SimulatorDelegate {
    public interface IDataCollector {

        void SendMoveData(string MoveType, string TargetObject, string MoveDirection);
        void IncrementMoveData(string DataType);
        void RemoveLastMove();
    }
}
