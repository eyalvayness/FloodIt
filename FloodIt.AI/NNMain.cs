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

            var manager = new NeuroEvolutionManager(builder, 5, new() { Size = 4 });
            await manager.Epoch();
        }

        static INNBuilder ConfigureNeuralNetwork(INNBuilderInput builderInput)
        {
            return builderInput.Input(16)
                               .Dense(10, Activations.Tanh)
                               //.Dense(30, Activations.Tanh)
                               //.Dense(15, Activations.Tanh)
                               .Dense(7, Activations.Softmax);
            //return builder;
        }
    }
}
