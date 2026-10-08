using UnityEngine;

namespace MiniMayhem
{
    /// <summary>
    /// All procedural drawings: cute, chunky shapes with thick dark outlines. Each ArtId has a pixel size and a
    /// drawing routine; enemies are assembled from a body shape + features + colours.
    /// </summary>
    public static class ArtDraw
    {
        public static readonly Color Ink = new(0.12f, 0.09f, 0.17f);
        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        static readonly Color Skin = C("#FFD3A8"), Hair = C("#5A3A22"), Shirt = C("#FF6B5B"), Jeans = C("#4D7BD6"),
            Shoe = C("#F4F4F4"), Pea = C("#7BD94B"), Gold = C("#FFD23F"), Blue = C("#4FB6FF"), Pink = C("#FF8FC7"),
            Purple = C("#A66BFF"), Orange = C("#FF9A3C"), Red = C("#FF4D4D"), Brown = C("#9C6B3E"), Grey = C("#A9B1C2"),
            Teal = C("#3ED6C0"), Lime = C("#C6F25A"), Cream = C("#FFF1D6"), DarkGrey = C("#4B4F5E");

        public static int SizeOf(ArtId id) => id switch
        {
            ArtId.Hero or ArtId.HeroStep => 112,
            ArtId.UiVignette => 256,
            ArtId.Puddle or ArtId.StinkCloud or ArtId.ToxicCloud or ArtId.HazardPatch or ArtId.Ring or ArtId.Circle or ArtId.Glow or ArtId.Telegraph => 128,
            ArtId.Swoosh => 128,
            ArtId.Pixel => 8,
            ArtId.Beam => 32,
            ArtId.Wall or ArtId.Crate => 64,
            ArtId.PropTree or ArtId.PropPine => 96,
            ArtId.Chest => 80,
            _ => 64,
        };

        /// <summary>Whether to give the sprite the standard chunky outline.</summary>
        static bool Outlined(ArtId id) => id switch
        {
            ArtId.Circle or ArtId.Ring or ArtId.Glow or ArtId.Pixel or ArtId.Shadow or ArtId.Telegraph or ArtId.Puddle
                or ArtId.StinkCloud or ArtId.ToxicCloud or ArtId.Swoosh or ArtId.Beam or ArtId.HazardPatch or ArtId.UiVignette
                or ArtId.Spark or ArtId.Puff or ArtId.Bolt or ArtId.UiNode or ArtId.UiCard or ArtId.Wall => false,
            // Decorative props are flat, outline-free background art; only things that matter get the ink outline.
            >= ArtId.PropFlower and <= ArtId.PropSkull => false,
            _ => true,
        };

        public static PixelCanvas Draw(ArtId id)
        {
            int s = SizeOf(id);
            var c = new PixelCanvas(s, s);
            DrawInto(c, id);
            if (Outlined(id)) c.Outline(Mathf.Max(2f, s / 22f), Ink);
            return c;
        }

