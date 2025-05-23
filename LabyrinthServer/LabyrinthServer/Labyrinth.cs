using System;
using System.Collections.Generic;
using System.Text;

namespace LabyrinthServer
{
    class Labyrinth
    {
        private byte[,] labData = new byte[512, 512];
        private int x = 26;
        private int y = 26;

        private int lowerX;
        private int lowerY;
        private int upperX;
        private int upperY;

        private int relX;
        private int relY;

        public Labyrinth()
        {
            Random r = new Random();

            if (r.Next(10) == 2)
            {
                lowerX = r.Next(-6, -3);
                lowerY = r.Next(-6, -3);
                upperX = r.Next(4, 7);
                upperY = r.Next(4, 7);
            }
            else
            {
                lowerX = -5;
                lowerY = -5;
                upperX = 5;
                upperY = 5;
            }

            relX = r.Next(-512, 513);
            relY = r.Next(-512, 513);

            generateLabyrinth();

            while (!checkWay(26, 26, 0))
                generateLabyrinth();
        }

        public string Print()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("3 ");
            sb.Append(x + relX);
            sb.Append(" ");
            sb.Append(y + relY);
            sb.AppendLine("\r");

            for (int iY = y + lowerY; iY <= y + upperY; iY++)
            {
                sb.Append("4 ");

                for (int iX = x + lowerX; iX <= x + upperX; iX++)
                    if (iX == x && iY == y)
                        sb.Append("S");
                    else
                        switch (labData[iX, iY])
                        {
                            case 1:
                                sb.Append("W");
                                break;
                            case 2:
                                sb.Append("Z");
                                break;
                            default:
                                sb.Append(".");
                                break;
                        }

                sb.AppendLine("\r");
            }

            return sb.ToString();
        }

        public bool Left()
        {
            if (labData[x - 1, y] == 1)
                return false;

            x--;

            return true;
        }

        public bool Right()
        {
            if (labData[x + 1, y] == 1)
                return false;

            x++;

            return true;
        }

        public bool Up()
        {
            if (labData[x, y - 1] == 1)
                return false;

            y--;

            return true;
        }

        public bool Down()
        {
            if (labData[x, y + 1] == 1)
                return false;

            y++;

            return true;
        }

        public bool IsOnTarget
        {
            get
            {
                return labData[x, y] == 2;
            }
        }

        private void generateLabyrinth()
        {
            Random r = new Random();

            for (int i = 5; i < 507; i++)
            {
                labData[i, 5] = 1;
                labData[i, 506] = 1;
                labData[5, i] = 1;
                labData[506, i] = 1;
            }

            for (int x = 6; x < 506; x++)
                for (int y = 6; y < 506; y++)
                    labData[x, y] = (byte)(r.NextDouble() < 0.4 ? 1 : 0);

            labData[26, 26] = 0;
            labData[505, 505] = 2;
        }

        private bool checkWay(int x, int y, int runNo)
        {
            if (labData[x, y] == 2)
                return true;

            if (runNo > 16384)
                return false;

            labData[x, y] = 16;

            if (labData[x + 1, y] != 1 && labData[x + 1, y] != 16 && checkWay(x + 1, y, runNo + 1))
                return true;

            if (labData[x - 1, y] != 1 && labData[x - 1, y] != 16 && checkWay(x - 1, y, runNo + 1))
                return true;

            if (labData[x, y + 1] != 1 && labData[x, y + 1] != 16 && checkWay(x, y + 1, runNo + 1))
                return true;

            if (labData[x, y - 1] != 1 && labData[x, y - 1] != 16 && checkWay(x, y - 1, runNo + 1))
                return true;

            return false;
        }
    }
}
