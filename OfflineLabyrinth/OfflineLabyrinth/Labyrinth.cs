using System;
using System.Collections.Generic;
using System.Text;

namespace OfflineLabyrinth
{
    class Labyrinth
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int Depth;

        byte[,,] data;

        private int positionW;
        private int positionH;
        private int positionD;

        private int targetW;
        private int targetH;
        private int targetD;

        public Labyrinth(int width, int height, int depth)
        {
            Width = width;
            Height = height;
            Depth = depth;

            data = new byte[depth, height, width];
        }

        public int W => positionW;
        public int H => positionH;
        public int D => positionD;

        public bool OnTarget => positionD == targetD && positionH == targetH && positionW == targetW;

        public void Randomize(Random rng, double propability)
        {
            for (int d = 0; d < Depth; d++)
                for (int h = 0; h < Height; h++)
                    for (int w = 0; w < Width; w++)
                        data[d, h, w] = rng.NextDouble() <= propability ? (byte)1 : (byte)0;

            int transferNodes = 0;

            if (Depth > 1)
                transferNodes = Width * Height * (Depth - 1) / 9216 + 2;

            for (int d = 0; d < Depth - 1; d++)
                for (int t = 0; t < transferNodes; t++)
                {
                    int x = rng.Next(0, Width);
                    int y = rng.Next(0, Height);

                    if (data[d, y, x] == 0 && data[d + 1, y, x] == 0)
                    {
                        data[d, y, x] = 2;
                        data[d + 1, y, x] = 3;
                    }
                    else
                        t--;
                }
        }

        public int SetupBiggestDistance()
        {
            positionW = 0;
            positionH = 0;
            positionD = 0;

            int mostTiles = 0;
            int tiles;

            for (int d = 0; d < Depth; d++)
                for (int h = 0; h < Height; h++)
                    for (int w = 0; w < Width; w++)
                        if (data[d, h, w] == 0b000)
                        {
                            tiles = GetTilesAccessible(w, h, d);

                            if (mostTiles < tiles)
                            {
                                positionW = w;
                                positionH = h;
                                positionD = d;

                                mostTiles = tiles;
                            }
                        }

            ClearRoutes();

            GetBiggestDistance(positionW, positionH, positionD, out targetW, out targetH, out targetD);

            positionW = targetW;
            positionH = targetH;
            positionD = targetD;

            ClearRoutes();

            return GetBiggestDistance(positionW, positionH, positionD, out targetW, out targetH, out targetD);
        }

        public bool Up()
        {
            if (positionH == 0)
                return false;

            if (data[positionD, positionH - 1, positionW] == 0b001)
                return false;

            positionH--;

            return true;
        }

        public bool Down()
        {
            if (positionH == Height - 1)
                return false;

            if (data[positionD, positionH + 1, positionW] == 0b001)
                return false;

            positionH++;

            return true;
        }

        public bool Left()
        {
            if (positionW == 0)
                return false;

            if (data[positionD, positionH, positionW - 1] == 0b001)
                return false;

            positionW--;

            return true;
        }

        public bool Right()
        {
            if (positionW == Width - 1)
                return false;

            if (data[positionD, positionH, positionW + 1] == 0b001)
                return false;

            positionW++;

            return true;
        }

        public bool Enter()
        {
            if (data[positionD, positionH, positionW] == 0b010)
            {
                positionD++;
                return true;
            }

            if (data[positionD, positionH, positionW] == 0b011)
            {
                positionD--;
                return true;
            }

            return false;
        }

        public int GetTilesAccessible(int sW, int sH, int sD)
        {
            if (data[sD, sH, sW] == 0b001)
                return -1;

            Queue<Node> nodes = new Queue<Node>();

            nodes.Enqueue(new Node(sW, sH, sD));

            Node current;

            data[sD, sH, sW] |= 0b100;

            int used = 0;

            while (nodes.TryDequeue(out current))
            {
                used++;

                if (current.W > 0 && ((data[current.D, current.H, current.W - 1] & 0b101) == 0 || (data[current.D, current.H, current.W - 1] & 0b110) == 0b010))
                {
                    data[current.D, current.H, current.W - 1] |= 0b100;
                    nodes.Enqueue(new Node(current.W - 1, current.H, current.D));
                }

                if (current.W < Width - 1 && ((data[current.D, current.H, current.W + 1] & 0b101) == 0 || (data[current.D, current.H, current.W + 1] & 0b110) == 0b010))
                {
                    data[current.D, current.H, current.W + 1] |= 0b100;
                    nodes.Enqueue(new Node(current.W + 1, current.H, current.D));
                }

                if (current.H > 0 && ((data[current.D, current.H - 1, current.W] & 0b101) == 0 || (data[current.D, current.H - 1, current.W] & 0b110) == 0b010))
                {
                    data[current.D, current.H - 1, current.W] |= 0b100;
                    nodes.Enqueue(new Node(current.W, current.H - 1, current.D));
                }

                if (current.H < Height - 1 && ((data[current.D, current.H + 1, current.W] & 0b101) == 0 || (data[current.D, current.H + 1, current.W] & 0b110) == 0b010))
                {
                    data[current.D, current.H + 1, current.W] |= 0b100;
                    nodes.Enqueue(new Node(current.W, current.H + 1, current.D));
                }

                if (data[current.D, current.H, current.W] == 0b110 && data[current.D + 1, current.H, current.W] == 0b011)
                {
                    data[current.D + 1, current.H, current.W] |= 0b100;
                    nodes.Enqueue(new Node(current.W, current.H, current.D + 1));
                }

                if (data[current.D, current.H, current.W] == 0b111 && data[current.D - 1, current.H, current.W] == 0b010)
                {
                    data[current.D - 1, current.H, current.W] |= 0b100;
                    nodes.Enqueue(new Node(current.W, current.H, current.D - 1));
                }
            }

            return used;
        }

