using NetworkAccess;

namespace ConsoleTerminalEmulation
{
    public delegate void TextEntry_EventHandler(string text);

    /// <summary>The base class for any type of <see cref="VirtualTerminal"/> used by a .NET application</summary>
    public abstract class VirtualTerminal
    {
        /// <summary>The <see cref="VirtualTerminal"/>'s <see cref="NetworkInterface"/></summary>
        internal NetworkInterface Network { get; private protected set; }

        /// <summary>Used by a <see cref="VirtualTerminal"/> to translate keystrokes into the appropriate byte strings over the network</summary>
        public KeyboardEncoder Keyboard { get; private protected set; }

        /// <summary>Decodes escape sequences incoming from the network and translates them into terminal commands, mostly for control of the display</summary>
        public TerminalDecoder Decoder { get; private protected set; }

        /// <summary>Maintains a 2D array of the character locations on a virtual display that can be queried by a client application to accomplish screen scraping.</summary>
        public TerminalDisplay Display { get; private protected set; }

        /// <summary>Connect the Display so it receives commands from the Decoder</summary>
        public void EnableScreen() => Display.RegisterForEventsFrom(Decoder);

        /// <summary>Disconnect the Display so it does NOT receive commands from the Decoder</summary>
        /// <remarks>The current Display implementation does not gracefully support scrolling.  If the server application sends some output that would normally cause the display to scoll,
        /// this method can be used to temporarily disable the screen and avoid writing to locations that are outside the screen bounds.</remarks>
        public void DisableScreen() => Display.UnRegisterForEventsFrom(Decoder);

        /// <summary>Shut down the <see cref="VirtualTerminal"/></summary>
        public void ShutDown()
        {
            DisableScreen();
            Network.ShutDown();
            Decoder.Dispose();
        }

        /// <summary>Read and return the next byte from the network.</summary>
        /// <remarks>May have side effects on the Display.</remarks>
        public void GetNextByte() => Network.ReadByte();

        /// <summary>Read and return all available input from the network.</summary>
        /// <remarks>May have side effects on the Display.</remarks>
        public void ReadAllInput() => Network.ReadAllInput();

        /// <summary>Send <paramref name="someOutput"/> out on the Network, and raise the <see cref="TextEntered"/> event</summary>
        public void SendOutput(string someOutput)
        {
            Network.Write(someOutput);
            OnTextEntered(someOutput);
        }

        /// <summary>Send <paramref name="someOutput"/> out on the Network with CR/LF appended, and raise the <see cref="TextEntered"/> event</summary>
        public void SendLine(string aLine)
        {
            Network.WriteLine(aLine);
            OnTextEntered(aLine + @"\r\n");
        }

        /// <summary>Subscribe to this event to be notified when this <see cref="VirtualTerminal"/> has sent text out over the network</summary>
        public event TextEntry_EventHandler TextEntered;
        private void OnTextEntered(string text) => TextEntered?.Invoke(text);
    }
}
