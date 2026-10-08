using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>Pooled floating damage numbers (world-space TextMeshPro). Capped and toggleable in settings.</summary>
    public class DamageNumbers
    {
        class Num { public TextMeshPro text; public float life; public Vector2 pos, vel; public float scale; }

        const int Cap = 70;
        readonly Transform root;
        readonly List<Num> active = new();
        readonly Stack<Num> pool = new();
        float lastQuiet;

        public bool Enabled { get; set; } = true;

        public DamageNumbers(Transform parent)
        {
            root = new GameObject("DamageNumbers").transform;
            root.SetParent(parent, false);
        }

        public void Show(Vector2 pos, float amount, bool crit, bool quiet = false)
        {
            if (!Enabled) return;
            if (quiet)
            {
                // Damage-over-time ticks: show some, not all.
                if (Time.time - lastQuiet < 0.05f) return;
                lastQuiet = Time.time;
            }
            if (active.Count >= Cap)
            {
                if (!crit) return;
                Recycle(0);
            }
            var n = pool.Count > 0 ? pool.Pop() : Create();
            n.text.gameObject.SetActive(true);
            n.text.text = Mathf.Max(1, Mathf.RoundToInt(amount)).ToString();
            n.text.color = crit ? new Color(1f, 0.85f, 0.2f) : quiet ? new Color(0.7f, 1f, 0.5f) : Color.white;
            n.scale = crit ? 1.35f : quiet ? 0.75f : 1f;
            n.life = 0.6f;
            n.pos = pos + new Vector2(Random.Range(-0.25f, 0.25f), 0.1f);
            n.vel = new Vector2(Random.Range(-0.6f, 0.6f), 2.6f);
            active.Add(n);
        }

        Num Create()
        {
            var go = new GameObject("Dmg");
            go.transform.SetParent(root, false);
            var t = go.AddComponent<TextMeshPro>();
            t.fontSize = 4.2f;
            t.alignment = TextAlignmentOptions.Center;
            t.fontStyle = FontStyles.Bold;
            t.outlineWidth = 0.28f;
            t.outlineColor = new Color32(30, 20, 45, 255);
            t.sortingOrder = 60;
            t.rectTransform.sizeDelta = new Vector2(3, 1);
            return new Num { text = t };
        }

        void Recycle(int i)
        {
            var n = active[i];
            n.text.gameObject.SetActive(false);
            pool.Push(n);
            active.RemoveAt(i);
        }

        public void Tick(float dt)
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var n = active[i];
                n.life -= dt;
                if (n.life <= 0) { Recycle(i); continue; }
                n.pos += n.vel * dt;
                n.vel.y -= 7f * dt;
                n.text.transform.position = new Vector3(n.pos.x, n.pos.y, 0);
                float pop = n.life > 0.5f ? Mathf.Lerp(1.4f, 1f, (0.6f - n.life) / 0.1f) : 1f;
                n.text.transform.localScale = Vector3.one * n.scale * pop;
                var c = n.text.color; c.a = Mathf.Clamp01(n.life / 0.25f); n.text.color = c;
            }
        }

        public void Clear()
        {
            while (active.Count > 0) Recycle(active.Count - 1);
        }
    }
}