        public int GetBiggestDistance(int sW, int sH, int sD, out int eW, out int eH, out int eD)
        {
            Queue<Node> nodes = new Queue<Node>();

            nodes.Enqueue(new Node(sW, sH, sD));

            Node current;
            Node last = null;

            data[sD, sH, sW] |= 0b100;

            eW = sW;
            eH = sH;
            eD = sD;

            while (nodes.TryDequeue(out current))
            {
                if (current.W > 0 && ((data[current.D, current.H, current.W - 1] & 0b101) == 0 || (data[current.D, current.H, current.W - 1] & 0b110) == 0b010))
                {
                    data[current.D, current.H, current.W - 1] |= 0b100;
                    nodes.Enqueue(new Node(current.W - 1, current.H, current.D, current));
                }

                if (current.W < Width - 1 && ((data[current.D, current.H, current.W + 1] & 0b101) == 0 || (data[current.D, current.H, current.W + 1] & 0b110) == 0b010))
                {
                    data[current.D, current.H, current.W + 1] |= 0b100;
                    nodes.Enqueue(new Node(current.W + 1, current.H, current.D, current));
                }

                if (current.H > 0 && ((data[current.D, current.H - 1, current.W] & 0b101) == 0 || (data[current.D, current.H - 1, current.W] & 0b010) == 0b110))
                {
                    data[current.D, current.H - 1, current.W] |= 0b100;
                    nodes.Enqueue(new Node(current.W, current.H - 1, current.D, current));
                }

                if (current.H < Height - 1 && ((data[current.D, current.H + 1, current.W] & 0b101) == 0 || (data[current.D, current.H + 1, current.W] & 0b110) == 0b010))
                {
                    data[current.D, current.H + 1, current.W] |= 0b100;
                    nodes.Enqueue(new Node(current.W, current.H + 1, current.D, current));
                }

                if (data[current.D, current.H, current.W] == 0b110 && data[current.D + 1, current.H, current.W] == 0b011)
                {
                    data[current.D + 1, current.H, current.W] |= 0b100;
                    nodes.Enqueue(new Node(current.W, current.H, current.D + 1, current));
                }

                if (data[current.D, current.H, current.W] == 0b111 && data[current.D - 1, current.H, current.W] == 0b010)
                {
                    data[current.D - 1, current.H, current.W] |= 0b100;
                    nodes.Enqueue(new Node(current.W, current.H, current.D - 1, current));
                }

                last = current;
            }

            while (last != null && data[last.D, last.H, last.W] != 0b100)
                last = last.Previous;

            if (last == null)
                return 0;

            eW = last.W;
            eH = last.H;
            eD = last.D;

            int steps = 1;

            while (last != null)
            {
                last = last.Previous;
                steps++;
            }

            return steps;
        }

        public void ClearRoutes()
        {
            for (int d = 0; d < Depth; d++)
                for (int h = 0; h < Height; h++)
                    for (int w = 0; w < Width; w++)
                        if (data[d, h, w] >= 0b100)
                            data[d, h, w] &= 0b011;
        }

        public string Print()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("3 ");
            builder.Append(positionW);
            builder.Append("x");
            builder.Append(positionH);
            builder.Append("x");
            builder.Append(positionD);
            builder.Append(".");

            for (int h = positionH - 5; h <= positionH + 5; h++)
            {
                builder.Append("\r\n4 ");

                for (int w = positionW - 5; w <= positionW + 5; w++)
                    if (h < -1 || w < -1 || h > Height || w > Width)
                        builder.Append('.');
                    else if (h == -1 || w == -1 || h == Height || w == Width)
                        builder.Append('W');
                    else
                        switch (data[positionD, h, w])
                        {
                            default:
                                if (h == positionH && w == positionW)
                                    builder.Append('P');
                                else if (positionD == targetD && h == targetH && w == targetW)
                                    builder.Append('T');
                                else
                                    builder.Append(' ');
                                break;
                            case 0b001:
                                builder.Append('W');
                                break;
                            case 0b010:
                                if (h == positionH && w == positionW)
                                    builder.Append('P');
                                else
                                    builder.Append('D');
                                break;
                            case 0b011:
                                if (h == positionH && w == positionW)
                                    builder.Append('P');
                                else
                                    builder.Append('U');
                                break;
                        }
            }

            return builder.ToString();
        }

        public string PrintAll()
        {
            StringBuilder builder = new StringBuilder();

            for (int d = 0; d < Depth; d++)
            {
                builder.Append("Level ");
                builder.AppendLine(d.ToString());

                builder.AppendLine(new string('█', Width + 2));

                for (int h = 0; h < Height; h++)
                {
                    builder.Append('█');

                    for (int w = 0; w < Width; w++)
                        switch (data[d, h, w])
                        {
                            case 0b000:
                                if (d == positionD && h == positionH && w == positionW)
                                    builder.Append('P');
                                else if (d == targetD && h == targetH && w == targetW)
                                    builder.Append('T');
                                else
                                    builder.Append(' ');
                                break;
                            case 0b001:
                                builder.Append('█');
                                break;
                            case 0b010:
                            case 0b110:
                                builder.Append('D');
                                break;
                            case 0b011:
                            case 0b111:
                                builder.Append('U');
                                break;
                            case 0b100:
                                builder.Append('░');
                                break;
                        }

                    builder.AppendLine("█");
                }

                builder.AppendLine(new string('█', Width + 2));
            }

            return builder.ToString();
        }
    }
}
