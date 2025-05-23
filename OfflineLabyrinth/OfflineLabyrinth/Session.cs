using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace OfflineLabyrinth;

internal static class Session
{
    private static string mem(long size)
    {
        if (size >= 10737418240)
            return $"{size / 1073741824}GB";
        if (size >= 10485760)
            return $"{size / 1048576}MB";
        if (size >= 10240)
            return $"{size / 1024}KB";
        return $"{size}B";
    }

    public static void Run(Stream networkStream, CancellationToken token)
    {
        string gameCommand;
        Command command;

        using (networkStream)
        using (StreamWriter writer = new StreamWriter(networkStream, Encoding.ASCII, 4096, true))
        using (StreamReader reader = new StreamReader(networkStream, Encoding.ASCII, false, 256, true))
        {
            writer.NewLine = "\r\n";

            int width = 362;
            int height = 362;
            int depth = 2;

            bool start = false;

            writer.WriteLine("0 Welcome to ctrl-s NetworkLabyrinth v0.3.\r\n" +
                             "0 \r\n" +
                             "0 Please read the documentation on https://labyrinth.ctrl-s.de/.\r\n" +
                             "0 \r\n" +
                             "0 You are on the main menu. Use the following commands to change settings\r\n" +
                             "0 or to start the game:\r\n" +
                             "0 \r\n" +
                             "0  * WIDTH <32-65536>    Specifies the width of the labyrinth.\r\n" +
                             "0  * HEIGHT <32-65536>   Specifies the height of the labyrinth.\r\n" +
                             "0  * DEPTH <1-16>        Specifies the depth of the labyrinth.\r\n" +
                             "0  * START               Starts the game.\r\n" +
                             "0 \r\n" +
                             "0 Please note that the requested labyrinth can't exceed 16 MB of RAM and that\r\n" +
                             "0 the network timeout has been configured to 120 seconds of inactivity. Just\r\n" +
                             "0 disconnect when you want to stop playing.\r\n" +
                             "0 ");

            writer.WriteLine($"9 [W:{width};H:{height};D:{depth};M={mem((long)width * height * depth)}]>");
            writer.Flush();

            while (!start && !token.IsCancellationRequested)
            {
                try
                {
                    var line = reader.ReadLine();
                    if (line == null) return;
                    command = new Command(line);
                }
                catch (IOException)
                {
                    return;
                }

                switch (command.Name)
                {
                    case "width":
                        if (command.Parameter < 32 || command.Parameter > 65536)
                        {
                            writer.WriteLine("5 Parameter out of bounds. Use a value between [32 and 65536].");
                            writer.Flush();
                        }
                        else
                        {
                            writer.WriteLine($"2 OK.");
                            writer.Flush();
                            width = command.Parameter;
                        }
                        break;
                    case "height":
                        if (command.Parameter < 32 || command.Parameter > 65536)
                        {
                            writer.WriteLine("5 Parameter out of bounds. Use a value between [32 and 65536].");
                            writer.Flush();
                        }
                        else
                        {
                            writer.WriteLine($"2 OK.");
                            writer.Flush();
                            height = command.Parameter;
                        }
                        break;
                    case "depth":
                        if (command.Parameter < 1 || command.Parameter > 16)
                        {
                            writer.WriteLine("5 Parameter out of bounds. Use a value between [1 and 16].");
                            writer.Flush();
                        }
                        else
                        {
                            writer.WriteLine($"2 OK.");
                            writer.Flush();
                            depth = command.Parameter;
                        }
                        break;
                    case "start":
                        if ((long)width * height * depth > 16777216)
                        {
                            writer.WriteLine($"5 The labyrinth you have chosen requires {mem((long)width * height * depth)} of RAM. (Max=16MB.)");
                        }
                        else
                        {
                            start = true;
                        }
                        break;
                    default:
                        writer.WriteLine("5 Unknown command. Use WIDTH ?, HEIGHT ?, DEPTH ? or START.");
                        writer.Flush();
                        break;
                }

            }

            if (!start || token.IsCancellationRequested)
                return;

            writer.Write("0 We now prepare the labyrinth you have requested, which will consist of\r\n" +
                         "0 those Settings:\r\n" +
                        $"0 Your labyrinth has the dimensions of [WxHxD] {width}x{height}x{depth}.\r\n" +
                         "0 \r\n" +
                         "0 The following commands are available when the labyrinth has been generated:\r\n" +
                         "0 \r\n" +
                         "0  * UP          Walks Y--.\r\n" +
                         "0  * DOWN        Walks Y++.\r\n" +
                         "0  * LEFT        Walks X--.\r\n" +
                         "0  * RIGHT       Walks X++.\r\n" +
                         "0  * ENTER       Enters the ladder (Z-- or Z++) or enters the exit.\r\n" +
                         "0  * PRINT       Prints the environment of the player on his current floor.\r\n" +
                         "0 \r\n" +
                         "0 The following Elements will be returned by the PRINT command:\r\n" +
                         "0 \r\n" +
                         "0  ' ' (0x20)    This is floor where you can walk on.\r\n" +
                         "0  'W' (0x57)    This is a wall you can't pass.\r\n" +
                         "0  'P' (0x50)    This is the player (you) itself.\r\n" +
                         "0  '.' (0x2E)    This is a region outside the labyrinth border.\r\n" +
                         "0  'U' (0x55)    This are stairs up to the floor above you. (Z--)\r\n" +
                         "0  'D' (0x44)    This are stairs down to the floor below you. (Z++)\r\n" +
                         "0  'T' (0x54)    This is the target of the labyrinth. Use ENTER when reached.\r\n" +
                         "0 \r\n" +
                         "0 Please wait while we generate several labyrinths and chose the one suiting\r\n" +
                         "0 your settings the most:\r\n" +
                         "0 \r\n" +
                         "1");
            writer.Flush();

            int optimalDistance = 0;
            Random rng = new();
            Labyrinth labyrinth = null;
            Labyrinth checkLabyrinth = new(width, height, depth);

            for (double difficulty = 0.4; difficulty < 0.4205; difficulty += 0.001)
            {
                for (int c = 0; c < 2; c++)
                {
                    int tDistance;
                    checkLabyrinth.Randomize(rng, difficulty);
                    tDistance = checkLabyrinth.SetupBiggestDistance();
                    if (tDistance > optimalDistance)
                    {
                        optimalDistance = tDistance;
                        labyrinth = checkLabyrinth;
                        checkLabyrinth = new Labyrinth(width, height, depth);
                    }
                }

                if (difficulty < 0.4195)
                {
                    int pDifficulty = (int)((difficulty - 0.4) * 5000.0 + 0.5);
                    if (pDifficulty % 10 == 0)
                    {
                        writer.Write($" {pDifficulty}%");
                        writer.Flush();
                    }
                }
                else
                    writer.WriteLine(" 100% DONE.");
            }

            checkLabyrinth = null;
            labyrinth!.ClearRoutes();
            writer.WriteLine($"2 READY.");
            writer.Flush();

            Stopwatch stopWatch = Stopwatch.StartNew();

            while (start && !token.IsCancellationRequested)
            {
                try
                {
                    gameCommand = reader.ReadLine();
                    if (gameCommand == null) return;
                }
                catch (IOException)
                {
                    return;
                }

                switch (gameCommand.Trim().ToLower())
                {
                    case "up":
                        if (labyrinth.Up())
                        {
                            writer.WriteLine("2 DONE.");
                            writer.Flush();
                        }
                        else
                            writer.WriteLine("5 You can't enter that tile.");
                        break;
                    case "down":
                        if (labyrinth.Down())
                        {
                            writer.WriteLine("2 DONE.");
                            writer.Flush();
                        }
                        else
                            writer.WriteLine("5 You can't enter that tile.");
                        break;
                    case "left":
                        if (labyrinth.Left())
                        {
                            writer.WriteLine("2 DONE.");
                            writer.Flush();
                        }
                        else
                            writer.WriteLine("5 You can't enter that tile.");
                        break;
                    case "right":
                        if (labyrinth.Right())
                        {
                            writer.WriteLine("2 DONE.");
                            writer.Flush();
                        }
                        else
                            writer.WriteLine("5 You can't enter that tile.");
                        break;
                    case "enter":
                        if (labyrinth.OnTarget)
                        {
                            start = false;
                            break;
                        }
                        if (labyrinth.Enter())
                        {
                            writer.WriteLine("2 DONE.");
                            writer.Flush();
                        }
                        else
                            writer.WriteLine("5 You can't enter that tile.");
                        break;
                    case "print":
                        writer.WriteLine(labyrinth.Print());
                        writer.Flush();
                        break;
                    default:
                        writer.WriteLine($"5 Unknown command.");
                        writer.Flush();
                        break;
                }

            }

            stopWatch.Stop();
            if (stopWatch.Elapsed.TotalMinutes >= 10.0)
                writer.WriteLine($"8 Congratulation. You solved the labyrinth in {stopWatch.Elapsed.TotalMinutes:F} mins.");
            else
                writer.WriteLine($"8 Congratulation. You solved the labyrinth in {stopWatch.Elapsed.TotalSeconds:F} secs.");
        }
    }
}

