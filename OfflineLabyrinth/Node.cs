using System;
using System.Collections.Generic;
using System.Text;

namespace OfflineLabyrinth
{
    class Node
    {
        public readonly int W;
        public readonly int H;
        public readonly int D;

        public readonly Node Previous;

        public Node(int w, int h, int d)
        {
            W = w;
            H = h;
            D = d;
        }

        public Node(int w, int h, int d, Node previous)
        {
            W = w;
            H = h;
            D = d;

            Previous = previous;
        }
    }
}
