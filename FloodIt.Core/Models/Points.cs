using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodIt.Core.Models
{
    record Point(int X, int Y);

    class DPoint : IEquatable<DPoint>
    {
        public int X { get; }
        public int Y { get; }
        public int D { get; }

        public DPoint(int x, int y) : this(x, y, 0) { }
        public DPoint(int x, int y, int d)
        {
            X = x;
            Y = y;
            D = d;
        }

        public static explicit operator DPoint(Point p) => new DPoint(p.X, p.Y, 0);
        public static explicit operator Point(DPoint dp) => new Point(dp.X, dp.Y);

        public override bool Equals(object obj) => obj is DPoint point && Equals(point);
        public bool Equals(DPoint other) => X == other.X && Y == other.Y;
        public static bool operator ==(DPoint left, DPoint right) => left.Equals(right);
        public static bool operator !=(DPoint left, DPoint right) => !(left == right);
        public override int GetHashCode() => throw new NotImplementedException();
    }
}
