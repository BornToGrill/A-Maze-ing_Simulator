using System;
using System.Collections.Generic;
using System.Threading;
using VisualSimulatorController.Game_Logic.Helpers;

namespace VisualSimulatorController.Logging.Helpers {
    internal abstract class AsyncLoggerBase : IDisposable {

        // Log variables
        Queue<Action> LogQueue = new Queue<Action>();
        ManualResetEvent NewItems = new ManualResetEvent(false);
        ManualResetEvent Terminate = new ManualResetEvent(false);
        ManualResetEvent Waiting = new ManualResetEvent(false);
        internal bool IsMainProcess = false;

        // Thread
        Thread LogThread;
        
        internal AsyncLoggerBase() {
            LogThread = new Thread(new ThreadStart(ProcessQueue)) {
                IsBackground = true
            };
            LogThread.Start();
        }

        internal void LogData(GameData Data, string WinnerName) {
            lock (LogQueue) {
                LogQueue.Enqueue(() => AsyncLogData(Data, WinnerName));
            }
            NewItems.Set();
        }

        /// <summary>
        /// Method used to send log data to the background logger. The data will be written to the specified file asynchronously.
        /// </summary>
        /// <param name="Data">The GameData object with all logging data to be written to the log file.</param>
        internal abstract void AsyncLogData(GameData Data, string WinnerName);
        /// <summary>
        /// Method used to create a template
        /// </summary>
        /// <param name="GameData"></param>
        /// <param name="Runs"></param>
        /// <param name="VisualRuns"></param>
        /// <param name="Chance"></param>
        /// <param name="TurnTime"></param>
        /// <param name="PlayerColors"></param>
        /// <param name="PlayerNames"></param>
        /// <param name="Done"></param>
        internal abstract void CreateTemplate(int[] GameData, int Runs, int VisualRuns, int TurnTime, string[] PlayerColors, string[] PlayerNames, int[] PlayerChances, ManualResetEvent Done);

        void ProcessQueue() {
            while (true) {
                Waiting.Set();
                int x = WaitHandle.WaitAny(new WaitHandle[] { NewItems, Terminate });

                if (x == 1) // Terminate triggered. Exit processing
                    return;
                NewItems.Reset();
                Waiting.Reset();

                Queue<Action> QueueCopy;
                lock (LogQueue) {
                    QueueCopy = new Queue<Action>(LogQueue);
                    LogQueue.Clear();
                }
                int QueueLength = QueueCopy.Count;
                for(int i = 0; i < QueueLength; i++) {
                    QueueCopy.Peek().Invoke();
                    QueueCopy.Dequeue();
                }
            }
        }
        #region IDisposable implementation members
        /// <summary>
        /// Disposes of the logger after finishing it's current operation.
        /// </summary>
        public void Dispose() {
            Terminate.Set();    // Terminates the processing queue
            LogThread.Join();   // Rejoin the thread with the main ending the async logger.
        }
        #endregion
    }
}
