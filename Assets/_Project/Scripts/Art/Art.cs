using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// Runtime sprite cache. Every procedural drawing is packed into shared atlas pages so hundreds of enemies,
    /// projectiles and gems batch together. Built once per session (the textures are kept across scene loads).
    /// </summary>
    public static class Art
    {
        const int PageSize = 2048;
        const int Padding = 2;

        static readonly Dictionary<ArtId, Sprite> sprites = new();
        static readonly Dictionary<EnemyDefinition, Sprite> enemySprites = new();
        static readonly Dictionary<EnemyDefinition, Sprite> enemyFlash = new();
        static readonly Dictionary<BiomeDefinition, Sprite> grounds = new();
        static readonly List<Texture2D> pages = new();
        static Material spriteMaterial;
        static GameDatabase builtFor;

        public static bool Ready => builtFor != null;
        public static int PageCount => pages.Count;

        public static Material SpriteMaterial
        {
            get
            {
                if (spriteMaterial == null)
                {
                    var lib = ArtLibrary.Instance;
                    if (lib != null && lib.spriteMaterial != null) spriteMaterial = lib.spriteMaterial;
                    else
                    {
                        var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                        spriteMaterial = new Material(sh) { name = "SpriteRuntime" };
                    }
                }
                return spriteMaterial;
            }
        }

        public static Sprite Get(ArtId id)
        {
            if (sprites.TryGetValue(id, out var s)) return s;
            // Not built yet (e.g. edit-mode tests): build this one on its own page.
            BuildStandalone(id);
            return sprites[id];
        }

        public static Sprite Enemy(EnemyDefinition e)
        {
            if (e == null) return Get(ArtId.Circle);
            if (!enemySprites.TryGetValue(e, out var s)) { BuildEnemies(new[] { e }); s = enemySprites[e]; }
            return s;
        }

        public static Sprite EnemyFlash(EnemyDefinition e)
        {
            if (e == null) return Get(ArtId.Circle);
            if (!enemyFlash.TryGetValue(e, out var s)) { BuildEnemies(new[] { e }); s = enemyFlash[e]; }
            return s;
        }

        /// <summary>Pre-builds everything for the database into atlas pages. Safe to call repeatedly.</summary>
        public static void Build(GameDatabase db)
        {
            if (builtFor == db && db != null) return;
            var entries = new List<(object key, PixelCanvas canvas, bool flash)>();
            foreach (ArtId id in System.Enum.GetValues(typeof(ArtId)))
                if (id != ArtId.None && !sprites.ContainsKey(id)) entries.Add((id, ArtDraw.Draw(id), false));
            if (db != null)
                foreach (var e in db.enemies)
                {
                    if (e == null || enemySprites.ContainsKey(e)) continue;
                    var c = ArtDraw.DrawEnemy(e);
                    entries.Add((e, c, false));
                    entries.Add((e, c.Clone().Silhouette(Color.white), true));
                }
            Pack(entries);
            builtFor = db;
        }

        static void BuildStandalone(ArtId id) => Pack(new List<(object, PixelCanvas, bool)> { (id, ArtDraw.Draw(id), false) });

        static void BuildEnemies(IEnumerable<EnemyDefinition> list)
        {
            var entries = new List<(object, PixelCanvas, bool)>();
            foreach (var e in list)
            {
                var c = ArtDraw.DrawEnemy(e);
                entries.Add((e, c, false));
                entries.Add((e, c.Clone().Silhouette(Color.white), true));
            }
            Pack(entries);
        }

        /// <summary>Shelf-pack canvases (tallest first) into as many pages as needed.</summary>
        static void Pack(List<(object key, PixelCanvas canvas, bool flash)> entries)
        {
            entries.Sort((a, b) => b.canvas.H.CompareTo(a.canvas.H));
            int i = 0;
            while (i < entries.Count)
            {
                int size = PageSize;
                // Small batches get a small page.
                int area = 0;
                for (int k = i; k < entries.Count; k++) area += (entries[k].canvas.W + Padding) * (entries[k].canvas.H + Padding);
                while (size > 64 && area * 1.4f < size * size / 4f) size /= 2;
                size = Mathf.Max(size, entries[i].canvas.W + Padding * 2, entries[i].canvas.H + Padding * 2);
                size = Mathf.NextPowerOfTwo(size);

                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = $"MiniMayhemAtlas{pages.Count}", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                var clear = new Color32[size * size];
                tex.SetPixels32(clear);
                int x = Padding, y = Padding, shelfH = 0;
                var placed = new List<(object key, bool flash, RectInt rect)>();
                for (; i < entries.Count; i++)
                {
                    var c = entries[i].canvas;
                    if (x + c.W + Padding > size) { x = Padding; y += shelfH + Padding; shelfH = 0; }
                    if (y + c.H + Padding > size) break; // page full
                    tex.SetPixels(x, y, c.W, c.H, c.Px);
                    placed.Add((entries[i].key, entries[i].flash, new RectInt(x, y, c.W, c.H)));
                    x += c.W + Padding;
                    shelfH = Mathf.Max(shelfH, c.H);
                }
                tex.Apply(false, false);
                pages.Add(tex);
                foreach (var (key, flash, r) in placed)
                {
                    // UI panels/cards get a 9-slice border so they scale without stretching the rounded corners.
                    var border = key is ArtId a && (a == ArtId.UiCard || a == ArtId.UiNode) ? new Vector4(r.width * 0.3f, r.height * 0.3f, r.width * 0.3f, r.height * 0.3f) : Vector4.zero;
                    var sprite = Sprite.Create(tex, new Rect(r.x, r.y, r.width, r.height), new Vector2(0.5f, 0.5f), r.width, 0, SpriteMeshType.FullRect, border);
                    sprite.hideFlags = HideFlags.DontSave;
                    if (key is ArtId id) { sprite.name = id.ToString(); sprites[id] = sprite; }
                    else if (key is EnemyDefinition e)
                    {
                        sprite.name = e.id + (flash ? "_flash" : "");
                        if (flash) enemyFlash[e] = sprite; else enemySprites[e] = sprite;
                    }
                }
            }
        }

        /// <summary>Tileable ground texture for a biome (two tones + soft speckles).</summary>
        public static Sprite Ground(BiomeDefinition b)
        {
            if (b != null && grounds.TryGetValue(b, out var s)) return s;
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.DontSave, name = "Ground" };
            Color a = b != null ? b.groundA : new Color(0.55f, 0.8f, 0.4f), c2 = b != null ? b.groundB : new Color(0.5f, 0.74f, 0.36f);
            var px = new Color[N * N];
            var rnd = new System.Random(b != null ? b.index * 31 + 7 : 7);
            for (int i = 0; i < px.Length; i++) px[i] = a;
            // Soft blobs of the second tone (wrapped so the tile is seamless).
            for (int k = 0; k < 9; k++)
            {
                float bx = (float)rnd.NextDouble() * N, by = (float)rnd.NextDouble() * N, br = 8 + (float)rnd.NextDouble() * 16;
                for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = Mathf.Abs(x - bx); dx = Mathf.Min(dx, N - dx);
                    float dy = Mathf.Abs(y - by); dy = Mathf.Min(dy, N - dy);
                    float t = Mathf.Clamp01((br - Mathf.Sqrt(dx * dx + dy * dy)) / 3f);
                    if (t > 0) px[y * N + x] = Color.Lerp(px[y * N + x], c2, t);
                }
            }
            // Tiny speckles.
            for (int k = 0; k < 40; k++)
            {
                int x = rnd.Next(N), y = rnd.Next(N);
                var sp = Color.Lerp(a, Color.white, 0.18f);
                px[y * N + x] = sp;
                px[y * N + (x + 1) % N] = sp;
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N / 4f, 0, SpriteMeshType.FullRect);
            s.hideFlags = HideFlags.DontSave;
            if (b != null) grounds[b] = s;
            return s;
        }
    }
}
