using BepInEx;
using BepInEx.Logging;
using System.Security.Permissions;
using LibOracle;
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace TemplateIterator;
[BepInDependency("hexahedron1.liboracle")]
[BepInPlugin(GUID, Name, Version)]
sealed class Plugin : BaseUnityPlugin {
    public const string GUID = "templateiterator";
    public const string Name = "Template Iterator";
    public const string Version = "0.0.4";
    public new static ManualLogSource Logger;
    private static bool IsInit;

    public void OnEnable() {
        Logger = base.Logger;
        On.RainWorld.OnModsInit += OnModsInit;
    }

    private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self) {
        orig(self);
        if (IsInit) return;
        IsInit = true;
        OracleWrapper.RegisterOracle<TemplateOracle>("Template Oracle");
    }
}