        static void DrawInto(PixelCanvas c, ArtId id)
        {
            switch (id)
            {
                // ---------------------------------------------------------------- hero
                case ArtId.Hero: Hero(c, false); break;
                case ArtId.HeroStep: Hero(c, true); break;

                // ---------------------------------------------------------------- pickups
                case ArtId.GemSmall: Gem(c, Blue, 0.6f); break;
                case ArtId.GemMedium: Gem(c, C("#55E07A"), 0.75f); break;
                case ArtId.GemBig: Gem(c, C("#FF5577"), 0.9f); break;
                case ArtId.GemHuge: Gem(c, Purple, 1f); break;
                case ArtId.Coin:
                    c.Circle(0.5f, 0.5f, 0.36f, Gold).Ring(0.5f, 0.5f, 0.25f, 0.06f, C("#E0A800")).Shade(0.5f, 0.5f, 0.36f);
                    c.RoundRect(0.5f, 0.5f, 0.07f, 0.24f, 0.03f, C("#E0A800"));
                    break;
                case ArtId.HealthPickup:
                    Heart(c, 0.5f, 0.48f, 0.38f, Red); c.Shade(0.5f, 0.5f, 0.35f);
                    break;
                case ArtId.MagnetPickup:
                    c.Arc(0.5f, 0.52f, 0.24f, 0.17f, 180f, 360f, Red);
                    c.Rect(0.18f, 0.52f, 0.35f, 0.82f, Red).Rect(0.65f, 0.52f, 0.82f, 0.82f, Red);
                    c.Rect(0.18f, 0.72f, 0.35f, 0.86f, Grey).Rect(0.65f, 0.72f, 0.82f, 0.86f, Grey);
                    break;
                case ArtId.BombPickup:
                    c.Circle(0.46f, 0.42f, 0.3f, DarkGrey).Shade(0.46f, 0.42f, 0.3f, 1.5f);
                    c.RoundRect(0.66f, 0.7f, 0.14f, 0.12f, 0.03f, Grey, 40f);
                    c.Line(0.72f, 0.76f, 0.84f, 0.88f, 0.05f, Brown).Star(0.86f, 0.9f, 0.09f, 0.04f, 5, Orange);
                    break;
                case ArtId.Chest:
                    c.RoundRect(0.5f, 0.38f, 0.8f, 0.42f, 0.05f, Brown);
                    c.RoundRect(0.5f, 0.66f, 0.8f, 0.26f, 0.1f, C("#B57E48"));
                    c.Rect(0.08f, 0.52f, 0.92f, 0.58f, Gold).Rect(0.44f, 0.14f, 0.56f, 0.8f, Gold);
                    c.RoundRect(0.5f, 0.52f, 0.16f, 0.16f, 0.03f, C("#E0A800"));
                    break;

                // ---------------------------------------------------------------- projectiles
                case ArtId.Pea: c.Circle(0.5f, 0.5f, 0.32f, Pea).Shade(0.5f, 0.5f, 0.32f); break;
                case ArtId.PeaBig: c.Circle(0.5f, 0.5f, 0.38f, C("#4FC23A")).Circle(0.42f, 0.58f, 0.12f, Lime).Shade(0.5f, 0.5f, 0.38f); break;
                case ArtId.Boomerang:
                    c.Arc(0.5f, 0.35f, 0.3f, 0.16f, 20f, 160f, Orange).Shade(0.5f, 0.5f, 0.3f);
                    break;
                case ArtId.Buzzsaw:
                    c.Star(0.5f, 0.5f, 0.44f, 0.3f, 10, Grey).Circle(0.5f, 0.5f, 0.16f, Orange).Shade(0.5f, 0.5f, 0.4f);
                    break;
                case ArtId.Star: c.Star(0.5f, 0.5f, 0.42f, 0.2f, 5, Gold).Shade(0.5f, 0.5f, 0.35f); break;
                case ArtId.Comet:
                    c.Line(0.15f, 0.15f, 0.55f, 0.55f, 0.26f, new Color(1f, 0.6f, 0.2f, 0.8f));
                    c.Star(0.6f, 0.6f, 0.34f, 0.18f, 5, Gold).Shade(0.6f, 0.6f, 0.3f);
                    break;
                case ArtId.Balloon:
                    c.Circle(0.5f, 0.55f, 0.32f, Blue).Triangle(new Vector2(0.44f, 0.2f), new Vector2(0.56f, 0.2f), new Vector2(0.5f, 0.27f), Blue).Shade(0.5f, 0.55f, 0.32f);
                    break;
                case ArtId.BalloonBig:
                    c.Circle(0.5f, 0.55f, 0.38f, C("#2E8CFF")).Ring(0.5f, 0.55f, 0.24f, 0.05f, C("#9CD7FF")).Shade(0.5f, 0.55f, 0.38f);
                    break;
                case ArtId.Duck: Duck(c, C("#FFE14D"), Orange); break;
                case ArtId.GoldDuck: Duck(c, C("#FFC21A"), Red); c.Star(0.3f, 0.8f, 0.1f, 0.04f, 4, Color.white); break;
                case ArtId.Teddy: Teddy(c, Brown, false); break;
                case ArtId.RoboTeddy: Teddy(c, Grey, true); break;
                case ArtId.Firecracker:
                    c.RoundRect(0.5f, 0.42f, 0.3f, 0.56f, 0.06f, Red).Rect(0.35f, 0.5f, 0.65f, 0.58f, Gold);
                    c.Line(0.5f, 0.7f, 0.58f, 0.86f, 0.05f, Brown);
                    break;
                case ArtId.Puddle:
                    c.Ellipse(0.5f, 0.5f, 0.46f, 0.46f, new Color(0.45f, 0.75f, 1f, 0.55f)).Ellipse(0.38f, 0.6f, 0.12f, 0.06f, new Color(1, 1, 1, 0.5f));
                    break;
                case ArtId.StinkCloud: Cloud(c, new Color(0.65f, 0.85f, 0.3f, 0.35f)); break;
                case ArtId.ToxicCloud: Cloud(c, new Color(0.55f, 0.95f, 0.25f, 0.45f)); break;
                case ArtId.Bubble:
                    c.Circle(0.5f, 0.5f, 0.4f, new Color(0.7f, 0.9f, 1f, 0.45f)).Ring(0.5f, 0.5f, 0.38f, 0.05f, new Color(0.85f, 0.97f, 1f, 0.9f));
                    c.Ellipse(0.38f, 0.64f, 0.1f, 0.06f, Color.white, -35f);
                    break;
                case ArtId.EnemyBullet:
                    c.Circle(0.5f, 0.5f, 0.36f, C("#FF3B6B")).Circle(0.5f, 0.5f, 0.18f, C("#FFD1DC"));
                    break;
                case ArtId.EnemyBulletAlt:
                    c.Star(0.5f, 0.5f, 0.44f, 0.26f, 6, C("#FF00FF")).Circle(0.5f, 0.5f, 0.16f, Color.white);
                    break;
                case ArtId.TeddyShot: c.Circle(0.5f, 0.5f, 0.28f, Pink).Shade(0.5f, 0.5f, 0.28f); break;

                // ---------------------------------------------------------------- fx
                case ArtId.Circle: c.Circle(0.5f, 0.5f, 0.48f, Color.white); break;
                case ArtId.Ring: c.Ring(0.5f, 0.5f, 0.44f, 0.06f, Color.white); break;
                case ArtId.Glow: c.Glow(0.5f, 0.5f, 0.5f, Color.white, 1.6f); break;
                case ArtId.Swoosh:
                    for (int i = 0; i < 6; i++)
                        c.Arc(0.5f, 0.5f, 0.38f + i * 0.012f, 0.05f + i * 0.012f, -75f + i * 6f, 75f - i * 2f, new Color(1, 1, 1, 0.22f));
                    c.Arc(0.5f, 0.5f, 0.44f, 0.04f, -70f, 70f, Color.white);
                    break;
                case ArtId.Spark: c.Star(0.5f, 0.5f, 0.48f, 0.12f, 4, Color.white); break;
                case ArtId.Puff:
                    c.Circle(0.5f, 0.5f, 0.3f, Color.white).Circle(0.3f, 0.45f, 0.2f, Color.white).Circle(0.7f, 0.45f, 0.2f, Color.white).Circle(0.5f, 0.7f, 0.2f, Color.white);
                    break;
                case ArtId.Pixel: c.Rect(0, 0, 1, 1, Color.white); break;
                case ArtId.Shadow: c.Ellipse(0.5f, 0.5f, 0.46f, 0.3f, new Color(0, 0, 0, 0.28f)); break;
                case ArtId.Telegraph:
                    c.Circle(0.5f, 0.5f, 0.46f, new Color(1f, 0.2f, 0.2f, 0.22f)).Ring(0.5f, 0.5f, 0.45f, 0.04f, new Color(1f, 0.25f, 0.25f, 0.9f));
                    break;
                case ArtId.Exclaim:
                    c.Circle(0.5f, 0.5f, 0.42f, Red).RoundRect(0.5f, 0.58f, 0.12f, 0.36f, 0.05f, Color.white).Circle(0.5f, 0.26f, 0.07f, Color.white);
                    break;
                case ArtId.Beam:
                    for (int y = 0; y < c.H; y++)
                    {
                        float t = Mathf.Abs((y + 0.5f) / c.H - 0.5f) * 2f;
                        float a = t < 0.35f ? 1f : Mathf.Clamp01(1f - (t - 0.35f) / 0.65f);
                        for (int x = 0; x < c.W; x++) c.Px[y * c.W + x] = new Color(1, 1, 1, a);
                    }
                    break;
                case ArtId.Bolt:
                    c.Polygon(new[] { new Vector2(0.55f, 0.95f), new Vector2(0.25f, 0.45f), new Vector2(0.48f, 0.45f), new Vector2(0.4f, 0.05f), new Vector2(0.75f, 0.58f), new Vector2(0.52f, 0.58f) }, Color.white);
                    break;

                // ---------------------------------------------------------------- weapon icons
                case ArtId.IconPeaShooter: Blaster(c, Pea, C("#3D8B2A")); c.Circle(0.84f, 0.62f, 0.08f, Pea); break;
                case ArtId.IconBoomerang: c.Arc(0.5f, 0.32f, 0.32f, 0.17f, 15f, 165f, Orange).Shade(0.5f, 0.45f, 0.3f); break;
                case ArtId.IconFryingPan:
                    c.Line(0.62f, 0.38f, 0.92f, 0.1f, 0.1f, Brown).Circle(0.42f, 0.56f, 0.32f, DarkGrey).Circle(0.42f, 0.56f, 0.24f, C("#6B7080"));
                    c.Ellipse(0.42f, 0.56f, 0.12f, 0.1f, C("#FFE38A")).Circle(0.42f, 0.56f, 0.05f, Orange);
                    break;
                case ArtId.IconDuckOrbit: Duck(c, C("#FFE14D"), Orange); c.Ring(0.5f, 0.5f, 0.46f, 0.03f, new Color(1, 1, 1, 0.6f)); break;
                case ArtId.IconZapRod: Rod(c, C("#7A5CFF"), Gold); c.Polygon(new[] { new Vector2(0.75f, 0.95f), new Vector2(0.6f, 0.72f), new Vector2(0.7f, 0.72f), new Vector2(0.62f, 0.55f), new Vector2(0.86f, 0.8f), new Vector2(0.76f, 0.8f) }, Gold); break;
                case ArtId.IconWaterBalloon: DrawInto(c, ArtId.Balloon); break;
                case ArtId.IconStarWand: Rod(c, Pink, Gold); c.Star(0.72f, 0.72f, 0.22f, 0.1f, 5, Gold); break;
                case ArtId.IconBaseballBat: c.Line(0.18f, 0.18f, 0.78f, 0.78f, 0.14f, C("#D9A066")).Line(0.62f, 0.62f, 0.82f, 0.82f, 0.24f, C("#D9A066")).Line(0.14f, 0.14f, 0.3f, 0.3f, 0.12f, DarkGrey).Shade(0.5f, 0.5f, 0.4f); break;
                case ArtId.IconStinkySocks: Sock(c, Cream, Lime); c.Line(0.66f, 0.8f, 0.72f, 0.92f, 0.04f, Lime).Line(0.78f, 0.74f, 0.88f, 0.84f, 0.04f, Lime); break;
                case ArtId.IconTeddyTurret: Teddy(c, Brown, false); break;
                case ArtId.IconFirecrackerTrap: DrawInto(c, ArtId.Firecracker); c.Star(0.62f, 0.9f, 0.1f, 0.04f, 5, Orange); break;
                case ArtId.IconLaserPointer:
                    c.RoundRect(0.35f, 0.35f, 0.18f, 0.5f, 0.06f, DarkGrey, 45f).Circle(0.5f, 0.5f, 0.05f, Red);
                    c.Line(0.52f, 0.52f, 0.92f, 0.92f, 0.06f, Red).Circle(0.9f, 0.9f, 0.06f, C("#FFB0B0"));
                    break;
                case ArtId.IconPeaCannon: Blaster(c, C("#3FAE2A"), C("#245E18")); c.Circle(0.82f, 0.62f, 0.14f, Lime); break;
                case ArtId.IconBuzzsaw: DrawInto(c, ArtId.Buzzsaw); break;
                case ArtId.IconCastIron:
                    c.Ring(0.42f, 0.56f, 0.4f, 0.04f, Orange).Line(0.62f, 0.38f, 0.92f, 0.1f, 0.12f, DarkGrey);
                    c.Circle(0.42f, 0.56f, 0.32f, C("#2E2F38")).Circle(0.42f, 0.56f, 0.22f, C("#464A57"));
                    break;
                case ArtId.IconGoldDuck: Duck(c, C("#FFC21A"), Red); c.Star(0.2f, 0.82f, 0.12f, 0.05f, 4, Color.white).Star(0.82f, 0.25f, 0.08f, 0.03f, 4, Color.white); break;
                case ArtId.IconThunderRod:
                    c.Ellipse(0.5f, 0.75f, 0.42f, 0.2f, Grey).Circle(0.3f, 0.78f, 0.18f, Grey).Circle(0.62f, 0.85f, 0.2f, Grey);
                    c.Polygon(new[] { new Vector2(0.55f, 0.66f), new Vector2(0.32f, 0.32f), new Vector2(0.48f, 0.32f), new Vector2(0.4f, 0.04f), new Vector2(0.7f, 0.42f), new Vector2(0.55f, 0.42f) }, Gold);
                    break;
                case ArtId.IconFloodBomb: DrawInto(c, ArtId.BalloonBig); c.Ellipse(0.5f, 0.12f, 0.4f, 0.08f, Blue); break;
                case ArtId.IconCometWand: Rod(c, Purple, Gold); c.Line(0.4f, 0.55f, 0.7f, 0.75f, 0.14f, Orange).Star(0.74f, 0.76f, 0.2f, 0.1f, 5, Gold); break;
                case ArtId.IconSlugger:
                    c.Line(0.18f, 0.18f, 0.78f, 0.78f, 0.16f, Gold).Line(0.6f, 0.6f, 0.84f, 0.84f, 0.28f, Gold).Line(0.14f, 0.14f, 0.3f, 0.3f, 0.14f, Red);
                    c.Star(0.8f, 0.3f, 0.12f, 0.05f, 5, Color.white);
                    break;
                case ArtId.IconToxicLaundry: Cloud(c, new Color(0.55f, 0.95f, 0.25f, 1f)); Sock(c, Cream, Lime, 0.7f); break;
                case ArtId.IconRoboTeddy: Teddy(c, Grey, true); break;
                case ArtId.IconFireworks:
                    for (int i = 0; i < 8; i++) { float a = i * 45f * Mathf.Deg2Rad; c.Line(0.5f, 0.5f, 0.5f + Mathf.Cos(a) * 0.4f, 0.5f + Mathf.Sin(a) * 0.4f, 0.07f, i % 2 == 0 ? Red : Gold); }
                    c.Circle(0.5f, 0.5f, 0.12f, Color.white);
                    break;
                case ArtId.IconDiscoLaser:
                    c.Circle(0.5f, 0.5f, 0.3f, Grey);
                    for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) c.Rect(0.22f + i * 0.14f, 0.22f + j * 0.14f, 0.34f + i * 0.14f, 0.34f + j * 0.14f, (i + j) % 2 == 0 ? Pink : Teal, true);
                    c.Shade(0.5f, 0.5f, 0.3f);
                    break;
                case ArtId.IconStarBlaster: Blaster(c, Gold, Orange); c.Star(0.84f, 0.64f, 0.14f, 0.06f, 5, Gold); break;
                case ArtId.IconMegaMallet:
                    c.Line(0.25f, 0.15f, 0.55f, 0.55f, 0.1f, Brown).RoundRect(0.6f, 0.62f, 0.62f, 0.36f, 0.08f, DarkGrey, 37f).Shade(0.6f, 0.62f, 0.3f);
                    break;
                case ArtId.IconPrismStorm:
                    c.Triangle(new Vector2(0.5f, 0.85f), new Vector2(0.18f, 0.25f), new Vector2(0.82f, 0.25f), new Color(0.8f, 0.95f, 1f));
                    c.Line(0.5f, 0.5f, 0.95f, 0.65f, 0.05f, Red).Line(0.5f, 0.5f, 0.95f, 0.5f, 0.05f, Gold).Line(0.5f, 0.5f, 0.95f, 0.35f, 0.05f, Teal);
                    break;
                case ArtId.IconBubbleBath:
                    DrawInto(c, ArtId.Bubble); c.Circle(0.3f, 0.3f, 0.16f, new Color(0.75f, 0.92f, 1f)).Circle(0.75f, 0.35f, 0.12f, new Color(0.75f, 0.92f, 1f));
                    break;

                // ---------------------------------------------------------------- items
                case ArtId.ItemSpinach:
                    c.RoundRect(0.5f, 0.45f, 0.5f, 0.66f, 0.08f, Grey).RoundRect(0.5f, 0.45f, 0.5f, 0.36f, 0.02f, C("#3AA845"), 0, true);
                    c.Ellipse(0.5f, 0.45f, 0.12f, 0.1f, Lime).Ellipse(0.5f, 0.78f, 0.25f, 0.06f, C("#C7CCD8"));
                    break;
                case ArtId.ItemStickyGlove: Glove(c, Pink); c.Circle(0.3f, 0.82f, 0.07f, Lime).Circle(0.62f, 0.86f, 0.06f, Lime); break;
                case ArtId.ItemOvenMitt: Glove(c, Red); c.Rect(0.22f, 0.12f, 0.78f, 0.24f, Cream, true); break;
                case ArtId.ItemBathBubbles: c.Circle(0.38f, 0.4f, 0.24f, new Color(0.75f, 0.92f, 1f)).Circle(0.66f, 0.6f, 0.18f, new Color(0.75f, 0.92f, 1f)).Circle(0.4f, 0.74f, 0.12f, new Color(0.75f, 0.92f, 1f)); c.Ellipse(0.3f, 0.5f, 0.06f, 0.04f, Color.white, -30f); break;
                case ArtId.ItemStaticSock: Sock(c, Purple, Gold); c.Star(0.75f, 0.78f, 0.12f, 0.04f, 4, Gold); break;
                case ArtId.ItemBigBucket:
                    c.Polygon(new[] { new Vector2(0.2f, 0.75f), new Vector2(0.8f, 0.75f), new Vector2(0.7f, 0.15f), new Vector2(0.3f, 0.15f) }, Blue);
                    c.Ellipse(0.5f, 0.75f, 0.3f, 0.07f, C("#9CD7FF")).Arc(0.5f, 0.75f, 0.28f, 0.04f, 0f, 180f, Grey);
                    break;
                case ArtId.ItemStarCookie:
                    c.Star(0.5f, 0.5f, 0.42f, 0.24f, 5, C("#E3A65C")).Circle(0.4f, 0.55f, 0.04f, Brown).Circle(0.58f, 0.42f, 0.04f, Brown).Circle(0.55f, 0.62f, 0.03f, Brown).Shade(0.5f, 0.5f, 0.4f);
                    break;
                case ArtId.ItemLuckyCap:
                    c.Arc(0.5f, 0.38f, 0.26f, 0.5f, 0f, 180f, C("#3FAE2A")).Ellipse(0.72f, 0.36f, 0.24f, 0.07f, C("#2E8A1E"));
                    Clover(c, 0.45f, 0.55f, 0.08f, Lime);
                    break;
                case ArtId.ItemClothespin: c.RoundRect(0.42f, 0.5f, 0.14f, 0.78f, 0.05f, C("#D9A066"), 10f).RoundRect(0.58f, 0.5f, 0.14f, 0.78f, 0.05f, C("#C48A52"), -10f).Ring(0.5f, 0.45f, 0.08f, 0.04f, Grey); break;
                case ArtId.ItemBatteries:
                    c.RoundRect(0.38f, 0.45f, 0.24f, 0.6f, 0.04f, DarkGrey).RoundRect(0.38f, 0.32f, 0.24f, 0.32f, 0.02f, Gold, 0, true).Rect(0.34f, 0.75f, 0.42f, 0.8f, Grey);
                    c.RoundRect(0.66f, 0.45f, 0.24f, 0.6f, 0.04f, DarkGrey).RoundRect(0.66f, 0.32f, 0.24f, 0.32f, 0.02f, Gold, 0, true).Rect(0.62f, 0.75f, 0.7f, 0.8f, Grey);
                    break;
                case ArtId.ItemMatchbox:
                    c.RoundRect(0.5f, 0.38f, 0.7f, 0.36f, 0.04f, Red).Rect(0.2f, 0.32f, 0.8f, 0.44f, Gold, true);
                    c.Line(0.55f, 0.56f, 0.75f, 0.86f, 0.05f, Cream).Circle(0.76f, 0.88f, 0.06f, Red);
                    break;
                case ArtId.ItemTinyLens: c.Line(0.6f, 0.4f, 0.88f, 0.12f, 0.1f, Brown).Circle(0.42f, 0.58f, 0.28f, Grey).Circle(0.42f, 0.58f, 0.21f, new Color(0.75f, 0.92f, 1f)).Ellipse(0.35f, 0.66f, 0.07f, 0.04f, Color.white, -35f); break;
                case ArtId.ItemSneakers:
                    c.RoundRect(0.5f, 0.32f, 0.78f, 0.22f, 0.1f, Shoe).RoundRect(0.38f, 0.5f, 0.42f, 0.32f, 0.12f, Red);
                    c.Rect(0.12f, 0.2f, 0.88f, 0.27f, DarkGrey, true).Line(0.32f, 0.55f, 0.46f, 0.5f, 0.03f, Color.white);
                    break;
                case ArtId.ItemMagnetRing: c.Ring(0.5f, 0.5f, 0.3f, 0.1f, Gold).RoundRect(0.5f, 0.82f, 0.22f, 0.14f, 0.04f, Red).Shade(0.5f, 0.5f, 0.3f); break;
                case ArtId.ItemHeartLocket: c.Line(0.2f, 0.95f, 0.5f, 0.7f, 0.03f, Gold).Line(0.8f, 0.95f, 0.5f, 0.7f, 0.03f, Gold); Heart(c, 0.5f, 0.42f, 0.32f, Pink); c.Shade(0.5f, 0.45f, 0.3f); break;
                case ArtId.ItemClover: Clover(c, 0.5f, 0.55f, 0.17f, C("#3FAE2A")); c.Line(0.5f, 0.45f, 0.58f, 0.1f, 0.05f, C("#2E8A1E")); break;
                case ArtId.ItemArmorPatch:
                    c.Polygon(new[] { new Vector2(0.5f, 0.92f), new Vector2(0.86f, 0.78f), new Vector2(0.8f, 0.35f), new Vector2(0.5f, 0.08f), new Vector2(0.2f, 0.35f), new Vector2(0.14f, 0.78f) }, Grey);
                    c.Line(0.5f, 0.8f, 0.5f, 0.2f, 0.06f, C("#7E8696"), true).Shade(0.5f, 0.5f, 0.4f);
                    break;
                case ArtId.ItemPiggyBank:
                    c.Ellipse(0.5f, 0.45f, 0.36f, 0.28f, Pink).Circle(0.82f, 0.48f, 0.09f, C("#FF6FB0")).Triangle(new Vector2(0.3f, 0.68f), new Vector2(0.42f, 0.7f), new Vector2(0.32f, 0.84f), Pink);
                    c.Rect(0.26f, 0.15f, 0.34f, 0.25f, Pink).Rect(0.62f, 0.15f, 0.7f, 0.25f, Pink).Rect(0.44f, 0.68f, 0.58f, 0.72f, DarkGrey).Circle(0.68f, 0.55f, 0.03f, Ink);
                    break;
                case ArtId.ItemSmartHat:
                    c.Polygon(new[] { new Vector2(0.08f, 0.6f), new Vector2(0.5f, 0.78f), new Vector2(0.92f, 0.6f), new Vector2(0.5f, 0.42f) }, DarkGrey);
                    c.RoundRect(0.5f, 0.42f, 0.4f, 0.2f, 0.04f, DarkGrey).Line(0.8f, 0.62f, 0.84f, 0.32f, 0.03f, Gold).Circle(0.84f, 0.3f, 0.05f, Gold);
                    break;

                // ---------------------------------------------------------------- props (tinted per biome)
                case ArtId.PropFlower:
                    for (int i = 0; i < 5; i++) { float a = i * 72f * Mathf.Deg2Rad; c.Circle(0.5f + Mathf.Cos(a) * 0.18f, 0.55f + Mathf.Sin(a) * 0.18f, 0.14f, Color.white); }
                    c.Circle(0.5f, 0.55f, 0.11f, Gold);
                    break;
                case ArtId.PropGrass:
                    c.Triangle(new Vector2(0.2f, 0.15f), new Vector2(0.36f, 0.15f), new Vector2(0.24f, 0.7f), Color.white);
                    c.Triangle(new Vector2(0.4f, 0.15f), new Vector2(0.58f, 0.15f), new Vector2(0.5f, 0.85f), Color.white);
                    c.Triangle(new Vector2(0.62f, 0.15f), new Vector2(0.8f, 0.15f), new Vector2(0.76f, 0.65f), Color.white);
                    break;
                case ArtId.PropRock: c.Ellipse(0.5f, 0.4f, 0.4f, 0.28f, Color.white).Shade(0.5f, 0.45f, 0.35f, 1.5f); break;
                case ArtId.PropMushroom:
                    c.RoundRect(0.5f, 0.3f, 0.2f, 0.3f, 0.06f, Cream).Arc(0.5f, 0.45f, 0.18f, 0.3f, 0f, 180f, Color.white);
                    c.Circle(0.4f, 0.6f, 0.05f, Cream).Circle(0.58f, 0.66f, 0.04f, Cream);
                    break;
                case ArtId.PropReed:
                    c.Line(0.35f, 0.1f, 0.38f, 0.8f, 0.05f, C("#4A7A2A")).Line(0.6f, 0.1f, 0.58f, 0.7f, 0.05f, C("#4A7A2A"));
                    c.RoundRect(0.38f, 0.78f, 0.1f, 0.22f, 0.05f, Color.white).RoundRect(0.58f, 0.68f, 0.1f, 0.2f, 0.05f, Color.white);
                    break;
                case ArtId.PropLilyPad: c.Circle(0.5f, 0.5f, 0.38f, Color.white).Triangle(new Vector2(0.5f, 0.5f), new Vector2(0.92f, 0.62f), new Vector2(0.92f, 0.4f), Color.clear); c.Circle(0.62f, 0.6f, 0.08f, Pink); break;
                case ArtId.PropCactus:
                    c.RoundRect(0.5f, 0.45f, 0.24f, 0.7f, 0.12f, Color.white).RoundRect(0.28f, 0.55f, 0.14f, 0.3f, 0.07f, Color.white).RoundRect(0.72f, 0.6f, 0.14f, 0.26f, 0.07f, Color.white);
                    c.Circle(0.5f, 0.83f, 0.06f, Pink);
                    break;
                case ArtId.PropBones: c.Line(0.2f, 0.3f, 0.8f, 0.6f, 0.08f, Cream).Circle(0.2f, 0.34f, 0.07f, Cream).Circle(0.22f, 0.24f, 0.07f, Cream).Circle(0.8f, 0.56f, 0.07f, Cream).Circle(0.78f, 0.66f, 0.07f, Cream); break;
                case ArtId.PropSnowdrift: c.Ellipse(0.5f, 0.35f, 0.42f, 0.2f, Color.white).Circle(0.4f, 0.45f, 0.16f, Color.white).Circle(0.62f, 0.42f, 0.13f, Color.white); break;
                case ArtId.PropIceCrystal:
                    c.Polygon(new[] { new Vector2(0.5f, 0.92f), new Vector2(0.64f, 0.5f), new Vector2(0.5f, 0.1f), new Vector2(0.36f, 0.5f) }, Color.white);
                    c.Polygon(new[] { new Vector2(0.28f, 0.6f), new Vector2(0.36f, 0.35f), new Vector2(0.26f, 0.1f), new Vector2(0.18f, 0.35f) }, Color.white);
                    c.Line(0.5f, 0.85f, 0.5f, 0.2f, 0.04f, new Color(1, 1, 1, 0.6f));
                    break;
                case ArtId.PropPine:
                    c.Rect(0.45f, 0.05f, 0.55f, 0.25f, Brown);
                    c.Triangle(new Vector2(0.15f, 0.22f), new Vector2(0.85f, 0.22f), new Vector2(0.5f, 0.6f), Color.white);
                    c.Triangle(new Vector2(0.22f, 0.45f), new Vector2(0.78f, 0.45f), new Vector2(0.5f, 0.8f), Color.white);
                    c.Triangle(new Vector2(0.3f, 0.65f), new Vector2(0.7f, 0.65f), new Vector2(0.5f, 0.95f), Color.white);
                    break;
                case ArtId.PropLavaRock: c.Ellipse(0.5f, 0.4f, 0.4f, 0.28f, Color.white).Line(0.3f, 0.45f, 0.5f, 0.35f, 0.05f, Orange, true).Line(0.5f, 0.35f, 0.7f, 0.48f, 0.05f, Orange, true); break;
                case ArtId.PropEmber: c.Glow(0.5f, 0.5f, 0.45f, Orange, 1.2f).Circle(0.5f, 0.5f, 0.12f, Gold); break;
                case ArtId.PropCandyCane:
                    c.Line(0.45f, 0.1f, 0.45f, 0.7f, 0.12f, Color.white).Arc(0.6f, 0.7f, 0.15f, 0.12f, 0f, 180f, Color.white);
                    for (int i = 0; i < 4; i++) c.Line(0.38f, 0.15f + i * 0.15f, 0.52f, 0.22f + i * 0.15f, 0.04f, Red, true);
                    break;
                case ArtId.PropLollipop: c.Line(0.5f, 0.1f, 0.5f, 0.5f, 0.05f, Cream).Circle(0.5f, 0.66f, 0.26f, Color.white).Arc(0.5f, 0.66f, 0.14f, 0.06f, 0f, 300f, Pink, true); break;
                case ArtId.PropGumdrop: c.Arc(0.5f, 0.3f, 0.16f, 0.32f, 0f, 180f, Color.white).Rect(0.18f, 0.22f, 0.82f, 0.32f, Color.white).Shade(0.5f, 0.4f, 0.3f); break;
                case ArtId.PropBush: c.Circle(0.35f, 0.4f, 0.22f, Color.white).Circle(0.62f, 0.42f, 0.24f, Color.white).Circle(0.5f, 0.58f, 0.22f, Color.white).Shade(0.5f, 0.45f, 0.35f); break;
                case ArtId.PropTree:
                    c.RoundRect(0.5f, 0.2f, 0.14f, 0.3f, 0.04f, Brown);
                    c.Circle(0.5f, 0.6f, 0.3f, Color.white).Circle(0.3f, 0.5f, 0.18f, Color.white).Circle(0.7f, 0.5f, 0.18f, Color.white).Shade(0.5f, 0.6f, 0.3f);
                    break;
                case ArtId.PropSkull: c.Circle(0.5f, 0.55f, 0.28f, Cream).RoundRect(0.5f, 0.3f, 0.3f, 0.16f, 0.04f, Cream).Circle(0.4f, 0.55f, 0.07f, Ink).Circle(0.6f, 0.55f, 0.07f, Ink); break;
                case ArtId.Wall:
                    c.Rect(0, 0, 1, 1, Color.white);
                    c.Rect(0, 0.48f, 1, 0.52f, new Color(0, 0, 0, 0.18f), true).Rect(0.48f, 0.52f, 0.52f, 1f, new Color(0, 0, 0, 0.18f), true).Rect(0.0f, 0f, 0.04f, 0.48f, new Color(0, 0, 0, 0.18f), true);
                    c.Rect(0, 0.9f, 1, 1, new Color(1, 1, 1, 0.2f), true);
                    break;
                case ArtId.Crate:
                    c.RoundRect(0.5f, 0.5f, 0.84f, 0.84f, 0.06f, Color.white).Line(0.15f, 0.15f, 0.85f, 0.85f, 0.08f, new Color(0, 0, 0, 0.2f), true);
                    c.RoundRect(0.5f, 0.5f, 0.84f, 0.84f, 0.06f, new Color(0, 0, 0, 0f)).Shade(0.5f, 0.5f, 0.42f);
                    break;
                case ArtId.HazardPatch:
                    c.Circle(0.5f, 0.5f, 0.42f, Color.white).Circle(0.3f, 0.35f, 0.2f, Color.white).Circle(0.72f, 0.62f, 0.2f, Color.white).Circle(0.62f, 0.3f, 0.18f, Color.white);
                    break;

                // ---------------------------------------------------------------- ui
                case ArtId.UiNode: c.Circle(0.5f, 0.5f, 0.46f, Color.white); break;
                case ArtId.UiCard: c.RoundRect(0.5f, 0.5f, 0.96f, 0.96f, 0.12f, Color.white); break;
                case ArtId.UiLock:
                    c.Arc(0.5f, 0.55f, 0.18f, 0.08f, 0f, 180f, Grey).RoundRect(0.5f, 0.38f, 0.5f, 0.38f, 0.06f, Gold).Circle(0.5f, 0.4f, 0.05f, Ink);
                    break;
                case ArtId.UiCheck: c.Circle(0.5f, 0.5f, 0.4f, C("#3FD16A")).Line(0.3f, 0.5f, 0.45f, 0.35f, 0.09f, Color.white).Line(0.45f, 0.35f, 0.72f, 0.66f, 0.09f, Color.white); break;
                case ArtId.UiSkull: DrawInto(c, ArtId.PropSkull); break;
                case ArtId.UiStar: c.Star(0.5f, 0.5f, 0.44f, 0.2f, 5, Gold); break;
                case ArtId.UiQuestion: c.Circle(0.5f, 0.5f, 0.42f, DarkGrey).Arc(0.5f, 0.6f, 0.13f, 0.08f, -60f, 180f, Color.white).Line(0.56f, 0.5f, 0.5f, 0.4f, 0.08f, Color.white).Circle(0.5f, 0.24f, 0.05f, Color.white); break;
                case ArtId.UiGoldIcon: DrawInto(c, ArtId.Coin); break;
                case ArtId.UiVignette:
                    for (int y = 0; y < c.H; y++)
                    for (int x = 0; x < c.W; x++)
                    {
                        float dx = (x + 0.5f) / c.W - 0.5f, dy = (y + 0.5f) / c.H - 0.5f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                        c.Px[y * c.W + x] = new Color(1, 1, 1, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.95f, d)));
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ building blocks

