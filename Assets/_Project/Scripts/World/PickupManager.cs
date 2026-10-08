using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    public enum PickupKind { Gem, Coin, Heal, Magnet, Bomb, Chest }

    /// <summary>
    /// XP gems, coins and special pickups. Gems inside the pickup radius fly to the hero; beyond the gem cap new
    /// gems merge into existing ones (which grow a tier) so huge swarms never flood the scene.
    /// </summary>
    public class PickupManager
    {
        public class Pickup
        {
            public bool alive, attracted;
            public PickupKind kind;
            public int value;
            public Vector2 pos, vel;
            public float age;
            public SpriteRenderer sr;
            public Transform tf;
        }

        readonly RunController run;
        readonly Transform root;
        readonly List<Pickup> active = new();
        readonly Stack<Pickup> pool = new();
        int gemCount;
        float magnetTimer, healthTimer;

        public IReadOnlyList<Pickup> Active => active;
        public int GemCount => gemCount;

        public PickupManager(RunController run, Transform parent)
        {
            this.run = run;
            root = new GameObject("Pickups").transform;
            root.SetParent(parent, false);
            var cfg = run.Config;
            magnetTimer = Random.Range(cfg.magnetInterval.x, cfg.magnetInterval.y);
            healthTimer = Random.Range(cfg.healthInterval.x, cfg.healthInterval.y);
        }

        static ArtId GemArt(int v) => v < 3 ? ArtId.GemSmall : v < 10 ? ArtId.GemMedium : v < 40 ? ArtId.GemBig : ArtId.GemHuge;

        static ArtId ArtFor(PickupKind k, int v) => k switch
        {
            PickupKind.Gem => GemArt(v),
            PickupKind.Coin => ArtId.Coin,
            PickupKind.Heal => ArtId.HealthPickup,
            PickupKind.Magnet => ArtId.MagnetPickup,
            PickupKind.Bomb => ArtId.BombPickup,
            _ => ArtId.Chest,
        };

        static float SizeFor(PickupKind k, int v) => k switch
        {
            PickupKind.Gem => v < 3 ? 0.42f : v < 10 ? 0.5f : v < 40 ? 0.6f : 0.75f,
            PickupKind.Coin => 0.42f,
            PickupKind.Chest => 1.1f,
            _ => 0.75f,
        };

        public void SpawnGem(Vector2 pos, int value)
        {
            if (value <= 0) return;
            if (gemCount >= run.Config.maxGems)
            {
                // Merge into the nearest existing gem.
                Pickup best = null;
                float bd = float.MaxValue;
                foreach (var p in active)
                {
                    if (!p.alive || p.kind != PickupKind.Gem || p.attracted) continue;
                    float d = (p.pos - pos).sqrMagnitude;
                    if (d < bd) { bd = d; best = p; }
                }
                if (best != null)
                {
                    best.value += value;
                    best.sr.sprite = Art.Get(GemArt(best.value));
                    best.tf.localScale = Vector3.one * SizeFor(PickupKind.Gem, best.value);
                    return;
                }
            }
            Spawn(PickupKind.Gem, pos, value);
        }

        public Pickup Spawn(PickupKind kind, Vector2 pos, int value)
        {
            var p = pool.Count > 0 ? pool.Pop() : null;
            if (p == null)
            {
                var go = new GameObject("Pickup");
                go.transform.SetParent(root, false);
                p = new Pickup { tf = go.transform, sr = go.AddComponent<SpriteRenderer>() };
                p.sr.sharedMaterial = Art.SpriteMaterial;
            }
            p.alive = true;
            p.attracted = false;
            p.kind = kind;
            p.value = value;
            p.pos = pos;
            p.vel = kind == PickupKind.Gem || kind == PickupKind.Coin ? Random.insideUnitCircle * 1.5f : Vector2.zero;
            p.age = 0;
            p.sr.gameObject.SetActive(true);
            p.sr.sprite = Art.Get(ArtFor(kind, value));
            p.sr.sortingOrder = kind == PickupKind.Chest ? -3 : -6;
            p.tf.localScale = Vector3.one * SizeFor(kind, value);
            p.tf.position = pos;
            if (kind == PickupKind.Gem) gemCount++;
            active.Add(p);
            return p;
        }

        /// <summary>Pull every gem and coin on the map to the hero.</summary>
        public void MagnetAll()
        {
            foreach (var p in active)
                if (p.alive && (p.kind == PickupKind.Gem || p.kind == PickupKind.Coin)) p.attracted = true;
        }

        public void Tick(float dt)
        {
            var hero = run.Hero;
            float radius = run.Stats.PickupRadius;
            float r2 = radius * radius;

            // Random world pickups near the hero.
            magnetTimer -= dt;
            healthTimer -= dt;
            if (magnetTimer <= 0) { magnetTimer = Random.Range(run.Config.magnetInterval.x, run.Config.magnetInterval.y); SpawnNearHero(PickupKind.Magnet); }
            if (healthTimer <= 0) { healthTimer = Random.Range(run.Config.healthInterval.x, run.Config.healthInterval.y); SpawnNearHero(PickupKind.Heal); }

            for (int i = 0; i < active.Count; i++)
            {
                var p = active[i];
                if (!p.alive) continue;
                p.age += dt;
                Vector2 to = hero.Position - p.pos;
                float d2 = to.sqrMagnitude;
                if (!p.attracted && p.kind != PickupKind.Chest && d2 < r2) p.attracted = true;
                if (p.attracted)
                {
                    float d = Mathf.Sqrt(d2);
                    float speed = 6f + p.age * 2f + (p.kind == PickupKind.Gem ? 0 : 2f);
                    Vector2 want = d > 1e-4f ? to / d * Mathf.Max(speed, hero.Speed * 1.6f) : Vector2.zero;
                    p.vel = Vector2.Lerp(p.vel, want, 1f - Mathf.Exp(-8f * dt));
                }
                else p.vel *= Mathf.Exp(-6f * dt);
                p.pos += p.vel * dt;

                float collectR = p.kind == PickupKind.Chest ? 0.9f : 0.45f;
                if (d2 < collectR * collectR) { Collect(p); continue; }

                float bob = p.kind == PickupKind.Gem || p.kind == PickupKind.Coin ? 0f : Mathf.Sin(p.age * 4f) * 0.08f;
                p.tf.position = new Vector3(p.pos.x, p.pos.y + bob, 0);
                if (p.kind == PickupKind.Coin) p.tf.localScale = new Vector3(SizeFor(p.kind, 1) * Mathf.Abs(Mathf.Cos(p.age * 3f)), SizeFor(p.kind, 1), 1);
            }

            int w = 0;
            for (int i = 0; i < active.Count; i++)
            {
                var p = active[i];
                if (!p.alive) { p.sr.gameObject.SetActive(false); pool.Push(p); continue; }
                active[w++] = p;
            }
            active.RemoveRange(w, active.Count - w);
        }

        void SpawnNearHero(PickupKind kind)
        {
            Vector2 p = run.Hero.Position + Random.insideUnitCircle.normalized * Random.Range(4.5f, 7.5f);
            run.Map.ResolveCircle(ref p, 0.5f);
            Spawn(kind, p, 1);
        }

        void Collect(Pickup p)
        {
            p.alive = false;
            if (p.kind == PickupKind.Gem) gemCount--;
            switch (p.kind)
            {
                case PickupKind.Gem:
                    run.AddXp(p.value);
                    Sfx.Play(SfxId.Gem, 0.18f, 1f + Mathf.Min(0.5f, p.value * 0.01f), 0.02f);
                    break;
                case PickupKind.Coin:
                    run.AddGold(p.value);
                    Sfx.Play(SfxId.Coin, 0.2f, 1f, 0.05f);
                    break;
                case PickupKind.Heal:
                    run.Hero.Heal(run.Config.healPickupAmount);
                    Sfx.Play(SfxId.Heal, 0.6f);
                    break;
                case PickupKind.Magnet:
                    MagnetAll();
                    Sfx.Play(SfxId.Magnet, 0.6f);
                    run.Fx.Spawn(ArtId.Ring, run.Hero.Position, 2f, new Color(1f, 0.4f, 0.4f, 0.8f), 0.5f, growTo: 8f);
                    break;
                case PickupKind.Bomb:
                    run.Bomb();
                    break;
                case PickupKind.Chest:
                    run.OpenChest(p.value);
                    Sfx.Play(SfxId.Chest, 0.8f);
                    run.Fx.Burst(p.pos, new Color(1f, 0.85f, 0.3f), 18, 6f, 0.35f);
                    break;
            }
        }

        public void Clear()
        {
            foreach (var p in active) { p.sr.gameObject.SetActive(false); pool.Push(p); }
            active.Clear();
            gemCount = 0;
        }
    }
}
