namespace MiniMayhem
{
    /// <summary>Every fixed procedural sprite. Enemy sprites are built per EnemyDefinition instead.</summary>
    public enum ArtId
    {
        None,
        // Hero
        Hero, HeroStep,
        // Pickups
        GemSmall, GemMedium, GemBig, GemHuge, Coin, HealthPickup, MagnetPickup, BombPickup, Chest,
        // Projectiles / weapon bodies
        Pea, PeaBig, Boomerang, Buzzsaw, Star, Comet, Balloon, BalloonBig, Duck, GoldDuck, Teddy, RoboTeddy,
        Firecracker, Puddle, StinkCloud, ToxicCloud, Bubble, EnemyBullet, EnemyBulletAlt, TeddyShot,
        // FX
        Circle, Ring, Swoosh, Spark, Puff, Pixel, Shadow, Telegraph, Exclaim, Beam, Glow, Bolt,
        // Weapon icons (base, evolved, fused)
        IconPeaShooter, IconBoomerang, IconFryingPan, IconDuckOrbit, IconZapRod, IconWaterBalloon, IconStarWand,
        IconBaseballBat, IconStinkySocks, IconTeddyTurret, IconFirecrackerTrap, IconLaserPointer,
        IconPeaCannon, IconBuzzsaw, IconCastIron, IconGoldDuck, IconThunderRod, IconFloodBomb, IconCometWand,
        IconSlugger, IconToxicLaundry, IconRoboTeddy, IconFireworks, IconDiscoLaser,
        IconStarBlaster, IconMegaMallet, IconPrismStorm, IconBubbleBath,
        // Item icons
        ItemSpinach, ItemStickyGlove, ItemOvenMitt, ItemBathBubbles, ItemStaticSock, ItemBigBucket, ItemStarCookie,
        ItemLuckyCap, ItemClothespin, ItemBatteries, ItemMatchbox, ItemTinyLens, ItemSneakers, ItemMagnetRing,
        ItemHeartLocket, ItemClover, ItemArmorPatch, ItemPiggyBank, ItemSmartHat,
        // Props
        PropFlower, PropGrass, PropRock, PropMushroom, PropReed, PropLilyPad, PropCactus, PropBones, PropSnowdrift,
        PropIceCrystal, PropPine, PropLavaRock, PropEmber, PropCandyCane, PropLollipop, PropGumdrop, PropBush,
        PropTree, PropSkull, Wall, Crate,
        // Hazard patches
        HazardPatch,
        // UI
        UiNode, UiLock, UiCheck, UiSkull, UiStar, UiQuestion, UiVignette, UiGoldIcon, UiCard,
    }
}