        static void Hero(PixelCanvas c, bool step)
        {
            float lift = step ? 0.02f : 0f;
            // Legs + sneakers
            c.RoundRect(0.41f, 0.17f + lift, 0.12f, 0.16f, 0.04f, Jeans).RoundRect(0.59f, 0.17f - lift, 0.12f, 0.16f, 0.04f, Jeans);
            c.RoundRect(0.39f, 0.08f + lift, 0.16f, 0.08f, 0.04f, Shoe).RoundRect(0.61f, 0.08f - lift, 0.16f, 0.08f, 0.04f, Shoe);
            // Body (t-shirt) + arms
            c.RoundRect(0.5f, 0.32f, 0.34f, 0.2f, 0.08f, Shirt);
            c.RoundRect(0.3f, 0.3f, 0.1f, 0.16f, 0.05f, Skin, 15f).RoundRect(0.7f, 0.3f, 0.1f, 0.16f, 0.05f, Skin, -15f);
            c.Rect(0.25f, 0.33f, 0.35f, 0.4f, Shirt).Rect(0.65f, 0.33f, 0.75f, 0.4f, Shirt);
            c.Circle(0.5f, 0.42f, 0.04f, new Color(1, 1, 1, 0.8f));
            // Big head
            c.Circle(0.5f, 0.64f, 0.27f, Skin);
            c.Ellipse(0.5f, 0.8f, 0.27f, 0.14f, Hair).Circle(0.3f, 0.72f, 0.08f, Hair).Circle(0.7f, 0.74f, 0.07f, Hair);
            c.Ellipse(0.6f, 0.87f, 0.1f, 0.06f, Hair, 20f);
            c.Eye(0.41f, 0.62f, 0.065f).Eye(0.59f, 0.62f, 0.065f);
            c.Ellipse(0.33f, 0.54f, 0.05f, 0.03f, new Color(1f, 0.55f, 0.55f, 0.6f)).Ellipse(0.67f, 0.54f, 0.05f, 0.03f, new Color(1f, 0.55f, 0.55f, 0.6f));
            c.Arc(0.5f, 0.55f, 0.05f, 0.02f, 200f, 340f, Ink);
        }

