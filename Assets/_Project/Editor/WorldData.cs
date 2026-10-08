using System;
using System.Collections.Generic;
using UnityEngine;
using static MiniMayhem.EditorTools.BuilderUtil;
using F = MiniMayhem.BodyFeature;
using B = MiniMayhem.EnemyBehaviour;
using P = MiniMayhem.BossPattern;

namespace MiniMayhem.EditorTools
{
    /// <summary>Enemies (6 normal + 2 mini bosses + 1 boss per biome), the 6 biomes and their wave timelines.</summary>
    internal static class WorldData
    {
        const string EnemyDir = SoRoot + "/Enemies";
        const string BiomeDir = SoRoot + "/Biomes";

        static EnemyDefinition E(string biome, string id, string name, string desc, EnemyTier tier, B beh, float hp, float dmg, float spd, float r, int xp, float gold,
            BodyShape body, F feats, string p, string s, string a, Action<EnemyDefinition> extra = null) =>
            LoadOrCreate<EnemyDefinition>($"{EnemyDir}/{biome}/{id}.asset", e =>
            {
                e.id = id; e.displayName = name; e.description = desc; e.tier = tier; e.behaviour = beh;
                e.maxHp = hp; e.damage = dmg; e.speed = spd; e.radius = r; e.xp = xp; e.goldChance = gold;
                e.body = body; e.features = feats; e.primary = Hex(p); e.secondary = Hex(s); e.accent = Hex(a);
                if (tier == EnemyTier.Boss) { e.knockbackResist = 0.95f; e.visualScale = 1.25f; e.projectileSpeed = 5.5f; }
                else if (tier == EnemyTier.MiniBoss) { e.knockbackResist = 0.8f; e.visualScale = 1.3f; e.projectileSpeed = 6f; }
                extra?.Invoke(e);
            });

        static Action<EnemyDefinition> Shoot(float interval, float range, float speed, int count = 1) => e => { e.attackInterval = interval; e.attackRange = range; e.projectileSpeed = speed; e.projectileCount = count; };
        static Action<EnemyDefinition> Charge(float interval, float range) => e => { e.attackInterval = interval; e.attackRange = range; };
        static Action<EnemyDefinition> Child(EnemyDefinition c, int n, float interval = 5f) => e => { e.child = c; e.childCount = n; e.attackInterval = interval; };
        static Action<EnemyDefinition> Boss(EnemyDefinition child, int n, params P[] patterns) => e => { e.child = child; e.childCount = n; e.patterns = patterns; };
        static Action<EnemyDefinition> Armor(float a) => e => e.armor = a;
        static Action<EnemyDefinition> All(params Action<EnemyDefinition>[] a) => e => { foreach (var x in a) x(e); };

