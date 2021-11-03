using FloodIt.Core.Interfaces;
using FloodIt.Core.Models;
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
using System.Text.Json.Serialization;

namespace FloodIt.Core
{
    public delegate Brush BrushGetter(int index);
    public delegate void BrushSetter(int index, Brush brush);

    public class Game
    {
        //readonly UniformGrid _container;
        readonly BrushGetter _getBrush;
        readonly BrushSetter _setBrush;
        //List<Rectangle> _rects => _conatiner.Children.OfType<Rectangle>().ToList();
        public event EventHandler<Brush>? OnBrushPlayed;
        GameState _currentState;

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
                CheckInRange(x, 0, Settings.Size - 1, nameof(x));
                CheckInRange(x, 0, Settings.Size - 1, nameof(y));
                int index = x + y * Settings.Size;
                return this[index];
            }
            private set
            {
                CheckInRange(x, 0, Settings.Size - 1, nameof(x));
                CheckInRange(x, 0, Settings.Size - 1, nameof(y));
                int index = x + y * Settings.Size;
                this[index] = value;
            }
        }
        public Brush this[int index]
        {
            get
            {
                CheckInRange(index, 0, Settings.Count - 1, nameof(index));
                return _getBrush(index);
            }
            set
            {
                CheckInRange(index, 0, Settings.Count - 1, nameof(index));
                _setBrush(index, value);
            }
        }

        static void CheckInRange(int val, int min, int max, string paramName)
        {
            if (!(min <= val && val <= max))
                throw new ArgumentOutOfRangeException(null, $"{paramName} must be between {min} and {max}");
        }

        public Game(BrushGetter getBrush, BrushSetter setBrush, GameSettings? settings = null)
        {
            Settings = settings ?? new();

            _setBrush = setBrush;
            _getBrush = getBrush;

            _currentState = new GameState(Settings);
            ApplyChanges();
        }

        void ApplyChanges()
        {
            var g = _currentState.GetLastChanges();
            while (g != null) 
            {
                foreach (var p in g)
                    this[p.X, p.Y] = _currentState[p.X, p.Y];
                g = _currentState.GetLastChanges();
            }
        }
        
        async Task ApplyChangesAsync(int delay = 20)
        {
            var g = _currentState.GetLastChanges();
            while (g != null)
            {
                await Task.Delay(delay);
                foreach (var p in g)
                    this[p.X, p.Y] = _currentState[p.X, p.Y];
                g = _currentState.GetLastChanges();
            }
        }

        public bool StartGame(IStrategy strat)
        {
            while (!IsFinished)
            {
                Brush? nullable = strat.Play(_currentState);
                if (nullable == null)
                {
                    return false;
                }
                Brush played = nullable;
                if (!Settings.PreventSameBrush || played != UpperLeft)
                {
                    OnBrushPlayed?.Invoke(this, played);
                    _currentState = _currentState.PlayBrush(played);
                    ApplyChanges();
                }
            }
            return true;
        }

        public async Task<bool> StartGameAsync(IAsyncStrategy strat, bool colorAsync = true, CancellationToken cancellationToken = default)
        {
            while (!IsFinished)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;
                Brush? nullable = await strat.PlayAsync(_currentState, cancellationToken);
                if (nullable == null)
                {
                    return false;
                }
                Brush played = nullable;
                if (!Settings.PreventSameBrush || played != UpperLeft)
                {
                    OnBrushPlayed?.Invoke(this, played);
                    _currentState = _currentState.PlayBrush(played);
                    //await PlayBrushAsync(played);
                    if (colorAsync)
                        await ApplyChangesAsync();
                    else
                        ApplyChanges();
                }
            }
            return true;
        }
    }

    public record GameSettings
    {
        public int? Seed { get; init; } = null;
        public int Size { get; init; } = 9;
        public int Count => Size * Size;
        public Brush[] UsedBrushes { get; init; } = new Brush[] { Brushes.Red, Brushes.Yellow, Brushes.Green, Brushes.Orange, Brushes.Magenta, Brushes.Blue, Brushes.Purple, Brushes.DeepSkyBlue };
        public bool PreventSameBrush { get; init; } = true;
    }
}
