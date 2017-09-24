using System;
using UnityEngine;

[Serializable]
public struct Point2 {
    public readonly int x;
    public readonly int y;

    public Point2(int p_x, int p_y) {
        x = p_x;
        y = p_y;
    }

    public Point2(float p_x, float p_y) {
        x = (int)p_x;
        y = (int)p_y;
    }

    public Point2(Vector2 vec) {
        x = (int)vec.x;
        y = (int)vec.y;
    }

    public Point2(Point2 p) {
        x = p.x;
        y = p.y;
    }

    public static implicit operator Vector2(Point2 p) {
        return new Vector2(p.x, p.y);
    }

    public static Point2 operator +(Point2 a, Point2 b) {
        return new Point2(a.x + b.x, a.y + b.y);
    }

    public static Point2 operator -(Point2 a, Point2 b) {
        return new Point2(a.x - b.x, a.y - b.y);
    }
    public static Point2 operator -(Point2 a) {
        return new Point2(-a.x, -a.y);
    }

    public static Point2 operator *(Point2 p, int c) {
        return new Point2(p.x * c, p.y * c);
    }

    public static Point2 operator *(Point2 p, float c) {
        return new Point2(p.x * c, p.y * c);
    }

    public static Point2 operator /(Point2 p, int c) {
        return new Point2(p.x / c, p.y / c);
    }

    public static bool operator ==(Point2 a, Point2 b) {
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(Point2 a, Point2 b) {
        return !(a == b);
    }

    public override bool Equals(object p) {
        if (!(p is Point2)) {
            return false;
        }

        Point2 po = (Point2) p;
        return po.x == x && po.y == y;
    }

    public override int GetHashCode() {
        int hash = 486187739;
        hash = hash * 16777619 + x;
        hash = hash * 16777619 + y;
        return hash;
    }

    public override string ToString() {
        return "Point2 {x:"+x+", y:"+y+"}";
    }

    public static Point2 zero {
        get { return new Point2(); }
    }

    public static Point2 one {
        get { return new Point2(1, 1); }
    }

    public static Point2 up {
        get { return new Point2(0, 1); }
    }

    public static Point2 right {
        get { return new Point2(1, 0); }
    }

    public static Point2 down {
        get { return new Point2(0, -1); }
    }

    public static Point2 left {
        get { return new Point2(-1, 0); }
    }
}

public static class Point2Extensions {

    public static Vector3 ToVector3XY(this Point2 p) {
        return new Vector3(p.x, p.y, 0f);
    }

    public static Vector3 ToVector3XZ(this Point2 p) {
        return new Vector3(p.x, 0f, p.y);
    }
}
