using System;
using System.Drawing;

namespace ConsoleTerminalEmulation
{
    public class AnsiCompatibleDisplay : TerminalDisplay
    {
        private Point savedCursorPosition;

        public AnsiCompatibleDisplay(int width, int height) : base(width, height)
        {
            savedCursorPosition = Point.Empty;
        }

        internal override void RegisterForEventsFrom(TerminalDecoder decoder)
        {
            base.RegisterForEventsFrom(decoder);
            RegisterForAnsiEventsFrom((AnsiDecoder)decoder);
        }

        internal override void UnRegisterForEventsFrom(TerminalDecoder decoder)
        {
            base.RegisterForEventsFrom(decoder);
            UnRegisterForAnsiEventsFrom((AnsiDecoder)decoder);
        }

        protected void RegisterForAnsiEventsFrom(AnsiDecoder decoder)
        {
            decoder.SaveCursor += SaveCursor;
            decoder.RestoreCursor += RestoreCursor;
            decoder.MoveCursor += MoveCursor;
            decoder.MoveCursorTo += MoveCursorTo;
            decoder.MoveCursorToColumn += MoveCursorToColumn;
            decoder.MoveCursorToBeginningOfLineBelow += MoveCursorToBeginningOfLineBelow;
            decoder.MoveCursorToBeginningOfLineAbove += MoveCursorToBeginningOfLineAbove;
            decoder.ClearScreen += ClearScreen;
            decoder.ClearLine += ClearLine;
            decoder.SetGraphicRendition += SetGraphicRendition;
            decoder.ModeChanged += ModeChanged;
            decoder.GetSize += GetSize;
            decoder.GetCursorPosition += GetCursorPosition;
        }

        protected void UnRegisterForAnsiEventsFrom(AnsiDecoder decoder)
        {
            decoder.SaveCursor -= SaveCursor;
            decoder.RestoreCursor -= RestoreCursor;
            decoder.MoveCursor -= MoveCursor;
            decoder.MoveCursorTo -= MoveCursorTo;
            decoder.MoveCursorToColumn -= MoveCursorToColumn;
            decoder.MoveCursorToBeginningOfLineBelow -= MoveCursorToBeginningOfLineBelow;
            decoder.MoveCursorToBeginningOfLineAbove -= MoveCursorToBeginningOfLineAbove;
            decoder.ClearScreen -= ClearScreen;
            decoder.ClearLine -= ClearLine;
            decoder.SetGraphicRendition -= SetGraphicRendition;
            decoder.ModeChanged -= ModeChanged;
            decoder.GetSize -= GetSize;
            decoder.GetCursorPosition -= GetCursorPosition;
        }

        #region Decoder event handlers

        private void SaveCursor() => savedCursorPosition = cursorPosition;

        private void RestoreCursor()
        {
            CursorPosition = savedCursorPosition;
            OnCursorMoved();
        }

        private void MoveCursor(Direction direction, int amount)
        {
            switch (direction)
            {
                case Direction.Up: while (amount > 0) { CursorUp(); amount--; } break;
                case Direction.Down: while (amount > 0) { CursorDown(); amount--; } break;
                case Direction.Forward: while (amount > 0) { CursorForward(); amount--; } break;
                case Direction.Backward: while (amount > 0) { CursorBackward(); amount--; } break;
            }
            OnCursorMoved();
        }

        private void MoveCursorTo(Point position)
        {
            cursorPosition = position;
            OnCursorMoved();
        }

        private void MoveCursorToColumn(int columnNumber)
        {
            cursorPosition.X = columnNumber;
            OnCursorMoved();
        }

        private void MoveCursorToBeginningOfLineAbove(int lineNumberRelativeToCurrentLine)
        {
            cursorPosition.X = 0;
            while (lineNumberRelativeToCurrentLine > 0)
            {
                CursorUp();
                lineNumberRelativeToCurrentLine--;
            }
            OnCursorMoved();
        }

        private void ClearScreen(ClearDirection direction) => ClearScreen();

        private void ClearLine(ClearDirection direction)
        {
            switch (direction)
            {
                case ClearDirection.Forward:
                    for (int x = cursorPosition.X; x < Width; ++x) ClearCharacterPosition(x, cursorPosition.Y);
                    OnRegionChanged(new Rectangle(cursorPosition.X, cursorPosition.Y, Width - cursorPosition.X, 1));
                    break;
                case ClearDirection.Backward:
                    for (int x = cursorPosition.X; x >= 0; --x) ClearCharacterPosition(x, cursorPosition.Y);
                    OnRegionChanged(new Rectangle(0, cursorPosition.Y, cursorPosition.X, 1));
                    break;
                case ClearDirection.Both:
                    for (int x = 0; x < Width; ++x) ClearCharacterPosition(x, cursorPosition.Y);
                    OnRegionChanged(new Rectangle(0, cursorPosition.Y, Width, 1));
                    break;
            }
        }

