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


        public NeuroEvolutionManager(INNBuilder builderTempate, int poolSize)
        {
            PoolSize = poolSize;

            _networkPool = NeuroEvolutionPool.CreateNewPool(builderTempate, poolSize);
        }

        void Epoch()
        {

            _networkPool.ReproduceFromBest();
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

        public void ReproduceFromBest()
        {
            //var tmp = _list[0];
            var best = _list.OrderByDescending(nn => nn.Fitness).First();
            _list.Clear();
            _list.Add(best);

            var children = best.CreateChildren(_poolSize - 1);
            _list.AddRange(children);
        }

        public static NeuroEvolutionPool CreateNewPool(INNBuilder builderTemplate, int poolSize)
        {
            NeuroEvolutionPool nnp = new(poolSize);

            for (int i = 0; i < poolSize; i++)
                nnp._list.Add(builderTemplate.Build());
            return nnp;
        }

        public IEnumerator<NeuralNetwork> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
