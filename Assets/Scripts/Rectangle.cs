using UnityEngine;

namespace Mandarin {

    public struct Rectangle {
        public int l;
        public int r;
        public int t;
        public int b;

        public Rectangle(int top, int right, int bottom, int left) {
            l = left;
            r = right;
            t = top;
            b = bottom;
        }

        public static Rectangle GetOverlap(Rectangle a, Rectangle b, Point2 apos) {
            return new Rectangle(
                Mathf.Min(apos.y + a.t, b.t) - apos.y,
                Mathf.Min(apos.x + a.r, b.r) - apos.x,
                Mathf.Max(apos.y + a.b, b.b) - apos.y,
                Mathf.Max(apos.x + a.l, b.l) - apos.x
            );
        }
    }
}