        private void SetGraphicRendition(GraphicRendition[] commands)
        {
            foreach (GraphicRendition command in commands)
            {
                switch (command)
                {
                    case GraphicRendition.Reset: currentAttributes.Reset(); break;
                    case GraphicRendition.Bold: currentAttributes.Bold = true; break;
                    case GraphicRendition.Faint: currentAttributes.Faint = true; break;
                    case GraphicRendition.Italic: currentAttributes.Italic = true; break;
                    case GraphicRendition.Underline: currentAttributes.Underline = Underline.Single; break;
                    case GraphicRendition.BlinkSlow: currentAttributes.Blink = Blink.Slow; break;
                    case GraphicRendition.BlinkRapid: currentAttributes.Blink = Blink.Rapid; break;
                    case GraphicRendition.Positive:
                    case GraphicRendition.Inverse:
                        {
                            TextColor tmp = currentAttributes.Foreground;
                            currentAttributes.Foreground = currentAttributes.Background;
                            currentAttributes.Background = tmp;
                        }
                        break;
                    case GraphicRendition.Conceal: currentAttributes.Conceal = true; break;
                    case GraphicRendition.UnderlineDouble: currentAttributes.Underline = Underline.Double; break;
                    case GraphicRendition.NormalIntensity: currentAttributes.Bold = false; currentAttributes.Faint = false; break;
                    case GraphicRendition.NoUnderline: currentAttributes.Underline = Underline.None; break;
                    case GraphicRendition.NoBlink: currentAttributes.Blink = Blink.None; break;
                    case GraphicRendition.Reveal: currentAttributes.Conceal = false; break;
                    case GraphicRendition.ForegroundNormalBlack: currentAttributes.Foreground = TextColor.Black; break;
                    case GraphicRendition.ForegroundNormalRed: currentAttributes.Foreground = TextColor.Red; break;
                    case GraphicRendition.ForegroundNormalGreen: currentAttributes.Foreground = TextColor.Green; break;
                    case GraphicRendition.ForegroundNormalYellow: currentAttributes.Foreground = TextColor.Yellow; break;
                    case GraphicRendition.ForegroundNormalBlue: currentAttributes.Foreground = TextColor.Blue; break;
                    case GraphicRendition.ForegroundNormalMagenta: currentAttributes.Foreground = TextColor.Magenta; break;
                    case GraphicRendition.ForegroundNormalCyan: currentAttributes.Foreground = TextColor.Cyan; break;
                    case GraphicRendition.ForegroundNormalWhite: currentAttributes.Foreground = TextColor.White; break;
                    case GraphicRendition.ForegroundNormalReset: currentAttributes.Foreground = TextColor.White; break;
                    case GraphicRendition.BackgroundNormalBlack: currentAttributes.Background = TextColor.Black; break;
                    case GraphicRendition.BackgroundNormalRed: currentAttributes.Background = TextColor.Red; break;
                    case GraphicRendition.BackgroundNormalGreen: currentAttributes.Background = TextColor.Green; break;
                    case GraphicRendition.BackgroundNormalYellow: currentAttributes.Background = TextColor.Yellow; break;
                    case GraphicRendition.BackgroundNormalBlue: currentAttributes.Background = TextColor.Blue; break;
                    case GraphicRendition.BackgroundNormalMagenta: currentAttributes.Background = TextColor.Magenta; break;
                    case GraphicRendition.BackgroundNormalCyan: currentAttributes.Background = TextColor.Cyan; break;
                    case GraphicRendition.BackgroundNormalWhite: currentAttributes.Background = TextColor.White; break;
                    case GraphicRendition.BackgroundNormalReset: currentAttributes.Background = TextColor.Black; break;
                    case GraphicRendition.ForegroundBrightBlack: currentAttributes.Foreground = TextColor.BrightBlack; break;
                    case GraphicRendition.ForegroundBrightRed: currentAttributes.Foreground = TextColor.BrightRed; break;
                    case GraphicRendition.ForegroundBrightGreen: currentAttributes.Foreground = TextColor.BrightGreen; break;
                    case GraphicRendition.ForegroundBrightYellow: currentAttributes.Foreground = TextColor.BrightYellow; break;
                    case GraphicRendition.ForegroundBrightBlue: currentAttributes.Foreground = TextColor.BrightBlue; break;
                    case GraphicRendition.ForegroundBrightMagenta: currentAttributes.Foreground = TextColor.BrightMagenta; break;
                    case GraphicRendition.ForegroundBrightCyan: currentAttributes.Foreground = TextColor.BrightCyan; break;
                    case GraphicRendition.ForegroundBrightWhite: currentAttributes.Foreground = TextColor.BrightWhite; break;
                    case GraphicRendition.ForegroundBrightReset: currentAttributes.Foreground = TextColor.White; break;
                    case GraphicRendition.BackgroundBrightBlack: currentAttributes.Background = TextColor.BrightBlack; break;
                    case GraphicRendition.BackgroundBrightRed: currentAttributes.Background = TextColor.BrightRed; break;
                    case GraphicRendition.BackgroundBrightGreen: currentAttributes.Background = TextColor.BrightGreen; break;
                    case GraphicRendition.BackgroundBrightYellow: currentAttributes.Background = TextColor.BrightYellow; break;
                    case GraphicRendition.BackgroundBrightBlue: currentAttributes.Background = TextColor.BrightBlue; break;
                    case GraphicRendition.BackgroundBrightMagenta: currentAttributes.Background = TextColor.BrightMagenta; break;
                    case GraphicRendition.BackgroundBrightCyan: currentAttributes.Background = TextColor.BrightCyan; break;
                    case GraphicRendition.BackgroundBrightWhite: currentAttributes.Background = TextColor.BrightWhite; break;
                    case GraphicRendition.BackgroundBrightReset: currentAttributes.Background = TextColor.Black; break;
                    case GraphicRendition.Font1: break;
                    default: throw new Exception("Unknown rendition command");
                }
            }
            OnGraphicRenditionChanged();
        }

        private void ModeChanged(AnsiMode mode)
        {
            switch (mode)
            {
                case AnsiMode.HideCursor: showCursor = false; break;
                case AnsiMode.ShowCursor: showCursor = true; break;
            }
        }

        #endregion
    }
}
