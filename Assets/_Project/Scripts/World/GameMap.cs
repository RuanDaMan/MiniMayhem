using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// The play space. Endless: a ground tile that follows the camera plus props and hazard patches streamed in
    /// chunks. Fixed arena: walled box with obstacles. Expanding arena: a box whose walls move out at each boss.
    /// </summary>
    public class GameMap
    {
        struct Patch { public Vector2 pos; public float radius; }
        class Chunk { public readonly List<SpriteRenderer> sprites = new(); public readonly List<Patch> patches = new(); }

        const float ChunkSize = 12f;
        const float GroundTile = 4f;

        readonly RunController run;
        readonly BiomeDefinition biome;
        readonly Transform root;
        readonly Dictionary<Vector2Int, Chunk> chunks = new();
        readonly Stack<SpriteRenderer> srPool = new();
        readonly List<Rect> obstacles = new();
        readonly List<Patch> arenaPatches = new();
        readonly List<Vector2Int> toRemove = new();
        SpriteRenderer ground, outside;
        readonly SpriteRenderer[] walls = new SpriteRenderer[4];
        Rect bounds, targetBounds;
        int expandStage;

        public MapStyle Style { get; }
        public bool Bounded => Style != MapStyle.Endless;
        public Rect Bounds => bounds;

        public static readonly Vector2 FixedSize = new(34f, 22f);
        public static readonly Vector2[] ExpandSizes = { new(20f, 13f), new(27f, 17f), new(34f, 22f), new(40f, 26f) };

        public GameMap(RunController run, BiomeDefinition biome, MapStyle style, Transform parent, int seed)
        {
            this.run = run;
            this.biome = biome;
            Style = style;
            root = new GameObject("Map").transform;
            root.SetParent(parent, false);

            ground = NewSprite("Ground", -100);
            ground.sprite = Art.Ground(biome);
            ground.drawMode = SpriteDrawMode.Tiled;

            if (Bounded)
            {
                Vector2 size = style == MapStyle.FixedArena ? FixedSize : ExpandSizes[0];
                bounds = targetBounds = new Rect(-size * 0.5f, size);
                outside = NewSprite("Outside", -101);
                outside.sprite = Art.Ground(biome);
                outside.drawMode = SpriteDrawMode.Tiled;
                outside.size = new Vector2(160, 160);
                outside.color = new Color(0.55f, 0.55f, 0.6f);
                for (int i = 0; i < 4; i++)
                {
                    walls[i] = NewSprite("Wall", -2);
                    walls[i].sprite = Art.Get(ArtId.Wall);
                    walls[i].drawMode = SpriteDrawMode.Simple;
                    walls[i].color = biome.wallColor;
                }
                var rnd = new System.Random(seed);
                if (style == MapStyle.FixedArena) BuildObstacles(rnd);
                BuildArenaDecor(rnd);
                LayoutArena();
            }
            else ground.size = new Vector2(96, 96);
        }

        SpriteRenderer NewSprite(string name, int order)
        {
            SpriteRenderer sr = srPool.Count > 0 ? srPool.Pop() : null;
            if (sr == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(root, false);
                sr = go.AddComponent<SpriteRenderer>();
                sr.sharedMaterial = Art.SpriteMaterial;
            }
            sr.gameObject.SetActive(true);
            sr.sortingOrder = order;
            sr.color = Color.white;
            sr.flipX = false;
            sr.transform.rotation = Quaternion.identity;
            return sr;
        }

        // ------------------------------------------------------------------ arena

        void BuildObstacles(System.Random rnd)
        {
            int n = 6;
            for (int i = 0, guard = 0; i < n && guard < 100; guard++)
            {
                float w = 1.5f + (float)rnd.NextDouble() * 2.5f, h = 1.5f + (float)rnd.NextDouble() * 1.5f;
                float x = Mathf.Lerp(bounds.xMin + 3f, bounds.xMax - 3f - w, (float)rnd.NextDouble());
                float y = Mathf.Lerp(bounds.yMin + 3f, bounds.yMax - 3f - h, (float)rnd.NextDouble());
                var r = new Rect(x, y, w, h);
                if (r.Overlaps(new Rect(-4.5f, -4.5f, 9f, 9f))) continue;
                bool clash = false;
                foreach (var o in obstacles) if (new Rect(o.x - 1.5f, o.y - 1.5f, o.width + 3f, o.height + 3f).Overlaps(r)) clash = true;
                if (clash) continue;
                obstacles.Add(r);
                var sr = NewSprite("Obstacle", -1);
                sr.sprite = Art.Get(ArtId.Crate);
                sr.color = Color.Lerp(biome.wallColor, Color.white, 0.25f);
                sr.transform.position = r.center;
                sr.transform.localScale = new Vector3(r.width * 1.18f, r.height * 1.18f, 1);
                i++;
            }
        }

        void BuildArenaDecor(System.Random rnd)
        {
            Rect full = new(-ExpandSizes[3] * 0.5f, ExpandSizes[3]);
            if (Style == MapStyle.FixedArena) full = bounds;
            int props = Mathf.RoundToInt(full.width * full.height / 144f * biome.propDensity);
            for (int i = 0; i < props; i++)
            {
                Vector2 p = new(Mathf.Lerp(full.xMin, full.xMax, (float)rnd.NextDouble()), Mathf.Lerp(full.yMin, full.yMax, (float)rnd.NextDouble()));
                PlaceProp(p, rnd, null);
            }
            if (biome.hazard != HazardType.None && biome.hazard != HazardType.Sandstorm)
            {
                int n = Mathf.RoundToInt(full.width * full.height / 144f * biome.hazardDensity);
                for (int i = 0; i < n; i++)
                {
                    Vector2 p = new(Mathf.Lerp(full.xMin + 2, full.xMax - 2, (float)rnd.NextDouble()), Mathf.Lerp(full.yMin + 2, full.yMax - 2, (float)rnd.NextDouble()));
                    if (p.magnitude < 4f) continue;
                    var patch = new Patch { pos = p, radius = 1.2f + (float)rnd.NextDouble() * 1.4f };
                    arenaPatches.Add(patch);
                    DrawPatch(patch, null);
                }
            }
        }

        void LayoutArena()
        {
            ground.size = bounds.size;
            ground.transform.position = bounds.center;
            float t = 0.9f;
            SetWall(walls[0], new Rect(bounds.xMin - t, bounds.yMax, bounds.width + t * 2, t));
            SetWall(walls[1], new Rect(bounds.xMin - t, bounds.yMin - t, bounds.width + t * 2, t));
            SetWall(walls[2], new Rect(bounds.xMin - t, bounds.yMin, t, bounds.height));
            SetWall(walls[3], new Rect(bounds.xMax, bounds.yMin, t, bounds.height));
        }

        static void SetWall(SpriteRenderer sr, Rect r)
        {
            sr.transform.position = r.center;
            sr.transform.localScale = new Vector3(r.width, r.height, 1);
        }

        /// <summary>Expanding arenas grow at each boss (called by the director).</summary>
        public void Expand()
        {
            if (Style != MapStyle.ExpandingArena || expandStage >= ExpandSizes.Length - 1) return;
            expandStage++;
            var s = ExpandSizes[expandStage];
            targetBounds = new Rect(-s * 0.5f, s);
            run.Announce("The arena grows!", new Color(0.6f, 0.9f, 1f));
            Sfx.Play(SfxId.Boing, 0.6f, 0.6f);
        }

        // ------------------------------------------------------------------ endless streaming

        void PlaceProp(Vector2 p, System.Random rnd, Chunk chunk)
        {
            if (biome.props == null || biome.props.Length == 0) return;
            int k = rnd.Next(biome.props.Length);
            var sr = NewSprite("Prop", -60);
            sr.sprite = Art.Get(biome.props[k]);
            var tint = biome.propTints != null && biome.propTints.Length > 0 ? biome.propTints[k % biome.propTints.Length] : Color.white;
            // Blend decor into the ground so enemies, pickups and obstacles (which keep their ink outlines) pop.
            var c = Color.Lerp(tint, biome.groundB, 0.5f);
            c.a = 0.8f;
            sr.color = c;
            float size = 0.55f + (float)rnd.NextDouble() * 0.5f;
            if (biome.props[k] == ArtId.PropTree || biome.props[k] == ArtId.PropPine) size *= 1.5f;
            sr.transform.position = p;
            sr.transform.localScale = Vector3.one * size;
            sr.flipX = rnd.Next(2) == 0;
            chunk?.sprites.Add(sr);
        }

        void DrawPatch(Patch patch, Chunk chunk)
        {
            var sr = NewSprite("Hazard", -80);
            sr.sprite = Art.Get(ArtId.HazardPatch);
            sr.color = biome.hazardColor;
            sr.transform.position = patch.pos;
            sr.transform.localScale = Vector3.one * patch.radius * 2.3f;
            sr.transform.rotation = Quaternion.Euler(0, 0, patch.pos.x * 37f);
            chunk?.sprites.Add(sr);
        }

        Chunk BuildChunk(Vector2Int c)
        {
            var ch = new Chunk();
            var rnd = new System.Random(c.x * 73856093 ^ c.y * 19349663 ^ biome.index * 83492791);
            Vector2 origin = new(c.x * ChunkSize, c.y * ChunkSize);
            int props = Mathf.RoundToInt(biome.propDensity + (float)rnd.NextDouble() * 1.5f);
            for (int i = 0; i < props; i++)
                PlaceProp(origin + new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble()) * ChunkSize, rnd, ch);
            if (biome.hazard != HazardType.None && biome.hazard != HazardType.Sandstorm && c.sqrMagnitude > 0)
            {
                float expected = biome.hazardDensity;
                while (expected > 0f)
                {
                    if (rnd.NextDouble() < expected)
                    {
                        var patch = new Patch { pos = origin + new Vector2(1.5f + (float)rnd.NextDouble() * 9f, 1.5f + (float)rnd.NextDouble() * 9f), radius = 1.2f + (float)rnd.NextDouble() * 1.6f };
                        ch.patches.Add(patch);
                        DrawPatch(patch, ch);
                    }
                    expected -= 1f;
                }
            }
            return ch;
        }

        public void Tick(Vector2 cameraPos, float dt)
        {
            if (Bounded)
            {
                if (bounds != targetBounds)
                {
                    Vector2 min = Vector2.MoveTowards(bounds.min, targetBounds.min, 3f * dt);
                    Vector2 max = Vector2.MoveTowards(bounds.max, targetBounds.max, 3f * dt);
                    bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                    LayoutArena();
                }
                outside.transform.position = new Vector3(Mathf.Round(cameraPos.x / GroundTile) * GroundTile, Mathf.Round(cameraPos.y / GroundTile) * GroundTile, 0);
                return;
            }

            ground.transform.position = new Vector3(Mathf.Round(cameraPos.x / GroundTile) * GroundTile, Mathf.Round(cameraPos.y / GroundTile) * GroundTile, 0);
            var cc = new Vector2Int(Mathf.FloorToInt(cameraPos.x / ChunkSize), Mathf.FloorToInt(cameraPos.y / ChunkSize));
            for (int y = -3; y <= 3; y++)
            for (int x = -3; x <= 3; x++)
            {
                var c = new Vector2Int(cc.x + x, cc.y + y);
                if (!chunks.ContainsKey(c)) chunks[c] = BuildChunk(c);
            }
            toRemove.Clear();
            foreach (var kv in chunks)
                if (Mathf.Abs(kv.Key.x - cc.x) > 4 || Mathf.Abs(kv.Key.y - cc.y) > 4) toRemove.Add(kv.Key);
            foreach (var k in toRemove)
            {
                foreach (var sr in chunks[k].sprites) { sr.gameObject.SetActive(false); srPool.Push(sr); }
                chunks.Remove(k);
            }
        }

        // ------------------------------------------------------------------ queries

        public void ResolveCircle(ref Vector2 p, float r)
        {
            if (!Bounded) return;
            p.x = Mathf.Clamp(p.x, bounds.xMin + r, bounds.xMax - r);
            p.y = Mathf.Clamp(p.y, bounds.yMin + r, bounds.yMax - r);
            foreach (var o in obstacles)
            {
                Vector2 closest = new(Mathf.Clamp(p.x, o.xMin, o.xMax), Mathf.Clamp(p.y, o.yMin, o.yMax));
                Vector2 d = p - closest;
                float sq = d.sqrMagnitude;
                if (sq >= r * r) continue;
                if (sq < 1e-8f)
                {
                    // Centre inside the box: push out along the shallowest axis.
                    float left = p.x - o.xMin, right = o.xMax - p.x, down = p.y - o.yMin, up = o.yMax - p.y;
                    float m = Mathf.Min(Mathf.Min(left, right), Mathf.Min(down, up));
                    if (m == left) p.x = o.xMin - r; else if (m == right) p.x = o.xMax + r; else if (m == down) p.y = o.yMin - r; else p.y = o.yMax + r;
                }
                else p = closest + d / Mathf.Sqrt(sq) * r;
            }
        }

        public bool Blocks(Vector2 p)
        {
            if (!Bounded) return false;
            if (!bounds.Contains(p)) return true;
            foreach (var o in obstacles) if (o.Contains(p)) return true;
            return false;
        }

        public HazardType HazardAt(Vector2 p)
        {
            var h = biome.hazard;
            if (h == HazardType.None || h == HazardType.Sandstorm) return HazardType.None;
            if (Bounded)
            {
                foreach (var patch in arenaPatches) if ((patch.pos - p).sqrMagnitude < patch.radius * patch.radius) return h;
                return HazardType.None;
            }
            var cc = new Vector2Int(Mathf.FloorToInt(p.x / ChunkSize), Mathf.FloorToInt(p.y / ChunkSize));
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
                if (chunks.TryGetValue(new Vector2Int(cc.x + x, cc.y + y), out var ch))
                    foreach (var patch in ch.patches)
                        if ((patch.pos - p).sqrMagnitude < patch.radius * patch.radius) return h;
            return HazardType.None;
        }

        /// <summary>A random point inside the arena at least minDist from the hero (arena spawns).</summary>
        public Vector2 ArenaPoint(Vector2 hero, float minDist, System.Random rnd)
        {
            Rect b = targetBounds;
            for (int i = 0; i < 30; i++)
            {
                Vector2 p = new(Mathf.Lerp(b.xMin + 1f, b.xMax - 1f, (float)rnd.NextDouble()), Mathf.Lerp(b.yMin + 1f, b.yMax - 1f, (float)rnd.NextDouble()));
                if ((p - hero).sqrMagnitude < minDist * minDist || Blocks(p)) continue;
                return p;
            }
            // Fallback: the far corner.
            return new Vector2(hero.x > b.center.x ? b.xMin + 1.5f : b.xMax - 1.5f, hero.y > b.center.y ? b.yMin + 1.5f : b.yMax - 1.5f);
        }

        public IReadOnlyList<Rect> Obstacles => obstacles;

        /// <summary>Nearest hazard patch centre within range (for tests and debugging).</summary>
        public bool FindHazard(Vector2 near, float range, out Vector2 centre)
        {
            centre = default;
            float best = range * range;
            bool found = false;
            var all = new List<Patch>(arenaPatches);
            foreach (var ch in chunks.Values) all.AddRange(ch.patches);
            foreach (var p in all)
            {
                float d = (p.pos - near).sqrMagnitude;
                if (d < best) { best = d; centre = p.pos; found = true; }
            }
            return found;
        }
    }
}
