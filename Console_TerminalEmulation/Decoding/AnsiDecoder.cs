using System;
using System.Text;
using System.Drawing;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace ConsoleTerminalEmulation
{
    [Serializable]
    public class InvalidByteException : Exception
    {
        protected byte m_byte;
        public byte Byte => m_byte;

        public InvalidByteException(byte b, string message) : base(message) => m_byte = b;
        protected InvalidByteException(SerializationInfo info, StreamingContext context) : base(info, context) => info.AddValue("Byte", m_byte);
    }

    [Serializable]
    public class InvalidParameterException : InvalidByteException
    {
        protected string m_parameter;
        public byte Command => m_byte;

        public string Parameter => m_parameter;

        public InvalidParameterException(byte command, string parameter) : base(command, string.Format("Invalid parameter for command {0:X2} '{1}', parameter = \"{2}\"", command, (char)command, parameter))
        {
            m_parameter = parameter;
        }

        protected InvalidParameterException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            info.AddValue("Paramter", m_parameter);
        }
    }

    [Serializable]
    public class InvalidCommandException : InvalidByteException
    {
        protected string m_parameter;
        public byte Command => Byte;
        public string Parameter => m_parameter;

        public InvalidCommandException(byte command, string parameter) : base(command, string.Format("Invalid command {0:X2} '{1}', parameter = \"{2}\"", command, (char)command, parameter))
        {
            m_parameter = parameter;
        }

        protected InvalidCommandException(SerializationInfo info, StreamingContext context) : base(info, context) => info.AddValue("Paramter", m_parameter);
    }

    public enum Direction
    {
        Up,
        Down,
        Forward,
        Backward
    }

    public enum ClearDirection
    {
        Forward = 0,
        Backward = 1,
        Both = 2
    }

    public enum GraphicRendition
    {
        /// all attributes off
        Reset = 0,
        /// Intensity: Bold
        Bold = 1,
        /// Intensity: Faint     not widely supported
        Faint = 2,
        /// Italic: on     not widely supported. Sometimes treated as inverse.
        Italic = 3,
        /// Underline: Single     not widely supported
        Underline = 4,
        /// Blink: Slow     less than 150 per minute
        BlinkSlow = 5,
        /// Blink: Rapid     MS-DOS ANSI.SYS; 150 per minute or more
        BlinkRapid = 6,
        /// Image: Negative     inverse or reverse; swap foreground and background
        Inverse = 7,
        /// Conceal     not widely supported
        Conceal = 8,
        /// Font selection (not sure which)
        Font1 = 10,
        /// Underline: Double
        UnderlineDouble = 21,
        /// Intensity: Normal     not bold and not faint
        NormalIntensity = 22,
        /// Underline: None     
        NoUnderline = 24,
        /// Blink: off     
        NoBlink = 25,
        /// Image: Positive
        ///
        /// Not sure what this is supposed to be, the opposite of inverse???
        Positive = 27,
        /// Reveal,     conceal off
        Reveal = 28,
        /// Set foreground color, normal intensity
        ForegroundNormalBlack = 30,
        ForegroundNormalRed = 31,
        ForegroundNormalGreen = 32,
        ForegroundNormalYellow = 33,
        ForegroundNormalBlue = 34,
        ForegroundNormalMagenta = 35,
        ForegroundNormalCyan = 36,
        ForegroundNormalWhite = 37,
        ForegroundNormalReset = 39,
        /// Set background color, normal intensity
        BackgroundNormalBlack = 40,
        BackgroundNormalRed = 41,
        BackgroundNormalGreen = 42,
        BackgroundNormalYellow = 43,
        BackgroundNormalBlue = 44,
        BackgroundNormalMagenta = 45,
        BackgroundNormalCyan = 46,
        BackgroundNormalWhite = 47,
        BackgroundNormalReset = 49,
        /// Set foreground color, high intensity (aixtem)
        ForegroundBrightBlack = 90,
        ForegroundBrightRed = 91,
        ForegroundBrightGreen = 92,
        ForegroundBrightYellow = 93,
        ForegroundBrightBlue = 94,
        ForegroundBrightMagenta = 95,
        ForegroundBrightCyan = 96,
        ForegroundBrightWhite = 97,
        ForegroundBrightReset = 99,
        /// Set background color, high intensity (aixterm)
        BackgroundBrightBlack = 100,
        BackgroundBrightRed = 101,
        BackgroundBrightGreen = 102,
        BackgroundBrightYellow = 103,
        BackgroundBrightBlue = 104,
        BackgroundBrightMagenta = 105,
        BackgroundBrightCyan = 106,
        BackgroundBrightWhite = 107,
        BackgroundBrightReset = 109,
    }

    public enum AnsiMode
    {
        ShowCursor,
        HideCursor,
        LineFeed,
        NewLine,
        CursorKeyToCursor,
        CursorKeyToApplication,
        ANSI,
        VT52,
        Columns80,
        Columns132,
        JumpScrolling,
        SmoothScrolling,
        NormalVideo,
        ReverseVideo,
        OriginIsAbsolute,
        OriginIsRelative,
        LineWrap,
        DisableLineWrap,
        AutoRepeat,
        DisableAutoRepeat,
        Interlacing,
        DisableInterlacing,
        NumericKeypad,
        AlternateKeypad,
    }

    public class AnsiDecoder : EscapeCharacterDecoder, IDisposable
    {
        new Encoding Encoding
        {
            get => m_encoding; 
            set
            {
                if (m_encoding != value)
                {
                    m_encoding = value;
                    m_decoder = m_encoding.GetDecoder();
                    m_encoder = m_encoding.GetEncoder();
                }
            }
        }

        public AnsiDecoder() : base() { }

        private int DecodeInt(string value, int defaultResult)
        {
            int result;
            return value.Length > 0 && int.TryParse(value.TrimStart('0'), out result) ? result : defaultResult;
        }

        protected override void ProcessCommand(byte _command, string _parameter)
        {
            switch ((char) _command)
            {
                case 'A': OnMoveCursor(Direction.Up, DecodeInt(_parameter, 1)); break;
                case 'B': OnMoveCursor(Direction.Down, DecodeInt(_parameter, 1)); break;
                case 'C': OnMoveCursor(Direction.Forward, DecodeInt(_parameter, 1)); break;
                case 'D': OnMoveCursor(Direction.Backward, DecodeInt(_parameter, 1)); break;
                case 'E': OnMoveCursorToBeginningOfLineBelow(DecodeInt(_parameter, 1)); break;
                case 'F': OnMoveCursorToBeginningOfLineAbove(DecodeInt(_parameter, 1)); break;
                case 'G': OnMoveCursorToColumn(DecodeInt(_parameter, 1 ) - 1); break;
                case 'H': 
                case 'f':
                    {
                        int separator = _parameter.IndexOf(';');
                        if (separator == -1) OnMoveCursorTo(new Point(0, 0));
                        else
                        {
                            string row = _parameter.Substring(0, separator);
                            string column = _parameter.Substring(separator + 1, _parameter.Length - separator - 1);
                            OnMoveCursorTo(new Point(DecodeInt(column, 1) - 1, DecodeInt(row, 1) - 1));
                        }
                    }; break;
                case 'J': OnClearScreen((ClearDirection)DecodeInt(_parameter, 0)); break;
                case 'K': OnClearLine((ClearDirection)DecodeInt(_parameter, 0)); break;
                case 'S': OnScrollPageUpwards(DecodeInt(_parameter, 1)); break;
                case 'T': OnScrollPageDownwards(DecodeInt(_parameter, 1)); break;
                case 'm':
                    {
                        string[] commands = _parameter.Split(';');
                        GraphicRendition[] renditionCommands = new GraphicRendition[commands.Length];
                        for (int i = 0 ; i < commands.Length ; ++i) renditionCommands[i] = (GraphicRendition)DecodeInt(commands[i], 0);
                        OnSetGraphicRendition(renditionCommands);
                    }; break;
                case 'n': if (_parameter == "6")  
                            {
                                Point cursorPosition = OnGetCursorPosition();
                                cursorPosition.X++;
                                cursorPosition.Y++;
                                string row = cursorPosition.Y.ToString();
                                string column = cursorPosition.X.ToString();
                                byte[] output = new byte[2 + row.Length + 1 + column.Length + 1];
                                int i = 0;
                                output[i++] = EscapeCharacter;
                                output[i++] = LeftBracketCharacter;
                                foreach (char c in row) output[i++] = (byte)c;
                                output[i++] = (byte)';';
                                foreach (char c in column) output[i++] = (byte)c;
                                output[i++] = (byte)'R';
                                OnOutput(output);
                            }; break;
                case 's': OnSaveCursor(); break;
                case 'u': OnRestoreCursor(); break;
                case 'l': switch (_parameter)
                            {
                                case "20": OnModeChanged(AnsiMode.LineFeed); break;                     // Set line feed mode
                                case "?1": OnModeChanged(AnsiMode.CursorKeyToCursor); break;            // Set cursor key to cursor  DECCKM 
                                case "?2": OnModeChanged(AnsiMode.VT52); break;                         // Set ANSI (versus VT52)  DECANM
                                case "?3": OnModeChanged(AnsiMode.Columns80); break;                    // Set number of columns to 80  DECCOLM 
                                case "?4": OnModeChanged(AnsiMode.JumpScrolling); break;                // Set jump scrolling  DECSCLM 
                                case "?5": OnModeChanged(AnsiMode.NormalVideo); break;                  // Set normal video on screen  DECSCNM 
                                case "?6": OnModeChanged(AnsiMode.OriginIsAbsolute); break;             // Set origin to absolute  DECOM 
                                case "?7": OnModeChanged(AnsiMode.DisableLineWrap); break;              // Reset auto-wrap mode  DECAWM  -- Disable line wrap
                                case "?8": OnModeChanged(AnsiMode.DisableAutoRepeat); break;            // Reset auto-repeat mode  DECARM 
                                case "?9": OnModeChanged(AnsiMode.DisableInterlacing); break;           // Reset interlacing mode  DECINLM 
                                case "?25": OnModeChanged(AnsiMode.HideCursor); break;
                                default: throw new InvalidParameterException(_command, _parameter);
                            }; break;
                case 'h': switch (_parameter)
                            {
                                case "": OnModeChanged(AnsiMode.ANSI); break;                          // Set ANSI (versus VT52)  DECANM
                                case "20": OnModeChanged(AnsiMode.NewLine); break;                      // Set new line mode
                                case "?1": OnModeChanged(AnsiMode.CursorKeyToApplication); break;       // Set cursor key to application  DECCKM
                                case "?3": OnModeChanged(AnsiMode.Columns132); break;                   // Set number of columns to 132  DECCOLM
                                case "?4": OnModeChanged(AnsiMode.SmoothScrolling); break;              // Set smooth scrolling  DECSCLM
                                case "?5": OnModeChanged(AnsiMode.ReverseVideo); break;                 // Set reverse video on screen  DECSCNM
                                case "?6": OnModeChanged(AnsiMode.OriginIsRelative); break;             // Set origin to relative  DECOM
                                case "?7": OnModeChanged(AnsiMode.LineWrap); break;                     // Set auto-wrap mode  DECAWM -- Enable line wrap
                                case "?8": OnModeChanged(AnsiMode.AutoRepeat); break;                   // Set auto-repeat mode  DECARM
                                case "?9": OnModeChanged(AnsiMode.Interlacing); break;                  // Set interlacing mode 
                                case "?25": OnModeChanged(AnsiMode.ShowCursor); break;
                                default: throw new InvalidParameterException(_command, _parameter);
                            }; break;
                case '>': OnModeChanged(AnsiMode.NumericKeypad); break;                                 // Set numeric keypad mode
                case '=': OnModeChanged(AnsiMode.AlternateKeypad); break;                               // Set alternate keypad mode (rto: non-numeric, presumably)


                default: throw new InvalidCommandException(_command, _parameter);
            }
        }

        protected override bool IsValidOneCharacterCommand(char command) => command == '=' || command == '>';
        
        private static string[] FUNCTIONKEY_MAP = { 
        //   F1    F2    F3    F4    F5    F6    F7    F8    F9    F10   F11   F12
            "11", "12", "13", "14", "15", "17", "18", "19", "20", "21", "23", "24",
        //   F13   F14   F15   F16   F17   F18   F19   F20   F21   F22
            "25", "26", "28", "29", "31", "32", "33", "34", "23", "24" };

        public override bool KeyPressed(Keys modifiers, Keys key)
        {
            if ((int)Keys.F1 <= (int) key && (int) key <= (int)Keys.F12)
            {
                byte[] r = new byte[5];
                r[0] = 0x1B;
                r[1] = (byte) '[';
                int n = (int) key - (int)Keys.F1;
                if ((modifiers & Keys.Shift) != Keys.None) n += 10;
                char tail;
                if (n >= 20) tail = (modifiers & Keys.Control) != Keys.None ? '@' : '$';
                else tail = (modifiers & Keys.Control) != Keys.None ? '^' : '~';
                string f = FUNCTIONKEY_MAP[n];
                r[2] = (byte) f[0];
                r[3] = (byte) f[1];
                r[4] = (byte) tail;
                OnOutput( r );
                return true;
            }
            else if (key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down)
            {
                byte[] r = new byte[3];
                r[0] = 0x1B;
                r[1] = (byte) '[';
                switch (key)
                {
                    case Keys.Up: r[2] = (byte) 'A'; break;
                    case Keys.Down: r[2] = (byte) 'B'; break;
                    case Keys.Right: r[2] = (byte) 'C'; break;
                    case Keys.Left: r[2] = (byte) 'D'; break;
                    default: throw new ArgumentException("unknown cursor key code", "key");
                }
                OnOutput(r);
                return true;
            }
            else
            {
                byte[] r = new byte[4];
                r[0] = 0x1B;
                r[1] = (byte) '[';
                r[3] = (byte) '~';
                switch (key)
                {
                    case Keys.Insert: r[2] = (byte)'1'; break;
                    case Keys.Home: r[2] = (byte)'2'; break;
                    case Keys.PageUp: r[2] = (byte)'3'; break;
                    case Keys.Delete: r[2] = (byte)'4'; break;
                    case Keys.End: r[2] = (byte)'5'; break;
                    case Keys.PageDown: r[2] = (byte)'6'; break;
                    case Keys.Enter: r = new byte[] { 13 }; break;
                    case Keys.Escape: r = new byte[] { 0x1B }; break;
                    case Keys.Tab: r = new byte[] { (byte)'\t' }; break;
                    default: return false;
                }
                OnOutput(r);
                return true;
            }
        }

    }
}
