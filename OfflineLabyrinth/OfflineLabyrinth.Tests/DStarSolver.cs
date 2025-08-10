using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OfflineLabyrinth.Tests
{
    internal sealed class DStarSolver : IDisposable
    {
        private global::OfflineLabyrinth.OfflineLabyrinth _labyrinth;
        private StreamWriter _writer;
        private StreamReader _reader;
        private int _width;
        private int _height;
        private char[,] _map;
        private int _playerX;
        private int _playerY;
        private int _targetX;
        private int _targetY;

        private struct Coord
        {
            public int X;
            public int Y;
            public Coord(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        public DStarSolver()
        {
            _labyrinth = new global::OfflineLabyrinth.OfflineLabyrinth(0);
            _writer = new StreamWriter(_labyrinth.Stream);
            _writer.NewLine = "\r\n";
            _writer.AutoFlush = true;
            _reader = new StreamReader(_labyrinth.Stream);
            _targetX = -1;
            _targetY = -1;
        }

        public async Task<string> SolveAsync(int width, int height, int depth)
        {
            _width = width;
            _height = height;
            _map = new char[_height, _width];

            for (int y = 0; y < _height; y++)
                for (int x = 0; x < _width; x++)
                    _map[y, x] = '?';

            for (int i = 0; i < 20; i++)
            {
                string? introLine = await _reader.ReadLineAsync();
                if (introLine == null || introLine.StartsWith("9 "))
                    break;
            }

            string response = await SendCommandAsync("WIDTH " + width);
            response = await SendCommandAsync("HEIGHT " + height);
            response = await SendCommandAsync("DEPTH " + depth);

            await _writer.WriteLineAsync("START");
            string? startLine;
            do
            {
                startLine = await _reader.ReadLineAsync();
            } while (startLine != null && !startLine.StartsWith("2 READY."));
            await PrintAndUpdateAsync();

            while (!(_targetX >= 0 && _playerX == _targetX && _playerY == _targetY))
            {
                List<Coord> path;
                if (_targetX >= 0)
                {
                    path = ComputePath(new Coord(_playerX, _playerY), new Coord(_targetX, _targetY));
                }
                else
                {
                    path = FindPathToUnknown();
                }
                if (path.Count < 2)
                {
                    break;
                }
                Coord next = path[1];
                await MoveStepAsync(next);
                await PrintAndUpdateAsync();
            }

            await _writer.WriteLineAsync("ENTER");
            string? finalMessage = await _reader.ReadLineAsync();
            if (finalMessage == null)
            {
                return string.Empty;
            }
            return finalMessage;
        }

        private async Task<string> SendCommandAsync(string command)
        {
            await _writer.WriteLineAsync(command);
            string? line = await _reader.ReadLineAsync();
            return line ?? string.Empty;
        }

        private async Task MoveStepAsync(Coord next)
        {
            string command;
            if (next.X > _playerX)
            {
                command = "RIGHT";
            }
            else if (next.X < _playerX)
            {
                command = "LEFT";
            }
            else if (next.Y > _playerY)
            {
                command = "DOWN";
            }
            else
            {
                command = "UP";
            }
            string response = await SendCommandAsync(command);
        }

        private async Task PrintAndUpdateAsync()
        {
            await _writer.WriteLineAsync("PRINT");
            string? header = await _reader.ReadLineAsync();
            if (header == null)
            {
                return;
            }
            string positionPart = header.Substring(2).TrimEnd('.');
            string[] parts = positionPart.Split('x');
            _playerX = int.Parse(parts[0]);
            _playerY = int.Parse(parts[1]);

            for (int row = 0; row < 11; row++)
            {
                string? line = await _reader.ReadLineAsync();
                if (line == null)
                {
                    break;
                }
                string content = line.Substring(2);
                for (int col = 0; col < 11 && col < content.Length; col++)
                {
                    int globalX = _playerX - 5 + col;
                    int globalY = _playerY - 5 + row;
                    if (globalX >= 0 && globalX < _width && globalY >= 0 && globalY < _height)
                    {
                        char tile = content[col];
                        if (tile == 'P')
                        {
                            tile = ' ';
                        }
                        if (tile == 'T')
                        {
                            _targetX = globalX;
                            _targetY = globalY;
                        }
                        if (tile == ' ' || tile == 'W' || tile == 'T')
                        {
                            _map[globalY, globalX] = tile;
                        }
                    }
                }
            }
        }

        private List<Coord> ComputePath(Coord start, Coord goal)
        {
            Queue<Coord> queue = new Queue<Coord>();
            bool[,] visited = new bool[_height, _width];
            Dictionary<Coord, Coord> previous = new Dictionary<Coord, Coord>();
            queue.Enqueue(start);
            visited[start.Y, start.X] = true;

            int[] dx = new int[4] { 1, -1, 0, 0 };
            int[] dy = new int[4] { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                Coord current = queue.Dequeue();
                if (current.X == goal.X && current.Y == goal.Y)
                {
                    return Reconstruct(previous, current);
                }
                for (int i = 0; i < 4; i++)
                {
                    int nx = current.X + dx[i];
                    int ny = current.Y + dy[i];
                    if (nx >= 0 && nx < _width && ny >= 0 && ny < _height && !visited[ny, nx] && _map[ny, nx] != 'W')
                    {
                        visited[ny, nx] = true;
                        Coord next = new Coord(nx, ny);
                        previous[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }
            return new List<Coord>();
        }

        private List<Coord> FindPathToUnknown()
        {
            Queue<Coord> queue = new Queue<Coord>();
            bool[,] visited = new bool[_height, _width];
            Dictionary<Coord, Coord> previous = new Dictionary<Coord, Coord>();
            Coord start = new Coord(_playerX, _playerY);
            queue.Enqueue(start);
            visited[_playerY, _playerX] = true;

            int[] dx = new int[4] { 1, -1, 0, 0 };
            int[] dy = new int[4] { 0, 0, 1, -1 };

            while (queue.Count > 0)
            {
                Coord current = queue.Dequeue();
                if (HasUnknownNeighbor(current))
                {
                    return Reconstruct(previous, current);
                }
                for (int i = 0; i < 4; i++)
                {
                    int nx = current.X + dx[i];
                    int ny = current.Y + dy[i];
                    if (nx >= 0 && nx < _width && ny >= 0 && ny < _height && !visited[ny, nx] && _map[ny, nx] != 'W')
                    {
                        visited[ny, nx] = true;
                        Coord next = new Coord(nx, ny);
                        previous[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }
            return new List<Coord>();
        }

        private bool HasUnknownNeighbor(Coord cell)
        {
            int[] dx = new int[4] { 1, -1, 0, 0 };
            int[] dy = new int[4] { 0, 0, 1, -1 };
            for (int i = 0; i < 4; i++)
            {
                int nx = cell.X + dx[i];
                int ny = cell.Y + dy[i];
                if (nx >= 0 && nx < _width && ny >= 0 && ny < _height)
                {
                    if (_map[ny, nx] == '?')
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private List<Coord> Reconstruct(Dictionary<Coord, Coord> previous, Coord current)
        {
            List<Coord> path = new List<Coord>();
            Coord step = current;
            path.Add(step);
            while (previous.ContainsKey(step))
            {
                step = previous[step];
                path.Add(step);
            }
            path.Reverse();
            return path;
        }

        public void Dispose()
        {
            _reader.Dispose();
            _writer.Dispose();
            _labyrinth.Dispose();
        }
    }
}
