using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using FloodIt.AI.NN;

namespace FloodIt.AI
{
    public static class NNMain
    {
        public static async Task Main(string[] args)
        {
            await TrainAndSave();
            //Compiled();
        }

        static void Compiled()
        {
            var nn = NeuralNetwork.Load("NN.json");
            if (nn == null)
                return;
            var compiled = nn.Compile();
            var c1 = compiled.Save("CNN.json");

            var compiled2 = CompiledNeuralNetwork.Load("CNN.json")!;
            var c2 = compiled2.Save("CNN2.json");

            var b1 = compiled.Equals(compiled2);
            var b2 = c1 == c2;

            float[] xs = new float[compiled.InputSize];

            var ys1 = nn.FeedForward(xs);
            var ys2 = compiled.Compute(xs);

            for (int k = 0; k < compiled.OutputSize; k++)
            {
                float y1 = ys1[k], y2 = ys2[k];
                var b = y1 == y2;
            }
        }

        static async Task TrainAndSave()
        {
            var builder = ConfigureNeuralNetwork(new NNBuilder());

            var manager = new NeuroEvolutionManager(builder, poolSize: 20, new() { Size = 4 });
            await manager.Epochs(n: 20);

            var best = manager.BestNetwork;
            string content = best.Save("NN.json", true);

            var compiled = best.Compile();
            var c1 = compiled.Save("CNN.json");
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
