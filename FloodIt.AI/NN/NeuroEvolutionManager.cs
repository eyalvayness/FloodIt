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
                stats.Debug($"--------- Epoch #{i + 1} ---------");
            }
        }

        async Task<NeuroEvolutionPool.PoolStat> Epoch()
        { 
            var runTasks = _networkPool.Select(nn => nn.TrainAsync(maxIteration: 1_000, settings: Settings));

            await Task.WhenAll(runTasks);
            var stats = _networkPool.ReproduceFromBest();
            return stats;
        }
    }

    internal class NeuroEvolutionPool : IEnumerable<NeuralNetwork>, IReadOnlyList<NeuralNetwork>
    {
        readonly int _poolSize;
        readonly List<NeuralNetwork> _list;

        public NeuralNetwork this[int index] => _list[index];
        public int Count => _list.Count;

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
            return new(fitnesses);
        }

        public static NeuroEvolutionPool CreateNewPool(INNBuilder builderTemplate, int poolSize)
        {
            NeuroEvolutionPool nnp = new(poolSize);

            for (int i = 0; i < poolSize; i++)
                nnp._list.Add(builderTemplate.Build());
            return nnp;
        }

        public record PoolStat
        {
            readonly float[] _fitnesses;

            public float BestFitness { get; }
            public float WorstFitness { get; }
            public float AverageFitness { get; }
            public float MeanFitness { get; }
            public float PositiveFitnesses { get; }

            public PoolStat(IEnumerable<float> fitnesses)
            {
                _fitnesses = fitnesses.ToArray();

                BestFitness = _fitnesses.Max();
                WorstFitness = _fitnesses.Min(); 
                AverageFitness = _fitnesses.Average();
                MeanFitness = (_fitnesses[(_fitnesses.Length - 1) / 2] + _fitnesses[_fitnesses.Length / 2]) / 2f;
                PositiveFitnesses = _fitnesses.Count(f => f > 0);
            }

            public void Debug(string? title = null)
            {
                if (string.IsNullOrEmpty(title) == false)
                    System.Diagnostics.Debug.WriteLine(title);
                System.Diagnostics.Debug.WriteLine($"Best fitness:\t\t{BestFitness}");
                System.Diagnostics.Debug.WriteLine($"Worst fitness:\t\t{WorstFitness}");
                System.Diagnostics.Debug.WriteLine($"Average fitness:\t{AverageFitness}");
                System.Diagnostics.Debug.WriteLine($"Mean fitness:\t\t{MeanFitness}");
                System.Diagnostics.Debug.WriteLine($"Positive fitnesses:\t{PositiveFitnesses/_fitnesses.Length}");
            }
        }

        public IEnumerator<NeuralNetwork> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
