using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace NetworkAccess
{
    /// <summary>Represents a Telnet Network Virtual Terminal (NVT).  Responsible for Telnet command processing and Option negotiation.</summary>
    public class TelnetNVT : NetworkInterface 
    {
        /// <summary>The network socket over which this <see cref="TelnetNVT"/> communicates</summary>
        private readonly TcpClient Socket;
        private NetworkStream TCP { get; set; }                         // Gives access to the socket
        private int readTimeout = -1;                                   // Infinite (no timeout)

        internal void EnableReadTimeout(int timeoutMS) => readTimeout = timeoutMS;
        internal void DisableReadTimeout() => readTimeout = -1;         // Infinite (no timeout)

        /// <summary>Create a <see cref="TelnetNVT"/> to communicate with the host at <paramref name="hostname"/> and <paramref name="port"/></summary>
        public TelnetNVT(string hostname, int port)
        {
            Socket = new TcpClient(hostname, port);
            TCP = Socket.GetStream();
        }

        /// <summary>Shut down this <see cref="TelnetNVT"/> and dispose its resources</summary>
        public override void ShutDown()
        {
            TCP.Close();
            TCP.Dispose();
            Socket.Dispose();
        }

        /// <summary>Return true if input is available for reading on the socket</summary>
        protected override bool InputIsAvailable => TCP.DataAvailable;

        /// <summary>Read and return one "application level" byte from the telnet network interface.</summary>
        /// <remarks>"Application level" means the payload delivered by Telnet.  It DOES NOT include traffic of the Telnet protocol itself.
        /// If Telnet has some work to do as part of its own protocol -- for example, negotiation of options -- calling this method may cause
        /// a lot more than one byte to be exchanged over the wire.</remarks>
        public override byte ReadByte() => GetApplicationInputByte();

        /// <summary>The private implementation of <see cref="ReadByte"/>.  This implementation is specific to the Telnet protocol.</summary>
        /// <returns>A <see cref="byte"/> of data destined for the application layer</returns> 
        /// <remarks>Telnet option negotiation happens inside this method, hidden from the client</remarks>
        private byte GetApplicationInputByte()
        {
            // Keep reading bytes until we get one destined for the application layer
            while (true)
            {
                if (InputIsAvailable)
                {
                    byte inputByte = ReadByteFromTCP();
                    if (inputByte == TelnetCommand.IAC) // Decide whether to send the input to the Application, or process it within the NVT
                    {
                        byte possibleCommandDescriptionByte = ReadByteFromTCP();
                        if (possibleCommandDescriptionByte != TelnetCommand.IAC) // The IAC meant that a Telnet command follows, and the inputByte we just read is a command type.
                        {
                            TelnetCommand cmd = TelnetCommand.OfType(possibleCommandDescriptionByte).IncomingOn(this);
                            cmd.Process();
                        }
                        else // The IAC is part of a two-byte escape sequence, meaning a literal 0xFF character is part of the Application data stream
                        {
                            OnByteReceived(TelnetCommand.IAC);
                            return TelnetCommand.IAC;
                        }
                    }
                    else // inputByte was NOT an IAC, so send it to the Application
                    {
                        OnByteReceived(inputByte);
                        return inputByte;
                    }
                }
                else  // Now that there's no more input to be read, we can send queued telnet commands
                {
                    SendQueuedTelnetCommands();
                }
            }

            void SendQueuedTelnetCommands()
            {
                while (CommandsToBeSent.Count > 0)
                {
                    byte[] output = CommandsToBeSent.SelectMany(command => command.TelnetForm).ToArray();
                    WriteByteArray(output);
                    CommandsToBeSent.Clear();
                }
            }
        }

        /// <summary>Read and return a single <see cref="byte"/> of input from the socket</summary>
        internal byte ReadByteFromTCP() => (byte)TCP.ReadByte();

        /// <summary>Put the <see cref="TelnetCommand"/> <paramref name="commandToBeSent"/> in a queue so it can be sent when we finish reading input</summary>
        internal void EnqueueForSending(TelnetCommand commandToBeSent) => CommandsToBeSent.Add(commandToBeSent);

        /// <summary>The queue of <see cref="TelnetCommand"/>s waiting to be sent</summary>
        private List<TelnetCommand> CommandsToBeSent = new List<TelnetCommand>();

        /// <summary>The implementation of the generic <see cref="WriteByte(byte)"/> method for a Telnet NVT</summary>
        /// <param name="b">The <see cref="byte"/> to be sent</param>
        private protected override sealed void WriteByte(byte b) => WriteByteToTCP(b);

        /// <summary>Write a single <see cref="byte"/> <paramref name="b"/> to the socket</summary>
        private void WriteByteToTCP(byte b) => TCP.WriteByte(b);

        /// <summary>Write <paramref name="bytes"/> to the wire.</summary>
        /// <remarks>This is a TELNET PROTOCOL LEVEL method.  It will NOT "escape" 0xFF characters.</remarks> 
        private protected override sealed void WriteByteArray(byte[] bytes) { TCP.Write(bytes, 0, bytes.Length); }

        /// <summary>Write <paramref name="outString"/> to the wire.</summary>
        /// <remarks>This is an APPLICATION LEVEL method.  It will "escape" 0xFF characters if present, which you DO NOT want at the Telnet protocol level.</remarks>
        public override void Write(string outString)
        {
            base.Write(outString.Replace("\xFF", "\xFF\xFF"));
        }
    }
}
