using System;
using System.Collections.Generic;
using System.Text;

namespace OfflineLabyrinth
{
    class Command
    {
        public readonly string Name;
        public readonly int Parameter;

        public Command(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            string[] results = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (results.Length == 0 || results.Length > 2)
                return;

            if (string.IsNullOrWhiteSpace(results[0]))
                return;

            if (results.Length == 2 && (!int.TryParse(results[1], out Parameter) || Parameter < 0))
                return;

            Name = results[0].ToLower();
        }
    }
}
