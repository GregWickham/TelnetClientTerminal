using System;
using System.Text;
using System.Collections.Generic;
using System.Windows.Forms;

using NetworkAccess;

namespace ConsoleTerminalEmulation
{
    public delegate void DecoderOutputDelegate(TerminalDecoder decoder, byte[] output);

    public abstract class EscapeCharacterDecoder : TerminalDecoder, IDisposable
    {
        public const byte EscapeCharacter = 0x1B;
        public const byte LeftBracketCharacter = 0x5B;
        public const byte XonCharacter = 17;
        public const byte XoffCharacter = 19;
        
        protected enum State
        {
            Normal,
            Command,
        }
        protected State m_state;
        protected Encoding m_encoding;
        protected Decoder m_decoder;
        protected Encoder m_encoder;
        private List<byte> m_commandBuffer;
        protected bool m_supportXonXoff;
        protected bool m_xOffReceived;
        protected List<byte[]> m_outBuffer;

        public Encoding Encoding
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
        
        public EscapeCharacterDecoder()
        {
            m_state = State.Normal;
            Encoding = Encoding.ASCII;
            m_commandBuffer = new List<byte>();
            m_supportXonXoff = true;
            m_xOffReceived = false;
            m_outBuffer = new List<byte[]>();
        }
        
        public void RegisterForInputFrom(NetworkInterface t)
        {
            t.ByteReceived += Input;
        }

        public void UnRegisterForInputFrom(NetworkInterface t)
        {
            t.ByteReceived -= Input;
        }

        protected virtual bool IsValidParameterCharacter(char c) => char.IsNumber(c) || c == ';' || c == '"' || c == '?';

        protected void AddToCommandBuffer(byte b)
        {
            if (m_supportXonXoff && (b == XonCharacter || b == XoffCharacter)) return;
            else m_commandBuffer.Add(b);
        }
       
        protected void AddToCommandBuffer(byte[] bytes)
        {
            if (m_supportXonXoff) foreach (byte b in bytes) if (!(b == XonCharacter || b == XoffCharacter)) m_commandBuffer.Add(b);
            else m_commandBuffer.AddRange(bytes);
        }

        protected virtual bool IsValidOneCharacterCommand(char command) => false;
       
        protected void ProcessCommandBuffer()
        {            
            m_state = State.Command;            
            if (m_commandBuffer.Count > 1)
            {
                if (m_commandBuffer[0] != EscapeCharacter) throw new Exception ( "Internal error, first command character _MUST_ be the escape character, please report this bug to the author." );
                
                int start = 1;
                // Is this a one or two byte escape code?
                if (m_commandBuffer[start] == LeftBracketCharacter)
                {
                    start ++;                    
                    // It is a two byte escape code, but we still need more data
                    if (m_commandBuffer.Count < 3) return;
                }

                bool insideQuotes = false;
                int end = start;
                while (end < m_commandBuffer.Count && (IsValidParameterCharacter((char) m_commandBuffer[end]) || insideQuotes))
                {
                    if (m_commandBuffer[end] == '"') insideQuotes = !insideQuotes;
                    end++;
                }

                if (m_commandBuffer.Count == 2 && IsValidOneCharacterCommand((char) m_commandBuffer[start]))
                {
                    end = m_commandBuffer.Count - 1;
                }
                if (end == m_commandBuffer.Count) return;   // More data needed
                
                Decoder decoder = Encoding.GetDecoder();
                byte[] parameterData = new byte[end - start];
                for (int i = 0; i < parameterData.Length; i++)
                {
                    parameterData[i] = m_commandBuffer[start + i];
                }
                int parameterLength = decoder.GetCharCount(parameterData, 0, parameterData.Length);
                char[] parameterChars = new char[parameterLength];
                decoder.GetChars(parameterData, 0, parameterData.Length, parameterChars, 0);
                string parameter = new string(parameterChars);
                
                byte command = m_commandBuffer[end];

                try
                {
                    ProcessCommand(command, parameter);
                }
                finally     // Remove the processed commands
                {                    
                    if (m_commandBuffer.Count == end - 1)   // All command bytes processed, we can go back to normal handling
                    {                        
                        m_commandBuffer.Clear();
                        m_state = State.Normal;
                    }
                    else
                    {
                        bool returnToNormalState = true;
                        for (int i = end + 1; i < m_commandBuffer.Count; i++)
                        {
                            if (m_commandBuffer[i] == EscapeCharacter)
                            {
                                m_commandBuffer.RemoveRange(0, i);
                                ProcessCommandBuffer();
                                returnToNormalState = false;
                            }
                            else ProcessNormalInput(m_commandBuffer[i]);
                        }
                        if (returnToNormalState)
                        {
                            m_commandBuffer.Clear();
                            m_state = State.Normal;
                        }
                    }
                }
            }
        }

