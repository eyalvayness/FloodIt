using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodIt.AI.NN
{
    class Activation
    {
        public static Activation Identity => (Activation)((float f) => f);
        public static Activation Tanh => (Activation)MathF.Tanh;
        public static Activation ReLU => (Activation)((float f) => f < 0 ? 0 : f);
        public static Activation Sigmoid => (Activation)((float f) => 1 / (1 + MathF.Exp(-f)));
        public static Activation BinaryStep => (Activation)((float f) => f < 0 ? 0 : 1);
        public static Activation LeakyReLU => (Activation)((float f) => f < 0 ? 0.01f * f : f);
        public static Activation Softmax => (Activation)((float[] fs) =>
        {
            var ffs = fs.Select(MathF.Exp);
            var sum = ffs.Sum();
            return ffs.Select(f => f / sum).ToArray();
        });


        readonly Func<float[], float[]> _activation;

        public Activation(Func<float[], float[]> activation)
        {
            _activation = activation;
        }
        public Activation(Func<float, float> singleActivation)
        {
            _activation = (float[] fs) => fs.Select(singleActivation).ToArray();
        }

        public float[] Activate(float[] fs) => _activation(fs);

        public static implicit operator Activation(Func<float[], float[]> af) => new(af);
        public static implicit operator Activation(Func<float, float> af) => new(af);

        public static implicit operator Activation(Activations a) => a switch
        {
            Activations.Identity => Identity,
            Activations.Tanh => Tanh,
            Activations.ReLU => ReLU,
            Activations.Sigmoid => Sigmoid,
            Activations.BinaryStep => BinaryStep,
            Activations.LeakyReLU => LeakyReLU,
            Activations.Softmax => Softmax,
            _ => throw new NotImplementedException()
        };
    }

    public enum Activations
    {
        Identity,
        Tanh,
        ReLU,
        Sigmoid,
        BinaryStep,
        LeakyReLU,
        Softmax,
    }
}
