using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static MiniMayhem.EditorTools.BuilderUtil;

namespace MiniMayhem.EditorTools
{
    /// <summary>Content draft v0 for the 12 weapons, their 12 evolutions, 4 fusions and 19 items (GAME_PLAN 22.2-22.4).</summary>
    internal static class WeaponData
    {
        const string WeaponDir = SoRoot + "/Weapons";
        const string ItemDir = SoRoot + "/Items";
        const string RecipeDir = SoRoot + "/Recipes";

        // Global balance knobs applied to the tables below (first playtest pass).
        const float DamageScale = 1.35f, CooldownScale = 0.85f;

        static WeaponLevel Lv(float dmg, float cd, int amt, float area, float speed, float dur, int pierce, float kb, string note) =>
            new() { damage = Mathf.Round(dmg * DamageScale), cooldown = cd * CooldownScale, amount = amt, area = area, speed = speed, duration = dur, pierce = pierce, knockback = kb, note = note };

        static WeaponDefinition W(string id, string name, string desc, WeaponArchetype arch, TargetMode target, WeaponTags tags, WeaponForm form,
            ArtId icon, ArtId proj, string c0, string c1, bool starts, int unlockBiome, WeaponBehaviour b, params WeaponLevel[] levels)
        {
            return LoadOrCreate<WeaponDefinition>($"{WeaponDir}/{(form == WeaponForm.Base ? "" : form + "/")}{id}.asset", w =>
            {
                w.id = id; w.displayName = name; w.description = desc; w.archetype = arch; w.targeting = target; w.tags = tags; w.form = form;
                w.icon = icon; w.projectileArt = proj; w.color = Hex(c0); w.colorMax = Hex(c1); w.startsUnlocked = starts; w.unlockBiome = unlockBiome;
                w.behaviour = b; w.levels = levels;
            });
        }

