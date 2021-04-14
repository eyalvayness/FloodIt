using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodIt.AI.NN
{
    public class NeuroEvolutionManager
    {
        readonly INNBuilder _builderTemplate;
        readonly NeuralNetwork[] _networks;

        public int PoolSize { get; }

        public NeuralNetwork this[int index] => _networks[index];
        public NeuralNetwork BestNetwork => _networks[0];


        public NeuroEvolutionManager(INNBuilder builderTempate, int poolSize)
        {
            _builderTemplate = builderTempate;
            PoolSize = poolSize;

            _networks = new NeuralNetwork[poolSize];
            InitNetworks();
        }

        void InitNetworks()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                NeuralNetwork nn = _builderTemplate.Build();
                _networks[i] = nn;
            }
        }
    }
}
