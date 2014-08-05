using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;
using Nito.AsyncEx;

namespace client
{

    class SocketClient
    {

        private static Dictionary<string, ClientSocket> _clientSocketDictionary = new Dictionary<string, ClientSocket>();

        private static List<Job> listJobs = new List<Job>();



        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            AsyncContext.Run(() => MainAsync(args));
        }

        private static async Task MainAsync(string[] args)
        {
            try
            {
                if (args.Length == 1)
                {
                    String fileName = "";
                    if (args[0].Contains("\\"))
                    {
                        fileName = args[0];
                    }
                    else
                    {
                        fileName = "..\\..\\" + args[0];
                    }
                    try
                    {
                        if (File.Exists(fileName))
                        {
                            using (StreamReader file = new System.IO.StreamReader(fileName))
                            {
                                List<Task> tasksList = new List<Task>();

                                String lineFile;
                                while ((lineFile = await file.ReadLineAsync()) != null)
                                {
                                    String[] lineFileSplit = lineFile.Split(' ');
                                    Job job = new Job
                                              {
                                                  Ip = lineFileSplit[0],
                                                  Port = Int16.Parse(lineFileSplit[1]),
                                                  StartIndex = ushort.Parse(lineFileSplit[2]),
                                                  FinalIndex = ushort.Parse(lineFileSplit[3])
                                              };
                                    listJobs.Add(job);
                                    Console.WriteLine("processing ip: {0}", job.Ip);
                                    Console.WriteLine("requesting records: {0} - {1}", job.StartIndex, job.FinalIndex);

                                    ClientSocket clientSocketTemp = new ClientSocket();

                                    _clientSocketDictionary.Add(job.Ip, clientSocketTemp);

                                    tasksList.Add(Task.Run(async () => { await StartClientAsync(job, clientSocketTemp); }));

                                    
                                }
                                await Task.WhenAll(tasksList.ToArray());
                                Console.Write("Hit any key to finish.");
                                Console.ReadKey();
                            }
                        }
                        else
                        {
                            PrintError("file does not exist !!!");
                        }
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine(ex.ToString());
                    }
                }
                else
                {
                    PrintError("parameter is missing !!!");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void PrintError(String message)
        {
            Console.WriteLine("ERROR : " + message);
            Console.WriteLine();
            Console.WriteLine("Usage : client listaDePedidos.txt");
            Console.WriteLine(
                "ps: considering the root project folder as defaut folder, or enter the full path with file name, as a parameter");
            Console.WriteLine();
        }

        private static async Task StartClientAsync(Job jobFile, ClientSocket clientSocket)
        {
            try
            {
                Job currentJob = new Job();

                clientSocket.Connect(jobFile.Ip, jobFile.Port);

                if (clientSocket._socket.Connected)
                {
                    GetSerialNumber(clientSocket);
                    GetStatus(clientSocket);

                    //identify range based on file and status
                    currentJob.StartIndex = jobFile.StartIndex < clientSocket.StatusJob.StartIndex
                        ? clientSocket.StatusJob.StartIndex
                        : jobFile.StartIndex;
                    currentJob.FinalIndex = jobFile.FinalIndex > clientSocket.StatusJob.FinalIndex
                        ? clientSocket.StatusJob.FinalIndex
                        : jobFile.FinalIndex;

                    for (Int32 i = currentJob.StartIndex; i <= currentJob.FinalIndex; i++)
                    {
#if DEBUG
                        Console.WriteLine("Record : {0}", i);
#endif
                        SetCurrentIndex(i, clientSocket);
                        if (clientSocket.CurrentIndex == i)
                        {

#if !DEBUG
                            Console.Write(".");
#endif
                            MeasureData measureData = new MeasureData();
                            measureData.Index = i;
                            clientSocket._measureDataList.Add(measureData);
                            GetDateTime(clientSocket);
                            GetMesaureData(clientSocket);
                        }
                    }

                    clientSocket._socket.Shutdown(SocketShutdown.Both);
                    clientSocket._socket.Close();
                    StringBuilder fileStringBuilder = new StringBuilder();
                    fileStringBuilder.AppendLine(clientSocket.SerialNumber);
                    foreach (MeasureData measureData in clientSocket._measureDataList)
                    {
                        fileStringBuilder.AppendLine(String.Format("{0};{1:yyyy-MM-dd hh:mm:ss};{2:N2};",
                              measureData.Index, measureData.DateTime, measureData.Value));
                    }
                    string exeFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    if (!Directory.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\data"))
                    {
                        Directory.CreateDirectory(exeFolder + "\\data");
                    }
                    await WriteTextFileAsync(String.Format(exeFolder + "\\data\\{0}.txt", jobFile.Ip.Replace('.', '_') + "_" + DateTime.Now.Hour +
                        DateTime.Now.Minute + DateTime.Now.Second), fileStringBuilder);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


        }

        private static async Task WriteTextFileAsync(string filePath, StringBuilder sb)
        {
            try
            {
                Console.WriteLine("\nSaving data file: {0}", Path.GetFileName(filePath));
                byte[] encodedText = Encoding.ASCII.GetBytes(sb.ToString());

                using (FileStream sourceStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
                {
                    await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        private static void GetSerialNumber(ClientSocket clientSocket)
        {
            try
            {

                Frame frame = new Frame();
                var serialNumberFrame = frame.GetSerialNumberFrame();
#if DEBUG
                Console.WriteLine();
                Console.WriteLine("Sending command (GetSerialNumber): 0x{0:X2}", serialNumberFrame[2]);
#endif
                clientSocket.Send(serialNumberFrame);
                clientSocket.sendDone.WaitOne();

                clientSocket.Receive();
                clientSocket.receiveDone.WaitOne();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void GetStatus(ClientSocket clientSocket)
        {
            try
            {
                Frame frame = new Frame();
                var statusFrame = frame.GetStatusFrame();
#if DEBUG
                Console.WriteLine();
                Console.WriteLine("Sending command (GetStatus): 0x{0:X2}", statusFrame[2]);
#endif
                // Send test data to the remote device.
                clientSocket.Send(statusFrame);
                clientSocket.sendDone.WaitOne();

                // Receive the response from the remote device.
                clientSocket.Receive();
                clientSocket.receiveDone.WaitOne();


            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void GetDateTime(ClientSocket clientSocket)
        {
            try
            {
                Frame frame = new Frame();
                var dateTimeFrame = frame.GetDateTimeFrame();
#if DEBUG
                Console.WriteLine();
                Console.WriteLine("Sending command (GetDateTime): 0x{0:X2}", dateTimeFrame[2]);
                // Send test data to the remote device.
#endif
                clientSocket.Send(dateTimeFrame);
                clientSocket.sendDone.WaitOne();

                // Receive the response from the remote device.
                clientSocket.Receive();
                clientSocket.receiveDone.WaitOne();


            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private static void GetMesaureData(ClientSocket clientSocket)
        {
            try
            {
                Frame frame = new Frame();
                var meausreFrame = frame.GetEnergyMeasureFrame();
#if DEBUG
                Console.WriteLine();
                Console.WriteLine("Sending command (GetMeasureData): 0x{0:X2}", meausreFrame[2]);
#endif
                // Send test data to the remote device.
                clientSocket.Send(meausreFrame);
                clientSocket.sendDone.WaitOne();

                // Receive the response from the remote device.
                clientSocket.Receive();
                clientSocket.receiveDone.WaitOne();


            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void SetCurrentIndex(Int32 index, ClientSocket clientSocket)
        {
            try
            {
                clientSocket.CurrentIndex = index;

                byte[] indexArrayTemp = BitConverter.GetBytes(index);

                byte[] indexArray = new byte[2];
                Array.Copy(indexArrayTemp, indexArray, 2);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(indexArray);
                Frame frame = new Frame();
                var setIndexFrame = frame.GetSetIndexReadFrame(indexArray);
#if DEBUG
                Console.WriteLine();
                Console.WriteLine("Sending command (SetIndex): 0x{0:X2}", setIndexFrame[2]);
#endif
                // Send test data to the remote device.
                clientSocket.Send(setIndexFrame);
                clientSocket.sendDone.WaitOne();

                // Receive the response from the remote device.
                clientSocket.Receive();
                clientSocket.receiveDone.WaitOne();


            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        static void OnProcessExit(object sender, EventArgs e)
        {
            foreach (var value in _clientSocketDictionary.Values)
            {
                if ((ClientSocket)value != null && ((ClientSocket)value)._socket.Connected)
                {
                    ((ClientSocket)value)._socket.Shutdown(SocketShutdown.Both);
                    ((ClientSocket)value)._socket.Close();
                }
            }


        }

    }

}