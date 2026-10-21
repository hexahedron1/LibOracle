using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace LibOracle;
[BepInDependency("rwmodding.coreorg.pom")]
[BepInPlugin(GUID, Name, Version)]
sealed class Plugin : BaseUnityPlugin {
    public const string GUID = "hexahedron1.liboracle";
    public const string Name = "LibOracle";
    public const string Version = "0.1.0";
    public new static ManualLogSource Logger;
    private static bool IsInit;

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

        // Initialize assets, your mod config, and anything that uses RainWorld here
        Logger.LogDebug("Hello world!");
    }

    internal static Dictionary<string, object> Memories = [];
}
