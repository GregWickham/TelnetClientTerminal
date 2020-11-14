namespace ConsoleTerminalEmulation
{
    public class VT100Display : AnsiCompatibleDisplay
    {
        internal VT100Display(TerminalDecoder d) : base(80, 24) { RegisterForEventsFrom(d); }
    }
}
