using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Lulu {

    public class KeyboardHotkey : Control {

        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32", SetLastError = true)]
        private static extern int UnregisterHotKey(IntPtr hwnd, int id);

        [DllImport("kernel32", SetLastError = true)]
        private static extern short GlobalAddAtom(string lpString);

        [DllImport("kernel32", SetLastError = true)]
        private static extern short GlobalDeleteAtom(short nAtom);

        public const int WM_HOTKEY = 0x312;

        public const int MOD_ALT = 1;
        public const int MOD_CONTROL = 2;
        public const int MOD_SHIFT = 4;
        public const int MOD_WIN = 8;

        public delegate void HotKeyPressedDelegate ();
        public event HotKeyPressedDelegate HotKeyPressed;

        public short HotKeyId { get; private set; }

        public void RegisterGlobalHotKey (int hotkey, int modifiers) {
            this.UnregisterGlobalHotKey();
            try {
                var atomName = Thread.CurrentThread.ManagedThreadId.ToString("X8") + this.GetType().FullName;
                this.HotKeyId = GlobalAddAtom(atomName);
                if (this.HotKeyId == 0) throw new Exception("Unable to generate unique hotkey ID. Error: " + Marshal.GetLastWin32Error());
                if (!RegisterHotKey(this.Handle, this.HotKeyId, (uint) modifiers, (uint) hotkey)) throw new Exception("Unable to register hotkey. Error: " + Marshal.GetLastWin32Error());
            }
            catch (Exception ex) {
                this.Dispose();
                Console.WriteLine(ex);
            }
        }

        public void UnregisterGlobalHotKey () {
            if (this.HotKeyId != 0) {
                UnregisterHotKey(this.Handle, this.HotKeyId);
                GlobalDeleteAtom(this.HotKeyId);
                this.HotKeyId = 0;
            }
        }

        protected override void WndProc(ref Message m) {
            if (m.Msg == WM_HOTKEY && (short) m.WParam == this.HotKeyId)
                this.HotKeyPressed?.Invoke();
            base.WndProc(ref m);
        }

        public new void Dispose() {
            this.UnregisterGlobalHotKey();
            base.Dispose();
        }

    }

}
