using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodIt.Utils
{
    public static class RandomExtensions
    {
        readonly static Random _rand;
        static RandomExtensions() => _rand = new();

        public static T Random<T>(this T[] array) => array[_rand.Next(array.Length)];
        public static T Random<T>(this IList<T> list) => list[_rand.Next(list.Count)];
    }
}
