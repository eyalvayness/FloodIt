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
        public static void Main(string[] args)
        {
            //int[] layers = new int[] { 2, 3, 4 };
            var builder = ConfigureNeuralNetwork(new NNBuilder());
            //var nn = builder.Build();

            //float[] input = new float[] { 3, 4 };


            //float[] output = nn.FeedForward(input);

            var manager = new NeuroEvolutionManager(builder, 5);
        }

        static INNBuilder ConfigureNeuralNetwork(INNBuilderInput builderInput)
        {
            return builderInput.Input(2)
                               .Dense(3, Activations.Tanh)
                               .Dense(4, Activations.Tanh);
            //return builder;
        }
    }
}
