using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ClassLibrary
{
    public class ServerSocket : CustomSocket
    {
        private const ushort StartIndex = 200;
        private const ushort FinalIndex = 2000;
        private List<MeasureData> _measureDataList = new List<MeasureData>();
        private uint CurrentIndex = 0;

        public void Bind(int port)
        {
            _socket.Bind(new IPEndPoint(IPAddress.Any, port));
        }

        public void Listen(int backlog)
        {
            _socket.Listen(backlog);
        }

        public void Accept()
        {
            acceptDone.Reset();
            _socket.BeginAccept(new AsyncCallback(AcceptCallback), _socket);
            acceptDone.WaitOne();
        }


        private void AcceptCallback(IAsyncResult ar)
        {
            Socket workSocket = _socket.EndAccept(ar);
            Console.WriteLine("\nconnected client.");
            Receive(workSocket);
            acceptDone.Set();
            Accept();
        }
        protected override void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
#if !DEBUG
                Console.Write(".");
#endif
                String response = String.Empty;

                StateObject stateObject = (StateObject)ar.AsyncState;
                Socket workerSocket = stateObject.WorkSocket;

                int bytesRead = workerSocket.EndReceive(ar);

                if (bytesRead > 0)
                {
                    stateObject.Response.Append(Encoding.ASCII.GetString(stateObject.Buffer, 0, bytesRead));

                    response = stateObject.Response.ToString();
                    if (stateObject.Response.Length == (stateObject.Buffer[1] + 4))
                    {
                        Frame frame = new Frame().Parse(stateObject.Buffer.Trim());

                        if (frame._checkSum != frame.CalculateCheckSum())
                        {
                            // checksum  error, send error frame
                            SendError(workerSocket);
                            this.sendDone.WaitOne();
                        }
                        else
                        {
#if(DEBUG)
                            {
                                Console.WriteLine("Read {0} bytes from socket.", response.Length);
                                ConsoleWriteFrameHex(stateObject.Buffer.Trim());
                            }
#endif

                            if (frame.IsServerCode)
                            {
                                // received in server not a client code
                                SendError(workerSocket);
                                sendDone.WaitOne();
                            }
                            else
                            {
                                // process frame receveid and return response
                                ProcessFrame(frame, workerSocket);
                            }
                            // prepare to receive new fresh message
                            Receive(workerSocket);
                            receiveDone.Set();
                        }
                    }
                    else
                    {
                        // Not all data received. Get more.
                        workerSocket.BeginReceive(stateObject.Buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(ReceiveCallback), stateObject);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }



        private void ProcessFrame(Frame frame, Socket workerSocket)
        {
            try
            {
                switch (frame.GetCode())
                {
                    case 0x01:
                        {
                            Byte[] bytes = Encoding.ASCII.GetBytes("ABCDEFG ");

                            bytes[bytes.Length - 1] = 0x00;
                            Frame frameResponse = new Frame(0x08, 0x81, bytes);

                            Send(workerSocket, frameResponse.GetFrame());
                            break;
                        }
                    case 0x02:
                        {
                            byte[] message = new byte[4];

                            // BitConverter.GetBytes inverted bytes....research deeper!!
                            // fixed manually!!
                            // it shoul evalute BitConverter.IsLittleEndian and invert using Array.Reverse
                            message[0] = BitConverter.GetBytes(StartIndex)[1];
                            message[1] = BitConverter.GetBytes(StartIndex)[0];
                            message[2] = BitConverter.GetBytes(FinalIndex)[1];
                            message[3] = BitConverter.GetBytes(FinalIndex)[0];

                            Frame frameResponse = new Frame(0x04, 0x82, message);

                            Send(workerSocket, frameResponse.GetFrame());
                            break;
                        }
                    case 0x03:
                        {
                            byte[] indexBytes = new byte[2];

                            Array.Copy(frame.GetFrame(), 3, indexBytes, 0, 2);

                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(indexBytes);

                            uint indexUint = BitConverter.ToUInt16(indexBytes, 0);
                            CurrentIndex = indexUint;
                            byte[] message = new byte[1];
                            if (_measureDataList.Any(m => m.Index == indexUint))
                            {
                                message[0] = 0x00;
                            }
                            else
                            {
                                message[0] = 0xFF;
                            }
                            Frame frameResponse = new Frame(0x01, 0x83, message);

                            Send(workerSocket, frameResponse.GetFrame());
                            break;
                        }
                    case 0x04:
                        {
                            MeasureData currentMeasureData = _measureDataList.SingleOrDefault(m => m.Index == CurrentIndex);
                            if (currentMeasureData != null)
                            {
                                byte[] message = currentMeasureData.GetDateTimeMessage().ToByteArray();

                                Frame frameResponse = new Frame(0x05, 0x84, message);

                                Send(workerSocket, frameResponse.GetFrame());
                            }
                            break;
                        }
                    case 0x05:
                        {
                            MeasureData currentMeasureData = _measureDataList.SingleOrDefault(m => m.Index == CurrentIndex);
                            if (currentMeasureData != null)
                            {
                                byte[] message = currentMeasureData.GetMeasureDataMessage();

                                Frame frameResponse = new Frame(0x04, 0x85, message);

                                Send(workerSocket, frameResponse.GetFrame());
                            }
                            break;
                        }
                    case 0xFF:
                        {
                            // resend last frame
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



        protected override void ConsoleWriteFrameHex(byte[] byteData)
        {
#if DEBUG

            Console.WriteLine("Command Received - Code : 0x{0:X2}", byteData[2]);
            // Write the response to the console.
            for (int i = 0; i < Convert.ToInt16(byteData[1] + 4); i++)
            {
                if (i < Convert.ToInt16(byteData[1] + 4))
                    Console.Write("0x{0:X2} ", byteData[i]);
            }
            Console.WriteLine();
#endif
        }

        public void GenerateMeasureData()
        {
            try
            {

                Random rnd = new Random();
                for (Int32 i = StartIndex; i < FinalIndex; i++)
                {

                    MeasureData measureData = new MeasureData();

                    measureData.DateTime = DateTime.Now;
                    measureData.Index = i;


                    measureData.Value = (float)(rnd.Next(10, 100) * 1.1234);
                    _measureDataList.Add(measureData);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}