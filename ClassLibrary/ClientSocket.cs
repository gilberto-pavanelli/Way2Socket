using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace ClassLibrary
{
    public class ClientSocket : CustomSocket
    {
        public Job StatusJob = new Job();
        public Int32 CurrentIndex = 0;
        public List<MeasureData> _measureDataList = new List<MeasureData>();
        public String SerialNumber;

        public ClientSocket()
        {

        }


        public void Connect(string ipAddress, int port)
        {
            try
            {


                connectDone.Reset();
                var asyncResult = _socket.BeginConnect(new IPEndPoint(IPAddress.Parse(ipAddress), port), new AsyncCallback(ConnectCallback), null);

                var appSettings = ConfigurationManager.AppSettings;

                asyncResult.AsyncWaitHandle.WaitOne(Convert.ToInt16(appSettings["connectTimeOut"]), true);

                if (!_socket.Connected)
                {
                    _socket.Close();
                    throw new SocketException(10060);
                }
                connectDone.WaitOne();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                _socket.EndConnect(ar);
#if DEBUG
                       Console.WriteLine("Socket connected to {0}", _socket.RemoteEndPoint.ToString());
#endif
                connectDone.Set();
            }
            catch (Exception e)
            {
                // Console.WriteLine(e.ToString());
            }

        }
        protected override void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                String content = String.Empty;

                StateObject stateObject = (StateObject)ar.AsyncState;
                Socket workerSocket = stateObject.WorkSocket;

                int bytesRead = workerSocket.EndReceive(ar);

                if (bytesRead > 0)
                {
                    stateObject.Response.Append(Encoding.ASCII.GetString(stateObject.Buffer, 0, bytesRead));

                    content = stateObject.Response.ToString();


                    if (stateObject.Response.Length == Convert.ToInt16(stateObject.Buffer[1]) + 4)
                    {
                        // received whole frame
#if DEBUG
                        {
                            Console.WriteLine("Read {0} bytes from socket.", content.Length);
                            ConsoleWriteFrameHex(stateObject.Buffer);
                        }
#endif

                        ProcessFrame(new Frame().Parse(stateObject.Buffer), workerSocket);
                        receiveDone.Set();

                    }
                    else
                    {
                        // Not all data received. Get more.
                        workerSocket.BeginReceive(stateObject.Buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(ReceiveCallback), stateObject);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public new void Send(byte[] data)
        {
            Send(this._socket, data);
        }
        public new void Receive()
        {
            Receive(this._socket);
        }

        private void ProcessFrame(Frame frame, Socket workerSocket)
        {
            // extract values from message to all codes
            try
            {
                switch (frame.GetCode())
                {
                    case 0x81:
                        {
                            SerialNumber = Encoding.ASCII.GetString(frame._message).Substring(0, frame._message.Length - 1);
                            break;
                        }
                    case 0x82:
                        {
                            StatusJob = new Job();
                            byte[] startBytes = new byte[2];
                            byte[] finalBytes = new byte[2];
                            Array.Copy(frame.GetFrame(), 3, startBytes, 0, 2);
                            Array.Copy(frame.GetFrame(), 5, finalBytes, 0, 2);
                            if (BitConverter.IsLittleEndian)
                            {
                                Array.Reverse(startBytes);
                                Array.Reverse(finalBytes);
                            }
                            StatusJob.StartIndex = BitConverter.ToUInt16(startBytes, 0);
                            StatusJob.FinalIndex = BitConverter.ToUInt16(finalBytes, 0);

                            break;
                        }
                    case 0x83:
                        {
                            if (frame.GetFrame()[3] != 0x00)
                            {
                                CurrentIndex = 0;
                            }

                            break;
                        }
                    case 0x84:
                        {
                            MeasureData measureData = _measureDataList.SingleOrDefault(m => m.Index == CurrentIndex);
                            if (measureData != null)
                            {
                                measureData.DateTime = Helper.ExtractDateTime(frame._message);
                            }
                            break;
                        }
                    case 0x85:
                        {
                            MeasureData measureData = _measureDataList.SingleOrDefault(m => m.Index == CurrentIndex);
                            if (measureData != null)
                            {
                                measureData.Value = Helper.ConvertToFloatIEEE754Simple(frame._message);
                            }
                            break;
                        }
                    case 0xFF:
                        {
                            SendLastFrame(workerSocket);
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
                this.sendDone.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

    }
}