using System;

namespace AutoPolarAlign
{
    public struct Vec2
    {
        public double X;
        public double Y;

        public double Azimuth
        {
            get => X;
            set => X = value;
        }

        public double Altitude
        {
            get => Y;
            set => Y = value;
        }

        public double Length => (double)Math.Sqrt(X * X + Y * Y);

        public Vec2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double Dot(Vec2 other)
        {
            return X * other.X + Y * other.Y;
        }

        public Vec2 Normalized()
        {
            return this / Length;
        }

        public static Vec2 operator +(Vec2 p) => p;

        public static Vec2 operator -(Vec2 p) => new Vec2(-p.X, -p.Y);

        public static Vec2 operator +(Vec2 p1, Vec2 p2) => new Vec2(p1.X + p2.X, p1.Y + p2.Y);

        public static Vec2 operator -(Vec2 p1, Vec2 p2) => new Vec2(p1.X - p2.X, p1.Y - p2.Y);

        public static Vec2 operator *(Vec2 p, double scalar) => new Vec2(p.X * scalar, p.Y * scalar);

        public static Vec2 operator /(Vec2 p, double scalar) => new Vec2(p.X / scalar, p.Y / scalar);

        public override string ToString()
        {
            return "(" + X + ", " + Y + ")";
        }
    }
}
