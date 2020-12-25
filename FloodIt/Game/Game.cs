using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FloodIt.Game
{
    public class Game : IDisposable
    {
        readonly UniformGrid _container;
        //List<Rectangle> _rects => _conatiner.Children.OfType<Rectangle>().ToList();
        public event EventHandler<Brush> OnBrushPlayed;

        public bool IsFinished
        {
            get
            {
                for (int i = 1; i < Settings.Count; i++)
                    if (this[i] != UpperLeft)
                        return false;
                return true;
            }
        }
        public GameSettings Settings { get; }
        public Brush UpperLeft => this[0];
        public Brush this[int x, int y]
        {
            get
            {
                //if (x < 0 || Size <= x)
                //    throw new ArgumentOutOfRangeException(nameof(x), $"{nameof(x)} must be between 0 and {Size - 1}");
                //if (y < 0 || Size <= y)
                //    throw new ArgumentOutOfRangeException(nameof(y), $"{nameof(y)} must be between 0 and {Size - 1}");
                Check(x, 0, Settings.Size - 1, nameof(x));
                Check(x, 0, Settings.Size - 1, nameof(y));
                int index = x + y * Settings.Size;
                return this[index];
            }
            private set
            {
                Check(x, 0, Settings.Size - 1, nameof(x));
                Check(x, 0, Settings.Size - 1, nameof(y));
                int index = x + y * Settings.Size;
                this[index] = value;
            }
        }
        public Brush this[int index]
        {
            get
            {
                //if (index < 0 || Size * Size <= index)
                //    throw new ArgumentOutOfRangeException(nameof(index), $"{nameof(index)} must be between 0 and {Size * Size}");
                Check(index, 0, Settings.Count - 1, nameof(index));
                if (_container.Dispatcher.Thread == Thread.CurrentThread)
                    return (_container.Children[index] as Rectangle).Fill;
                return _container.Dispatcher.Invoke(() => (_container.Children[index] as Rectangle).Fill);
            }
            set
            {
                Check(index, 0, Settings.Count - 1, nameof(index));
                if (_container.Dispatcher.Thread == Thread.CurrentThread)
                    (_container.Children[index] as Rectangle).Fill = value;
                _container.Dispatcher.Invoke(() => (_container.Children[index] as Rectangle).Fill = value);
            }
        }

        static void Check(int val, int min, int max, string paramName)
        {
            if (!(min <= val && val <= max))
                throw new ArgumentOutOfRangeException(null, $"{paramName} must be between {min} and {max}");
        }

        public Game(UniformGrid container, IRectangleCreation rectCreation, GameSettings settings = null)
        {
            Settings = settings ?? new();
            _container = container;

            _container.Children.Clear();
            for (int i = 0; i < Settings.Count; i++)
            {
                _container.Children.Add(rectCreation.GetNewRectangle(Settings.UsedBrushes));
                //_board.Add(rectCreation.GetNewRectangle());
            }
        }

        record Point(int X, int Y);
        void PlayBrush(Brush played)
        {
            Stack<Point> stack = new();
            stack.Push(new(0, 0));
            Brush ul = UpperLeft;
            while (stack.Count > 0)
            {
                Point p = stack.Pop();
                Debug.WriteLine(p);
                this[p.X, p.Y] = played;

                if (p.X > 0 && this[p.X - 1, p.Y] == ul)
                    stack.Push(new(p.X - 1, p.Y));
                if (p.Y > 0 && this[p.X, p.Y - 1] == ul)
                    stack.Push(new(p.X, p.Y - 1));
                if (p.X < Settings.Size - 1 && this[p.X + 1, p.Y] == ul)
                    stack.Push(new(p.X + 1, p.Y));
                if (p.Y < Settings.Size - 1 && this[p.X, p.Y + 1] == ul)
                    stack.Push(new(p.X, p.Y + 1));
            }
        }
        record DPoint(int X, int Y, int D) : Point(X, Y);
        async Task PlayBrushAsync(Brush played)
        {
            List<Point> mark = new();
            List<DPoint> dMark = new();
            Queue<Point> queue = new();
            Queue<DPoint> dQueue = new();
            queue.Enqueue(new(0, 0));
            mark.Add(new(0, 0));

            dQueue.Enqueue(new(0, 0, 0));
            dMark.Add(new(0, 0, 0));
            Brush ul = UpperLeft;
            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();
                DPoint dp = dQueue.Dequeue();
                //await Task.Delay(50);
                //Application.Current.Dispatcher.Invoke(new Action(() => this[p.X, p.Y] = played));

                if (p.X > 0 && this[p.X - 1, p.Y] == ul && !mark.Contains(new(p.X - 1, p.Y)))
                {
                    queue.Enqueue(new(p.X - 1, p.Y));
                    mark.Add(new(p.X - 1, p.Y));
                    dQueue.Enqueue(new(p.X - 1, p.Y, dp.D + 1));
                    dMark.Add(new(p.X - 1, p.Y, dp.D + 1));
                }
                if (p.Y > 0 && this[p.X, p.Y - 1] == ul && !mark.Contains(new(p.X, p.Y - 1)))
                {
                    queue.Enqueue(new(p.X, p.Y - 1));
                    mark.Add(new(p.X, p.Y - 1));
                    dQueue.Enqueue(new(p.X, p.Y - 1, dp.D + 1));
                    dMark.Add(new(p.X, p.Y - 1, dp.D + 1));
                }
                if (p.X < Settings.Size - 1 && this[p.X + 1, p.Y] == ul && !mark.Contains(new(p.X + 1, p.Y)))
                {
                    queue.Enqueue(new(p.X + 1, p.Y));
                    mark.Add(new(p.X + 1, p.Y));
                    dQueue.Enqueue(new(p.X + 1, p.Y, dp.D + 1));
                    dMark.Add(new(p.X + 1, p.Y, dp.D + 1));
                }
                if (p.Y < Settings.Size - 1 && this[p.X, p.Y + 1] == ul && !mark.Contains(new(p.X, p.Y + 1)))
                {
                    queue.Enqueue(new(p.X, p.Y + 1));
                    mark.Add(new(p.X, p.Y + 1));
                    dQueue.Enqueue(new(p.X, p.Y + 1, dp.D + 1));
                    dMark.Add(new(p.X, p.Y + 1, dp.D + 1));
                }
            }

            foreach (var g in dMark.GroupBy(dp => dp.D).ToArray())
            {
                await Task.Delay(25);
                foreach (var dp in g.ToArray())
                    this[dp.X, dp.Y] = played;
            }
        }

        public async Task<bool> StartGame(IStrategy strat, CancellationToken cancellationToken = default)
        {
            while (!IsFinished)
            {
                Brush played = await strat.Play(null, cancellationToken);
                if (!Settings.PreventSameBrush || played != UpperLeft)
                {
                    OnBrushPlayed?.Invoke(this, played);
                    await PlayBrushAsync(played);
                }
            }
            return true;
        }

        public void Dispose()
        {
        }
    }
    public class GameState
    {

    }

    public record GameSettings
    {
        public int Size { get; init; } = 9;
        public int Count => Size * Size;
        public Brush[] UsedBrushes { get; init; } = new Brush[] { Brushes.Red, Brushes.Yellow, Brushes.Green, Brushes.Orange, Brushes.Magenta, Brushes.Blue, Brushes.Purple, Brushes.DeepSkyBlue };
        public bool PreventSameBrush = true;
    }

    public interface IRectangleCreation
    {
        Rectangle GetNewRectangle(Brush[] allBrushes);
    }

    public interface IStrategy
    {
        Task<Brush> Play(GameState state, CancellationToken cancellationToken = default);
    }
}