        public static void Build(GameDatabase db)
        {
            var enemies = new List<EnemyDefinition>();
            var biomes = new List<BiomeDefinition>();
            const EnemyTier N = EnemyTier.Normal, Mini = EnemyTier.MiniBoss, Bo = EnemyTier.Boss;

            // ---------------------------------------------------------------- 1. Meadow
            var slime = E("Meadow", "slime", "Slime", "A wobbly green blob. Mostly harmless. Mostly.", N, B.Chaser, 8, 6, 1.9f, 0.42f, 1, 0.08f, BodyShape.Blob, F.Eyes | F.Cheeks | F.Shine, "#7BD94B", "#C6F25A", "#2E8A1E");
            var bee = E("Meadow", "bee", "Bee Swarm", "Buzzes around in big angry packs.", N, B.Swarm, 4, 4, 3.0f, 0.32f, 1, 0.04f, BodyShape.Round, F.Eyes | F.Wings | F.Stripes | F.Antennae, "#FFD23F", "#FFF1A8", "#3A2A1A");
            var shroom = E("Meadow", "mushroom", "Mushroom", "Slow, sturdy and very stubborn.", N, B.Tank, 30, 8, 1.2f, 0.55f, 3, 0.15f, BodyShape.Tall, F.Eyes | F.Hat | F.Cheeks, "#FFF1D6", "#FFFFFF", "#FF4D4D", e => e.knockbackResist = 0.5f);
            var snail = E("Meadow", "snail", "Snail", "A tough shell makes it hard to hurt.", N, B.Tank, 40, 8, 0.9f, 0.55f, 3, 0.15f, BodyShape.Blob, F.Eyes | F.Shell | F.Antennae, "#C7E08A", "#D9A066", "#FF8FC7", All(Armor(0.3f), e => e.knockbackResist = 0.6f));
            var ladybug = E("Meadow", "ladybug", "Ladybug Rusher", "Flashes, then charges straight at you.", N, B.Charger, 14, 10, 2.2f, 0.42f, 2, 0.1f, BodyShape.Bug, F.Eyes | F.Antennae | F.Angry, "#FF4D4D", "#2B2B2B", "#2B2B2B", Charge(3f, 6.5f));
            var flower = E("Meadow", "flower_shooter", "Flower Shooter", "Keeps its distance and spits seeds.", N, B.Shooter, 12, 6, 1.3f, 0.45f, 2, 0.1f, BodyShape.Flower, F.Eyes | F.Petals | F.Cheeks, "#FFD23F", "#FF8FC7", "#3FAE2A", Shoot(2.6f, 7f, 5f));
            var mole = E("Meadow", "big_mole", "Big Mole", "Digs, charges and calls up slimes.", Mini, B.Boss, 380, 14, 2.0f, 0.95f, 40, 1f, BodyShape.Round, F.Eyes | F.Ears | F.Teeth, "#8A6A5A", "#FFB8C8", "#FFFFFF", Boss(slime, 6, P.Charge, P.Summon, P.Slam));
            var beeGuard = E("Meadow", "bumble_guard", "Bumblebee Queen's Guard", "A big bee with a stinger cannon.", Mini, B.Boss, 340, 12, 2.4f, 0.9f, 40, 1f, BodyShape.Round, F.Eyes | F.Wings | F.Stripes | F.Antennae | F.Angry, "#FFC21A", "#FFF1A8", "#3A2A1A", Boss(bee, 10, P.AimedSpread, P.RadialBurst, P.Summon));
            var sunflower = E("Meadow", "giant_sunflower", "Giant Sunflower", "The meadow's boss. Spins out seeds and calls the bees.", Bo, B.Boss, 2000, 18, 1.1f, 1.6f, 150, 1f, BodyShape.Flower, F.Eyes | F.Petals | F.Cheeks | F.Crown, "#8A5A2A", "#FFD23F", "#3FAE2A", Boss(bee, 12, P.RadialBurst, P.Summon, P.Spiral, P.Slam));
            biomes.Add(Biome(0, "meadow", "Meadow", "Sunny fields full of flowers, bugs and slimes. A gentle start.", 1f, 500, new[] { slime, bee, shroom, snail, ladybug, flower }, new[] { mole, beeGuard }, sunflower,
                "#8ED16B", "#7FC45E", "#8B6B4A", new[] { ArtId.PropFlower, ArtId.PropFlower, ArtId.PropGrass, ArtId.PropBush, ArtId.PropTree, ArtId.PropRock, ArtId.PropMushroom },
                new[] { "#FF8FC7", "#FFF1A8", "#5FAE45", "#5FAE45", "#4E9A3A", "#B8BEC8", "#FF6B5B" }, HazardType.None, "#00000000", 60, false, 128, 11));
            enemies.AddRange(new[] { slime, bee, shroom, snail, ladybug, flower, mole, beeGuard, sunflower });

            // ---------------------------------------------------------------- 2. Swamp
            var droplet = E("Swamp", "bog_droplet", "Bog Droplet", "What is left of a Bog Blob. Still sticky.", N, B.Chaser, 6, 5, 2.4f, 0.3f, 1, 0.02f, BodyShape.Blob, F.Eyes, "#6E9A5A", "#A8D08A", "#2E4A2A");
            var frog = E("Swamp", "frog", "Frog Hopper", "Moves in big, hard-to-read hops.", N, B.Hopper, 12, 8, 2.1f, 0.42f, 1, 0.08f, BodyShape.Round, F.Eyes | F.Cheeks, "#5FC45A", "#C6F25A", "#2E8A1E");
            var leech = E("Swamp", "leech", "Leech", "Wriggles fast towards you.", N, B.Rusher, 9, 6, 2.9f, 0.36f, 1, 0.06f, BodyShape.Worm, F.Eyes | F.Teeth, "#5A3A5A", "#8A5A8A", "#FF8FC7");
            var bog = E("Swamp", "bog_blob", "Bog Blob", "Splits into droplets when popped.", N, B.Splitter, 26, 8, 1.6f, 0.55f, 2, 0.12f, BodyShape.Blob, F.Eyes | F.Shine, "#6E9A5A", "#A8D08A", "#2E4A2A", Child(droplet, 3));
            var dragonfly = E("Swamp", "dragonfly", "Dragonfly", "Zips around in huge swarms.", N, B.Swarm, 5, 5, 3.3f, 0.32f, 1, 0.04f, BodyShape.Bug, F.Eyes | F.Wings, "#3ED6C0", "#BFF6FF", "#1E5A6A");
            var toad = E("Swamp", "toad_spitter", "Toad Spitter", "Spits a triple shot of swamp goo.", N, B.Shooter, 18, 7, 1.2f, 0.5f, 2, 0.12f, BodyShape.Round, F.Eyes | F.Cheeks | F.Angry, "#8A9A3A", "#D0D88A", "#4A4A1A", Shoot(3f, 7f, 5f, 3));
            var lurker = E("Swamp", "reed_lurker", "Reed Lurker", "Hides under the water and pops up right next to you.", N, B.Ambusher, 20, 10, 2.2f, 0.45f, 2, 0.12f, BodyShape.Tall, F.Eyes | F.Teeth, "#4A7A3A", "#C08A5A", "#2E4A2A");
            var croc = E("Swamp", "croc", "Croc", "All teeth. Charges and stomps.", Mini, B.Boss, 600, 18, 2.2f, 1.0f, 50, 1f, BodyShape.Worm, F.Eyes | F.Teeth | F.Angry, "#4A8A3A", "#A8D08A", "#FFFFFF", Boss(frog, 6, P.Charge, P.AimedSpread, P.Slam));
            var witch = E("Swamp", "fog_witch", "Fog Witch", "Summons frogs and drops bubbling bog pools.", Mini, B.Boss, 520, 14, 1.8f, 0.9f, 50, 1f, BodyShape.Ghost, F.Eyes | F.Hat | F.Cheeks, "#9A8ABF", "#E0D8FF", "#3A2A5A", Boss(frog, 8, P.Summon, P.RadialBurst, P.HazardDrop));
            var hydra = E("Swamp", "swamp_hydra", "Swamp Hydra", "Three grumpy heads, one big appetite.", Bo, B.Boss, 3200, 22, 1.2f, 1.7f, 180, 1f, BodyShape.Worm, F.Eyes | F.Teeth | F.Horns | F.Angry, "#3A8A6A", "#8AD0A8", "#FFFFFF", Boss(leech, 10, P.Spiral, P.AimedSpread, P.HazardDrop, P.Summon));
            biomes.Add(Biome(1, "swamp", "Swamp", "Murky water, hopping frogs and slow, squelchy mud.", 1.6f, 600, new[] { frog, leech, bog, dragonfly, toad, lurker }, new[] { croc, witch }, hydra,
                "#6E8F5A", "#617F4F", "#5A4A3A", new[] { ArtId.PropReed, ArtId.PropLilyPad, ArtId.PropMushroom, ArtId.PropRock, ArtId.PropGrass },
                new[] { "#C08A5A", "#6FBF5A", "#B07AE0", "#7D8C7A", "#4F7A3A" }, HazardType.SlowMud, "#5A4028C0", 57, true, 112, 23));
            enemies.AddRange(new[] { frog, leech, bog, dragonfly, toad, lurker, croc, witch, hydra, droplet });

            // ---------------------------------------------------------------- 3. Desert
            var scorpion = E("Desert", "scorpion", "Scorpion", "Lines up and charges with its stinger out.", N, B.Charger, 22, 12, 2.3f, 0.45f, 2, 0.1f, BodyShape.Bug, F.Eyes | F.Tail | F.Legs | F.Angry, "#D08A3A", "#F0C07A", "#5A2A1A", Charge(2.8f, 7f));
            var cactus = E("Desert", "cactus_shooter", "Cactus Shooter", "Fires a fan of needles.", N, B.Shooter, 26, 8, 1.0f, 0.5f, 2, 0.12f, BodyShape.Tall, F.Eyes | F.Spikes | F.Cheeks, "#5FAE45", "#C6F25A", "#FFF1D6", Shoot(2.8f, 7.5f, 5.5f, 3));
            var sprite = E("Desert", "sand_sprite", "Sand Sprite", "Circles you, closing in bit by bit.", N, B.Orbiter, 14, 8, 3.0f, 0.38f, 1, 0.08f, BodyShape.Ghost, F.Eyes | F.Shine, "#F0D08A", "#FFF1D6", "#C08A3A");
            var scarab = E("Desert", "scarab", "Scarab Swarm", "Shiny beetles in a clicking tide.", N, B.Swarm, 8, 6, 3.0f, 0.34f, 1, 0.05f, BodyShape.Bug, F.Eyes | F.Shine, "#3ED6C0", "#2E8A8A", "#1A3A3A");
            var mummy = E("Desert", "mummy", "Mummy", "Wrapped up and in no hurry.", N, B.Tank, 70, 12, 1.1f, 0.58f, 3, 0.18f, BodyShape.Tall, F.Eyes | F.Bandage, "#E8DCC0", "#FFFFFF", "#8A7A5A", e => e.knockbackResist = 0.6f);
            var devil = E("Desert", "dust_devil", "Dust Devil", "A tiny tornado that never stops spinning.", N, B.Rusher, 16, 8, 3.2f, 0.42f, 1, 0.08f, BodyShape.Cone, F.Eyes | F.Angry, "#D8B880", "#F0D8A8", "#8A6A3A");
            var golem = E("Desert", "sand_golem", "Sand Golem", "Slams the ground and calls mummies.", Mini, B.Boss, 900, 22, 1.6f, 1.1f, 60, 1f, BodyShape.Square, F.Eyes | F.Angry, "#D8B060", "#F0D8A8", "#5A3A1A", Boss(mummy, 4, P.Slam, P.Charge, P.Summon));
            var cobra = E("Desert", "cobra", "Cobra", "Hypnotic spirals of venom.", Mini, B.Boss, 800, 18, 2.2f, 0.95f, 60, 1f, BodyShape.Worm, F.Eyes | F.Teeth | F.Stripes, "#C0A030", "#F0E080", "#5A3A1A", Boss(scarab, 10, P.AimedSpread, P.Spiral, P.Charge));
            var pharaoh = E("Desert", "pharaoh_cat", "Pharaoh Cat", "Ruler of the dunes. Expects to be worshipped.", Bo, B.Boss, 4800, 26, 1.3f, 1.7f, 220, 1f, BodyShape.Round, F.Eyes | F.Ears | F.Crown | F.Cheeks, "#F0C060", "#3A6AD0", "#FFD23F", Boss(mummy, 6, P.RadialBurst, P.Summon, P.Charge, P.Spiral));
            biomes.Add(Biome(2, "desert", "Desert", "Hot sand, prickly cacti and sudden sandstorms that hide what is coming.", 2.4f, 700, new[] { scorpion, cactus, sprite, scarab, mummy, devil }, new[] { golem, cobra }, pharaoh,
                "#F0D08A", "#E6C27A", "#C48A52", new[] { ArtId.PropCactus, ArtId.PropBones, ArtId.PropRock, ArtId.PropSkull, ArtId.PropCactus },
                new[] { "#5FAE45", "#FFF1D6", "#C9A36A", "#FFF1D6", "#4E9A3A" }, HazardType.Sandstorm, "#00000000", 62, true, 124, 37));
            enemies.AddRange(new[] { scorpion, cactus, sprite, scarab, mummy, devil, golem, cobra, pharaoh });

            // ---------------------------------------------------------------- 4. Frozen Tundra
            var snowman = E("Frozen", "snowman", "Snowman", "Throws snowballs. Hates summer.", N, B.Shooter, 30, 9, 1.0f, 0.5f, 2, 0.12f, BodyShape.Snowman, F.Eyes | F.Hat | F.Cheeks, "#F4F8FF", "#FFFFFF", "#3A3A4A", Shoot(2.4f, 7.5f, 6f));
            var penguin = E("Frozen", "penguin", "Penguin Slider", "Belly-slides at you at full speed.", N, B.Charger, 26, 12, 2.0f, 0.45f, 2, 0.1f, BodyShape.Round, F.Eyes | F.Cheeks, "#3A3F5A", "#FFFFFF", "#FF9A3C", Charge(2.6f, 8f));
            var iceSprite = E("Frozen", "ice_sprite", "Ice Sprite", "Dances around you in a closing circle.", N, B.Orbiter, 18, 9, 3.2f, 0.38f, 1, 0.08f, BodyShape.Star, F.Eyes | F.Shine, "#9CD7FF", "#FFFFFF", "#3A6AD0");
            var yeti = E("Frozen", "yeti_cub", "Yeti Cub", "Fluffy, big and surprisingly strong.", N, B.Chaser, 60, 12, 1.7f, 0.55f, 3, 0.15f, BodyShape.Round, F.Eyes | F.Ears | F.Horns, "#E8F0FF", "#BFD4FF", "#5A6A8A", e => e.knockbackResist = 0.4f);
            var wolf = E("Frozen", "frost_wolf", "Frost Wolf", "Hunts in fast packs.", N, B.Swarm, 14, 9, 3.4f, 0.4f, 1, 0.06f, BodyShape.Round, F.Eyes | F.Ears | F.Teeth, "#A8B8D0", "#E8F0FF", "#3A4A6A");
            var icicle = E("Frozen", "icicle_dropper", "Icicle Dropper", "Drops icicles where you are about to be.", N, B.Bomber, 24, 10, 1.6f, 0.45f, 2, 0.12f, BodyShape.Ghost, F.Eyes | F.Spikes, "#BFE6FF", "#FFFFFF", "#7FB8E0", e => e.attackInterval = 3f);
            var troll = E("Frozen", "ice_troll", "Ice Troll", "Big fists, bigger stomps.", Mini, B.Boss, 1300, 26, 1.7f, 1.15f, 70, 1f, BodyShape.Square, F.Eyes | F.Horns | F.Teeth, "#8AB8D8", "#E8F0FF", "#FFFFFF", Boss(yeti, 3, P.Slam, P.Charge, P.Summon));
            var guard = E("Frozen", "snow_guard", "Snow Queen's Guard", "A frosty sentinel with icy volleys.", Mini, B.Boss, 1150, 22, 2.0f, 1.0f, 70, 1f, BodyShape.Tall, F.Eyes | F.Crown | F.Shine, "#BFD4FF", "#FFFFFF", "#3A6AD0", Boss(iceSprite, 8, P.AimedSpread, P.RadialBurst, P.Summon));
            var mammoth = E("Frozen", "frost_mammoth", "Frost Mammoth", "An ancient woolly giant that shakes the ice.", Bo, B.Boss, 6800, 30, 1.3f, 1.9f, 260, 1f, BodyShape.Round, F.Eyes | F.Horns | F.Ears, "#9A7A6A", "#E8DCC0", "#FFFFFF", Boss(wolf, 10, P.Charge, P.Slam, P.RadialBurst, P.Summon));
            biomes.Add(Biome(3, "frozen", "Frozen Tundra", "Snow, ice and slippery ground that keeps you sliding.", 3.4f, 800, new[] { snowman, penguin, iceSprite, yeti, wolf, icicle }, new[] { troll, guard }, mammoth,
                "#E8F4FF", "#D6EAFB", "#7F9AB8", new[] { ArtId.PropSnowdrift, ArtId.PropIceCrystal, ArtId.PropPine, ArtId.PropRock },
                new[] { "#FFFFFF", "#9CD7FF", "#3E7A6A", "#9AA8BC" }, HazardType.Ice, "#B4E6FFB0", 64, false, 118, 41));
            enemies.AddRange(new[] { snowman, penguin, iceSprite, yeti, wolf, icicle, troll, guard, mammoth });

            // ---------------------------------------------------------------- 5. Volcano
            var ember = E("Volcano", "ember_blob", "Ember Blob", "A hot little piece of a Lava Slime.", N, B.Chaser, 12, 8, 2.6f, 0.32f, 1, 0.02f, BodyShape.Blob, F.Eyes, "#FF8A3C", "#FFD23F", "#8A2A1A");
            var lavaSlime = E("Volcano", "lava_slime", "Lava Slime", "Splits into embers when popped.", N, B.Splitter, 40, 11, 1.7f, 0.55f, 2, 0.12f, BodyShape.Blob, F.Eyes | F.Shine | F.Angry, "#FF5A2A", "#FFD23F", "#8A2A1A", Child(ember, 3));
            var imp = E("Volcano", "fire_imp", "Fire Imp", "Runs up, fizzes... and explodes.", N, B.Exploder, 22, 14, 3.0f, 0.4f, 2, 0.1f, BodyShape.Round, F.Eyes | F.Horns | F.Tail | F.Angry, "#FF4D4D", "#FFB000", "#3A1A1A");
            var bat = E("Volcano", "ember_bat", "Ember Bat", "Flaps in from the smoke in huge flocks.", N, B.Swarm, 12, 9, 3.5f, 0.36f, 1, 0.05f, BodyShape.Bird, F.Eyes | F.Ears, "#5A2A3A", "#FF8A3C", "#FFD23F");
            var crab = E("Volcano", "rock_crab", "Rock Crab", "A shell of solid rock.", N, B.Tank, 90, 14, 1.2f, 0.6f, 3, 0.18f, BodyShape.Bug, F.Eyes | F.Legs | F.Shell, "#6A4A4A", "#8A6A6A", "#FF8A3C", All(Armor(0.4f), e => e.knockbackResist = 0.7f));
            var worm = E("Volcano", "magma_worm", "Magma Worm", "Swims through the rock and bursts out next to you.", N, B.Ambusher, 45, 14, 2.4f, 0.5f, 2, 0.12f, BodyShape.Worm, F.Eyes | F.Teeth, "#FF7A2A", "#FFC14D", "#5A1A1A");
            var ghost = E("Volcano", "ash_ghost", "Ash Ghost", "Hangs back and summons ember bats.", N, B.Summoner, 40, 10, 1.6f, 0.5f, 3, 0.15f, BodyShape.Ghost, F.Eyes | F.Angry, "#7A7A8A", "#BFBFCF", "#FF8A3C", Child(bat, 3, 5.5f));
            var lavaGolem = E("Volcano", "lava_golem", "Lava Golem", "Leaves lava pools wherever it stomps.", Mini, B.Boss, 1900, 30, 1.6f, 1.2f, 80, 1f, BodyShape.Square, F.Eyes | F.Angry | F.Shine, "#5A3A3A", "#FF7A2A", "#FFD23F", Boss(ember, 6, P.Slam, P.HazardDrop, P.Charge));
            var drake = E("Volcano", "fire_drake", "Fire Drake", "Spits spirals of fire.", Mini, B.Boss, 1700, 26, 2.3f, 1.05f, 80, 1f, BodyShape.Bird, F.Eyes | F.Horns | F.Tail, "#D03A2A", "#FFB000", "#FFD23F", Boss(bat, 8, P.Spiral, P.AimedSpread, P.Charge));
            var dragon = E("Volcano", "magma_dragon", "Magma Dragon", "The mountain's furious heart.", Bo, B.Boss, 9500, 34, 1.4f, 1.9f, 300, 1f, BodyShape.Bird, F.Eyes | F.Horns | F.Teeth | F.Tail | F.Angry, "#B0201A", "#FF8A3C", "#FFD23F", Boss(imp, 6, P.Spiral, P.HazardDrop, P.RadialBurst, P.Charge, P.Summon));
            biomes.Add(Biome(4, "volcano", "Volcano", "Scorching rock and bubbling lava pools that burn to stand in.", 4.6f, 900, new[] { lavaSlime, imp, bat, crab, worm, ghost }, new[] { lavaGolem, drake }, dragon,
                "#5A3A3A", "#4D3030", "#2E1E1E", new[] { ArtId.PropLavaRock, ArtId.PropEmber, ArtId.PropSkull, ArtId.PropBones, ArtId.PropRock },
                new[] { "#3A2A2A", "#FFFFFF", "#D8C8B0", "#D8C8B0", "#6A4A4A" }, HazardType.Lava, "#FF7319D9", 55, true, 136, 53));
            enemies.AddRange(new[] { lavaSlime, imp, bat, crab, worm, ghost, lavaGolem, drake, dragon, ember });

            // ---------------------------------------------------------------- 6. Candy Realm
            var jellyBit = E("Candy", "jelly_bit", "Jelly Bit", "A wobbly chunk of Jelly Blob.", N, B.Chaser, 16, 10, 2.8f, 0.32f, 1, 0.02f, BodyShape.Blob, F.Eyes | F.Shine, "#FF6FB0", "#FFC8E6", "#8A1A4A");
            var gummy = E("Candy", "gummy_bear", "Gummy Bear", "Chewy, cheerful and coming for you.", N, B.Chaser, 50, 14, 2.2f, 0.45f, 2, 0.1f, BodyShape.Round, F.Eyes | F.Ears | F.Cheeks | F.Shine, "#FF4D6D", "#FFB8C8", "#8A1A2A");
            var cupcake = E("Candy", "cupcake_bomber", "Cupcake Bomber", "Lobs frosting bombs where you are heading.", N, B.Bomber, 45, 14, 1.6f, 0.48f, 2, 0.12f, BodyShape.Cone, F.Eyes | F.Sprinkles | F.Cheeks, "#D9A066", "#FFC8E6", "#FF4D6D", e => e.attackInterval = 2.6f);
            var lolly = E("Candy", "lollipop_spinner", "Lollipop Spinner", "Spins circles around you.", N, B.Orbiter, 34, 12, 3.4f, 0.42f, 1, 0.08f, BodyShape.Round, F.Eyes | F.Stripes, "#FF8FC7", "#FFFFFF", "#3ED6C0");
            var jelly = E("Candy", "jelly_blob", "Jelly Blob", "Splits into jelly bits.", N, B.Splitter, 60, 13, 1.8f, 0.55f, 2, 0.12f, BodyShape.Blob, F.Eyes | F.Shine | F.Cheeks, "#FF6FB0", "#FFC8E6", "#8A1A4A", Child(jellyBit, 3));
            var cookie = E("Candy", "cookie_shield", "Cookie Shield", "Thick and crunchy: takes much less damage.", N, B.Tank, 130, 16, 1.2f, 0.6f, 3, 0.18f, BodyShape.Round, F.Eyes | F.Sprinkles | F.Angry, "#D9A066", "#8A5A3A", "#5A3A1A", All(Armor(0.45f), e => e.knockbackResist = 0.7f));
            var choc = E("Candy", "choc_worm", "Choc Worm", "Tunnels through the candy and pops up beside you.", N, B.Ambusher, 70, 16, 2.6f, 0.5f, 2, 0.12f, BodyShape.Worm, F.Eyes | F.Sprinkles, "#6A3A2A", "#8A5A3A", "#FF8FC7");
            var donut = E("Candy", "giant_donut", "Giant Donut", "Rolls at you and sprays sprinkles.", Mini, B.Boss, 2800, 32, 2.0f, 1.2f, 90, 1f, BodyShape.Round, F.Eyes | F.Sprinkles | F.Cheeks, "#FF8FC7", "#D9A066", "#FFFFFF", Boss(jellyBit, 8, P.RadialBurst, P.Charge, P.Spiral));
            var knight = E("Candy", "candy_cane_knight", "Candy Cane Knight", "A minty knight with a mean charge.", Mini, B.Boss, 2600, 34, 2.3f, 1.1f, 90, 1f, BodyShape.Tall, F.Eyes | F.Stripes | F.Hat | F.Angry, "#FFFFFF", "#FF4D4D", "#FF4D4D", Boss(gummy, 4, P.Charge, P.AimedSpread, P.Slam));
            var cake = E("Candy", "cake_king", "Cake King", "The sweetest, meanest ruler of all. The finale.", Bo, B.Boss, 14000, 38, 1.5f, 2.0f, 400, 1f, BodyShape.Square, F.Eyes | F.Crown | F.Sprinkles | F.Cheeks | F.Angry, "#FFC8E6", "#FF8FC7", "#FFD23F", Boss(gummy, 6, P.RadialBurst, P.Summon, P.Spiral, P.Charge, P.Slam, P.HazardDrop, P.AimedSpread));
            biomes.Add(Biome(5, "candy", "Candy Realm", "A sugary dream with a sticky twist. The finale biome.", 6f, 1000, new[] { gummy, cupcake, lolly, jelly, cookie, choc }, new[] { donut, knight }, cake,
                "#FFC8E6", "#FFB8DC", "#FF8FC7", new[] { ArtId.PropCandyCane, ArtId.PropLollipop, ArtId.PropGumdrop, ArtId.PropGumdrop, ArtId.PropTree },
                new[] { "#FFFFFF", "#FFF1D6", "#7FE0FF", "#FFD23F", "#FF9ADF" }, HazardType.Sugar, "#FFF2CCD0", 65, false, 140, 67));
            enemies.AddRange(new[] { gummy, cupcake, lolly, jelly, cookie, choc, donut, knight, cake, jellyBit });

            db.enemies = enemies;
            db.biomes = biomes;
        }