        static void Gem(PixelCanvas c, Color col, float size)
        {
            float r = 0.38f * size;
            c.Polygon(new[] { new Vector2(0.5f, 0.5f + r), new Vector2(0.5f + r * 0.75f, 0.5f), new Vector2(0.5f, 0.5f - r), new Vector2(0.5f - r * 0.75f, 0.5f) }, col);
            c.Polygon(new[] { new Vector2(0.5f, 0.5f + r), new Vector2(0.5f + r * 0.75f, 0.5f), new Vector2(0.5f, 0.5f) }, new Color(1, 1, 1, 0.35f));
            c.Polygon(new[] { new Vector2(0.5f, 0.5f - r), new Vector2(0.5f - r * 0.75f, 0.5f), new Vector2(0.5f, 0.5f) }, new Color(0, 0, 0, 0.2f));
        }

        static void Heart(PixelCanvas c, float cx, float cy, float r, Color col)
        {
            c.Circle(cx - r * 0.42f, cy + r * 0.25f, r * 0.48f, col).Circle(cx + r * 0.42f, cy + r * 0.25f, r * 0.48f, col);
            c.Triangle(new Vector2(cx - r * 0.88f, cy + r * 0.12f), new Vector2(cx + r * 0.88f, cy + r * 0.12f), new Vector2(cx, cy - r * 0.85f), col);
        }