        public static void Build(GameDatabase db)
        {
            const WeaponForm Base = WeaponForm.Base, Evo = WeaponForm.Evolved, Fus = WeaponForm.Fused;

            // ---------------------------------------------------------------- base weapons
            var pea = W("pea_shooter", "Pea Shooter", "Fires peas at the nearest enemy.", WeaponArchetype.Projectile, TargetMode.Nearest,
                WeaponTags.Projectile | WeaponTags.Physical, Base, ArtId.IconPeaShooter, ArtId.Pea, "#7BD94B", "#2EE6A0", true, -1,
                new WeaponBehaviour { range = 9f, spread = 10f },
                Lv(10, 0.9f, 1, 1, 12, 1, 0, 0.5f, ""), Lv(14, 0.9f, 1, 1, 12, 1, 0, 0.5f, "+40% damage."), Lv(14, 0.9f, 2, 1, 12, 1, 0, 0.5f, "+1 pea."),
                Lv(14, 0.7f, 2, 1, 13, 1, 0, 0.5f, "Fires faster."), Lv(22, 0.7f, 3, 1.15f, 14, 1, 1, 0.7f, "Big spike: +1 pea, bigger, pierces 1."));
            var boom = W("boomerang", "Boomerang", "Thrown out and back, hitting enemies both ways.", WeaponArchetype.Boomerang, TargetMode.Nearest,
                WeaponTags.Projectile | WeaponTags.Physical, Base, ArtId.IconBoomerang, ArtId.Boomerang, "#FF9A3C", "#FF4D6D", false, 0,
                new WeaponBehaviour { range = 8f, spread = 28f },
                Lv(14, 1.6f, 1, 1, 10, 1.3f, 0, 1, ""), Lv(19, 1.6f, 1, 1, 10, 1.3f, 0, 1, "+35% damage."), Lv(19, 1.6f, 2, 1, 10, 1.3f, 0, 1, "+1 boomerang."),
                Lv(19, 1.3f, 2, 1, 11, 1.3f, 0, 1, "Thrown more often."), Lv(27, 1.3f, 2, 1.3f, 11, 1.5f, 0, 1.2f, "Big spike: bigger, longer flight."));
            var pan = W("frying_pan", "Frying Pan", "Swings an arc in front of you with a little knockback.", WeaponArchetype.MeleeArc, TargetMode.Nearest,
                WeaponTags.Melee | WeaponTags.Physical, Base, ArtId.IconFryingPan, ArtId.Swoosh, "#C7CCD8", "#FFB347", true, -1,
                new WeaponBehaviour { range = 4f, arcDegrees = 150f },
                Lv(18, 1.1f, 1, 1, 0, 0, 0, 2, ""), Lv(24, 1.1f, 1, 1, 0, 0, 0, 2, "+33% damage."), Lv(24, 1.1f, 1, 1.25f, 0, 0, 0, 2.2f, "Bigger swing."),
                Lv(24, 0.9f, 1, 1.25f, 0, 0, 0, 2.2f, "Swings faster."), Lv(34, 0.9f, 2, 1.3f, 0, 0, 0, 2.5f, "Big spike: swings front AND back."));
            var duck = W("duck_orbit", "Duck Orbit", "Rubber ducks circle around you, bonking anything they touch.", WeaponArchetype.Orbit, TargetMode.AllAround,
                WeaponTags.Physical | WeaponTags.Water, Base, ArtId.IconDuckOrbit, ArtId.Duck, "#FFE14D", "#FFB000", true, -1,
                new WeaponBehaviour { range = 3f, hitCooldown = 0.5f },
                Lv(8, 3, 2, 1, 3, 0, 0, 1, ""), Lv(11, 3, 2, 1, 3, 0, 0, 1, "+35% damage."), Lv(11, 3, 3, 1, 3, 0, 0, 1, "+1 duck."),
                Lv(11, 3, 3, 1, 4, 0, 0, 1.2f, "Ducks spin faster."), Lv(15, 3, 4, 1.2f, 4, 0, 0, 1.4f, "Big spike: +1 duck, wider orbit."));
            var zap = W("zap_rod", "Zap Rod", "Chain lightning jumps between nearby enemies.", WeaponArchetype.Chain, TargetMode.RandomInRange,
                WeaponTags.Electric | WeaponTags.Magic, Base, ArtId.IconZapRod, ArtId.Bolt, "#A88BFF", "#7FF3FF", false, 0,
                new WeaponBehaviour { range = 7.5f, chains = 2 },
                Lv(16, 1.4f, 1, 1, 0, 0, 0, 0.3f, ""), Lv(20, 1.4f, 1, 1, 0, 0, 0, 0.3f, "+25% damage."), Lv(20, 1.4f, 1, 1, 0, 0, 1, 0.3f, "+1 chain."),
                Lv(20, 1.1f, 1, 1, 0, 0, 1, 0.3f, "Zaps more often."), Lv(28, 1.1f, 2, 1.1f, 0, 0, 1, 0.4f, "Big spike: two bolts at once."));
            var balloon = W("water_balloon", "Water Balloon", "Lobbed in an arc; the splash leaves a puddle that slows enemies.", WeaponArchetype.Lob, TargetMode.RandomInRange,
                WeaponTags.Water | WeaponTags.Area, Base, ArtId.IconWaterBalloon, ArtId.Balloon, "#4FB6FF", "#2E6BFF", false, 1,
                new WeaponBehaviour { range = 8.5f, slow = 0.4f },
                Lv(12, 2.0f, 1, 1, 0, 2.5f, 0, 0.5f, ""), Lv(16, 2.0f, 1, 1, 0, 2.5f, 0, 0.5f, "+33% damage."), Lv(16, 2.0f, 2, 1, 0, 2.5f, 0, 0.5f, "+1 balloon."),
                Lv(16, 1.8f, 2, 1.3f, 0, 2.5f, 0, 0.6f, "Bigger splash."), Lv(22, 1.8f, 2, 1.35f, 0, 4f, 0, 0.7f, "Big spike: puddles last much longer."));
            var star = W("star_wand", "Star Wand", "Shoots homing stars at enemies.", WeaponArchetype.Projectile, TargetMode.Nearest,
                WeaponTags.Magic | WeaponTags.Projectile, Base, ArtId.IconStarWand, ArtId.Star, "#FFD23F", "#FF8FC7", true, -1,
                new WeaponBehaviour { range = 10f, homing = 6f, spread = 25f },
                Lv(9, 1.0f, 1, 1, 9, 2, 0, 0.3f, ""), Lv(12, 1.0f, 1, 1, 9, 2, 0, 0.3f, "+33% damage."), Lv(12, 1.0f, 2, 1, 9, 2, 0, 0.3f, "+1 star."),
                Lv(12, 0.8f, 2, 1, 10, 2, 0, 0.3f, "Casts faster."), Lv(16, 0.8f, 3, 1.15f, 10, 2, 0, 0.4f, "Big spike: +1 star."));
            var bat = W("baseball_bat", "Baseball Bat", "A heavy swing with BIG knockback.", WeaponArchetype.MeleeArc, TargetMode.Nearest,
                WeaponTags.Melee | WeaponTags.Physical, Base, ArtId.IconBaseballBat, ArtId.Swoosh, "#D9A066", "#FF6B5B", false, 1,
                new WeaponBehaviour { range = 4f, arcDegrees = 120f },
                Lv(30, 1.8f, 1, 1.1f, 0, 0, 0, 6, ""), Lv(40, 1.8f, 1, 1.1f, 0, 0, 0, 6, "+33% damage."), Lv(40, 1.8f, 1, 1.3f, 0, 0, 0, 9, "Bigger swing, more knockback."),
                Lv(40, 1.4f, 1, 1.3f, 0, 0, 0, 9, "Swings faster."), Lv(55, 1.4f, 1, 1.35f, 0, 0, 0, 10, "Big spike: +40% damage."));
            var socks = W("stinky_socks", "Stinky Socks", "A smelly aura that hurts every enemy close to you.", WeaponArchetype.Aura, TargetMode.AllAround,
                WeaponTags.Poison | WeaponTags.Area, Base, ArtId.IconStinkySocks, ArtId.StinkCloud, "#B7E05A", "#7CFF4A", false, 2,
                new WeaponBehaviour { range = 3f },
                Lv(5, 0.5f, 1, 1, 0, 0, 0, 0.5f, ""), Lv(7, 0.5f, 1, 1, 0, 0, 0, 0.5f, "+40% damage."), Lv(7, 0.5f, 1, 1.3f, 0, 0, 0, 0.5f, "Bigger stink cloud."),
                Lv(7, 0.4f, 1, 1.3f, 0, 0, 0, 0.5f, "Ticks faster."), Lv(10, 0.4f, 1, 1.5f, 0, 0, 0, 0.6f, "Big spike: bigger and nastier."));
            var teddy = W("teddy_turret", "Teddy Turret", "Drops a teddy bear that shoots nearby enemies for a while.", WeaponArchetype.Turret, TargetMode.Nearest,
                WeaponTags.Summon | WeaponTags.Projectile, Base, ArtId.IconTeddyTurret, ArtId.Teddy, "#C48A52", "#FF8FC7", false, 2,
                new WeaponBehaviour { range = 7.5f, tickInterval = 0.6f },
                Lv(8, 7, 1, 1, 0, 6, 0, 0.3f, ""), Lv(11, 7, 1, 1, 0, 6, 0, 0.3f, "+35% damage."), Lv(11, 7, 2, 1, 0, 6, 0, 0.3f, "+1 teddy."),
                Lv(11, 5, 2, 1, 0, 8, 0, 0.3f, "Teddies drop sooner and stay longer."), Lv(15, 5, 2, 1, 0, 9, 1, 0.4f, "Big spike: shots pierce."));
            var cracker = W("firecracker_trap", "Firecracker Trap", "Places firecrackers that blow up when enemies get close.", WeaponArchetype.Trap, TargetMode.AllAround,
                WeaponTags.Fire | WeaponTags.Trap | WeaponTags.Area, Base, ArtId.IconFirecrackerTrap, ArtId.Firecracker, "#FF4D4D", "#FFB000", false, 3,
                new WeaponBehaviour { range = 4f },
                Lv(25, 2.5f, 4, 1, 0, 12, 0, 2, ""), Lv(32, 2.5f, 4, 1, 0, 12, 0, 2, "+28% damage."), Lv(32, 2.5f, 6, 1, 0, 12, 0, 2, "+2 traps at once."),
                Lv(32, 1.8f, 6, 1, 0, 12, 0, 2, "Placed faster."), Lv(45, 1.8f, 6, 1.3f, 0, 12, 0, 2.5f, "Big spike: bigger blasts."));
            var laser = W("laser_pointer", "Laser Pointer", "A continuous beam that sweeps across enemies.", WeaponArchetype.Beam, TargetMode.Nearest,
                WeaponTags.Beam | WeaponTags.Fire, Base, ArtId.IconLaserPointer, ArtId.Beam, "#FF4D6D", "#FF2EC4", false, 4,
                new WeaponBehaviour { range = 9f, tickInterval = 0.15f },
                Lv(6, 3, 1, 1, 0, 2, 0, 0.2f, ""), Lv(8, 3, 1, 1, 0, 2, 0, 0.2f, "+33% damage."), Lv(8, 3, 1, 1.4f, 0, 2, 0, 0.2f, "Wider beam."),
                Lv(8, 3, 1, 1.4f, 0, 3, 0, 0.2f, "Beam lasts longer."), Lv(11, 3, 2, 1.45f, 0, 3, 0, 0.3f, "Big spike: two beams."));

            // ---------------------------------------------------------------- evolutions
            var peaCannon = W("pea_cannon", "Pea Cannon", "Huge peas that plough through everything.", WeaponArchetype.Projectile, TargetMode.Nearest,
                WeaponTags.Projectile | WeaponTags.Physical, Evo, ArtId.IconPeaCannon, ArtId.PeaBig, "#3FAE2A", "#3FAE2A", false, -1,
                new WeaponBehaviour { range = 10f, spread = 12f }, Lv(45, 0.8f, 3, 2.0f, 14, 1, 99, 2, ""));
            var buzzsaw = W("buzzsaw_boomerang", "Buzzsaw Boomerang", "A spinning saw that bounces from enemy to enemy before coming back.", WeaponArchetype.Boomerang, TargetMode.Nearest,
                WeaponTags.Projectile | WeaponTags.Physical, Evo, ArtId.IconBuzzsaw, ArtId.Buzzsaw, "#E8ECF4", "#E8ECF4", false, -1,
                new WeaponBehaviour { range = 9f, spread = 30f, bounces = 5 }, Lv(36, 1.2f, 3, 1.4f, 12, 1.6f, 999, 1.2f, ""));
            var castIron = W("cast_iron_slam", "Cast Iron Slam", "Front and back slams that send out a shockwave ring.", WeaponArchetype.MeleeArc, TargetMode.Nearest,
                WeaponTags.Melee | WeaponTags.Physical | WeaponTags.Area, Evo, ArtId.IconCastIron, ArtId.Swoosh, "#FFB347", "#FFB347", false, -1,
                new WeaponBehaviour { range = 4.5f, arcDegrees = 170f, shockwave = true, explodeRadius = 3f }, Lv(60, 1.0f, 2, 1.5f, 0, 0, 0, 3, ""));
            var goldDuck = W("golden_duck_parade", "Golden Duck Parade", "Six golden ducks race around you and burst outward.", WeaponArchetype.Orbit, TargetMode.AllAround,
                WeaponTags.Physical | WeaponTags.Water, Evo, ArtId.IconGoldDuck, ArtId.GoldDuck, "#FFC21A", "#FFC21A", false, -1,
                new WeaponBehaviour { range = 4f, hitCooldown = 0.4f, pulses = true }, Lv(24, 3, 6, 1.5f, 5, 0, 0, 1.6f, ""));
            var thunder = W("thunder_rod", "Thunder Rod", "Longer chains, plus a storm that strikes enemies around you.", WeaponArchetype.Chain, TargetMode.RandomInRange,
                WeaponTags.Electric | WeaponTags.Magic | WeaponTags.Area, Evo, ArtId.IconThunderRod, ArtId.Bolt, "#BFD4FF", "#BFD4FF", false, -1,
                new WeaponBehaviour { range = 9f, chains = 5, stormStrikes = true }, Lv(40, 0.9f, 2, 1.3f, 0, 0, 0, 0.5f, ""));
            var flood = W("flood_bomb", "Flood Bomb", "Giant water bombs leave huge soaking zones.", WeaponArchetype.Lob, TargetMode.RandomInRange,
                WeaponTags.Water | WeaponTags.Area, Evo, ArtId.IconFloodBomb, ArtId.BalloonBig, "#2E8CFF", "#2E8CFF", false, -1,
                new WeaponBehaviour { range = 9f, slow = 0.6f }, Lv(35, 1.8f, 2, 2.2f, 0, 5, 0, 0.8f, ""));
            var comet = W("comet_wand", "Comet Wand", "Homing comets that explode on impact.", WeaponArchetype.Projectile, TargetMode.Nearest,
                WeaponTags.Magic | WeaponTags.Projectile | WeaponTags.Fire, Evo, ArtId.IconCometWand, ArtId.Comet, "#FFC14D", "#FFC14D", false, -1,
                new WeaponBehaviour { range = 11f, homing = 5f, spread = 30f, explodeRadius = 2.0f }, Lv(40, 0.9f, 2, 1.2f, 10, 2, 0, 1, ""));
            var slugger = W("home_run_slugger", "Home Run Slugger", "Knocks enemies flying; launched enemies bowl over the rest.", WeaponArchetype.MeleeArc, TargetMode.Nearest,
                WeaponTags.Melee | WeaponTags.Physical, Evo, ArtId.IconSlugger, ArtId.Swoosh, "#FFD23F", "#FFD23F", false, -1,
                new WeaponBehaviour { range = 4.5f, arcDegrees = 140f, launchEnemies = true }, Lv(90, 1.4f, 1, 1.45f, 0, 0, 0, 10, ""));
            var toxic = W("toxic_laundry_cloud", "Toxic Laundry Cloud", "A huge poison cloud that pulses and keeps enemies sick.", WeaponArchetype.Aura, TargetMode.AllAround,
                WeaponTags.Poison | WeaponTags.Area, Evo, ArtId.IconToxicLaundry, ArtId.ToxicCloud, "#7CFF4A", "#7CFF4A", false, -1,
                new WeaponBehaviour { range = 4f, poisonDps = 6f, pulses = true }, Lv(12, 0.35f, 1, 1.9f, 0, 0, 0, 0.6f, ""));
            var robo = W("robo_teddy", "Robo Teddy", "A robot teddy that follows you everywhere with rapid fire.", WeaponArchetype.Turret, TargetMode.Nearest,
                WeaponTags.Summon | WeaponTags.Projectile | WeaponTags.Electric, Evo, ArtId.IconRoboTeddy, ArtId.RoboTeddy, "#D9DEE8", "#D9DEE8", false, -1,
                new WeaponBehaviour { range = 8f, tickInterval = 0.15f, followsPlayer = true }, Lv(14, 1, 1, 1, 0, 0, 1, 0.4f, ""));
            var fireworks = W("fireworks_show", "Fireworks Show", "Every blast sets off a chain of colourful explosions.", WeaponArchetype.Trap, TargetMode.AllAround,
                WeaponTags.Fire | WeaponTags.Trap | WeaponTags.Area, Evo, ArtId.IconFireworks, ArtId.Firecracker, "#FF8FC7", "#FF8FC7", false, -1,
                new WeaponBehaviour { range = 4f, subBlasts = 3 }, Lv(50, 1.5f, 8, 1.4f, 0, 12, 0, 2.5f, ""));
            var disco = W("disco_laser", "Disco Laser", "Four colour-cycling beams spin around you nonstop.", WeaponArchetype.Beam, TargetMode.AllAround,
                WeaponTags.Beam | WeaponTags.Fire, Evo, ArtId.IconDiscoLaser, ArtId.Beam, "#FF2EC4", "#FF2EC4", false, -1,
                new WeaponBehaviour { range = 8.5f, tickInterval = 0.15f, alwaysOn = true, colorCycle = true }, Lv(10, 1, 4, 1.4f, 0, 0, 0, 0.3f, ""));

            // ---------------------------------------------------------------- fusions
            var starBlaster = W("star_blaster", "Star Blaster", "Bursts of homing stars that pierce through crowds.", WeaponArchetype.Projectile, TargetMode.Nearest,
                WeaponTags.Magic | WeaponTags.Projectile, Fus, ArtId.IconStarBlaster, ArtId.Star, "#FFE14D", "#FFE14D", false, -1,
                new WeaponBehaviour { range = 11f, homing = 6f, spread = 14f }, Lv(30, 0.7f, 5, 1.25f, 12, 2, 3, 0.6f, ""));
            var mallet = W("mega_mallet", "Mega Mallet", "A giant, slow, all-around slam with a huge shockwave.", WeaponArchetype.MeleeArc, TargetMode.Nearest,
                WeaponTags.Melee | WeaponTags.Physical | WeaponTags.Area, Fus, ArtId.IconMegaMallet, ArtId.Swoosh, "#C7CCD8", "#C7CCD8", false, -1,
                new WeaponBehaviour { range = 6f, arcDegrees = 360f, shockwave = true, explodeRadius = 5f }, Lv(120, 1.6f, 1, 2.0f, 0, 0, 0, 10, ""));
            var prism = W("prism_storm", "Prism Storm", "Three spinning rainbow beams that also chain lightning.", WeaponArchetype.Beam, TargetMode.AllAround,
                WeaponTags.Beam | WeaponTags.Electric, Fus, ArtId.IconPrismStorm, ArtId.Beam, "#7FF3FF", "#7FF3FF", false, -1,
                new WeaponBehaviour { range = 9f, tickInterval = 0.15f, alwaysOn = true, colorCycle = true, chains = 3 }, Lv(14, 1, 3, 1.5f, 0, 0, 0, 0.3f, ""));
            var bubble = W("bubble_bath", "Bubble Bath", "A huge poison bubble field with ducks paddling around inside.", WeaponArchetype.Aura, TargetMode.AllAround,
                WeaponTags.Poison | WeaponTags.Water | WeaponTags.Area, Fus, ArtId.IconBubbleBath, ArtId.Bubble, "#9CE6FF", "#9CE6FF", false, -1,
                new WeaponBehaviour { range = 5f, poisonDps = 8f, slow = 0.3f, orbiters = 6, hitCooldown = 0.4f }, Lv(16, 0.3f, 1, 2.1f, 3.5f, 0, 0, 0.6f, ""));

            db.weapons = new List<WeaponDefinition>
            {
                pea, boom, pan, duck, zap, balloon, star, bat, socks, teddy, cracker, laser,
                peaCannon, buzzsaw, castIron, goldDuck, thunder, flood, comet, slugger, toxic, robo, fireworks, disco,
                starBlaster, mallet, prism, bubble,
            };

            // ---------------------------------------------------------------- items
            var spinach = I("spinach_can", "Spinach Can", "Extra projectiles for every weapon. Evolves the Pea Shooter.", ArtId.ItemSpinach, "#3AA845", false,
                L(M(StatId.Amount, 1)), L(M(StatId.Damage, 0.05f)), L(M(StatId.Damage, 0.05f)), L(M(StatId.Damage, 0.05f)), L(M(StatId.Damage, 0.05f)));
            var glove = I("sticky_glove", "Sticky Glove", "Things stick around longer. Evolves the Boomerang.", ArtId.ItemStickyGlove, "#FF8FC7", false,
                L(M(StatId.Duration, 0.1f)), L(M(StatId.Duration, 0.1f)), L(M(StatId.Duration, 0.1f)), L(M(StatId.Duration, 0.1f)), L(M(StatId.Duration, 0.1f)));
            var mitt = I("oven_mitt", "Oven Mitt", "Bigger swings and blasts. Evolves the Frying Pan.", ArtId.ItemOvenMitt, "#FF4D4D", false,
                L(M(StatId.Area, 0.08f)), L(M(StatId.Area, 0.08f)), L(M(StatId.Area, 0.08f)), L(M(StatId.Area, 0.08f)), L(M(StatId.Area, 0.08f)));
            var bubbles = I("bath_bubbles", "Bath Bubbles", "Faster projectiles and orbits. Evolves the Duck Orbit.", ArtId.ItemBathBubbles, "#9CE6FF", false,
                L(M(StatId.ProjectileSpeed, 0.1f)), L(M(StatId.ProjectileSpeed, 0.1f)), L(M(StatId.ProjectileSpeed, 0.1f)), L(M(StatId.ProjectileSpeed, 0.1f)), L(M(StatId.ProjectileSpeed, 0.1f)));
            var sock = I("static_sock", "Static Sock", "Crackling energy speeds up all attacks. Evolves the Zap Rod.", ArtId.ItemStaticSock, "#A66BFF", false,
                L(M(StatId.AttackSpeed, 0.06f)), L(M(StatId.AttackSpeed, 0.06f)), L(M(StatId.AttackSpeed, 0.06f)), L(M(StatId.AttackSpeed, 0.06f)), L(M(StatId.AttackSpeed, 0.06f)));
            var bucket = I("big_bucket", "Big Bucket", "Bigger and longer-lasting effects. Evolves the Water Balloon.", ArtId.ItemBigBucket, "#4FB6FF", false,
                L(M(StatId.Area, 0.05f), M(StatId.Duration, 0.05f)), L(M(StatId.Area, 0.05f), M(StatId.Duration, 0.05f)), L(M(StatId.Area, 0.05f), M(StatId.Duration, 0.05f)),
                L(M(StatId.Area, 0.05f), M(StatId.Duration, 0.05f)), L(M(StatId.Area, 0.05f), M(StatId.Duration, 0.05f)));
            var cookie = I("star_cookie", "Star Cookie", "Lucky crumbs: more critical hits. Evolves the Star Wand.", ArtId.ItemStarCookie, "#E3A65C", false,
                L(M(StatId.CritChance, 0.03f)), L(M(StatId.CritChance, 0.03f)), L(M(StatId.CritChance, 0.03f)), L(M(StatId.CritChance, 0.03f)), L(M(StatId.CritChance, 0.03f)));
            var cap = I("lucky_cap", "Lucky Cap", "Crits hit much harder. Evolves the Baseball Bat.", ArtId.ItemLuckyCap, "#3FAE2A", false,
                L(M(StatId.CritDamage, 0.12f)), L(M(StatId.CritDamage, 0.12f)), L(M(StatId.CritDamage, 0.12f)), L(M(StatId.CritDamage, 0.12f)), L(M(StatId.CritDamage, 0.12f)));
            var pin = I("clothespin", "Clothespin", "Pinch your nose: bigger auras and a little regen. Evolves the Stinky Socks.", ArtId.ItemClothespin, "#D9A066", false,
                L(M(StatId.Area, 0.05f), M(StatId.Regen, 0.1f)), L(M(StatId.Area, 0.05f), M(StatId.Regen, 0.1f)), L(M(StatId.Area, 0.05f), M(StatId.Regen, 0.1f)),
                L(M(StatId.Area, 0.05f), M(StatId.Regen, 0.1f)), L(M(StatId.Area, 0.05f), M(StatId.Regen, 0.1f)));
            var batteries = I("fresh_batteries", "Fresh Batteries", "Summons last longer and everything fires a bit faster. Evolves the Teddy Turret.", ArtId.ItemBatteries, "#FFD23F", false,
                L(M(StatId.Duration, 0.08f), M(StatId.AttackSpeed, 0.03f)), L(M(StatId.Duration, 0.08f), M(StatId.AttackSpeed, 0.03f)), L(M(StatId.Duration, 0.08f), M(StatId.AttackSpeed, 0.03f)),
                L(M(StatId.Duration, 0.08f), M(StatId.AttackSpeed, 0.03f)), L(M(StatId.Duration, 0.08f), M(StatId.AttackSpeed, 0.03f)));
            var matchbox = I("matchbox", "Matchbox", "Everything burns hotter: more damage. Evolves the Firecracker Trap.", ArtId.ItemMatchbox, "#FF4D4D", false,
                L(M(StatId.Damage, 0.06f)), L(M(StatId.Damage, 0.06f)), L(M(StatId.Damage, 0.06f)), L(M(StatId.Damage, 0.06f)), L(M(StatId.Damage, 0.06f)));
            var lens = I("tiny_lens", "Tiny Lens", "Focused power: damage and area. Evolves the Laser Pointer.", ArtId.ItemTinyLens, "#9CD7FF", false,
                L(M(StatId.Damage, 0.04f), M(StatId.Area, 0.04f)), L(M(StatId.Damage, 0.04f), M(StatId.Area, 0.04f)), L(M(StatId.Damage, 0.04f), M(StatId.Area, 0.04f)),
                L(M(StatId.Damage, 0.04f), M(StatId.Area, 0.04f)), L(M(StatId.Damage, 0.04f), M(StatId.Area, 0.04f)));
            var sneakers = I("sneakers", "Sneakers", "Run faster.", ArtId.ItemSneakers, "#FF6B5B", true,
                L(M(StatId.MoveSpeed, 0.08f)), L(M(StatId.MoveSpeed, 0.08f)), L(M(StatId.MoveSpeed, 0.08f)), L(M(StatId.MoveSpeed, 0.08f)), L(M(StatId.MoveSpeed, 0.08f)));
            var ring = I("magnet_ring", "Magnet Ring", "Pull in gems and coins from further away.", ArtId.ItemMagnetRing, "#FFD23F", true,
                L(M(StatId.PickupRadius, 0.5f)), L(M(StatId.PickupRadius, 0.5f)), L(M(StatId.PickupRadius, 0.5f)), L(M(StatId.PickupRadius, 0.5f)), L(M(StatId.PickupRadius, 0.5f)));
            var locket = I("heart_locket", "Heart Locket", "More max HP and a little regeneration.", ArtId.ItemHeartLocket, "#FF8FC7", true,
                L(M(StatId.MaxHp, 15), M(StatId.Regen, 0.15f)), L(M(StatId.MaxHp, 15), M(StatId.Regen, 0.15f)), L(M(StatId.MaxHp, 15), M(StatId.Regen, 0.15f)),
                L(M(StatId.MaxHp, 15), M(StatId.Regen, 0.15f)), L(M(StatId.MaxHp, 15), M(StatId.Regen, 0.15f)));
            var clover = I("lucky_clover", "Lucky Clover", "Better level-up cards and more drops.", ArtId.ItemClover, "#3FAE2A", true,
                L(M(StatId.Luck, 0.06f)), L(M(StatId.Luck, 0.06f)), L(M(StatId.Luck, 0.06f)), L(M(StatId.Luck, 0.06f)), L(M(StatId.Luck, 0.06f)));
            var patch = I("armor_patch", "Armor Patch", "Every hit you take is 1 smaller per level.", ArtId.ItemArmorPatch, "#A9B1C2", true,
                L(M(StatId.Armor, 1)), L(M(StatId.Armor, 1)), L(M(StatId.Armor, 1)), L(M(StatId.Armor, 1)), L(M(StatId.Armor, 1)));
            var piggy = I("piggy_bank", "Piggy Bank", "More gold from every coin.", ArtId.ItemPiggyBank, "#FF8FC7", true,
                L(M(StatId.GoldGain, 0.1f)), L(M(StatId.GoldGain, 0.1f)), L(M(StatId.GoldGain, 0.1f)), L(M(StatId.GoldGain, 0.1f)), L(M(StatId.GoldGain, 0.1f)));
            var hat = I("smart_hat", "Smart Hat", "Learn faster: more XP from gems.", ArtId.ItemSmartHat, "#4B4F5E", true,
                L(M(StatId.XpGain, 0.08f)), L(M(StatId.XpGain, 0.08f)), L(M(StatId.XpGain, 0.08f)), L(M(StatId.XpGain, 0.08f)), L(M(StatId.XpGain, 0.08f)));

            db.items = new List<ItemDefinition> { spinach, glove, mitt, bubbles, sock, bucket, cookie, cap, pin, batteries, matchbox, lens, sneakers, ring, locket, clover, patch, piggy, hat };

            // ---------------------------------------------------------------- recipes
            db.evolutions = new List<EvolutionRecipe>
            {
                Evolution(pea, spinach, peaCannon), Evolution(boom, glove, buzzsaw), Evolution(pan, mitt, castIron), Evolution(duck, bubbles, goldDuck),
                Evolution(zap, sock, thunder), Evolution(balloon, bucket, flood), Evolution(star, cookie, comet), Evolution(bat, cap, slugger),
                Evolution(socks, pin, toxic), Evolution(teddy, batteries, robo), Evolution(cracker, matchbox, fireworks), Evolution(laser, lens, disco),
            };
            db.fusions = new List<FusionRecipe>
            {
                Fusion(pea, star, starBlaster), Fusion(pan, bat, mallet), Fusion(zap, laser, prism), Fusion(duck, socks, bubble),
            };
        }

        static StatMod M(StatId s, float v) => new(s, v);
        static ItemLevel L(params StatMod[] mods) => new() { mods = new List<StatMod>(mods) };

        static ItemDefinition I(string id, string name, string desc, ArtId icon, string color, bool general, params ItemLevel[] levels) =>
            LoadOrCreate<ItemDefinition>($"{ItemDir}/{id}.asset", i =>
            {
                i.id = id; i.displayName = name; i.description = desc; i.icon = icon; i.color = Hex(color); i.general = general; i.levels = levels;
            });

        static EvolutionRecipe Evolution(WeaponDefinition w, ItemDefinition item, WeaponDefinition result) =>
            LoadOrCreate<EvolutionRecipe>($"{RecipeDir}/Evo_{w.id}.asset", r => { r.weapon = w; r.item = item; r.result = result; });

        static FusionRecipe Fusion(WeaponDefinition a, WeaponDefinition b, WeaponDefinition result) =>
            LoadOrCreate<FusionRecipe>($"{RecipeDir}/Fusion_{result.id}.asset", r => { r.a = a; r.b = b; r.result = result; });
    }
}
