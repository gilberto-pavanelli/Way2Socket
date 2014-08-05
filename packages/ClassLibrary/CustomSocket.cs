using System;
using System.Net;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public class StateObject
    {
        public Socket WorkSocket = null;
        public const int BufferSize = 256;
        public byte[] Buffer = new byte[BufferSize];
        public StringBuilder Response = new StringBuilder();
    }


    public abstract class CustomSocket
    {
        public Socket _socket;

        public ManualResetEvent connectDone = new ManualResetEvent(false);
        public ManualResetEvent sendDone = new ManualResetEvent(false);
        public ManualResetEvent receiveDone = new ManualResetEvent(false);
        public ManualResetEvent acceptDone = new ManualResetEvent(false);
        public ManualResetEvent disconnectDone = new ManualResetEvent(false);

        public Frame LastSentFrame = new Frame();

        protected CustomSocket()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }


        protected abstract void ReceiveCallback(IAsyncResult ar);

        public void Receive(Socket workSocket)
        {
            receiveDone.Reset();
            StateObject stateObject = new StateObject();
            stateObject.WorkSocket = workSocket;
            workSocket.BeginReceive(stateObject.Buffer, 0, stateObject.Buffer.Length, SocketFlags.None,
                new AsyncCallback(ReceiveCallback), stateObject);
        }




        protected void Send(Socket workerSocket, byte[] data, Boolean copyLastFrame = true)
        {
            if (copyLastFrame)
            {
                LastSentFrame = new Frame().Parse(data);
            }
            sendDone.Reset();
            workerSocket.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), workerSocket);
        }

        protected void SendLastFrame(Socket workerSocket)
        {
            sendDone.Reset();
            workerSocket.BeginSend(LastSentFrame.GetFrame(), 0, LastSentFrame.GetFrame().Length, 0, new AsyncCallback(SendCallback), workerSocket);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket workerSocket = (Socket)ar.AsyncState;

                int bytesSent = workerSocket.EndSend(ar);
#if DEBUG
                if (this is ServerSocket)
                    Console.WriteLine("Sent {0} bytes to client. \n", bytesSent);
                else
                    Console.WriteLine("Sent {0} bytes to client.", bytesSent);
#endif

                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void SendError(Socket workerSocket)
        {
            Frame frameError = new Frame();

            Send(workerSocket, frameError.GetErrorFrame(), false);

        }


        public void Disconnect()
        {
            _socket.Shutdown(SocketShutdown.Both);
            disconnectDone.Reset();
            _socket.BeginDisconnect(false, new AsyncCallback(DisconnectCalback), null);
        }

        private void DisconnectCalback(IAsyncResult ar)
        {
            _socket.EndDisconnect(ar);
            disconnectDone.Set();
        }

        protected virtual void ConsoleWriteFrameHex(byte[] byteData)
        {
#if DEBUG
            Console.WriteLine("Response Received - Code : 0x{0:X2}", byteData[2]);
#endif
            // Write the response to the console.
            for (int i = 0; i < Convert.ToInt16(byteData[1]) + 4; i++)
            {
                if (i < Convert.ToInt16(byteData[1] + 4))
                    Console.Write("0x{0:X2} ", byteData[i]);
            }
            Console.WriteLine();
        }

        public void Connect(IPAddress[] addresses, int port, TimeSpan timeout)
        {
            AsyncConnect(_socket, (s, a, o) => s.BeginConnect(addresses, port, a, o), timeout);
        }
        private void AsyncConnect(Socket socket, Func<Socket, AsyncCallback, object, IAsyncResult> connect, TimeSpan timeout)
        {
            var asyncResult = connect(socket, null, null);
            if (!asyncResult.AsyncWaitHandle.WaitOne(timeout))
            {
                try
                {
                    socket.EndConnect(asyncResult);
                }
                catch (SocketException)
                { }
                catch (ObjectDisposedException)
                { }
            }
        }

    }
}


