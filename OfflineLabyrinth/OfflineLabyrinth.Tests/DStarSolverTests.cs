using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace OfflineLabyrinth.Tests
{
    public class DStarSolverTests
    {
        private readonly ITestOutputHelper output;

        public DStarSolverTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public async Task DStarSolverSolvesLabyrinth()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            using (DStarSolver solver = new DStarSolver())
            {
                string finalLine = await solver.SolveAsync(32, 32, 1);
                Assert.StartsWith("8 Congratulation.", finalLine);
            }
            stopwatch.Stop();
            this.output.WriteLine("DStarSolverSolvesLabyrinth took " + stopwatch.ElapsedMilliseconds + "ms");
        }
    }
}
