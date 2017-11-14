using Lulu.Properties;
using System;
using System.Collections.Generic;
using System.Media;
using System.Windows.Forms;

namespace Lulu {

    public class App {

        private readonly KeyboardHotkey _keyboardHotkey;
        private readonly IconHandler _iconHandler;
        private readonly Recorder _recorder;
        private static bool isRecording = false;

        public App() {
            var ipcHandler = new IpcHandler();
            var pingSuccessful = ipcHandler.PollForPing();
            if (pingSuccessful) Environment.Exit(1);

            this._iconHandler = new IconHandler(this.ToggleRecording, this.Exit);
            this._recorder = new Recorder();
            this._keyboardHotkey = this.RegisterHotkey();

            ipcHandler.ListenForPing(new Dictionary<byte, Action> {
                { 0x10, this._iconHandler.DrawAttention }
            });
        }

        private KeyboardHotkey RegisterHotkey() {
            var hotkey = new KeyboardHotkey();
            hotkey.RegisterGlobalHotKey((int) Keys.L, KeyboardHotkey.MOD_CONTROL | KeyboardHotkey.MOD_ALT);
            hotkey.HotKeyPressed += this.ToggleRecording;
            return hotkey;
        }

        private void ToggleRecording() {
            if (isRecording) {
                StopRecording();
                return;
            }
            StartRecording();
        }
        
        private void StartRecording() {
            this._iconHandler.SwitchToRecordingState();
            using (var soundPlayer = new SoundPlayer(Resources.Start)) {
                soundPlayer.PlaySync();
            }
            isRecording = true;
            this._recorder.StartRecording();
        }

        private void StopRecording() {
            this._recorder.StopRecording();
            this._iconHandler.SwitchToIdleState();
            using (var soundPlayer = new SoundPlayer(Resources.Stop)) {
                soundPlayer.Play();
            }
            isRecording = false;
        }

        private void Exit () {
            this._iconHandler.Hide();
            this._keyboardHotkey.UnregisterGlobalHotKey();
            Application.Exit();
        }

        ~App() {
            this._iconHandler?.Dispose();
            this._keyboardHotkey?.Dispose();
        }

        [STAThread]
        public static void Main() {
            new App();
            Application.Run();
        }

    }
}
