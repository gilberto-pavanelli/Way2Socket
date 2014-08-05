using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ClassLibrary
{

     class SocketServer
    {

        private static ServerSocket _serverSocket = new ServerSocket();

        public static void Main(String[] args)
        {
          
            StartServer();
            while (true)
                Console.ReadLine();
        }

        private static void StartServer()
        {
            try
            {
                _serverSocket.GenerateMeasureData();
                _serverSocket.Bind(4000);
                _serverSocket.Listen(0);
                Console.WriteLine("Listening...");

                // accept and start receiving
                _serverSocket.Accept();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }




    }
}