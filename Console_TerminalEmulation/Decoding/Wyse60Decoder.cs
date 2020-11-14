using System;
using System.Drawing;

using NetworkAccess;

namespace ConsoleTerminalEmulation
{
    public class Wyse60Decoder : AnsiDecoder
    {
        public event GetDeviceCode_EventHandler GetDeviceCode;
        public event GetDeviceStatus_EventHandler GetDeviceStatus;
        public event ResizeWindow_EventHandler ResizeWindow;
        public event MoveWindow_EventHandler MoveWindow;

        internal Wyse60Decoder(NetworkInterface network) => RegisterForInputFrom(network);

        protected override void ProcessCommand(byte command, string parameter)
        {
            switch ((char)command)
            {
                case '`': break;
                case 'e': break;
                case 'Z': break;
                case 'a': break;
                case '*': break;


                //case 'c': string deviceCode = OnGetDeviceCode(); break;
                //case 'n':
                //    if (parameter == "5")
                //    {
                //        DeviceStatus status = OnGetDeviceStatus();
                //        string stringStatus = ((int)status).ToString();
                //        byte[] output = new byte[2 + stringStatus.Length + 1];
                //        int i = 0;
                //        output[i++] = EscapeCharacter;
                //        output[i++] = LeftBracketCharacter;
                //        foreach (char c in stringStatus) output[i++] = (byte)c;
                //        output[i++] = (byte)'n';
                //        OnOutput(output);
                //    }
                //    else base.ProcessCommand(command, parameter);
                //    break;
                //case '(': break;    // Set normal font
                //case ')': break;    // Set alternative font
                //case 'r':
                //    if (parameter == "")
                //    {
                //        // Set scroll region to entire screen
                //    }
                //    else
                //    {
                //        // Set scroll region, separated by ;
                //    }
                //    break;
                //case 't':
                //    string[] parameters = parameter.Split(';');
                //    switch (parameters[0])
                //    {
                //        case "3":
                //            if (parameters.Length >= 3)
                //            {
                //                int left, top;
                //                if (int.TryParse(parameters[1], out left) && int.TryParse(parameters[2], out top)) OnMoveWindow(new Point(left, top));
                //            }
                //            break;

                //        case "8":
                //            if (parameters.Length >= 3)
                //            {
                //                int rows, columns;
                //                if (int.TryParse(parameters[1], out rows) && int.TryParse(parameters[2], out columns)) OnResizeWindow(new Size(columns, rows));
                //            }
                //            break;
                //    }
                //    break;
                //case '!': break;    // Graphics Repeat Introducer

                default: base.ProcessCommand(command, parameter); break;
            }
        }

        private string OnGetDeviceCode()
        {
            Delegate[] clients = GetDeviceCode.GetInvocationList();
            GetDeviceCode_EventHandler firstClient = (GetDeviceCode_EventHandler)clients[0];
            return firstClient.Invoke() ?? "UNKNOWN";
        }

        private DeviceStatus OnGetDeviceStatus()
        {
            Delegate[] clients = GetDeviceStatus.GetInvocationList();
            GetDeviceStatus_EventHandler firstClient = (GetDeviceStatus_EventHandler)clients[0];
            DeviceStatus status = firstClient.Invoke();
            return status != DeviceStatus.Unknown ? status : DeviceStatus.Failure;
        }

        private void OnResizeWindow(Size newSize) { ResizeWindow?.Invoke(newSize); }

        private void OnMoveWindow(Point newPosition) { MoveWindow?.Invoke(newPosition); }

        protected override bool IsValidParameterCharacter(char c) => c == '=' || c == ' ' || base.IsValidParameterCharacter(c);
    }
}