        static void Duck(PixelCanvas c, Color body, Color beak)
        {
            c.Ellipse(0.45f, 0.36f, 0.32f, 0.22f, body).Circle(0.62f, 0.62f, 0.18f, body);
            c.Ellipse(0.82f, 0.58f, 0.1f, 0.05f, beak).Eye(0.65f, 0.67f, 0.05f, new Vector2(1, 0));
            c.Ellipse(0.38f, 0.4f, 0.14f, 0.08f, PixelCanvas.Dark(body, 0.85f), 15f).Shade(0.45f, 0.42f, 0.3f, 0.7f);
        }

        static void Teddy(PixelCanvas c, Color fur, bool robo)
        {
            c.Circle(0.3f, 0.82f, 0.1f, fur).Circle(0.7f, 0.82f, 0.1f, fur);
            c.Ellipse(0.5f, 0.3f, 0.28f, 0.24f, fur).Circle(0.5f, 0.64f, 0.24f, fur);
            c.Ellipse(0.5f, 0.57f, 0.1f, 0.07f, robo ? C("#D9DEE8") : Cream).Circle(0.5f, 0.6f, 0.03f, Ink);
            if (robo)
            {
                c.Rect(0.36f, 0.66f, 0.64f, 0.74f, C("#3ED6C0")).Circle(0.42f, 0.7f, 0.03f, Red).Circle(0.58f, 0.7f, 0.03f, Red);
                c.Line(0.5f, 0.88f, 0.5f, 0.98f, 0.03f, DarkGrey).Circle(0.5f, 0.97f, 0.03f, Red);
            }
            else c.Eye(0.42f, 0.7f, 0.045f).Eye(0.58f, 0.7f, 0.045f);
            c.Ellipse(0.5f, 0.28f, 0.14f, 0.12f, robo ? C("#D9DEE8") : Cream);
        }

