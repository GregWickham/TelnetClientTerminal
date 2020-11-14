// If TelnetCommandLogging is defined, Telnet commands are displayed on Debug output
//#define TelnetCommandLogging

using System;
using System.Collections.Generic;
using System.Text;

namespace NetworkAccess
{
    internal abstract class TelnetCommand
    {
        internal const byte IAC = 0xFF;     // Interpret As Command
        internal TelnetNVT terminal;        // Every instance of TelnetCommand is either received or sent on a Telnet NVT

        protected internal enum TelnetCommandType
        {
            IS = 0x00,
            SEND = 0x01,
            SE = 0xF0,                      // Subnegotiation End
            NOP = 0xF1,                     // No Operation
            DataMark = 0xF2,
            Break = 0xF3,
            InterruptProcess = 0xF4,
            AbortOutput = 0xF5,
            YouThere = 0xF6,                // Are You There?
            EraseChar = 0xF7,
            EraseLine = 0xF8,
            GoAhead = 0xF9,
            SB = 0xFA,                      // Subnegotiation Begin
            WILL = 0xFB,
            WONT = 0xFC,
            DO = 0xFD,
            DONT = 0xFE
        }

        /// <summary>Return a TelnetCommand instance of the type specified by <paramref name="cmdType"/></summary>
        internal static TelnetCommand OfType(int cmdType)
        {
            TelnetCommand cmd;

            switch (cmdType)
            {
                case (int)TelnetCommandType.DO:
                    cmd = new TelnetDoCommand();
                    break;
                case (int)TelnetCommandType.DONT:
                    cmd = new TelnetDontCommand();
                    break;
                case (int)TelnetCommandType.WILL:
                    cmd = new TelnetWillCommand();
                    break;
                case (int)TelnetCommandType.WONT:
                    cmd = new TelnetWontCommand();
                    break;
                case (int)TelnetCommandType.SB:
                    cmd = new TelnetSBCommand();
                    break;
                default:
                    // This should only be hit if an incoming command is not recognized, i.e., if its type specifier is not in the CommandType enum.
                    // If your application uses a command type that is in the CommandType enum, but not in this switch statement, 
                    // you'll probably need to create a new TelnetCommand subclass and add it to this switch.
                    throw new InvalidOperationException("Unrecognized Telnet command type");
            }
            return cmd;
        }

        /// <summary>Return a readable form of <paramref name="commandTypeNumber"/></summary>
        protected internal static string NameOfCommandTypeNumber(int commandTypeNumber) => Enum.GetName(typeof(TelnetCommandType), commandTypeNumber);

        #region Command Flow Direction

        internal enum CommandFlow
        {
            Incoming = 0,
            Outgoing = 1
        }

        private CommandFlow direction;

        /// <summary>Mark this as a command incoming on <paramref name="theNVT"/>, and parse its contents</summary>
        internal TelnetCommand IncomingOn(TelnetNVT theNVT) 
        {
            direction = CommandFlow.Incoming;
            terminal = theNVT;
            Parse();
#if TelnetCommandLogging
            Debug.WriteLine("IN: " + humanReadableForm());
#endif
            return this;
        }

        private protected bool IsIncoming => direction == CommandFlow.Incoming;

        /// <summary>Mark this as an outgoing command, and assign it to be sent on <paramref name="theNVT"/></summary>
        internal TelnetCommand OutgoingOn(TelnetNVT theNVT)
        {
            direction = CommandFlow.Outgoing;
            terminal = theNVT;

            return this;
        }

        private protected bool IsOutgoing => direction == CommandFlow.Outgoing;

        #endregion Command Flow Direction

        /// <summary>Only called on incoming commands.</summary>
        /// <remarks>Subclasses should override if their command type includes additional data from the NVT stream.</remarks>
        private protected virtual void Parse() { }

        /// <summary>Return the sequence of bytes for the command as it actually goes over the wire</summary>
        internal abstract IEnumerable<byte> TelnetForm { get; }

        /// <summary>Return a string containing a human-readable form of the command for logging purposes</summary>
        private protected virtual string HumanReadableForm => "Unspecified Telnet Command"; 

        /// <summary>Carry out the command's purpose.</summary>
        /// <remarks>If this is an incoming command, this method may have effects on an option, and may transmit a response over the NVT.
        /// If this is an outgoing command, this method may have effects on an option, and will transmit itself over the NVT.</remarks>
        internal abstract void Process();

        private protected virtual string CommandTypeDescription => "Unspecified Telnet Command";

    }

    /// <summary>The base class for <see cref="TelnetCommand"/>s related to option negotiation</summary>
    internal abstract class TelnetOptionCommand : TelnetCommand
    {
        public int option;

        protected abstract TelnetCommandType CommandType { get; }

        internal sealed override void Process()
        {
            HandleOption();
            if (this.IsIncoming)
            {
                Respond();
            }
            else
            {
                terminal.EnqueueForSending(this);
            }
        }

