using AForge.Video.FFMPEG;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Lulu {
    public class Recorder {

        public static string StoragePath;
        public const short FRAMES_PER_SECOND = 24;
        public const short STASH_THREADS = 2;
        public const bool DELETE_STASH_FOLDER = true;

        private CancellationTokenSource _cancellationTokenSource;

        private System.Threading.Timer captureTimer;

        static Recorder() {
            StoragePath = KnownFolders.GetPath(KnownFolder.Videos) + "\\Lulu\\";
            Directory.CreateDirectory(StoragePath);
        }

        private void Capture(object state) { // Background Thread
            var stashQueue = state as Queue<Bitmap>;
            var screenSize = Screen.PrimaryScreen.Bounds;
            var capture = new Bitmap(screenSize.Width, screenSize.Height);
            using (Graphics g = Graphics.FromImage(capture)) {
                g.CopyFromScreen(0, 0, 0, 0, new Size(screenSize.Width, screenSize.Height));
            }
            stashQueue.Enqueue(capture);
        }

        private long latestFrame = -1L;
        private void Stash(Queue<Bitmap> stashQueue, Queue<string> encodingQueue, string stamp, CancellationToken cancellationToken) { // Foreground Thread
            Directory.CreateDirectory(StoragePath + "//" + stamp + "//");
            while (true) {
                if (stashQueue.Count == 0) {
                    if (!cancellationToken.IsCancellationRequested) continue;
                    break;
                }
                Bitmap capture;
                lock (stashQueue) {
                    if (stashQueue.Count == 0) continue;
                    capture = stashQueue.Dequeue();
                }
                using (capture) {
                    Interlocked.Increment(ref latestFrame);
                    var filePath = StoragePath + stamp + "\\" + latestFrame + ".bmp";
                    capture.Save(filePath);
                    encodingQueue.Enqueue(filePath);
                }
            }
        }

        private void Write(Queue<string> encodingQueue, string stamp, CancellationToken cancellationToken) { // Foreground Thread
            var screen = Screen.PrimaryScreen.Bounds;
            var writer = new VideoFileWriter();
            using (writer) {
                writer.Open(StoragePath + "//" + stamp + ".avi", screen.Width, screen.Height, FRAMES_PER_SECOND, VideoCodec.Raw);
                while (true) {
                    if (encodingQueue.Count == 0) {
                        if (!cancellationToken.IsCancellationRequested) continue;
                        break;
                    }
                    var filePath = encodingQueue.Dequeue();
                    var bitmap = (Bitmap)Bitmap.FromFile(filePath);
                    using (bitmap) {
                        writer.WriteVideoFrame(bitmap);
                    }
                }
            }
            ClearStashFolder(stamp);
            GC.Collect();
        }

        private void ClearStashFolder(string stamp) {
            Directory.EnumerateFiles(StoragePath + "//" + stamp + "//").AsParallel().ForAll(File.Delete);
            Directory.Delete(StoragePath + "//" + stamp + "//");
        }

        public void StartRecording() {
            if ((!this._cancellationTokenSource?.IsCancellationRequested) ?? false) {
                return;
            }
            this._cancellationTokenSource = new CancellationTokenSource();
            var token = this._cancellationTokenSource.Token;

            var stamp = DateTime.Now.ToString("d-MMM-yyyy HH.mm.ss");
            var stashQueue = new Queue<Bitmap>();
            var encodingQueue = new Queue<string>();
            
            this.captureTimer = new System.Threading.Timer(this.Capture, stashQueue, 0, 1_000 / FRAMES_PER_SECOND);
            for (var i = 1; i <= STASH_THREADS; i++) {
                var stashThread = new Thread(() => this.Stash(stashQueue, encodingQueue, stamp, token));
                stashThread.Start();
            }
            var writeThread = new Thread(() => this.Write(encodingQueue, stamp, token));
            writeThread.Start();
        }

        public void StopRecording() {
            this.captureTimer.Change(0, 0);
            this.captureTimer.Dispose();
            this.captureTimer = null;
            this._cancellationTokenSource.Cancel();
        }

    }
}
