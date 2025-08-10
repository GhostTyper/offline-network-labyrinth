using System.Threading.Tasks;
using Xunit;

namespace OfflineLabyrinth.Tests
{
    public class DStarSolverTests
    {
        [Fact]
        public async Task DStarSolverSolvesLabyrinth()
        {
            using (DStarSolver solver = new DStarSolver())
            {
                string finalLine = await solver.SolveAsync(32, 32, 1);
                Assert.StartsWith("8 Congratulation.", finalLine);
            }
        }
    }
}
