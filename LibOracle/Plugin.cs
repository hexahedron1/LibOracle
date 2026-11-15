using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using MoreSlugcats;
using Newtonsoft.Json;
using UnityEngine;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace LibOracle;
[
    BepInDependency("com.rainworldgame.garrakx.crs.mod", BepInDependency.DependencyFlags.SoftDependency),
    BepInDependency("regionkit", BepInDependency.DependencyFlags.SoftDependency),
    BepInDependency("rwmodding.coreorg.pom"),
    BepInPlugin(GUID, Name, Version)
]
sealed class Plugin : BaseUnityPlugin {
    public const string GUID = "hexahedron1.liboracle";
    public const string Name = "LibOracle";
    public const string Version = "0.2.33";
    public new static ManualLogSource Logger;
    private static bool IsInit;
    private static bool checkedCrs;
    private static bool crs;
    public static bool CrsLoaded {
        get {
            if (checkedCrs) return crs;
            crs = ModManager.ActiveMods.Exists(x => x.id == "crs");
            checkedCrs = true;
            return crs;
        }
    }

    public void OnEnable() {
        Logger = base.Logger;
        On.RainWorld.OnModsInit += OnModsInit;
        Hooks.Apply();
    }

    public void OnDisable() {
        Hooks.Unapply();
        On.RainWorld.OnModsInit -= OnModsInit;
    }
    private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self) {
        orig(self);
        if (IsInit) return;
        IsInit = true;
    }

    internal static readonly List<string> MemoryFields = [];
}
