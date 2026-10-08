using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// Tiny anti-aliased vector rasteriser for procedural sprites. Coordinates are normalised (0..1, y up) so a
    /// drawing looks the same at any resolution. Shapes are evaluated as signed distances for smooth edges.
    /// "Clip" draws only on top of pixels that are already painted (for shading and markings).
    /// </summary>
    public class PixelCanvas
    {
        public readonly int W, H;
        public readonly Color[] Px;

        public PixelCanvas(int w, int h)
        {
            W = w; H = h;
            Px = new Color[w * h];
        }

        delegate float Sdf(float x, float y);

        // ------------------------------------------------------------------ core

        void Fill(float minX, float minY, float maxX, float maxY, Sdf sdf, Color c, bool clip)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(minX * W) - 2), x1 = Mathf.Min(W - 1, Mathf.CeilToInt(maxX * W) + 2);
            int y0 = Mathf.Max(0, Mathf.FloorToInt(minY * H) - 2), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(maxY * H) + 2);
            float px = 1f / W;
            for (int y = y0; y <= y1; y++)
            {
                float ny = (y + 0.5f) / H;
                for (int x = x0; x <= x1; x++)
                {
                    float nx = (x + 0.5f) / W;
                    float d = sdf(nx, ny) / px; // distance in pixels
                    float a = Mathf.Clamp01(0.5f - d) * c.a;
                    if (a <= 0f) continue;
                    int i = y * W + x;
                    if (clip) a *= Px[i].a;
                    Blend(i, c, a);
                }
            }
        }

        void Blend(int i, Color c, float a)
        {
            var d = Px[i];
            float outA = a + d.a * (1f - a);
            if (outA <= 1e-5f) return;
            float k = d.a * (1f - a);
            Px[i] = new Color((c.r * a + d.r * k) / outA, (c.g * a + d.g * k) / outA, (c.b * a + d.b * k) / outA, outA);
        }

        static Vector2 Rot(float x, float y, float cx, float cy, float deg)
        {
            if (deg == 0f) return new Vector2(x - cx, y - cy);
            float r = -deg * Mathf.Deg2Rad, s = Mathf.Sin(r), c = Mathf.Cos(r);
            float dx = x - cx, dy = y - cy;
            return new Vector2(dx * c - dy * s, dx * s + dy * c);
        }

        // ------------------------------------------------------------------ shapes

        public PixelCanvas Circle(float cx, float cy, float r, Color c, bool clip = false)
        {
            Fill(cx - r, cy - r, cx + r, cy + r, (x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r, c, clip);
            return this;
        }

        public PixelCanvas Ellipse(float cx, float cy, float rx, float ry, Color c, float rot = 0f, bool clip = false)
        {
            float m = Mathf.Max(rx, ry);
            Fill(cx - m, cy - m, cx + m, cy + m, (x, y) =>
            {
                var p = Rot(x, y, cx, cy, rot);
                // Approximate ellipse SDF (good enough for AA edges).
                float k0 = Mathf.Sqrt(p.x * p.x / (rx * rx) + p.y * p.y / (ry * ry));
                float k1 = Mathf.Sqrt(p.x * p.x / (rx * rx * rx * rx) + p.y * p.y / (ry * ry * ry * ry));
                return k1 < 1e-6f ? -Mathf.Min(rx, ry) : k0 * (k0 - 1f) / k1;
            }, c, clip);
            return this;
        }

        public PixelCanvas RoundRect(float cx, float cy, float w, float h, float radius, Color c, float rot = 0f, bool clip = false)
        {
            float m = Mathf.Sqrt(w * w + h * h) * 0.5f + radius;
            float hx = w * 0.5f - radius, hy = h * 0.5f - radius;
            Fill(cx - m, cy - m, cx + m, cy + m, (x, y) =>
            {
                var p = Rot(x, y, cx, cy, rot);
                float qx = Mathf.Abs(p.x) - hx, qy = Mathf.Abs(p.y) - hy;
                float outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
                return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
            }, c, clip);
            return this;
        }

        public PixelCanvas Rect(float x0, float y0, float x1, float y1, Color c, bool clip = false) =>
            RoundRect((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 0f, c, 0f, clip);

        /// <summary>Thick line with round caps.</summary>
        public PixelCanvas Line(float x0, float y0, float x1, float y1, float thickness, Color c, bool clip = false)
        {
            float r = thickness * 0.5f;
            var a = new Vector2(x0, y0); var b = new Vector2(x1, y1);
            Fill(Mathf.Min(x0, x1) - r, Mathf.Min(y0, y1) - r, Mathf.Max(x0, x1) + r, Mathf.Max(y0, y1) + r, (x, y) =>
            {
                var p = new Vector2(x, y);
                var ba = b - a;
                float h = Mathf.Clamp01(Vector2.Dot(p - a, ba) / Mathf.Max(ba.sqrMagnitude, 1e-8f));
                return (p - a - ba * h).magnitude - r;
            }, c, clip);
            return this;
        }

        public PixelCanvas Ring(float cx, float cy, float r, float thickness, Color c, bool clip = false)
        {
            float m = r + thickness;
            Fill(cx - m, cy - m, cx + m, cy + m, (x, y) =>
                Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - thickness * 0.5f, c, clip);
            return this;
        }

        /// <summary>Arc (part of a ring) between two angles in degrees (0 = right, CCW).</summary>
        public PixelCanvas Arc(float cx, float cy, float r, float thickness, float fromDeg, float toDeg, Color c, bool clip = false)
        {
            float m = r + thickness;
            float mid = (fromDeg + toDeg) * 0.5f * Mathf.Deg2Rad, half = Mathf.Abs(toDeg - fromDeg) * 0.5f * Mathf.Deg2Rad;
            Fill(cx - m, cy - m, cx + m, cy + m, (x, y) =>
            {
                float dx = x - cx, dy = y - cy;
                float ang = Mathf.Atan2(dy, dx);
                float diff = Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, mid * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                float ringD = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - r) - thickness * 0.5f;
                if (diff <= half) return ringD;
                // Round caps at both ends.
                float a1 = mid + half, a0 = mid - half;
                var e1 = new Vector2(cx + Mathf.Cos(a1) * r, cy + Mathf.Sin(a1) * r);
                var e0 = new Vector2(cx + Mathf.Cos(a0) * r, cy + Mathf.Sin(a0) * r);
                var p = new Vector2(x, y);
                return Mathf.Min((p - e1).magnitude, (p - e0).magnitude) - thickness * 0.5f;
            }, c, clip);
            return this;
        }

        public PixelCanvas Polygon(Vector2[] pts, Color c, bool clip = false)
        {
            float minX = 1, minY = 1, maxX = 0, maxY = 0;
            foreach (var p in pts) { minX = Mathf.Min(minX, p.x); minY = Mathf.Min(minY, p.y); maxX = Mathf.Max(maxX, p.x); maxY = Mathf.Max(maxY, p.y); }
            Fill(minX, minY, maxX, maxY, (x, y) => PolySdf(pts, new Vector2(x, y)), c, clip);
            return this;
        }

        static float PolySdf(Vector2[] v, Vector2 p)
        {
            float d = (p - v[0]).sqrMagnitude;
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                var e = v[j] - v[i];
                var w = p - v[i];
                var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Mathf.Max(e.sqrMagnitude, 1e-9f));
                d = Mathf.Min(d, b.sqrMagnitude);
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        public PixelCanvas Star(float cx, float cy, float rOuter, float rInner, int points, Color c, float rotDeg = 90f, bool clip = false)
        {
            var pts = new Vector2[points * 2];
            for (int i = 0; i < pts.Length; i++)
            {
                float a = (rotDeg + i * 180f / points) * Mathf.Deg2Rad;
                float r = (i % 2 == 0) ? rOuter : rInner;
                pts[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }
            return Polygon(pts, c, clip);
        }

        public PixelCanvas Triangle(Vector2 a, Vector2 b, Vector2 c2, Color c, bool clip = false) => Polygon(new[] { a, b, c2 }, c, clip);

        /// <summary>Soft radial glow (alpha falls off to the edge).</summary>
        public PixelCanvas Glow(float cx, float cy, float r, Color c, float power = 2f)
        {
            int x0 = Mathf.Max(0, (int)((cx - r) * W)), x1 = Mathf.Min(W - 1, (int)((cx + r) * W));
            int y0 = Mathf.Max(0, (int)((cy - r) * H)), y1 = Mathf.Min(H - 1, (int)((cy + r) * H));
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float nx = (x + 0.5f) / W - cx, ny = (y + 0.5f) / H - cy;
                float t = 1f - Mathf.Sqrt(nx * nx + ny * ny) / r;
                if (t <= 0) continue;
                Blend(y * W + x, c, Mathf.Pow(t, power) * c.a);
            }
            return this;
        }

        // ------------------------------------------------------------------ common decorations

        /// <summary>Cute eye: white, pupil and a glint. look = pupil offset direction.</summary>
        public PixelCanvas Eye(float cx, float cy, float r, Vector2 look = default, bool angry = false)
        {
            Ellipse(cx, cy, r * 0.82f, r, Color.white);
            Circle(cx + look.x * r * 0.25f, cy + look.y * r * 0.25f - r * 0.1f, r * 0.55f, new Color(0.08f, 0.06f, 0.1f));
            Circle(cx + look.x * r * 0.25f - r * 0.2f, cy + look.y * r * 0.25f + r * 0.12f, r * 0.2f, Color.white);
            if (angry) Line(cx - r * 1.0f, cy + r * 1.25f, cx + r * 0.9f, cy + r * 0.75f, r * 0.35f, new Color(0.08f, 0.06f, 0.1f));
            return this;
        }

        /// <summary>Top-left highlight and bottom shade, clipped to what is already drawn.</summary>
        public PixelCanvas Shade(float cx, float cy, float r, float strength = 1f)
        {
            Ellipse(cx + r * 0.15f, cy - r * 0.55f, r * 1.1f, r * 0.6f, new Color(0, 0, 0, 0.16f * strength), 0, true);
            Ellipse(cx - r * 0.35f, cy + r * 0.45f, r * 0.32f, r * 0.2f, new Color(1, 1, 1, 0.45f * strength), -30f, true);
            return this;
        }

        // ------------------------------------------------------------------ post

        /// <summary>Chunky outline: paints the colour under every pixel within thickness of the drawing.</summary>
        public PixelCanvas Outline(float thicknessPx, Color c)
        {
            int t = Mathf.CeilToInt(thicknessPx);
            var src = (Color[])Px.Clone();
            var offsets = new System.Collections.Generic.List<(int, int, float)>();
            for (int dy = -t; dy <= t; dy++)
            for (int dx = -t; dx <= t; dx++)
            {
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d <= thicknessPx + 0.5f) offsets.Add((dx, dy, Mathf.Clamp01(thicknessPx + 0.5f - d)));
            }
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x;
                if (src[i].a >= 0.999f) continue;
                float m = 0f;
                foreach (var (dx, dy, w) in offsets)
                {
                    int sx = x + dx, sy = y + dy;
                    if (sx < 0 || sy < 0 || sx >= W || sy >= H) continue;
                    float a = src[sy * W + sx].a * w;
                    if (a > m) { m = a; if (m >= 0.999f) break; }
                }
                if (m <= 0f) continue;
                // Composite the original pixel over the outline colour.
                var s = src[i];
                float outA = s.a + m * c.a * (1f - s.a);
                if (outA <= 1e-5f) continue;
                float k = m * c.a * (1f - s.a);
                Px[i] = new Color((s.r * s.a + c.r * k) / outA, (s.g * s.a + c.g * k) / outA, (s.b * s.a + c.b * k) / outA, outA);
            }
            return this;
        }

        /// <summary>Flat white silhouette (hit flash variant).</summary>
        public PixelCanvas Silhouette(Color c)
        {
            for (int i = 0; i < Px.Length; i++)
            {
                var p = Px[i];
                if (p.a <= 0f) continue;
                Px[i] = new Color(Mathf.Lerp(p.r, c.r, 0.85f), Mathf.Lerp(p.g, c.g, 0.85f), Mathf.Lerp(p.b, c.b, 0.85f), p.a);
            }
            return this;
        }

        public PixelCanvas Clone()
        {
            var c = new PixelCanvas(W, H);
            System.Array.Copy(Px, c.Px, Px.Length);
            return c;
        }

        /// <summary>Darker version of a colour (for outlines and shading).</summary>
        public static Color Dark(Color c, float k = 0.45f) => new(c.r * k, c.g * k, c.b * k, c.a);
        public static Color Light(Color c, float k = 0.4f) => Color.Lerp(c, Color.white, k);
    }
}
