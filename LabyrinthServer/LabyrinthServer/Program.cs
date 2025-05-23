using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LabyrinthServer
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 50003);

            listener.Start();

            while (true)
                new Client(listener.AcceptTcpClient());
        }
    }
}
