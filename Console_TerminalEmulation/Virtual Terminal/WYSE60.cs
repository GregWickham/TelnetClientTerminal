using NetworkAccess;

namespace ConsoleTerminalEmulation
{
    public class Wyse60 : VirtualTerminal
    {
        public Wyse60(string hostname, int portNumber)
        {
            Network = new TelnetNVT(hostname, portNumber);
            Decoder = new Wyse60Decoder(Network);
            Display = new VT100Display(Decoder);
            Keyboard = new KeyboardEncoder_rxvt(Network);
        }
    }
}
