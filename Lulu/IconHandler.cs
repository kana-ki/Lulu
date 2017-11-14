using Lulu.Properties;
using System;
using System.Threading;
using System.Windows.Forms;

namespace Lulu {
    class IconHandler : IDisposable {

        private readonly NotifyIcon _notifyIcon;
        private bool _drawingAttention = false;
        private readonly ClickHandler luluHandler;
        private readonly ClickHandler exitHandler;

        public delegate void ClickHandler();

        public IconHandler(ClickHandler luluHandler, ClickHandler exitHandler) {
            this.luluHandler = luluHandler;
            this.exitHandler = exitHandler;
            this._notifyIcon = this.CreateIcon();
        }

        public void DrawAttention() {
            if (this._drawingAttention) return;
            this._drawingAttention = true;
            var previousIcon = this._notifyIcon.Icon;
            this._notifyIcon.ShowBalloonTip(3000, "Lulu", "I'm ready and waiting in the notification area! Press Ctrl + Alt + L to record!", ToolTipIcon.Info);
            for (var i = 0; i < 10; i++) {
                this._notifyIcon.Icon = i % 2 == 0 ? Resources.Icon_Red : previousIcon;
                Thread.Sleep(300);
            }
            Thread.Sleep(4000);
            this._drawingAttention = false;
        }

        private NotifyIcon CreateIcon() {
            var notifyIcon = new NotifyIcon {
                Icon = Resources.Icon_White,
                Text = "Lulu (Ctrl+Alt+L)",
                Visible = true
            };
            notifyIcon.DoubleClick += this.Lulu_Click;
            notifyIcon.ContextMenu = new ContextMenu(new[] {
                new MenuItem("Lulu " + Application.ProductVersion) { Enabled = false },
                new MenuItem("-"),
                new MenuItem("Exit Lulu", this.Exit_Click)
            });
            return notifyIcon;
        }

        private void Lulu_Click(object obj, EventArgs args) {
            this.luluHandler();
        }

        private void Exit_Click(object obj, EventArgs args) {
            this.exitHandler();
        }

        public void SwitchToRecordingState() {
            this._notifyIcon.Text = "Lulu (Ctrl+Alt+L) (Pretending to record)";
            this._notifyIcon.Icon = Resources.Icon_Red;
            this._notifyIcon.ShowBalloonTip(3000, "I'm recording!", "I would start recording now but... I don't know how to yet. :(", ToolTipIcon.Info);
        }

        public void SwitchToIdleState() {
            this._notifyIcon.Text = "Lulu (Ctrl+Alt+L)";
            this._notifyIcon.Icon = Resources.Icon_White;
            this._notifyIcon.ShowBalloonTip(3000, "I've stopped recording!", "I would stop recording now but... I don't know how to yet. :(", ToolTipIcon.Info);
        }

        public void Show() {
            this._notifyIcon.Visible = true;
        }

        public void Hide() {
            this._notifyIcon.Visible = false;
        }

        public void Dispose() {
            this._notifyIcon?.Dispose();
        }
    }
}