        static BiomeDefinition Biome(int index, string id, string name, string desc, float difficulty, int killTarget, EnemyDefinition[] normals, EnemyDefinition[] minis, EnemyDefinition boss,
            string groundA, string groundB, string wall, ArtId[] props, string[] tints, HazardType hazard, string hazardColor, int root, bool minor, float tempo, int seed)
        {
            var waves = LoadOrCreate<WaveTimeline>($"{BiomeDir}/{id}_waves.asset", w => FillWaves(w, normals, index));
            return LoadOrCreate<BiomeDefinition>($"{BiomeDir}/{id}.asset", b =>
            {
                b.id = id; b.displayName = name; b.description = desc; b.index = index; b.difficulty = difficulty; b.killTarget = killTarget;
                b.normals = normals; b.miniBosses = minis; b.boss = boss; b.waves = waves;
                // Map style alternates per match and flips each biome; later biomes use the expanding arena.
                var arena = index >= 3 ? MapStyle.ExpandingArena : MapStyle.FixedArena;
                b.matchStyles = index % 2 == 0 ? new[] { MapStyle.Endless, arena, MapStyle.Endless } : new[] { arena, MapStyle.Endless, arena };
                b.groundA = Hex(groundA); b.groundB = Hex(groundB); b.wallColor = Hex(wall);
                b.props = props;
                b.propTints = Array.ConvertAll(tints, Hex);
                b.hazard = hazard; b.hazardColor = Hex(hazardColor);
                b.hazardDensity = hazard == HazardType.Lava ? 0.45f : 0.7f;
                b.propDensity = 3f;
                b.musicRoot = root; b.musicMinor = minor; b.musicTempo = tempo; b.musicSeed = seed;
            });
        }

