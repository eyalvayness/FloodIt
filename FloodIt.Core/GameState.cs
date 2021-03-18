using FloodIt.Core.Interfaces;
using FloodIt.Core.Utils;
using System.Collections;
using System.Collections.Generic;
using FloodIt.Core.Models;
using System.Windows.Media;
using System.Linq;
using System;

namespace FloodIt.Core
{
    public class GameState : IEquatable<GameState>
    {
        readonly Queue<List<Point>> _changes;
        List<Point>? _tempChanges;
        readonly Brush[,] _board;
        Brush[] _playableBrushes;
        readonly GameSettings _settings;
        readonly List<List<Point>> _blobs;
        readonly List<DPoint> _markedULZ;
        readonly byte[] _simplifiedBoard;

        public int BlobCount => _blobs.Count;
        public bool IsFinished => BlobCount == 1;
        public int ULZCount => _markedULZ.Count;
        public Brush[] PlayableBrushes => _playableBrushes.ToArray();
        public int PlayableBrushCount => _playableBrushes.Length;
        public Brush this[int x, int y]
        {
            get
            {
                return _board[x, y];
            }
            private set
            {
                _ = _tempChanges ?? throw new Exception("Setting a Brush when no batch has begun.");
                _tempChanges.Add(new(x, y));
                _board[x, y] = value;
            }
        }

        public GameState(GameSettings settings)
        {
            _blobs = new();
            _changes = new();
            _markedULZ = new();
            _settings = settings;
            _playableBrushes = Array.Empty<Brush>();
            _simplifiedBoard = new byte[_settings.Count];
            _board = new Brush[_settings.Size, _settings.Size];

            Init();
            ComputeBlobs();
            CreateSimplifiedBoard();
            ComputePlayabelBrushes();
        }

        void ComputePlayabelBrushes()
        {
            var temp = _board.OfType<Brush>().Distinct().ToList();
            if (_settings.PreventSameBrush)
                temp.Remove(this[0, 0]);
            _playableBrushes = temp.ToArray();
        }

        void CreateSimplifiedBoard()
        {
            List<Brush> conversionTable = new();
            
            for (int index = 0; index < _settings.Count; index++)
            {
                int x = index % _settings.Size;
                int y = index / _settings.Size;
                if (!conversionTable.Contains(this[x, y]))
                    conversionTable.Add(this[x, y]);
                _simplifiedBoard[index] = (byte)conversionTable.IndexOf(this[x, y]);
            }
        }

        private GameState(GameState from)
        {
            _blobs = new();
            _changes = new();
            _markedULZ = new();
            _settings = from._settings;
            _playableBrushes = Array.Empty<Brush>();
            _simplifiedBoard = from._simplifiedBoard.ToArray();
            _board = new Brush[_settings.Size, _settings.Size];

            for (int x = 0; x < _settings.Size; x++)
                for (int y = 0; y < _settings.Size; y++)
                    _board[x, y] = from[x, y];

            ComputeBlobs();
            ComputePlayabelBrushes();
        }

        internal List<Point>? GetLastChanges() => _changes.Count == 0 ? null : _changes.Dequeue().ToList();
        void BeginBatch()
        {
            if (_tempChanges != null)
                throw new Exception("Batch was started before the previous one was ended.");
            _tempChanges = new();
        }
        void EndBatch()
        {
            _ = _tempChanges ?? throw new Exception("Batch was ended before it has begun.");
            _changes.Enqueue(_tempChanges.ToList());
            _tempChanges = null;
        }

        void Init()
        {
            //Random _rand = new(Seed: 1);
            BeginBatch();
            for (int x = 0; x < _settings.Size; x++)
                for (int y = 0; y < _settings.Size; y++)
                    this[x, y] = _settings.UsedBrushes.Random();
            //this[x, y] = _settings.UsedBrushes[_rand.Next(_settings.UsedBrushes.Length)];
            EndBatch();
        }

        public GameState PlayBrush(Brush b, bool usingDistance = true)
        {
            GameState newgs = new(this);
            if (usingDistance)
            {
                foreach (var g in newgs.GetUpperLeftZoneByDistance())
                {
                    newgs.BeginBatch();
                    foreach (var p in g)
                        newgs[p.X, p.Y] = b;
                    newgs.EndBatch();
                }
            }
            else
            {
                newgs.BeginBatch();
                foreach (var p in newgs.GetUpperLeftZone())
                    newgs[p.X, p.Y] = b;
                newgs.EndBatch();
            }

            newgs.ComputeBlobs();
            newgs.CreateSimplifiedBoard();
            newgs.ComputePlayabelBrushes();
            return newgs;
        }

        internal Point[] GetUpperLeftZone() => _markedULZ.Select(dp => new Point(dp.X, dp.Y)).ToArray();
        internal IGrouping<int, Point>[] GetUpperLeftZoneByDistance() => _markedULZ.GroupBy(dp => dp.D, dp => new Point(dp.X, dp.Y)).OrderBy(g => g.Key).ToArray();

