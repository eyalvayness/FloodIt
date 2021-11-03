using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodIt.Core.Utils
{
    public static class RandomExtensions
    {
        readonly static Random _rand;
        static RandomExtensions() => _rand = new();

        public static T Random<T>(this T[] array, Random? rand = null) => array[rand?.Next(array.Length) ?? _rand.Next(array.Length)];
        public static T Random<T>(this IList<T> list, Random? rand = null) => list[rand?.Next(list.Count) ?? _rand.Next(list.Count)];
    }
}
