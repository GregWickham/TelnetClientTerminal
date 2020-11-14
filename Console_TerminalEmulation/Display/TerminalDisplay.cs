// If #DisplayableCharacterLogging is defined, show characters that will be displayed on-Screen in debug output
#define DisplayableCharacterLogging

using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace ConsoleTerminalEmulation
{
    public class CursorPositionOffScreenException : Exception 
    {
        internal CursorPositionOffScreenException(string message) : base(message) { }
    }

    public delegate void CharacterLocationChanged_EventHandler(int column, int row);
    public delegate void CursorMoved_EventHandler(Point newCursorLocation);
    public delegate void GraphicRenditionChanged_EventHandler(GraphicAttributes attributes);
    public delegate void RegionChanged_EventHandler(Rectangle region);
    public delegate void ScreenChanged_EventHandler();

    public enum Blink
    {
        None,
        Slow,
        Rapid,
    }

    public enum Underline
    {
        None,
        Single,
        Double,
    }

    public enum TextColor
    {
        Black,
        Red,
        Green,
        Yellow,
        Blue,
        Magenta,
        Cyan,
        White,
        BrightBlack,
        BrightRed,
        BrightGreen,
        BrightYellow,
        BrightBlue,
        BrightMagenta,
        BrightCyan,
        BrightWhite,
    }

    public struct GraphicAttributes
    {
        public bool Bold { get; set; }
        public bool Faint { get; set; }
        public bool Italic { get; set; }
        public Underline Underline { get; set; }
        public Blink Blink { get; set; }
        public bool Conceal { get; set; }
        public TextColor Foreground { get; set; }
        public TextColor Background { get; set; }
        public Color ForegroundColor => TextColorToColor(Foreground); 
        public Color BackgroundColor => TextColorToColor(Background); 

        public Color TextColorToColor(TextColor textColor)
        {
            switch (textColor)
            {
                case TextColor.Black: return Color.Black;
                case TextColor.Red: return Color.DarkRed;
                case TextColor.Green: return Color.Green;
                case TextColor.Yellow: return Color.Yellow;
                case TextColor.Blue: return Color.Blue;
                case TextColor.Magenta: return Color.DarkMagenta;
                case TextColor.Cyan: return Color.Cyan;
                case TextColor.White: return Color.White;
                case TextColor.BrightBlack: return Color.Gray;
                case TextColor.BrightRed: return Color.Red;
                case TextColor.BrightGreen: return Color.LightGreen;
                case TextColor.BrightYellow: return Color.LightYellow;
                case TextColor.BrightBlue: return Color.LightBlue;
                case TextColor.BrightMagenta: return Color.DarkMagenta;
                case TextColor.BrightCyan: return Color.LightCyan;
                case TextColor.BrightWhite: return Color.Gray;
            }
            throw new ArgumentOutOfRangeException("textColor", "Unknown color value.");
        }

        public void Reset()
        {
            Bold = false;
            Faint = false;
            Italic = false;
            Underline = Underline.None;
            Blink = Blink.None;
            Conceal = false;
            Foreground = TextColor.White;
            Background = TextColor.Black;
        }
    }

    public class CharacterLocation
    {
        public char Char { get; set; }
        public GraphicAttributes Attributes { get; set; }

        public CharacterLocation() : this(' ') { }

        public CharacterLocation(char c)
        {
            Char = c;
            Attributes = new GraphicAttributes();
        }
    }

    public abstract class TerminalDisplay
    {
        protected Point cursorPosition;
        protected bool showCursor;
        protected CharacterLocation[,] locations;
        protected GraphicAttributes currentAttributes;
        private bool usingVT100PseudographicCharacters = false;
        private static readonly Dictionary<char, char> vt100PseudographicCharacters = new Dictionary<char, char>()
        {
            { 'j', '┘' },
            { 'k', '┐' },
            { 'l', '┌' },
            { 'm', '└' },
            { 'n', '┼' },
            { 'q', '─' },
            { 't', '├' },
            { 'u', '┤' },
            { 'v', '┴' },
            { 'w', '┬' },
            { 'x', '│' }
        };
        public bool CheckCursorBounds { get; set; } = true;

        public TerminalDisplay(int width, int height)
        {
            Size = new Size(width, height);
            showCursor = true;
            currentAttributes.Reset();            
        }

        internal virtual void RegisterForEventsFrom(TerminalDecoder decoder)
        {
            RegisterForDisplayEventsFrom(decoder);
        }

        internal virtual void UnRegisterForEventsFrom(TerminalDecoder decoder)
        {
            UnRegisterForDisplayEventsFrom(decoder);
        }

        protected void RegisterForDisplayEventsFrom(TerminalDecoder decoder)
        {
            decoder.DisplayableCharacter += DisplayCharacter;
        }

        protected void UnRegisterForDisplayEventsFrom(TerminalDecoder decoder)
        {
            decoder.DisplayableCharacter -= DisplayCharacter;
        }

        #region Decoder event handlers

        private void DisplayCharacter(char c)
        {
            switch (c)
            {
                case '\n': MoveCursorToBeginningOfLineBelow(1); break;
                case '\r': break;
                case '\u0007': break;   // BELL
                case '\u000e': usingVT100PseudographicCharacters = true; break;
                case '\u000f': usingVT100PseudographicCharacters = false; break;
                default:
                    {
                        char renderedCharacter;
                        if (usingVT100PseudographicCharacters) renderedCharacter = translateToVT100Pseudographic(c);
                        else renderedCharacter = c;
                        try
                        {
                            this[cursorPosition].Char = renderedCharacter;
                            this[cursorPosition].Attributes = currentAttributes;
                            OnLocationChanged(cursorPosition.X, cursorPosition.Y);
                            CursorForward();
                        }
                        // If the screen position is out of range, ignore it and continue.
                        // Sometimes SIMS does this and expects the screen to scroll, but it should get cleared up in short order.
                        catch (ArgumentOutOfRangeException ex) { Debug.WriteLine(ex.Message); }
                        break;
                    }
            }

            char translateToVT100Pseudographic(char ch)
            {
                char result;
                return vt100PseudographicCharacters.TryGetValue(ch, out result) ? result : ch;
            }
        }

        protected void MoveCursorToBeginningOfLineBelow(int lineNumberRelativeToCurrentLine)
        {
            cursorPosition.X = 0;
            while (lineNumberRelativeToCurrentLine > 0)
            {
                CursorDown();
                lineNumberRelativeToCurrentLine--;
            }
            OnCursorMoved();
        }

        protected Size GetSize() => Size;

        protected Point GetCursorPosition() => new Point(cursorPosition.X + 1, cursorPosition.Y + 1);

        #endregion

        protected Size Size
        {
            get => new Size(Width, Height);
            set
            {
                if (locations == null || value.Width != Width || value.Height != Height)
                {
                    locations = new CharacterLocation[value.Width, value.Height];
                    populateScreen();
                }

                void populateScreen()
                {
                    for (int y = 0; y < Height; ++y)
                        for (int x = 0; x < Width; ++x)
                            this[x, y] = new CharacterLocation();
                    cursorPosition.X = 0;
                    cursorPosition.Y = 0;
                    OnScreenChanged();
                }
            }
        }

        public int Width => locations.GetLength(0);

        public int Height => locations.GetLength(1);

        public CharacterLocation this[int column, int row]
        {
            get => PointIsOnscreen(column, row) ? locations[column, row] : new CharacterLocation();
            set
            {
                if (PointIsOnscreen(column, row))
                {
                    locations[column, row] = value;
                }
            }
        }

        protected bool PointIsOnscreen(int column, int row) => (column < Width) && (row < Height);

        public CharacterLocation this[Point position]
        {
            get => this[position.X, position.Y];
            set => this[position.X, position.Y] = value;
        }

        public Point CursorPosition
        {
            get => cursorPosition;
            protected set { if ((cursorPosition != value) && PointIsOnscreen(value.X, value.Y)) cursorPosition = value; }
        }

        protected void CursorForward()
        {
            if (cursorPosition.X + 1 >= Width)
            {
                cursorPosition.X = 0;
                ++cursorPosition.Y;
            }
            else ++cursorPosition.X;
            OnCursorMoved();
        }

        protected void CursorBackward()
        {
            if (cursorPosition.X - 1 < 0)
            {
                cursorPosition.X = Width - 1;
                --cursorPosition.Y;
            }
            else --cursorPosition.X;
            OnCursorMoved();
        }

        protected void CursorDown()
        {
            if (cursorPosition.Y + 1 >= Height)
            {
                if (CheckCursorBounds) throw new CursorPositionOffScreenException("Cannot move the cursor further down");
            }
            else
            {
                ++cursorPosition.Y;
                OnCursorMoved();
            }
        }

        protected void CursorUp()
        {
            if (cursorPosition.Y - 1 < 0)
            {
                if (CheckCursorBounds) throw new CursorPositionOffScreenException("Cannot move the cursor further up");
            }
            else
            {
                --cursorPosition.Y;
                OnCursorMoved();
            }
        }

        protected void ClearScreen()
        {
            for (int y = 0; y < Height; ++y)
                for (int x = 0; x < Width; ++x)
                    ClearCharacterPosition(x, y);
            cursorPosition.X = 0;
            cursorPosition.Y = 0;
            OnScreenChanged();
        }

        protected void ClearCharacterPosition(int x, int y)
        {
            this[x, y].Char = ' ';
            this[x, y].Attributes = new GraphicAttributes();
        }
        
        void Dispose() => locations = null;

        #region Scraping

        public string GetStringAtLocation(Point location, int length)
        {
            StringBuilder sb = new StringBuilder();
            for (int horizontalPosition = location.X; horizontalPosition < location.X + length; horizontalPosition++)
            {
                sb.Append(this[horizontalPosition, location.Y].Char);
            }
            return sb.ToString();
        }

        public bool StringIsAtLocation(string toLookFor, Point location)
        {
            for (int i=0; i < toLookFor.Length; i++)
            {
                if (this[location.X + i, location.Y].Char != toLookFor[i])
                    return false;
            }
            return true;
        }

        public bool TextIsToTheLeftOfCursor(string toLookFor, int offset)
        {
            int potentialStringStart = CursorPosition.X - offset - toLookFor.Length;
            return potentialStringStart < 0 ? false : StringIsAtLocation(toLookFor, new Point(potentialStringStart, CursorPosition.Y));
        }

        private static readonly Regex dateRegex = new Regex(@"\b\d{2}\/\d{2}\/\d{2}");

        public string ScrapeDateStringFrom(Point location, int length)
        {
            MatchCollection matches = dateRegex.Matches(GetStringAtLocation(location, length));
            if (matches.Count > 0)
            {
                return matches[0].Value;
            }
            else return null;
        }

        private DateTime? ParseDateFrom(string dateString)
        {
            if (DateTime.TryParseExact(dateString, "MM/dd/yy", null, DateTimeStyles.None, out DateTime dt))
                return dt;
            else return null;
        }

        public string ScrapeDateStringFromLine(int lineNumber) => ScrapeDateStringFrom(new Point(0, lineNumber), Width);

        public DateTime? ScrapeDateFromLine(int lineNumber) => ParseDateFrom(ScrapeDateStringFrom(new Point(0, lineNumber), Width));

        public bool BackgroundColorIs(Color c, Point location, int length)
        {
            bool result = true;
            Point currentPoint = new Point(location.X, location.Y);
            for (int i = 0; i < length; i++)
            {
                currentPoint.X = location.X + i;
                result = result && this[currentPoint].Attributes.BackgroundColor == c;
            }
            return result;
        }

        #endregion

        public event CharacterLocationChanged_EventHandler LocationChanged;
        protected void OnLocationChanged(int column, int row) => LocationChanged?.Invoke(column, row);

        public event CursorMoved_EventHandler CursorMoved;
        protected void OnCursorMoved() => CursorMoved?.Invoke(CursorPosition);

        public event GraphicRenditionChanged_EventHandler GraphicRenditionChanged;
        protected void OnGraphicRenditionChanged() => GraphicRenditionChanged?.Invoke(currentAttributes);

        public event RegionChanged_EventHandler RegionChanged;
        protected void OnRegionChanged(Rectangle region) => RegionChanged?.Invoke(region);

        public event ScreenChanged_EventHandler ScreenChanged;
        protected void OnScreenChanged() => ScreenChanged?.Invoke();
    }
}
