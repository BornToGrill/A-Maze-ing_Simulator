namespace SimulatorDelegate {
    public interface IDataCollector {

        bool IsReceivingData { get; }
        void SendMoveData(string MoveType, string TargetObject, string MoveDirection);
        void IncrementMoveData(string DataType);
        void RemoveLastMove();
    }
}
