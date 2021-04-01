using FloodIt.AI.Q;
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
    public static class QLearningMain
    {
        public static void Main(string[] args)
        {
            float alpha = 0.1f; 
            float gamma = 0.9f;
            GameSettings? settings = new() { Size = 6, UsedBrushes = new Brush[] { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Yellow, Brushes.Cyan, Brushes.Gray } };//new() { Size = 6 };// new() { Size = 6, UsedBrushes = new Brush[] { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Yellow } };
            var ai = new QLearning(alpha, gamma, settings);

            float average = 0;
            int nmax = 20;// 20;
            int batchSize = 200;// 5000;
            //string format = new('0', nmax.ToString().Length);

            for (int n = 0; n < nmax; n++)
            {
                var r = ai.Learn(batch: batchSize);
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
                var r = ai2.Learn(batch: batchSize);
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

                if (x.Alpha != y.Alpha || x.Gamma != y.Gamma)
                    return false;

                var s1 = x.Settings;
                var s2 = y.Settings;
                if (s1.Size != s2.Size || s1.PreventSameBrush != s2.PreventSameBrush || s1.UsedBrushes.Length != s2.UsedBrushes.Length)
                    return false;
                
                var u = s1.UsedBrushes.All(b => s1.UsedBrushes.Contains(b));
                if (u == false)
                    return false;

                var c = x.Q.Count == y.Q.Count;
                if (c == false)
                    return false;

                var k = x.Q.Keys.All(b => y.Q.ContainsKey(b));
                if (k == false)
                    return false;

                foreach (var key in x.Q.Keys)
                {
                    var b1 = x.Q[key];
                    var b2 = y.Q[key];
                    for (int i = 0; i < b1.Length; i++)
                        if (b1[i] != b2[i])
                            return false;
                }
                return true;
            }

            public int GetHashCode([DisallowNull] QLearning obj) => obj.Q.Count;
        }
    }
}