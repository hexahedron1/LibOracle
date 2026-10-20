using System;
using BepInEx;
using BepInEx.Logging;
using System.Security.Permissions;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace TemplateIterator;
[BepInDependency("hexahedron1.liboracle")]
[BepInPlugin(GUID, Name, Version)]
sealed class Plugin : BaseUnityPlugin {
    public const string GUID = "templateiterator";
    public const string Name = "Template Iterator";
    public const string Version = "0.0.3";
    public new static ManualLogSource Logger;
    private static bool IsInit;

    public void OnEnable()
    {
        Logger = base.Logger;
        On.RainWorld.OnModsInit += OnModsInit;
        On.Room.ReadyForAI += On_Room_ReadyForAI;
    }

    private void On_Room_ReadyForAI(On.Room.orig_ReadyForAI orig, Room self) {
        orig(self);
        if (self.game?.session is not StoryGameSession || !string.Equals(self.abstractRoom.name, "TMIR_AI", StringComparison.OrdinalIgnoreCase)) return; 
        var pos = self.MiddleOfTile(24, 17);
        self.AddObject(new TemplateOracle(new AbstractPhysicalObject(self.world, AbstractPhysicalObject.AbstractObjectType.Oracle, null, new WorldCoordinate(self.abstractRoom.index, (int)pos.x, (int)pos.y, -1), self.game.GetNewID()), self));
        self.waitToEnterAfterFullyLoaded = Math.Max(self.waitToEnterAfterFullyLoaded, 80 /*0x50*/);
    }

    private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self) {
        orig(self);

        if (IsInit) return;
        IsInit = true;

        // Initialize assets, your mod config, and anything that uses RainWorld here
        Logger.LogDebug("Hello world!");
    }
}
