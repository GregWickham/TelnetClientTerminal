using NetworkAccess;

namespace ConsoleTerminalEmulation
{
    /// <summary>This is a (mostly) VT100-compliant virtual terminal that can be instantiated by a .NET client application to communicate with a server application over a Telnet connection</summary>
    public class VT100 : VirtualTerminal
    { 
        public VT100(string hostname, int portNumber)
        {
            Network = new TelnetNVT(hostname, portNumber);
            Decoder = new VT100Decoder(Network);
            Display = new VT100Display(Decoder);
            Keyboard = new KeyboardEncoder_rxvt(Network);
        }
    }
}