        static void FillWaves(WaveTimeline w, EnemyDefinition[] normals, int biome)
        {
            w.trickleRate = new AnimationCurve(new Keyframe(0, 1.0f), new Keyframe(2, 2.4f), new Keyframe(5, 4.2f), new Keyframe(8, 6.2f), new Keyframe(10, 7.5f));
            w.normalUnlockMinute = new[] { 0f, 0.5f, 1.5f, 2.5f, 4f, 5.5f };
            w.swarmTimes = new[] { 150f, 300f, 450f, 570f };
            w.swarmSize = 60;
            w.miniBossTimes = new[] { 90f, 270f, 450f, 510f };
            w.bossTimes = new[] { 180f, 360f, 540f };
            w.eliteChance = new AnimationCurve(new Keyframe(0, 0f), new Keyframe(3, 0.01f), new Keyframe(10, 0.05f));
            w.breatherAfterBoss = 18f;
            w.entries = new List<SpawnEntry>();
            var patterns = new[] { SpawnPattern.Ring, SpawnPattern.Line, SpawnPattern.Pincer, SpawnPattern.Cluster };
            int k = 0;
            for (float t = 20f; t < 590f; t += 35f, k++)
            {
                int unlocked = 1;
                for (int i = 0; i < w.normalUnlockMinute.Length; i++) if (t / 60f >= w.normalUnlockMinute[i]) unlocked = i + 1;
                var e = normals[k % unlocked];
                w.entries.Add(new SpawnEntry { time = t, enemy = e, count = 8 + (int)(t / 25f), pattern = patterns[k % patterns.Length], elite = t > 300f && k % 5 == 0 });
            }
        }
    }
}