        protected void ProcessNormalInput(byte b)
        {
            if (b == EscapeCharacter) throw new Exception("Internal error, ProcessNormalInput was passed an escape character, please report this bug to the author.");
            if (m_supportXonXoff && (b == XonCharacter || b == XoffCharacter)) return;

            byte[] data = new byte[] { b };
            int charCount = m_decoder.GetCharCount(data, 0, 1);
            char[] characters = new char[charCount];
            m_decoder.GetChars(data, 0, 1, characters, 0);

            if (charCount > 0) OnCharacters(characters);
        }

        public void Input(byte[] data)
        {            
            if (data.Length == 0) throw new ArgumentException("Input can not process an empty array.");

            if (m_supportXonXoff)
            {
                foreach (byte b in data)
                {
                    if (b == XoffCharacter) m_xOffReceived = true;
                    else if (b == XonCharacter)
                    {
                        m_xOffReceived = false;
                        if (m_outBuffer.Count > 0) foreach (byte[] output in m_outBuffer) OnOutput(output);
                    }
                }
            }
            
            switch (m_state)
            {
                case State.Normal:
                    if (data[0] == EscapeCharacter)
                    {
                        AddToCommandBuffer(data);
                        ProcessCommandBuffer();
                    }
                    else
                    {
                        int i = 0;
                        while (i < data.Length && data[i] != EscapeCharacter)
                        {
                            ProcessNormalInput(data[i]);
                            i++;
                        }
                        if (i != data.Length)
                        {
                            while (i < data.Length)
                            {
                                AddToCommandBuffer(data[i]);
                                i++;
                            }
                            ProcessCommandBuffer();
                        }
                    }
                    break;                
                case State.Command:
                    AddToCommandBuffer(data);
                    ProcessCommandBuffer();
                    break;
            }
        }

        public void Input(byte b)
        {
            if (m_supportXonXoff)
            {
                if (b == XoffCharacter) m_xOffReceived = true;
                else if (b == XonCharacter)
                {
                    m_xOffReceived = false;
                    if (m_outBuffer.Count > 0) foreach (byte[] output in m_outBuffer) OnOutput(output);
                }
            }
            switch (m_state)
            {
                case State.Normal:
                    if (b == EscapeCharacter)
                    {
                        AddToCommandBuffer(b);
                        ProcessCommandBuffer();
                    }
                    else ProcessNormalInput(b);
                    break;
                case State.Command:
                    AddToCommandBuffer(b);
                    ProcessCommandBuffer();
                    break;
            }
        }


        public void CharacterTyped(char character)
        {
            byte[] data = m_encoding.GetBytes(new char[] { character });
            OnOutput(data);
        }
       
        public virtual bool KeyPressed(Keys modifiers, Keys key) => false;

        public override void Dispose()
        {
            m_encoding = null;
            m_decoder = null;
            m_encoder = null;
            m_commandBuffer = null;
        }
        
        abstract protected void ProcessCommand (byte command, string parameter);

        public virtual event DecoderOutputDelegate Output;
        protected virtual void OnOutput(byte[] output)
        {
            if (Output != null)
            {
                if (m_supportXonXoff && m_xOffReceived) m_outBuffer.Add(output);
                else Output(this, output);
            }
        }
    }
}
