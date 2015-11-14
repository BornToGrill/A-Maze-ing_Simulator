﻿using System;
using System.Linq;
using System.Text;

namespace VisualSimulatorController {
    public static class HandleInput {

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
            return (T)Convert.ChangeType(builder.ToString(), typeof(T));
        }
        public static T ReadLine<T>(Predicate<char> LimitCharacter, string message, Predicate<T> LimitLine, string errorMessage, bool BeepOnError, bool AllowDefault = false, T Default = default(T)) {
            return ReadLine(LimitCharacter, message, new[] { LimitLine }, new[] { errorMessage }, BeepOnError, AllowDefault, Default);
        }
        public static T ReadLine<T>(Predicate<char> LimitCharacter, string message, Predicate<T>[] LimitLine, string[] errorMessage, bool BeepOnError, bool AllowDefault = false, T Default = default(T)) {
            Console.Write(message);
            StringBuilder builder = new StringBuilder();

            Loop:
            while (true) {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) {
                    if(builder.Length == 0) {
                        if (AllowDefault) {
                            Console.WriteLine();
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
                            PrintColor(errorMessage[i], ConsoleColor.Red);
                            if (BeepOnError)
                                Console.Beep();
                            ClearLine(2);
                            Console.Write(message);
                            goto Loop;
                        }
                    }
                    ClearLine(-1);
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
        public static void PrintColor(string Message, ConsoleColor color) {
            Console.ForegroundColor = color;
            Console.WriteLine(Message);
            Console.ResetColor();
        }
        #endregion
    }
}