        static void Blaster(PixelCanvas c, Color body, Color dark)
        {
            c.RoundRect(0.48f, 0.6f, 0.62f, 0.2f, 0.08f, body).RoundRect(0.3f, 0.4f, 0.16f, 0.32f, 0.05f, dark, -15f);
            c.Circle(0.78f, 0.6f, 0.08f, dark).Shade(0.48f, 0.6f, 0.3f);
        }

        static void Rod(PixelCanvas c, Color body, Color tip)
        {
            c.Line(0.2f, 0.2f, 0.66f, 0.66f, 0.08f, body).Circle(0.2f, 0.2f, 0.06f, tip);
        }

        static void Sock(PixelCanvas c, Color body, Color stripe, float scale = 1f)
        {
            float k = scale;
            c.RoundRect(0.42f, 0.55f * k + 0.1f * (1 - k), 0.26f * k, 0.5f * k, 0.08f * k, body);
            c.RoundRect(0.5f, 0.3f * k + 0.1f * (1 - k), 0.44f * k, 0.22f * k, 0.1f * k, body);
            c.Rect(0.29f, (0.68f * k), 0.55f, (0.74f * k), stripe, true);
        }

        static void Glove(PixelCanvas c, Color col)
        {
            c.RoundRect(0.5f, 0.45f, 0.52f, 0.5f, 0.14f, col).RoundRect(0.22f, 0.55f, 0.12f, 0.26f, 0.06f, col, 30f);
            c.RoundRect(0.5f, 0.15f, 0.48f, 0.16f, 0.04f, PixelCanvas.Light(col, 0.4f)).Shade(0.5f, 0.45f, 0.3f);
        }

