using System.Collections.Generic;
using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// Uniform grid hashed into a fixed bucket array with intrusive linked lists (no allocations per frame).
    /// Rebuilt every frame from the active enemy list; used for targeting, separation and hit tests.
    /// </summary>
    public class SpatialHash
    {
        const int Buckets = 8192; // power of two
        readonly float cell;
        readonly float inv;
        readonly int[] head = new int[Buckets];
        int[] next;
        Enemy[] items;
        int count;

        public SpatialHash(float cellSize, int capacity)
        {
            cell = cellSize;
            inv = 1f / cellSize;
            next = new int[capacity];
            items = new Enemy[capacity];
        }

        public float CellSize => cell;

        /// <summary>Largest enemy radius in the hash (queries pad by this so big bosses are never missed).</summary>
        public float MaxRadius { get; set; } = 1f;

        static int Hash(int cx, int cy) => ((cx * 73856093) ^ (cy * 19349663)) & (Buckets - 1);

        public void Clear()
        {
            System.Array.Fill(head, -1);
            count = 0;
        }

        public void Insert(Enemy e)
        {
            if (count >= items.Length)
            {
                System.Array.Resize(ref items, items.Length * 2);
                System.Array.Resize(ref next, next.Length * 2);
            }
            int h = Hash(Mathf.FloorToInt(e.pos.x * inv), Mathf.FloorToInt(e.pos.y * inv));
            items[count] = e;
            next[count] = head[h];
            head[h] = count;
            count++;
        }

        /// <summary>All enemies whose centre is within r of p (plus their own radius if includeRadius).</summary>
        public void Query(Vector2 p, float r, List<Enemy> results, bool includeRadius = true)
        {
            results.Clear();
            float pad = includeRadius ? MaxRadius : 0f;
            int x0 = Mathf.FloorToInt((p.x - r - pad) * inv), x1 = Mathf.FloorToInt((p.x + r + pad) * inv);
            int y0 = Mathf.FloorToInt((p.y - r - pad) * inv), y1 = Mathf.FloorToInt((p.y + r + pad) * inv);
            // Very large queries: fall back to scanning cells would be slow; caller should use the active list.
            for (int cy = y0; cy <= y1; cy++)
            for (int cx = x0; cx <= x1; cx++)
            {
                int h = Hash(cx, cy);
                for (int i = head[h]; i >= 0; i = next[i])
                {
                    var e = items[i];
                    // Hash collisions can bring enemies from other cells: the distance test filters them.
                    int ecx = Mathf.FloorToInt(e.pos.x * inv), ecy = Mathf.FloorToInt(e.pos.y * inv);
                    if (ecx != cx || ecy != cy) continue;
                    float rr = r + (includeRadius ? e.radius : 0f);
                    if ((e.pos - p).sqrMagnitude <= rr * rr) results.Add(e);
                }
            }
        }

        /// <summary>Visit candidates in the 3x3 cells around p (for separation).</summary>
        public int Neighbours(Vector2 p, Enemy[] buffer)
        {
            int n = 0;
            int bx = Mathf.FloorToInt(p.x * inv), by = Mathf.FloorToInt(p.y * inv);
            for (int cy = by - 1; cy <= by + 1; cy++)
            for (int cx = bx - 1; cx <= bx + 1; cx++)
            {
                int h = Hash(cx, cy);
                for (int i = head[h]; i >= 0; i = next[i])
                {
                    if (n >= buffer.Length) return n;
                    buffer[n++] = items[i];
                }
            }
            return n;
        }
    }
}
