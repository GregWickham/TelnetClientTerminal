using Keys = System.Windows.Forms.Keys;

using NetworkAccess;

namespace ConsoleTerminalEmulation
{
    public delegate void KeyPress_EventHandler(Keys aKey);

    public abstract class KeyboardEncoder
    {
        public KeyboardEncoder(NetworkInterface n) => Network = n;

        protected NetworkInterface Network { get; private set; }

        public void Press(Keys aKey)
        {
            Network.Write(OutputSequenceFor(aKey));
            OnKeyPress(aKey);
        }

        protected abstract string OutputSequenceFor(Keys key);

        public event KeyPress_EventHandler KeyPress;
        private void OnKeyPress(Keys aKey) => KeyPress?.Invoke(aKey);
    }
}