        /// <summary>All commands related to options contain an option descriptor immediately following the command type.</summary>
        /// <remarks>Only called on incoming commands.</remarks>
        private protected override void Parse() => option = terminal.ReadByteFromTCP();

        internal override IEnumerable<byte> TelnetForm => new byte[]
            {
                IAC,
                (byte)CommandType,
                (byte)option
            };

        /// <summary>Only called on incoming commands, to create and execute an outgoing command in response.</summary>
        private void Respond() 
        {
            TelnetOptionCommand response = (TelnetOptionCommand)OfType((int)ResponseType).OutgoingOn(terminal);
            response.option = this.option;
            response.Process();
        }

        /// <summary>Take whatever effects on an option are appropriate to this command</summary>
        protected virtual void HandleOption() { } 

        /// <summary>Return the CommandType of the desired response to this command</summary>
        protected abstract TelnetCommandType ResponseType { get; }

        private protected sealed override string CommandTypeDescription => NameOfCommandTypeNumber((int)CommandType);

        private protected override string HumanReadableForm => CommandTypeDescription + " " + TelnetOption.NameOfOptionNumber(option);

    }

    #region Option Enable / Disable Commands

    sealed class TelnetDoCommand : TelnetOptionCommand
    {
        protected sealed override TelnetCommandType CommandType => TelnetCommandType.DO;
        protected sealed override TelnetCommandType ResponseType => TelnetOption.IsDesired(option) ? TelnetCommandType.WILL : TelnetCommandType.WONT;

        protected sealed override void HandleOption()
        {
            if (TelnetOption.IsDesired(option)) TelnetOption.Number(option).Enabled = true;
        }
    }

    sealed class TelnetDontCommand : TelnetOptionCommand
    {
        protected sealed override TelnetCommandType CommandType => TelnetCommandType.DONT;
        protected sealed override TelnetCommandType ResponseType => TelnetCommandType.WONT;
    }

    sealed class TelnetWillCommand : TelnetOptionCommand
    {
        protected sealed override TelnetCommandType CommandType => TelnetCommandType.WILL;
        protected sealed override TelnetCommandType ResponseType => TelnetOption.IsDesired(option) ? TelnetCommandType.DO : TelnetCommandType.DONT;

        protected sealed override void HandleOption()
        {
            if (TelnetOption.IsDesired(option)) TelnetOption.Number(option).Enabled = true;
        }
    }

    sealed class TelnetWontCommand : TelnetOptionCommand
    {
        protected sealed override TelnetCommandType CommandType => TelnetCommandType.WONT;
        protected sealed override TelnetCommandType ResponseType => TelnetCommandType.DONT;
    }

    #endregion

    #region Option Negotiation Commands

    sealed class TelnetSBCommand : TelnetOptionCommand
    {
        private 
            int sendCommand;
            int iac;
            int terminator;

        protected sealed override TelnetCommandType CommandType => TelnetCommandType.SB;
        protected sealed override TelnetCommandType ResponseType => TelnetCommandType.SB;

        private protected sealed override void Parse()
        {
            base.Parse();

            sendCommand = terminal.ReadByteFromTCP();
            iac = terminal.ReadByteFromTCP();
            terminator = terminal.ReadByteFromTCP();
        }

        internal sealed override IEnumerable<byte> TelnetForm
        {
            get
            {
                byte[] firstPart = new byte[]
                    {
                        IAC,
                        (byte)TelnetCommandType.SB,
                        (byte)option,
                        (byte)TelnetCommandType.IS
                    };
                byte[] secondPart = Encoding.ASCII.GetBytes(TelnetOption.Number(option).ProposedValue);
                byte[] thirdPart = new byte[]
                    {
                        IAC,
                        (byte)TelnetCommandType.SE
                    };

                byte[] result = new byte[firstPart.Length + secondPart.Length + thirdPart.Length];
                Buffer.BlockCopy(firstPart, 0, result, 0, firstPart.Length);
                Buffer.BlockCopy(secondPart, 0, result, firstPart.Length, secondPart.Length);
                Buffer.BlockCopy(thirdPart, 0, result, firstPart.Length + secondPart.Length, thirdPart.Length);

                return result;
            }
        }

        /// <summary>Enable the option handled by this command.  Assumes the subnegotiation has happened successfully.</summary>
        protected sealed override void HandleOption() => TelnetOption.Number(option).Active = true;

#if TelnetCommandLogging
        protected sealed override string humanReadableForm()
        {
            if (this.isIncoming())
                return commandAndOptionDescription() + NameOfCommandTypeNumber(sendCommand);
            else
                return commandAndOptionDescription() + NameOfCommandTypeNumber((int)CommandType.IS) + " " + TelnetOption.Number(option).proposedValue;
        }

        private string commandAndOptionDescription()
        {
            return commandTypeDescription() + " " + TelnetOption.NameOfOptionNumber(option) + " ";
        }
#endif
    }

    #endregion
}