        static void Clover(PixelCanvas c, float cx, float cy, float r, Color col)
        {
            c.Circle(cx - r * 0.7f, cy, r * 0.8f, col).Circle(cx + r * 0.7f, cy, r * 0.8f, col).Circle(cx, cy + r * 0.7f, r * 0.8f, col).Circle(cx, cy - r * 0.6f, r * 0.8f, col);
        }

        static void Cloud(PixelCanvas c, Color col)
        {
            var solid = new Color(col.r, col.g, col.b, 1f);
            var tmp = new PixelCanvas(c.W, c.H);
            tmp.Circle(0.5f, 0.5f, 0.32f, solid).Circle(0.28f, 0.45f, 0.2f, solid).Circle(0.72f, 0.45f, 0.2f, solid).Circle(0.4f, 0.7f, 0.18f, solid).Circle(0.64f, 0.68f, 0.18f, solid).Circle(0.5f, 0.28f, 0.2f, solid);
            for (int i = 0; i < c.Px.Length; i++) { var p = tmp.Px[i]; p.a *= col.a; c.Px[i] = p; }
        }

        // ------------------------------------------------------------------ enemies

        public static int EnemySize(EnemyDefinition e) => e.tier switch
        {
            EnemyTier.Boss => 224,
            EnemyTier.MiniBoss => 144,
            _ => 80,
        };

        public static PixelCanvas DrawEnemy(EnemyDefinition e)
        {
            int s = EnemySize(e);
            var c = new PixelCanvas(s, s);
            Color p = e.primary, q = e.secondary, a = e.accent;
            var f = e.features;
            bool has(BodyFeature x) => (f & x) != 0;

            // Behind-body features
            if (has(BodyFeature.Wings))
            {
                c.Ellipse(0.2f, 0.62f, 0.18f, 0.12f, new Color(0.9f, 0.95f, 1f, 0.85f), 25f);
                c.Ellipse(0.8f, 0.62f, 0.18f, 0.12f, new Color(0.9f, 0.95f, 1f, 0.85f), -25f);
            }
            if (has(BodyFeature.Petals))
                for (int i = 0; i < 8; i++) { float ang = i * 45f * Mathf.Deg2Rad; c.Ellipse(0.5f + Mathf.Cos(ang) * 0.3f, 0.52f + Mathf.Sin(ang) * 0.3f, 0.14f, 0.09f, q, i * 45f); }
            if (has(BodyFeature.Tail)) c.Line(0.72f, 0.3f, 0.92f, 0.5f, 0.07f, p).Circle(0.92f, 0.52f, 0.06f, a);
            if (has(BodyFeature.Legs))
                for (int i = 0; i < 3; i++) { c.Line(0.3f, 0.3f + i * 0.1f, 0.1f, 0.2f + i * 0.12f, 0.035f, PixelCanvas.Dark(p)); c.Line(0.7f, 0.3f + i * 0.1f, 0.9f, 0.2f + i * 0.12f, 0.035f, PixelCanvas.Dark(p)); }
            if (has(BodyFeature.Spikes))
                for (int i = 0; i < 7; i++) { float ang = (25f + i * 22f) * Mathf.Deg2Rad; var d = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)); c.Triangle(new Vector2(0.5f, 0.45f) + d * 0.28f + new Vector2(-d.y, d.x) * 0.06f, new Vector2(0.5f, 0.45f) + d * 0.28f - new Vector2(-d.y, d.x) * 0.06f, new Vector2(0.5f, 0.45f) + d * 0.45f, a); }

