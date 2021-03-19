using FloodIt.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            GameSettings? settings = new() { Size = 4, UsedBrushes = new Brush[] { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Yellow } };//new() { Size = 6 };// new() { Size = 6, UsedBrushes = new Brush[] { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Yellow } };
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

            average = 0;
            for (int n = 0; n < nmax; n++)
            {
                double r = ai2.Learn(batch: batchSize);
                average += r;
                Debug.WriteLine($"{n + 1:00}/{nmax}: averageR = {r}");
            }
            average /= nmax;
            Debug.WriteLine($"----- total average reward = {average} -----");

            if (json1 == json2) { }
            else { }
        }
    }
}
