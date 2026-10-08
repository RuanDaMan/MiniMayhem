using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>Short-lived sprite effects (pops, rings, swooshes, particles, lightning segments), pooled.</summary>
    public class FxService
    {
        class Fx
        {
            public SpriteRenderer sr;
            public Transform tf;
            public float life, maxLife, size, growTo, spin, rot;
            public Vector2 pos, vel;
            public Color color;
            public bool fade, follow;
            public float length, width; // for stretched segments
        }

        const int Cap = 700;
        readonly RunController run;
        readonly Transform root;
        readonly List<Fx> active = new();
        readonly Stack<Fx> pool = new();

        public int Count => active.Count;

        public FxService(RunController run, Transform parent)
        {
            this.run = run;
            root = new GameObject("Fx").transform;
            root.SetParent(parent, false);
        }

        Fx Rent()
        {
            if (active.Count >= Cap) return null;
            var f = pool.Count > 0 ? pool.Pop() : null;
            if (f == null)
            {
                var go = new GameObject("Fx");
                go.transform.SetParent(root, false);
                f = new Fx { tf = go.transform, sr = go.AddComponent<SpriteRenderer>() };
                f.sr.sharedMaterial = Art.SpriteMaterial;
            }
            f.sr.gameObject.SetActive(true);
            f.length = 0;
            active.Add(f);
            return f;
        }

        public void Spawn(ArtId art, Vector2 pos, float size, Color color, float life, float rot = 0f, Vector2 vel = default,
            float growTo = 1f, bool fade = true, int order = 30, float spin = 0f)
        {
            var f = Rent();
            if (f == null) return;
            f.sr.sprite = Art.Get(art);
            f.sr.sortingOrder = order;
            f.pos = pos; f.vel = vel; f.size = size; f.growTo = growTo; f.life = f.maxLife = Mathf.Max(0.01f, life);
            f.color = color; f.fade = fade; f.rot = rot; f.spin = spin;
            Apply(f, 0f);
        }

        /// <summary>Little particles flying out of a point.</summary>
        public void Burst(Vector2 pos, Color color, int count, float speed, float size)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 v = Random.insideUnitCircle.normalized * speed * Random.Range(0.4f, 1f);
                Spawn(i % 3 == 0 ? ArtId.Spark : ArtId.Circle, pos, size * Random.Range(0.6f, 1.1f), color, Random.Range(0.25f, 0.45f), Random.Range(0, 360f), v, 0.2f, true, 31);
            }
        }

        /// <summary>Stretched beam segment(s) between two points. Jagged = lightning.</summary>
        public void Line(Vector2 a, Vector2 b, Color color, float width, float life, bool jagged, int order = 25)
        {
            if (!jagged) { Segment(a, b, color, width, life, order); return; }
            int segs = Mathf.Clamp(Mathf.CeilToInt((b - a).magnitude / 0.9f), 2, 10);
            Vector2 prev = a;
            Vector2 perp = Vector2.Perpendicular((b - a).normalized);
            for (int i = 1; i <= segs; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)segs);
                if (i < segs) p += perp * Random.Range(-0.35f, 0.35f);
                Segment(prev, p, color, width, life, order);
                Segment(prev, p, new Color(1, 1, 1, color.a), width * 0.4f, life, order + 1);
                prev = p;
            }
        }

        void Segment(Vector2 a, Vector2 b, Color color, float width, float life, int order)
        {
            var f = Rent();
            if (f == null) return;
            f.sr.sprite = Art.Get(ArtId.Beam);
            f.sr.sortingOrder = order;
            f.pos = (a + b) * 0.5f;
            f.vel = Vector2.zero;
            f.length = (b - a).magnitude;
            f.width = width;
            f.rot = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            f.spin = 0; f.growTo = 1; f.fade = true;
            f.life = f.maxLife = life;
            f.color = color;
            Apply(f, 0f);
        }

        void Apply(Fx f, float t)
        {
            f.tf.position = new Vector3(f.pos.x, f.pos.y, 0);
            f.tf.rotation = Quaternion.Euler(0, 0, f.rot);
            if (f.length > 0) f.tf.localScale = new Vector3(f.length, f.width * Mathf.Lerp(1f, 0.4f, t), 1);
            else f.tf.localScale = Vector3.one * f.size * Mathf.Lerp(1f, f.growTo, t);
            var c = f.color;
            if (f.fade) c.a *= 1f - t * t;
            f.sr.color = c;
        }

        public void Tick(float dt)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var f = active[i];
                f.life -= dt;
                if (f.life <= 0)
                {
                    f.sr.gameObject.SetActive(false);
                    pool.Push(f);
                    active[i] = active[active.Count - 1];
                    active.RemoveAt(active.Count - 1);
                    continue;
                }
                f.pos += f.vel * dt;
                f.vel *= Mathf.Exp(-3f * dt);
                f.rot += f.spin * dt;
                Apply(f, 1f - f.life / f.maxLife);
            }
        }

        public void Clear()
        {
            foreach (var f in active) { f.sr.gameObject.SetActive(false); pool.Push(f); }
            active.Clear();
        }
    }
}
