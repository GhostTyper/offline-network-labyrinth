using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace LabyrinthServer
{
    class Client
    {
        private Thread thread;
        private TcpClient client;

        private StreamReader reader;
        private StreamWriter writer;

        private static int clientCount = 0;
        private static object sync = new object();

        public Client(TcpClient client)
        {
            this.client = client;

            client.NoDelay = true;

            writer = new StreamWriter(new BufferedStream(client.GetStream()), Encoding.GetEncoding(850));
            reader = new StreamReader(client.GetStream(), Encoding.GetEncoding(850));

            writer.NewLine = "\x0D\x0A";

            client.ReceiveTimeout = 8000;

            lock (sync)
            {
                if (clientCount > 2)
                {
                    try
                    {
                        writer.WriteLine("9 Zu viele Verbindungen.");
                        writer.Flush();

                        client.Close();
                    }
                    catch { }

                    return;
                }

                clientCount++;
            }

            thread = new Thread(work, 16777216);
            thread.Start();
        }

        private void work()
        {
            try
            {
                Stopwatch sw = new Stopwatch();

                writer.WriteLine("0 Willkommen zur \"Miniprojekt - Netzwerk\"-Aufgabe.");
                writer.WriteLine("0 ");
                writer.Write("0 Bitte geben sie das Aufgabenpasswort ein: ");
                writer.Flush();

                if (reader.ReadLine() != "supsup")
                {
                    writer.WriteLine("9 Falsches Passwort.");
                    writer.Flush();

                    throw new Exception("Passwort Falsch.");
                }

                writer.WriteLine("2 OK - fahre fort. Verfügbare Kommandos:");
                writer.WriteLine("0 PRINT - Gibt die Umgebung zurück.");
                writer.WriteLine("0 LEFT - Läuft einen Schritt nach links.");
                writer.WriteLine("0 RIGHT - Läuft einen Schritt nach rechts.");
                writer.WriteLine("0 UP - Läuft einen Schritt hoch.");
                writer.WriteLine("0 DOWN - Läuft einen schritt runter.");
                writer.WriteLine("1 Bitte warten...");
                writer.Flush();

                Labyrinth labyrinth = new Labyrinth();

                writer.WriteLine("2 Bereit.");
                writer.Flush();

                sw.Start();

                while (true)
                {
                    switch (reader.ReadLine())
                    {
                        case "PRINT":
                            writer.Write(labyrinth.Print());
                            writer.WriteLine("2 Bereit.");
                            writer.Flush();
                            break;
                        case "LEFT":
                            if (labyrinth.Left())
                                writer.WriteLine("2 OK.");
                            else
                                writer.WriteLine("5 Verweigert.");

                            writer.Flush();
                            break;
                        case "RIGHT":
                            if (labyrinth.Right())
                                writer.WriteLine("2 OK.");
                            else
                                writer.WriteLine("5 Verweigert.");

                            writer.Flush();
                            break;
                        case "UP":
                            if (labyrinth.Up())
                                writer.WriteLine("2 OK.");
                            else
                                writer.WriteLine("5 Verweigert.");

                            writer.Flush();
                            break;
                        case "DOWN":
                            if (labyrinth.Down())
                                writer.WriteLine("2 OK.");
                            else
                                writer.WriteLine("5 Verweigert.");

                            writer.Flush();
                            break;
                        default:
                            writer.WriteLine("6 Unbekanntes Kommando.");
                            writer.Flush();
                            break;
                    }

                    if (labyrinth.IsOnTarget)
                    {
                        writer.WriteLine("8 Ziel erreicht.");
                        writer.Write("7 ");
                        writer.Write(sw.Elapsed.TotalMilliseconds.ToString("F"));
                        writer.WriteLine(" ms.");

                        writer.Flush();
                        break;
                    }
                }
            }
            catch
            {
            }

            lock (sync)
                clientCount--;

            client.Close();
        }
    }
}
