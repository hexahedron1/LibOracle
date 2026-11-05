namespace LibOracle;
/*
   public static readonly IngameEvent A = new(nameof(A), true);
*/
// a comment *before* the entry specifies what additional data the event provides in a tuple
public class IngameEvent(string value, bool register) : ExtEnum<IngameEvent>(value, register) {
    #region Slugcat
    // Player
    public static readonly IngameEvent PlayerDied = new(nameof(PlayerDied), true);
    public static readonly IngameEvent PlayerMeow = new(nameof(PlayerMeow), true); // Push to Meow compat
    // Player, PlayerCarriableItem
    public static readonly IngameEvent PlayerGrabObject = new(nameof(PlayerGrabObject), true);
    // Player, PlayerCarriableItem
    public static readonly IngameEvent PlayerSwallowObject = new(nameof(PlayerSwallowObject), true);
    // Player
    public static readonly IngameEvent PlayerRegurgitateObject = new(nameof(PlayerRegurgitateObject), true);
    // Player
    public static readonly IngameEvent PlayerBiteObject = new(nameof(PlayerBiteObject), true);
    // Player, IPlayerEdible
    public static readonly IngameEvent PlayerEatObject = new(nameof(PlayerEatObject), true);
    // Player, Weapon, Creature (hitter)
    public static readonly IngameEvent PlayerHit = new(nameof(PlayerHit), true);
    // Player
    public static readonly IngameEvent PlayerJump = new(nameof(PlayerJump), true);
    // * Spearmaster
    // Player
    public static readonly IngameEvent SpearMake = new(nameof(SpearMake), true);
    // * Artificer
    // Player
    public static readonly IngameEvent BombJump = new(nameof(BombJump), true);
    // * Hunter
    // Player, Spear
    public static readonly IngameEvent SpearPutOnBack = new(nameof(SpearPutOnBack), true);
    // Player, Spear
    public static readonly IngameEvent SpearTakenFromBack = new(nameof(SpearTakenFromBack), true);
    // * Gourmand
    // Player
    public static readonly IngameEvent CraftObject = new(nameof(CraftObject), true); // yes arti can also craft. i know. stop yelling at me. gourmand has more crafting therefore they get the entry.
    // * Saint
    // Player
    public static readonly IngameEvent AscensionActivate = new(nameof(AscensionActivate), true);
    // Player
    public static readonly IngameEvent AscensionDeactivate = new(nameof(AscensionDeactivate), true);
    // Player, Creature
    public static readonly IngameEvent CreatureAscended = new(nameof(CreatureAscended), true);
    // Player
    public static readonly IngameEvent TongueAttach = new(nameof(TongueAttach), true);
    // Player
    public static readonly IngameEvent TongueRetract = new(nameof(TongueRetract), true);
    #endregion
    #region Oracle
    
    public static readonly IngameEvent OracleAscended = new(nameof(OracleAscended), true);
    // Explosion
    public static readonly IngameEvent OracleHitByExplosion = new(nameof(OracleHitByExplosion), true);
    // Weapon
    public static readonly IngameEvent OracleHitByWeapon = new(nameof(OracleHitByWeapon), true);
    #endregion
    #region Environment
    // Explosion
    public static readonly IngameEvent Explosion = new(nameof(Explosion), true);
    public static readonly IngameEvent Noise = new(nameof(Noise), true);
    public static readonly IngameEvent CycleEndSoon = new(nameof(CycleEndSoon), true);
    public static readonly IngameEvent CycleEnd = new(nameof(CycleEnd), true);
    public static readonly IngameEvent PrecycleEnd = new(nameof(PrecycleEnd), true);
    public static readonly IngameEvent NightStart = new(nameof(NightStart), true);
    // Creature, SingularityBomb
    public static readonly IngameEvent SingularityBombThrown = new(nameof(SingularityBombThrown), true);
    #endregion
}