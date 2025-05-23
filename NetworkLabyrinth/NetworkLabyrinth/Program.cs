using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace NetworkLabyrinth
{
    // Difficulty: 0,40 - 0,42

    // 0: Text
    // 1: Progress
    // 2: Confirmation
    // 3: Coordinates
    // 4: Layout
    // 5: Denied
    // 8: Solved
    // 9: Prompt

    class Program
    {
        public static object Sync = new object();
        public static List<IPAddress> Clients = new List<IPAddress>();

        static void Main(string[] args)
        {
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(new IPEndPoint(IPAddress.Any, 50000));

            listener.Listen(512);

            while (true)
            {
                Socket client = listener.Accept();

                IPAddress remote = ((IPEndPoint)client.RemoteEndPoint).Address;

                byte[] remoteBytes = remote.GetAddressBytes();

                int occurrences = 0;

                lock (Sync)
                    foreach (IPAddress address in Clients)
                    {
                        byte[] checkBytes = address.GetAddressBytes();

                        if (remoteBytes[0] == checkBytes[0] && remoteBytes[1] == checkBytes[1] && remoteBytes[2] == checkBytes[2] && remoteBytes[3] == checkBytes[3])
                            occurrences++;
                    }

                if (Clients.Count > 12)
                {
                    client.SendTimeout = 50;

                    using (client)
                    using (NetworkStream ns = new NetworkStream(client, false))
                    using (StreamWriter writer = new StreamWriter(ns))
                    {
                        writer.WriteLine($"0 Welcome to NetworkLabyrinth v0.3.\r\n0 \r\n0 We already have too many clients active.\r\n0 \r\n0 Please consider connecting at a later point in time or closing some of\r\n0 your connections.\r\n0 \r\n5 Policy violation.");
                    }
                }
                else if (occurrences > 2)
                {
                    client.SendTimeout = 50;

                    using (client)
                    using (NetworkStream ns = new NetworkStream(client, false))
                    using (StreamWriter writer = new StreamWriter(ns))
                    {
                        writer.WriteLine($"0 Welcome to NetworkLabyrinth v0.3.\r\n0 \r\n0 We already have too many concurrent connections from your IP address ({remote}).\r\n0 \r\n0 Please consider connecting at a later point in time or closing some of\r\n0 your connections.\r\n0 \r\n5 Policy violation.");
                    }
                }
                else
                {
                    lock (Sync)
                        Clients.Add(remote);

                    new Client(client);
                }
            }
        }
    }
}
