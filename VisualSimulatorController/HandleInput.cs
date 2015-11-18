using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualSimulatorController {
    public static class HandleInput {

        private static bool Waiting;
        private static Queue<Tuple<string, ConsoleColor>> PrintQueue = new Queue<Tuple<string, ConsoleColor>>();

        #region Single key Input
        public static bool ExpectKey(ConsoleKey key, string message, int timeout = -1) {
            ExpectKey(new[] { key }, message, timeout);
            return true;
        }

        public static int ExpectKey(ConsoleKey[] keys, string message, int timeout = -1) {
            Console.CursorVisible = false;

            Console.WriteLine(message);

            ConsoleKey? input = null;
            while (timeout != 0 && !keys.Contains((ConsoleKey)(input = Console.ReadKey(true).Key))) {
                timeout--;
            }
            Console.CursorVisible = true;

            return (input != null) ? Array.IndexOf(keys, input) : -1;
        }
        #endregion

        #region Line Input
        public static T ReadLine<T>(Predicate<char> LimitCharacter, string message) {
            Waiting = true;
            Console.Write(message);
            StringBuilder builder = new StringBuilder();

            while (true) {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter && builder.Length > 0)
                    break;
                else if(key.Key == ConsoleKey.Backspace && builder.Length > 0) {
                    Console.Write("\b");
                    Console.Write(default(char));
                    Console.Write("\b");
                    builder.Remove(builder.Length - 1, 1);
                }
                else if (LimitCharacter.Invoke(key.KeyChar)) {
                    builder.Append(key.KeyChar);
                    Console.Write(key.KeyChar);
                }
            }
            Console.WriteLine();
            ProcessPrintQueue();

            return (T)Convert.ChangeType(builder.ToString(), typeof(T));
        }
        public static T ReadLine<T>(Predicate<char> LimitCharacter, string message, Predicate<T> LimitLine, string errorMessage, bool BeepOnError, bool AllowDefault = false, T Default = default(T)) {
            return ReadLine(LimitCharacter, message, new[] { LimitLine }, new[] { errorMessage }, BeepOnError, AllowDefault, Default);
        }
        public static T ReadLine<T>(Predicate<char> LimitCharacter, string message, Predicate<T>[] LimitLine, string[] errorMessage, bool BeepOnError, bool AllowDefault = false, T Default = default(T)) {
            Waiting = true;
            Console.Write(message);
            StringBuilder builder = new StringBuilder();

            Loop:
            while (true) {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) {
                    if(builder.Length == 0) {
                        if (AllowDefault) {
                            Console.WriteLine();
                            ProcessPrintQueue();
                            return Default;
                        }
                        else
                            continue;
                    }
                    T temp = (T)Convert.ChangeType(builder.ToString(), typeof(T));
                    for (int i = 0; i < LimitLine.Length; i++) {
                        if (!LimitLine[i].Invoke(temp)) {
                            builder.Clear();
                            Console.WriteLine();
                            ClearLine(0, 1);
                            PrintColor(errorMessage[i], ConsoleColor.Red, true);
                            if (BeepOnError)
                                Console.Beep();
                            ClearLine(2);
                            Console.Write(message);
                            goto Loop;
                        }
                    }
                    ClearLine(-1);
                    ProcessPrintQueue();
                    return temp;
                }
                else if (key.Key == ConsoleKey.Backspace && builder.Length > 0) {
                    Console.Write("\b");
                    Console.Write(default(char));
                    Console.Write("\b");
                    builder.Remove(builder.Length - 1, 1);
                }
                else if (LimitCharacter.Invoke(key.KeyChar)) {
                    builder.Append(key.KeyChar);
                    Console.Write(key.KeyChar);
                }
            }

        }
        #endregion

        #region Console Manipulation
        private static void ClearLine(int RelativePostion = 0, int RelativeReturn = 1) {
            Console.SetCursorPosition(0, Console.CursorTop - RelativePostion);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop - RelativeReturn);
        }
        public static void PrintColor(string Message, ConsoleColor color, bool Force = false) {
            if (Waiting && !Force) {
                lock(PrintQueue)
                    PrintQueue.Enqueue(Tuple.Create(Message, color));
                return;
            }
            else if(Console.CursorLeft != 0 && !Force) {
                Console.WriteLine();
                PrintColor(Message, color);
                return;
            }
            Console.ForegroundColor = color;
            Console.WriteLine(Message);
            Console.ResetColor();
        }
        private static void ProcessPrintQueue() {
            lock (PrintQueue) {
                while (PrintQueue.Count > 0) {
                    Tuple<string, ConsoleColor> action = PrintQueue.Peek();
                    PrintColor(action.Item1, action.Item2, true);
                    PrintQueue.Dequeue();
                }
            }
            Waiting = false;
        }
        #endregion
    }
}
