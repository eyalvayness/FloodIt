using FloodIt.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FloodIt.AI
{
    public static class Console
    {
        public static void Main(string[] args)
        {
            double alpha = 0.1; 
            double gamma = 0.9;
            GameSettings? settings = null;// new() { Size = 5, UsedBrushes = new Brush[] { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Yellow, Brushes.Cyan, Brushes.Gray } };//new() { Size = 6 };// new() { Size = 6, UsedBrushes = new Brush[] { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Yellow } };
            var ai = new QLearning(alpha, gamma, settings);

            double average = 0;
            int nmax = 50;
            int batchSize = 1000;
            //string format = new('0', nmax.ToString().Length);

            for (int n = 0; n < nmax; n++)
            {
                double r = ai.Learn(batch: batchSize);
                average += r;
                Debug.WriteLine($"{n+1:00}/{nmax}: averageR = {r}");
            }
            average /= nmax;
            Debug.WriteLine($"----- total average reward = {average} -----");

            var filename = "AI.json";
            var json1 = ai.Save(filename);
            var ai2 = QLearning.Load(filename);
            _ = ai2 ?? throw new NullReferenceException();
            var json2 = ai2.Save("AI2.json");

            var comp = new QLearningEqualityComparer();
            if (comp.Equals(ai, ai2) && json1 == json2) { }
            else { }

            average = 0;
            for (int n = 0; n < nmax; n++)
            {
                double r = ai2.Learn(batch: batchSize);
                average += r;
                Debug.WriteLine($"{n + 1:00}/{nmax}: averageR = {r}");
            }
            average /= nmax;
            Debug.WriteLine($"----- total average reward = {average} -----");
        }

        public class QLearningEqualityComparer : IEqualityComparer<QLearning>
        {
            public bool Equals(QLearning? x, QLearning? y)
            {
                if (x is null || y is null)
                    return false;

                var c = x.Q.Count == y.Q.Count;
                var k1 = x.Q.Keys.All(b => y.Q.ContainsKey(b));
                var k2 = y.Q.Keys.All(b => x.Q.ContainsKey(b));
                var k = k1 && k2;

                var v1 = x.Q.Select(kvp =>
                {
                    var b1 = kvp.Value;
                    var b2 = y.Q[kvp.Key];
                    return b1.Select((d, i) => d == b2[i]).All(b => b);
                }).All(b => b);
                var v2 = y.Q.Select(kvp =>
                {
                    var b1 = kvp.Value;
                    var b2 = x.Q[kvp.Key];
                    return b1.Select((d, i) => d == b2[i]).All(b => b);
                }).All(b => b);

                return c && v1 && k;
            }

            public int GetHashCode([DisallowNull] QLearning obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}