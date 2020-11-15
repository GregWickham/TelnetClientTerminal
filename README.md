# TelnetClientTerminal
A virtual terminal that .NET applications can use for connecting to hosts over Telnet.

The Telnet implementation is based on MinimalisticTelnet, extended to support Telnet option negotiation.

The terminal emulator provides a virtual terminal factored into four components:

1.  A network interface for communicating with a host over Telnet.  The framework can support other protocols, but I've only implemented Telnet.
2.  A decoder for translating escape sequences to terminal control commands, mostly for the display.
3.  A virtual display that maintains a 2D array of character locations.  This virtual display can be queried by the client application to support screen scraping.
4.  A keyboard encoder for translating keystrokes to the byte sequences expected by a remote host.

The respective base classes for these four components are: 

1.  NetworkInterface.cs
2.  TerminalDecoder.cs
3.  TerminalDisplay.cs
4.  KeyboardEncoder.cs

To create a custom terminal emulator, you can subclass any or all of these base classes as necessary to accommodate the requirements of the host.
Then create a subclass of VirtualTerminal.cs made from your components.

VT100.cs is a fully functional virtual terminal that I created for a specific application.  It's mostly compliant with the VT100 standard, but does not
implement the entire VT100 specification.
