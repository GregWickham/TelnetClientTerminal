using System.Text;

namespace NetworkAccess
{
    /// <summary>Signature of the event handler for the <see cref="NetworkInterface.ByteReceived"/> event</summary>
    public delegate void ByteReceived_EventHandler(byte b);

    /// <summary>The base class for a <see cref="NetworkInterface"/> that provides client access to a Telnet socket</summary>
    public abstract class NetworkInterface
    {
        /// <summary>Close the <see cref="NetworkInterface"/></summary>
        public abstract void ShutDown();

        /// <summary>Subscribe to this event to be notified when a byte is received from the Telnet interface</summary>
        public event ByteReceived_EventHandler ByteReceived;
        protected void OnByteReceived(byte b) => ByteReceived?.Invoke(b);

        /// <summary>Write character <paramref name="c"/> to the <see cref="NetworkInterface"/></summary>
        public void WriteChar(char c) => WriteByte((byte)c);

        /// <summary>Write string <paramref name="outString"/> to the <see cref="NetworkInterface"/></summary>
        public virtual void Write(string outString)
        {
            WriteByteArray(Encoding.ASCII.GetBytes(outString));
        }

        /// <summary>Write string <paramref name="aLine"/> to the <see cref="NetworkInterface"/>, and append CR/LF</summary>
        public void WriteLine(string aLine)
        {
            StringBuilder sb = new StringBuilder(aLine);
            sb.Append("\r\n");
            byte[] output = Encoding.ASCII.GetBytes(sb.ToString());
            WriteByteArray(output);
        }

        /// <summary>Read all available input from the <see cref="NetworkInterface"/></summary>
        public void ReadAllInput() { while (InputIsAvailable) ReadByte(); }

        /// <summary>Read exactly one byte from the <see cref="NetworkInterface"/></summary>
        public abstract byte ReadByte();

        private protected abstract void WriteByte(byte b);

        private protected abstract void WriteByteArray(byte[] output);

        protected abstract bool InputIsAvailable { get; }
    }
}
