using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodIt.AI.NN
{
    class Activation
    {
        public static Activation Identity => new((float f) => f, Activations.Identity);
        public static Activation Tanh => new(MathF.Tanh, Activations.Tanh);
        public static Activation ReLU => new((float f) => f < 0 ? 0 : f, Activations.ReLU);
        public static Activation Sigmoid => new((float f) => 1 / (1 + MathF.Exp(-f)), Activations.Sigmoid);
        public static Activation BinaryStep => new((float f) => f < 0 ? 0 : 1, Activations.BinaryStep);
        public static Activation LeakyReLU => new((float f) => f < 0 ? 0.01f * f : f, Activations.LeakyReLU);
        public static Activation Softmax => new((float[] fs) =>
        {
            var ffs = fs.Select(MathF.Exp);
            var sum = ffs.Sum();
            return ffs.Select(f => f / sum).ToArray();
        }, Activations.Softmax);


        readonly Func<float[], float[]> _activation;
        readonly Activations _enumVal;

        private Activation(Func<float[], float[]> activation, Activations enumVal)
        {
            _activation = activation;
            _enumVal = enumVal;
        }
        private Activation(Func<float, float> singleActivation, Activations enumVal) : this((float[] fs) => fs.Select(singleActivation).ToArray(), enumVal)
        { }

        public float[] Activate(float[] fs) => _activation(fs);

        //public static implicit operator Activation(Func<float[], float[]> af) => new(af);
        //public static implicit operator Activation(Func<float, float> af) => new(af);

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
        public static explicit operator Activations(Activation a) => a._enumVal;
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
