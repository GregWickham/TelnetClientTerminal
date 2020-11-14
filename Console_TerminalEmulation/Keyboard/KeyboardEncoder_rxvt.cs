using System;
using System.Collections.Generic;
using System.Windows.Forms;

using NetworkAccess;

namespace ConsoleTerminalEmulation
{
    public class KeyboardEncoder_rxvt : KeyboardEncoder
    {
        public KeyboardEncoder_rxvt(NetworkInterface n) : base(n) { }

        protected override string OutputSequenceFor(Keys key)
        {
            if (Translations.TryGetValue(key, out string result))
            {
                return result;
            }
            else
            {
                return string.Empty;
            }
        }

        private static readonly Dictionary<Keys, string> Translations = new Dictionary<Keys, string>()
        {
            { Keys.Space, " " },
            { Keys.Enter, "\x0D\x0A" },
            { Keys.Insert, "\x1B\x5B\x32\x7E" },
            { Keys.Delete, "\x1B\x5B\x33\x7E" },
            { Keys.Home, "\x1B\x5B\x31\x7E" },
            { Keys.End, "\x1B\x5B\x34\x7E" },
            { Keys.PageUp, "\x1B\x5B\x35\x7E" },
            { Keys.PageDown, "\x1B\x5B\x36\x7E" },

            { Keys.A, "a" },
            { Keys.B, "b" },
            { Keys.C, "c" },
            { Keys.D, "d" },
            { Keys.E, "e" },
            { Keys.F, "f" },
            { Keys.G, "g" },
            { Keys.H, "h" },
            { Keys.I, "i" },
            { Keys.J, "j" },
            { Keys.K, "k" },
            { Keys.L, "l" },
            { Keys.M, "m" },
            { Keys.N, "n" },
            { Keys.O, "o" },
            { Keys.P, "p" },
            { Keys.Q, "q" },
            { Keys.R, "r" },
            { Keys.S, "s" },
            { Keys.T, "t" },
            { Keys.U, "u" },
            { Keys.V, "v" },
            { Keys.W, "w" },
            { Keys.X, "x" },
            { Keys.Y, "y" },
            { Keys.Z, "z" },

            { Keys.A | Keys.Shift, "A" },
            { Keys.B | Keys.Shift, "B" },
            { Keys.C | Keys.Shift, "C" },
            { Keys.D | Keys.Shift, "D" },
            { Keys.E | Keys.Shift, "E" },
            { Keys.F | Keys.Shift, "F" },
            { Keys.G | Keys.Shift, "G" },
            { Keys.H | Keys.Shift, "H" },
            { Keys.I | Keys.Shift, "I" },
            { Keys.J | Keys.Shift, "J" },
            { Keys.K | Keys.Shift, "K" },
            { Keys.L | Keys.Shift, "L" },
            { Keys.M | Keys.Shift, "M" },
            { Keys.N | Keys.Shift, "N" },
            { Keys.O | Keys.Shift, "O" },
            { Keys.P | Keys.Shift, "P" },
            { Keys.Q | Keys.Shift, "Q" },
            { Keys.R | Keys.Shift, "R" },
            { Keys.S | Keys.Shift, "S" },
            { Keys.T | Keys.Shift, "T" },
            { Keys.U | Keys.Shift, "U" },
            { Keys.V | Keys.Shift, "V" },
            { Keys.W | Keys.Shift, "W" },
            { Keys.X | Keys.Shift, "X" },
            { Keys.Y | Keys.Shift, "Y" },
            { Keys.Z | Keys.Shift, "Z" },

            { Keys.D0, "0" },
            { Keys.D1, "1" },
            { Keys.D2, "2" },
            { Keys.D3, "3" },
            { Keys.D4, "4" },
            { Keys.D5, "5" },
            { Keys.D6, "6" },
            { Keys.D7, "7" },
            { Keys.D8, "8" },
            { Keys.D9, "9" },

            { Keys.D0 | Keys.Shift, ")" },
            { Keys.D1 | Keys.Shift, "!" },
            { Keys.D2 | Keys.Shift, "@" },
            { Keys.D3 | Keys.Shift, "#" },
            { Keys.D4 | Keys.Shift, "$" },
            { Keys.D5 | Keys.Shift, "%" },
            { Keys.D6 | Keys.Shift, "^" },
            { Keys.D7 | Keys.Shift, "&" },
            { Keys.D8 | Keys.Shift, "*" },
            { Keys.D9 | Keys.Shift, "(" },

            { Keys.Oem1, ";" },
            { Keys.Oem2, "/" },
            { Keys.Oem3, "`" },
            { Keys.Oem4, "[" },
            { Keys.Oem5, "\\" },
            { Keys.Oem6, "]" },
            { Keys.Oem7, "'" },
            { Keys.Oemcomma, ";" },
            { Keys.OemPeriod, ";" },

            { Keys.Oem1 | Keys.Shift, ":" },
            { Keys.Oem2 | Keys.Shift, "?" },  // This appears to be the same thing as OemQuestion
            //{ Keys.OemQuestion, "?" },
            { Keys.Oem3 | Keys.Shift, "~" },
            { Keys.Oem4 | Keys.Shift, "{" },
            { Keys.Oem5 | Keys.Shift, "|" },
            { Keys.Oem6 | Keys.Shift, "}" },
            { Keys.Oem7 | Keys.Shift, "\"" },
            { Keys.Oemcomma | Keys.Shift, "<" },
            { Keys.OemPeriod | Keys.Shift, ">" },

            { Keys.F1, "\x1B\x5B\x31\x31\x7E" },
            { Keys.F2, "\x1B\x5B\x31\x32\x7E" },
            { Keys.F3, "\x1B\x5B\x31\x33\x7E" },
            { Keys.F4, "\x1B\x5B\x31\x34\x7E" },
            { Keys.F5, "\x1B\x5B\x31\x35\x7E" },
            { Keys.F6, "\x1B\x5B\x31\x37\x7E" },
            { Keys.F7, "\x1B\x5B\x31\x38\x7E" },
            { Keys.F8, "\x1B\x5B\x31\x39\x7E" },
            { Keys.F9, "\x1B\x5B\x32\x30\x7E" },
            { Keys.F10, "\x1B\x5B\x32\x31\x7E" },
            { Keys.F11, "\x1B\x5B\x32\x32\x7E" },
            { Keys.F12, "\x1B\x5B\x32\x33\x7E" },

            { Keys.F1 | Keys.Shift, "\x5E\x5E\x61" },
            //{ Keys.F2 | Keys.Shift, "\x01\x61\x0D\x00" },
            //{ Keys.F3 | Keys.Shift, "\x1B\x5B\x31\x33\x7E" },
            //{ Keys.F4 | Keys.Shift, "\x1B\x5B\x31\x34\x7E" },
            //{ Keys.F5 | Keys.Shift, "\x1B\x5B\x31\x35\x7E" },
            //{ Keys.F6 | Keys.Shift, "\x1B\x5B\x31\x37\x7E" },
            //{ Keys.F7 | Keys.Shift, "\x1B\x5B\x31\x38\x7E" },
            //{ Keys.F8 | Keys.Shift, "\x1B\x5B\x31\x39\x7E" },
            //{ Keys.F9 | Keys.Shift, "\x1B\x5B\x32\x30\x7E" },
            //{ Keys.F10 | Keys.Shift, "\x1B\x5B\x32\x31\x7E" },
            //{ Keys.F11 | Keys.Shift, "\x1B\x5B\x32\x32\x7E" },
            //{ Keys.F12 | Keys.Shift, "\x1B\x5B\x32\x33\x7E" },

            { Keys.Up, "\x1B\x5B\x41" },
            { Keys.Left, "\x1B\x5B\x44" },
            { Keys.Down, "\x1B\x5B\x42" },
            { Keys.Right, "\x1B\x5B\x43" },
        };
    }
}
