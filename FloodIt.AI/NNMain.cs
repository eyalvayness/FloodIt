using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FloodIt.AI.NN;

namespace FloodIt.AI
{
    public static class NNMain
    {
        public static async Task Main(string[] args)
        {
            //int[] layers = new int[] { 2, 3, 4 };
            var builder = ConfigureNeuralNetwork(new NNBuilder());
            //var nn = builder.Build();

            //float[] input = new float[] { 3, 4 };


            //float[] output = nn.FeedForward(input);

            var manager = new NeuroEvolutionManager(builder, poolSize: 20, new() { Size = 4 });
            await manager.Epochs(n: 1_000);
        }

        static INNBuilder ConfigureNeuralNetwork(INNBuilderInput builderInput)
        {
            return builderInput.Input(16)
                               .Dense(14, Activations.LeakyReLU)
                               .Dense(12, Activations.LeakyReLU)
                               .Dense(10, Activations.LeakyReLU)
                               .Dense(7, Activations.Softmax);
            //return builder;
        }
    }
}
