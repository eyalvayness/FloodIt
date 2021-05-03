using FloodIt.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodIt.AI.NN
{
    public class NeuroEvolutionManager
    {
        readonly NeuroEvolutionPool _networkPool;

        public int PoolSize { get; }

        public NeuralNetwork this[int index] => _networkPool[index];
        public NeuralNetwork BestNetwork => _networkPool[0];
        public GameSettings Settings { get; }

        public NeuroEvolutionManager(INNBuilder builderTempate, int poolSize, GameSettings? settings = null)
        {
            Settings = settings ?? new();
            PoolSize = poolSize;

            _networkPool = NeuroEvolutionPool.CreateNewPool(builderTempate, poolSize);
        }
        
        public async Task Epochs(int n = 10)
        {
            for (int i = 0; i < n; i++)
            {
                var stats = await Epoch();
                System.Diagnostics.Debug.WriteLine($"--------- Epoch #{i + 1} ---------");
                stats.Debug();
            }
        }

        async Task<NeuroEvolutionPool.PoolStat> Epoch()
        { 
            var runTasks = _networkPool.Select(nn => nn.TrainAsync(maxIteration: 1_000, settings: Settings));

            await Task.WhenAny(runTasks);
            var stats = _networkPool.ReproduceFromBest();
            return stats;
        }
    }

    internal class NeuroEvolutionPool : IEnumerable<NeuralNetwork>
    {
        readonly int _poolSize;
        readonly List<NeuralNetwork> _list;

        public NeuralNetwork this[int index] => _list[index];

        NeuroEvolutionPool(int poolSize)
        {
            _poolSize = poolSize;
            _list = new(_poolSize);
        }

        public PoolStat ReproduceFromBest()
        {
            //var tmp = _list[0];
            var order = _list.OrderByDescending(nn => nn.Fitness).ToList();
            var best = _list.First();

            var fitnesses = order.Select(nn => nn.Fitness).ToList();
            _list.Clear();
            _list.Add(best);

            var children = best.CreateChildren(_poolSize - 1);
            _list.AddRange(children);
            return new(fitnesses[0], fitnesses[^1], order.Average(nn => nn.Fitness), (fitnesses[(fitnesses.Count - 1) / 2] + fitnesses[fitnesses.Count / 2]) / 2f);
        }

        public static NeuroEvolutionPool CreateNewPool(INNBuilder builderTemplate, int poolSize)
        {
            NeuroEvolutionPool nnp = new(poolSize);

            for (int i = 0; i < poolSize; i++)
                nnp._list.Add(builderTemplate.Build());
            return nnp;
        }

        public record PoolStat(float BestFitness, float WorstFitness, float AverageFitness, float MeanFitness)
        {
            public void Debug()
            {
                System.Diagnostics.Debug.WriteLine($"Best fitness:\t\t{BestFitness}");
                System.Diagnostics.Debug.WriteLine($"Worst fitness:\t\t{WorstFitness}");
                System.Diagnostics.Debug.WriteLine($"Average fitness:\t{AverageFitness}");
                System.Diagnostics.Debug.WriteLine($"Mean fitness:\t\t{MeanFitness}");
            }
        }

        public IEnumerator<NeuralNetwork> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
