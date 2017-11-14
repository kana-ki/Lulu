using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;

namespace Lulu {
    class IpcHandler {

        public void ListenForPing(Dictionary<byte, Action> commands) {
            var pipeServer = new NamedPipeServerStream(Process.GetCurrentProcess().ProcessName, PipeDirection.InOut, -1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            pipeServer.BeginWaitForConnection(result => this.HandlePing(result, commands), pipeServer);
        }

        private void HandlePing(IAsyncResult result, Dictionary<byte, Action> commands) {
            this.ListenForPing(commands);
            var pipeServer = result.AsyncState as NamedPipeServerStream;
            if (!pipeServer.IsConnected) pipeServer.WaitForConnection();
            var commandByte = (byte)pipeServer.ReadByte();
            if (commands.ContainsKey(commandByte)) {
                pipeServer.WriteByte(0x0);
                pipeServer.Dispose();
                commands[commandByte]();
            }
            else {
                pipeServer.WriteByte(0x1);
                pipeServer.Dispose();
            }
        }

        public bool PollForPing() {
            var pipeClient = new NamedPipeClientStream
                (".", Process.GetCurrentProcess().ProcessName, PipeDirection.InOut);
            try {
                pipeClient.Connect(1000);
            }
            catch (TimeoutException) {
                return false;
            }
            pipeClient.WriteByte(0x10);
            try {
                pipeClient.Connect(1);
            }
            catch { }
            pipeClient.ReadByte();
            pipeClient.Close();
            return true;
        }

    }
}