        void ComputeBlobs()
        {
            _blobs.Clear();
            var range = Enumerable.Range(0, _settings.Size);
            List<Point> allPoints = range.Select(x => range.Select(y => new Point(x, y))).SelectMany(p => p).ToList();

            allPoints.Remove(new(0, 0));
            foreach (var p in ComputeUpperLeftZone())
                allPoints.Remove(p);
            
            while (allPoints.Count > 0)
            {
                var p = allPoints[0];
                var points = ComputeZoneFrom(p.X, p.Y);
                _blobs.Add(points);
                foreach (var point in points)
                    allPoints.Remove(point);
            }
        }

        private List<Point> ComputeUpperLeftZone()
        {
            _markedULZ.Clear();

            foreach (var dp in ComputeZoneWithDistanceFrom(0, 0))
                _markedULZ.Add(dp);

            var copy = _markedULZ.Select(dp => (Point)dp).ToList();
            _blobs.Add(copy);
            return copy;
        }

        List<Point> ComputeZoneFrom(int x, int y)
        {
            List<Point> mark = new();
            Queue<Point> queue = new();

            queue.Enqueue(new(x, y));
            mark.Add(new(x, y));
            Brush ul = this[x, y];
            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();

                if (p.X > 0 && this[p.X - 1, p.Y] == ul && !mark.Contains(new(p.X - 1, p.Y)))
                {
                    queue.Enqueue(new(p.X - 1, p.Y));
                    mark.Add(new(p.X - 1, p.Y));
                }
                if (p.Y > 0 && this[p.X, p.Y - 1] == ul && !mark.Contains(new(p.X, p.Y - 1)))
                {
                    queue.Enqueue(new(p.X, p.Y - 1));
                    mark.Add(new(p.X, p.Y - 1));
                }
                if (p.X < _settings.Size - 1 && this[p.X + 1, p.Y] == ul && !mark.Contains(new(p.X + 1, p.Y)))
                {
                    queue.Enqueue(new(p.X + 1, p.Y));
                    mark.Add(new(p.X + 1, p.Y));
                }
                if (p.Y < _settings.Size - 1 && this[p.X, p.Y + 1] == ul && !mark.Contains(new(p.X, p.Y + 1)))
                {
                    queue.Enqueue(new(p.X, p.Y + 1));
                    mark.Add(new(p.X, p.Y + 1));
                }
            }

            return mark;
        }

        List<DPoint> ComputeZoneWithDistanceFrom(int x, int y)
        {
            List<DPoint> mark = new();
            Queue<DPoint> queue = new();

            queue.Enqueue(new(x, y, 0));
            mark.Add(new DPoint(x, y, 0));
            Brush ul = this[x, y];
            while (queue.Count > 0)
            {
                DPoint p = queue.Dequeue();

                if (p.X > 0 && this[p.X - 1, p.Y] == ul && !mark.Contains(new(p.X - 1, p.Y)))
                {
                    queue.Enqueue(new(p.X - 1, p.Y, p.D + 1));
                    mark.Add(new(p.X - 1, p.Y, p.D + 1));
                }
                if (p.Y > 0 && this[p.X, p.Y - 1] == ul && !mark.Contains(new(p.X, p.Y - 1)))
                {
                    queue.Enqueue(new(p.X, p.Y - 1, p.D + 1));
                    mark.Add(new(p.X, p.Y - 1, p.D + 1));
                }
                if (p.X < _settings.Size - 1 && this[p.X + 1, p.Y] == ul && !mark.Contains(new(p.X + 1, p.Y)))
                {
                    queue.Enqueue(new(p.X + 1, p.Y, p.D + 1));
                    mark.Add(new(p.X + 1, p.Y, p.D + 1));
                }
                if (p.Y < _settings.Size - 1 && this[p.X, p.Y + 1] == ul && !mark.Contains(new(p.X, p.Y + 1)))
                {
                    queue.Enqueue(new(p.X, p.Y + 1, p.D + 1));
                    mark.Add(new(p.X, p.Y + 1, p.D + 1));
                }
            }

            return mark;
        }

        public bool Equals(GameState? state)
        {
            if (state is null)
                return false;
            if (_settings.Size != state._settings.Size)
                return false;
            for (int index = _settings.Size; index >= 0; index--)
                if (_simplifiedBoard[index] != state._simplifiedBoard[index])
                    return false;
            return true;
        }
        public override bool Equals(object? obj) => Equals(obj as GameState);
        public override int GetHashCode()
        {
            //var hc = HashCode.Combine(_simplifiedBoard);
            var hc = _simplifiedBoard.Sum(b => b);
            return hc;
        }

        public static bool operator !=(GameState s1, GameState s2) => !(s1 == s2);
        public static bool operator ==(GameState s1, GameState s2) => s1 is not null && s1.Equals(s2);
    }
}
