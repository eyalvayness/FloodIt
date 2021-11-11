using FloodIt.Core;
using FloodIt.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FloodIt.AI.Algos
{

    record Point(int X, int Y);

    public class MonteCarlo : IAsyncStrategy
    {
        int RecBFSFrom(GameState state, Brush b, int x, int y, List<Point> visited)
        {
            var count = state[x, y] == b ? 1 : 0;
            var n = Math.Sqrt(state.SimplifiedBoard.Length);
            visited.Add(new(x, y));
            
            int newX = x, newY = y;
            if (x > 0)
            {
                newX = x - 1;
                newY = y;
                if (visited.Contains(new(newX, newY)) == false && (state[newX, newY] == b || state[newX, newY] == state[0, 0]))
                    count += RecBFSFrom(state, b, newX, newY, visited);
            }
            if (y > 0)
            {
                newX = x;
                newY = y - 1;
                if (visited.Contains(new(newX, newY)) == false && (state[newX, newY] == b || state[newX, newY] == state[0, 0]))
                    count += RecBFSFrom(state, b, newX, newY, visited);
            }
            if (x < n - 1)
            {
                newX = x + 1;
                newY = y;
                if (visited.Contains(new(newX, newY)) == false && (state[newX, newY] == b || state[newX, newY] == state[0, 0]))
                    count += RecBFSFrom(state, b, newX, newY, visited);
            }
            if (y < n - 1)
            {
                newX = x;
                newY = y + 1;
                if (visited.Contains(new(newX, newY)) == false && (state[newX, newY] == b || state[newX, newY] == state[0, 0]))
                    count += RecBFSFrom(state, b, newX, newY, visited);
            }

            return count;
        }

        int BreadthFirstSearch(GameState state, Brush b) => RecBFSFrom(state, b, 0, 0, new());

        Brush[] GetWeightedBorderVector(GameState state)
        {
            List<Brush> result = new List<Brush>();

            foreach (Brush playableBrush in state.PlayableBrushes)
            {
                int count = BreadthFirstSearch(state, playableBrush);
                result.AddRange(Enumerable.Repeat(playableBrush, count));
            }

            return result.ToArray();
        }

        public async Task<Brush?> PlayAsync(GameState state, CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken);

            var vector = GetWeightedBorderVector(state);
            var index = Random.Shared.Next(vector.Length);

            return vector[index];
        }
    }
}
