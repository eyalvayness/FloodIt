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
            var builder = MakeNN(new NNBuilder());
            var nn = builder.Build();

            float[] input = new float[] { 3, 4 };


            var output = nn.FeedForward(input);
        }

        static INNBuilder MakeNN(INNBuilderInput builderInput)
        {
            return builderInput.Input(2)
                               .Dense(2, Activations.Tanh)
                               .Dense(3, Activations.Tanh)
                               .Dense(4, Activations.Tanh);
            //return builder;
        }
    }
}