            // Body
            float cx = 0.5f, cy = 0.45f, r = 0.32f;
            switch (e.body)
            {
                case BodyShape.Blob:
                    c.Ellipse(0.5f, 0.38f, 0.38f, 0.26f, p).Circle(0.5f, 0.5f, 0.28f, p);
                    cy = 0.45f; r = 0.33f; break;
                case BodyShape.Round:
                    c.Circle(0.5f, 0.47f, 0.36f, p); cy = 0.47f; r = 0.36f; break;
                case BodyShape.Bug:
                    c.Ellipse(0.5f, 0.42f, 0.34f, 0.3f, p).Circle(0.5f, 0.7f, 0.17f, PixelCanvas.Dark(p, 0.6f));
                    c.Line(0.5f, 0.15f, 0.5f, 0.68f, 0.03f, PixelCanvas.Dark(p, 0.5f), true);
                    cy = 0.68f; r = 0.18f; break;
                case BodyShape.Tall:
                    c.RoundRect(0.5f, 0.45f, 0.5f, 0.78f, 0.22f, p); cy = 0.55f; r = 0.28f; break;
                case BodyShape.Ghost:
                    c.Circle(0.5f, 0.56f, 0.32f, p).Rect(0.18f, 0.22f, 0.82f, 0.56f, p);
                    for (int i = 0; i < 4; i++) c.Circle(0.26f + i * 0.16f, 0.22f, 0.08f, p);
                    cy = 0.55f; r = 0.32f; break;
                case BodyShape.Worm:
                    c.Circle(0.25f, 0.3f, 0.15f, q).Circle(0.42f, 0.36f, 0.17f, p).Circle(0.6f, 0.42f, 0.19f, q).Circle(0.68f, 0.58f, 0.2f, p);
                    cx = 0.68f; cy = 0.6f; r = 0.2f; break;
                case BodyShape.Square:
                    c.RoundRect(0.5f, 0.45f, 0.7f, 0.66f, 0.12f, p); r = 0.33f; break;
                case BodyShape.Star:
                    c.Star(0.5f, 0.47f, 0.45f, 0.28f, 5, p); cy = 0.47f; r = 0.28f; break;
                case BodyShape.Bird:
                    c.Ellipse(0.5f, 0.42f, 0.34f, 0.28f, p).Triangle(new Vector2(0.2f, 0.55f), new Vector2(0.05f, 0.75f), new Vector2(0.3f, 0.65f), q).Triangle(new Vector2(0.8f, 0.55f), new Vector2(0.95f, 0.75f), new Vector2(0.7f, 0.65f), q);
                    c.Triangle(new Vector2(0.45f, 0.38f), new Vector2(0.55f, 0.38f), new Vector2(0.5f, 0.26f), Gold);
                    cy = 0.5f; r = 0.28f; break;
                case BodyShape.Flower:
                    c.Line(0.5f, 0.05f, 0.5f, 0.4f, 0.06f, C("#3FAE2A")).Ellipse(0.36f, 0.18f, 0.1f, 0.05f, C("#3FAE2A"), 30f);
                    c.Circle(0.5f, 0.52f, 0.27f, p); cy = 0.52f; r = 0.27f; break;
                case BodyShape.Cone:
                    c.Polygon(new[] { new Vector2(0.18f, 0.12f), new Vector2(0.82f, 0.12f), new Vector2(0.62f, 0.78f), new Vector2(0.38f, 0.78f) }, p);
                    c.Circle(0.5f, 0.8f, 0.14f, q);
                    cy = 0.45f; r = 0.26f; break;
                case BodyShape.Snowman:
                    c.Circle(0.5f, 0.3f, 0.26f, p).Circle(0.5f, 0.64f, 0.2f, p).Triangle(new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.56f), new Vector2(0.66f, 0.58f), Orange);
                    cy = 0.64f; r = 0.2f; break;
            }

            // Markings
            if (has(BodyFeature.Stripes))
                for (int i = 0; i < 3; i++) c.Rect(0.05f, 0.25f + i * 0.14f, 0.95f, 0.31f + i * 0.14f, a, true);
            if (has(BodyFeature.Shell)) c.Circle(0.5f, 0.38f, 0.26f, q, true).Ring(0.5f, 0.38f, 0.16f, 0.04f, PixelCanvas.Dark(q), true).Circle(0.5f, 0.38f, 0.06f, PixelCanvas.Dark(q), true);
            if (has(BodyFeature.Sprinkles))
            {
                var rnd = new System.Random(e.id != null ? e.id.GetHashCode() : 1);
                for (int i = 0; i < 9; i++)
                {
                    float x = 0.25f + (float)rnd.NextDouble() * 0.5f, y = 0.25f + (float)rnd.NextDouble() * 0.45f;
                    float ang = (float)rnd.NextDouble() * 180f;
                    var col = i % 3 == 0 ? Pink : i % 3 == 1 ? Teal : Gold;
                    c.RoundRect(x, y, 0.08f, 0.025f, 0.012f, col, ang, true);
                }
            }
            if (has(BodyFeature.Bandage))
                for (int i = 0; i < 3; i++) c.RoundRect(0.5f, 0.3f + i * 0.17f, 0.9f, 0.06f, 0.02f, PixelCanvas.Light(p, 0.5f), i * 8f - 8f, true);
            c.Shade(0.5f, 0.45f, 0.36f);

            // Head features
            if (has(BodyFeature.Ears)) { c.Circle(cx - r * 0.75f, cy + r * 0.85f, r * 0.3f, p).Circle(cx + r * 0.75f, cy + r * 0.85f, r * 0.3f, p); }
            if (has(BodyFeature.Horns))
            {
                c.Triangle(new Vector2(cx - r * 0.7f, cy + r * 0.6f), new Vector2(cx - r * 0.3f, cy + r * 0.8f), new Vector2(cx - r * 0.85f, cy + r * 1.35f), Cream);
                c.Triangle(new Vector2(cx + r * 0.7f, cy + r * 0.6f), new Vector2(cx + r * 0.3f, cy + r * 0.8f), new Vector2(cx + r * 0.85f, cy + r * 1.35f), Cream);
            }
            if (has(BodyFeature.Antennae))
            {
                c.Line(cx - r * 0.3f, cy + r * 0.8f, cx - r * 0.6f, cy + r * 1.45f, 0.025f, Ink).Circle(cx - r * 0.6f, cy + r * 1.45f, 0.035f, a);
                c.Line(cx + r * 0.3f, cy + r * 0.8f, cx + r * 0.6f, cy + r * 1.45f, 0.025f, Ink).Circle(cx + r * 0.6f, cy + r * 1.45f, 0.035f, a);
            }
            if (has(BodyFeature.Crown))
                c.Polygon(new[] { new Vector2(cx - r * 0.55f, cy + r * 0.8f), new Vector2(cx + r * 0.55f, cy + r * 0.8f), new Vector2(cx + r * 0.6f, cy + r * 1.35f), new Vector2(cx + r * 0.25f, cy + r * 1.08f), new Vector2(cx, cy + r * 1.42f), new Vector2(cx - r * 0.25f, cy + r * 1.08f), new Vector2(cx - r * 0.6f, cy + r * 1.35f) }, Gold);
            if (has(BodyFeature.Hat))
            {
                c.Ellipse(cx, cy + r * 0.85f, r * 0.85f, r * 0.18f, a);
                c.RoundRect(cx, cy + r * 1.15f, r * 0.9f, r * 0.55f, r * 0.12f, a);
            }

            // Face
            if (has(BodyFeature.BigEye)) c.Eye(cx, cy + r * 0.1f, r * 0.42f, Vector2.down, has(BodyFeature.Angry));
            else if (has(BodyFeature.Eyes))
            {
                float er = Mathf.Max(0.045f, r * 0.2f);
                c.Eye(cx - r * 0.36f, cy + r * 0.12f, er, Vector2.down, has(BodyFeature.Angry)).Eye(cx + r * 0.36f, cy + r * 0.12f, er, Vector2.down, has(BodyFeature.Angry));
            }
            if (has(BodyFeature.Cheeks))
            {
                c.Ellipse(cx - r * 0.62f, cy - r * 0.18f, r * 0.15f, r * 0.09f, new Color(1f, 0.45f, 0.55f, 0.65f));
                c.Ellipse(cx + r * 0.62f, cy - r * 0.18f, r * 0.15f, r * 0.09f, new Color(1f, 0.45f, 0.55f, 0.65f));
            }
            if (has(BodyFeature.Teeth))
            {
                c.Ellipse(cx, cy - r * 0.4f, r * 0.4f, r * 0.2f, Ink);
                c.Triangle(new Vector2(cx - r * 0.3f, cy - r * 0.27f), new Vector2(cx - r * 0.1f, cy - r * 0.27f), new Vector2(cx - r * 0.2f, cy - r * 0.45f), Color.white);
                c.Triangle(new Vector2(cx + r * 0.1f, cy - r * 0.27f), new Vector2(cx + r * 0.3f, cy - r * 0.27f), new Vector2(cx + r * 0.2f, cy - r * 0.45f), Color.white);
            }
            else if (has(BodyFeature.Eyes) || has(BodyFeature.BigEye))
                c.Arc(cx, cy - r * 0.2f, r * 0.18f, Mathf.Max(0.02f, r * 0.07f), has(BodyFeature.Angry) ? 20f : 200f, has(BodyFeature.Angry) ? 160f : 340f, Ink);
            if (has(BodyFeature.Shine)) c.Star(cx + r * 0.6f, cy + r * 0.65f, r * 0.2f, r * 0.07f, 4, Color.white);

            c.Outline(Mathf.Max(2.5f, s / 26f), Ink);
            return c;
        }
    }
}
