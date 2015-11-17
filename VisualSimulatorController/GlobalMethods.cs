using System;
using System.Threading;
using Microsoft.Xna.Framework;

namespace VisualSimulatorController {
    internal class GlobalMethods {
        /// <summary>
        /// Seed for the global randomizer.
        /// </summary>
        static int seed = Environment.TickCount;
        /// <summary>
        /// Thread safe random object.
        /// </summary>
        static readonly ThreadLocal<Random> Rnd =
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

        /// <summary>
        /// Gets a random integer from a thread safe randomizer.
        /// </summary>
        /// <returns></returns>
        internal static int NextRandom() {
            return Rnd.Value.Next();
        }
        internal static int NextRandom(int max) {
            return Rnd.Value.Next(max);
        }
        internal static int NextRandom(int min, int max) {
            return Rnd.Value.Next(min, max);
        }

        /// <summary>
        /// Tries to convert a string to a Xna.Framework color.
        /// </summary>
        /// <param name="ColorName">String with the name of the color.</param>
        /// <returns>The color if it exists. Else it returns null.</returns>
        internal static Color? GetColor(string ColorName) {
            var conv = typeof(Color).GetProperty(ColorName);
            return (conv == null) ? null : (Color?)conv.GetValue(null, null);
        }
    }
}
