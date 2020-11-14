using System;
using System.Drawing;

namespace ConsoleTerminalEmulation
{
    public delegate void DisplayableCharacter_EventHandler(char c);
    public delegate void SaveCursor_EventHandler();
    public delegate void RestoreCursor_EventHandler();
    public delegate void MoveCursor_EventHandler(Direction direction, int amount);
    public delegate void MoveCursorTo_EventHandler(Point position);
    public delegate void MoveCursorToColumn_EventHandler(int columnNumber);
    public delegate void MoveCursorToBeginningOfLineBelow_EventHandler(int lineNumberRelativeToCurrentLine);
    public delegate void MoveCursorToBeginningOfLineAbove_EventHandler(int lineNumberRelativeToCurrentLine);
    public delegate void ClearScreen_EventHandler(ClearDirection direction);
    public delegate void ClearLine_EventHandler(ClearDirection direction);
    public delegate void ScrollPageUpwards_EventHandler(int linesToScroll);
    public delegate void ScrollPageDownwards_EventHandler(int linesToScroll);
    public delegate void SetGraphicRendition_EventHandler(GraphicRendition[] commands);
    public delegate void ModeChanged_EventHandler(AnsiMode mode);
    public delegate Size GetSize_EventHandler();
    public delegate Point GetCursorPosition_EventHandler();

    public abstract class TerminalDecoder : IDisposable
    {
        public static bool Log { get; set; } = false;

        public event DisplayableCharacter_EventHandler DisplayableCharacter;
        public event SaveCursor_EventHandler SaveCursor;
        public event RestoreCursor_EventHandler RestoreCursor;
        public event MoveCursor_EventHandler MoveCursor;
        public event MoveCursorTo_EventHandler MoveCursorTo;
        public event MoveCursorToColumn_EventHandler MoveCursorToColumn;
        public event MoveCursorToBeginningOfLineBelow_EventHandler MoveCursorToBeginningOfLineBelow;
        public event MoveCursorToBeginningOfLineAbove_EventHandler MoveCursorToBeginningOfLineAbove;
        public event ClearScreen_EventHandler ClearScreen;
        public event ClearLine_EventHandler ClearLine;
        public event ScrollPageUpwards_EventHandler ScrollPageUpwards;
        public event ScrollPageDownwards_EventHandler ScrollPageDownwards;
        public event SetGraphicRendition_EventHandler SetGraphicRendition;
        public event ModeChanged_EventHandler ModeChanged;
        public event GetSize_EventHandler GetSize;
        public event GetCursorPosition_EventHandler GetCursorPosition;

        protected void OnCharacters(char[] characters)
        {
            foreach (char c in characters)
            {
                if (Log)
                {
                    Console.Write(c);
                }
                DisplayableCharacter?.Invoke(c);
            }
        }
        protected virtual void OnSaveCursor() { SaveCursor?.Invoke(); }
        protected virtual void OnRestoreCursor() { RestoreCursor?.Invoke(); }
        protected virtual void OnMoveCursor(Direction direction, int amount) { MoveCursor?.Invoke(direction, amount); }
        protected virtual void OnMoveCursorTo(Point position) { MoveCursorTo?.Invoke(position); }
        protected virtual void OnMoveCursorToColumn(int columnNumber) { MoveCursorToColumn?.Invoke(columnNumber); }
        protected virtual void OnMoveCursorToBeginningOfLineBelow(int lineNumberRelativeToCurrentLine) { MoveCursorToBeginningOfLineBelow?.Invoke(lineNumberRelativeToCurrentLine); }
        protected virtual void OnMoveCursorToBeginningOfLineAbove(int lineNumberRelativeToCurrentLine) { MoveCursorToBeginningOfLineAbove?.Invoke(lineNumberRelativeToCurrentLine); }
        protected virtual void OnClearScreen(ClearDirection direction) { ClearScreen?.Invoke(direction); }
        protected virtual void OnClearLine(ClearDirection direction) { ClearLine?.Invoke(direction); }
        protected virtual void OnScrollPageUpwards(int linesToScroll) { ScrollPageUpwards?.Invoke(linesToScroll); }
        protected virtual void OnScrollPageDownwards(int linesToScroll) { ScrollPageDownwards?.Invoke(linesToScroll); }
        protected virtual void OnSetGraphicRendition(GraphicRendition[] commands) { SetGraphicRendition?.Invoke(commands); }
        protected virtual void OnModeChanged(AnsiMode mode) { ModeChanged?.Invoke(mode); }

        // This is unlike other event handlers in that we are asking the client for information about its state.  In a real world console, there would be one decoder
        // serving one terminal with one screen, but in this simulation we can have multiple clients registered for event notifications from the decoder (through
        // multicast delegates of course).  In theory the response to this query would be the same from any attached screen, but in practice what would be the result
        // of sending this query to N screens and getting N responses?  It wouldn't make any sense.  So we just get the first invocation from the multicast event,
        // invoke that, and return the response.
        protected virtual Point OnGetCursorPosition()
        {
            Delegate[] clients = GetCursorPosition.GetInvocationList();
            GetCursorPosition_EventHandler firstClient = (GetCursorPosition_EventHandler)clients[0];
            return firstClient.Invoke();
        }

        public abstract void Dispose();
    }
}
