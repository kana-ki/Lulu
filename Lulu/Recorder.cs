using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Lulu {
    public class Recorder {

        private const short STOP_RECORDING_THREAD_TIMEOUT_IN_SECONDS = 5;
        private const short IMAGE_CAPTURE_INTERVAL_IN_MILLISECONDS = 1_000;

        private Thread _recordingThread;
        private bool _stopRecording;
        private string _folderPath;

        public Recorder() {
            this._folderPath = KnownFolders.GetPath(KnownFolder.Videos) + "\\Lulu\\";
            Directory.CreateDirectory(this._folderPath);
        }

        private void Record() {
            while ( ! this._stopRecording) {
                this.CaptureImage();
                Thread.Sleep(IMAGE_CAPTURE_INTERVAL_IN_MILLISECONDS);
            }
        }

        private void CaptureImage() {
            var screenSize = Screen.PrimaryScreen.Bounds;
            var capture = new Bitmap(screenSize.Width, screenSize.Height);
            using (Graphics g = Graphics.FromImage(capture)) {
                g.CopyFromScreen(0, 0, 0, 0, new Size(screenSize.Width, screenSize.Height));
            }
            var fileName = DateTime.Now.ToString("d-MMM-yyyy HH.mm.ss.f") + ".bmp";
            capture.Save(this._folderPath + fileName, ImageFormat.Bmp);
        }

        public void StartRecording() {
            this._recordingThread = new Thread(this.Record);
            this._recordingThread.Start();
        }

        public void StopRecording() {
            _stopRecording = true;
            this._recordingThread.Join(STOP_RECORDING_THREAD_TIMEOUT_IN_SECONDS);
            if (this._recordingThread.IsAlive) this._recordingThread.Abort();
            _stopRecording = false;
        }

    }
}
