using System;
using System.Collections.Generic;

namespace NetworkAccess
{
    /// <summary>Instances of <see cref="TelnetOption"/> represent options that can be negotiated when establishing a Telnet session</summary>
    public class TelnetOption
    {
        /// <summary>All the option types defined by the Telnet protocol</summary>
        private enum OptionType : int
        {
            BinaryXmit = 0x00,
            Echo = 0x01,
            Reconnect = 0x02,
            SuppressGoAhead = 0x03,
            MessageSize = 0x04,
            OptionStatus = 0x05,
            TimingMark = 0x06,
            RC_XmtEcho = 0x07,
            LineWidth = 0x08,
            PageLength = 0x09,
            CR_Use = 0x0A,
            HorizTabs = 0x0B,
            HorizTab_Use = 0x0C,
            FormFeed_Use = 0x0D,
            VertTabs = 0x0E,
            VertTabUse = 0x0F,
            LF_Use = 0x10,
            ExtendedASCII = 0x11,
            Logout = 0x12,
            ByteMacro = 0x13,
            DataTerm = 0x14,
            SUPDUP = 0x15,
            SUPDUP_Output = 0x16,
            SendLocate = 0x17,
            Terminal_Type = 0x18,
            EndRecord = 0x19,
            TACACS_ID = 0x1A,
            OutputMark = 0x1B,
            TermLoc = 0x1C,
            Term_3270_Regime = 0x1D,
            X3_PAD = 0x1E,
            Window_Size = 0x1F,
            Terminal_Speed = 0x20,
            Remote_Flow_Control = 0x21,
            Linemode = 0x22,
            X_Display_Location = 0x23,
            Environment_Option = 0x24,
            AuthOpt = 0x25,
            EncryptOpt = 0x26,
            New_Environment_Option = 0x27,
            TN3270E = 0x28,
            XAUTH = 0x29,
            CHARSET = 0x2A,
            TelnetRemote = 0x2B,
            ComPortControlOpt = 0x2C,
            SuppressLocalEcho = 0x2D,
            StartTLS = 0x2E,
            KERMIT = 0x2F,
            SEND_URL = 0x30,
            FORWARD_X = 0x31,
            TELOPT_PRAGMA_LOGON = 0x8A,
            TELOPT_SSPI_LOGON = 0x8B,
            TELOPT_PRAGMA_HEARTBEAT = 0x8C,
            ExtendedOpts = 0xFF
        }

        /// <summary>Keys in this dictionary are option types, values are instances of <see cref="TelnetOption"/>.</summary>
        /// <remarks>All desired options are instantiated and registered during initialization.  Options that are not desired are never instantiated.</remarks>
        private static Dictionary<int, TelnetOption> OptionRegistry = new Dictionary<int, TelnetOption>()   
        {
            { (int)OptionType.Echo, new TelnetOption((int)OptionType.Echo) },
            { (int)OptionType.SuppressGoAhead, new TelnetOption((int)OptionType.SuppressGoAhead) },
            { (int)OptionType.Terminal_Type, new TelnetOption((int)OptionType.Terminal_Type, "XTERM") },
            { (int)OptionType.Window_Size, new TelnetOption((int)OptionType.Window_Size, "\x00\x50\x00\x18") },
        };

        /// <summary>The option number represented by this instance</summary>
        private int optionNumber;                       // 
        internal string ProposedValue { get; set; }     // The value we (locally) would like to have for this option
        internal string NegotiatedValue { get; set; }   // The value that has been agreed upon by both ends
        internal bool Enabled { get; set; } = false;    // Is this option enabled?  True iff the option has been requested both locally and remotely.
        internal bool Active { get; set; } = false;     // Is this option fully negotiated and ready to take effect?

        /// <summary>Return the name of the option referenced by <paramref name="optionNumber"/></summary>
        public static string NameOfOptionNumber(int optionNumber) => Enum.GetName(typeof(OptionType), optionNumber);

        ///<summary>Return true if the local end would like to use the option referenced by <paramref name="optionNumber"/></summary> 
        public static bool IsDesired(int optionNumber) => OptionRegistry.ContainsKey(optionNumber);

        /// <summary>Take any option-related actions that are to be initiated on this end</summary>
        public static void ProcessLocallyInitiatedActions() { }

        /// <summary>Return a <see cref="TelnetOption"/> instance of the type specified by <paramref name="optionNumber"/></summary>
        /// <remarks>If the referenced option number is not desired, return null.
        /// Each <see cref="TelnetOption"/> should have at most one instance.  This method enforces that singleton behavior.
        /// If an option type requires special handling, create a subclass of <see cref="TelnetOption"/> and instantiate it in this method.</remarks>
        public static TelnetOption Number(int optionNumber)
        {
            TelnetOption theOption = null;
            if (OptionRegistry.ContainsKey(optionNumber))
                OptionRegistry.TryGetValue(optionNumber, out theOption);
            return theOption;
        }

        private TelnetOption(int optNum)
        {
            optionNumber = optNum;
        }

        private TelnetOption(int optNum, string desiredValue)
        {
            optionNumber = optNum;
            ProposedValue = desiredValue;
        }
    }
}
