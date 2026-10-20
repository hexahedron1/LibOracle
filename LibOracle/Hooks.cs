using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Newtonsoft.Json;
using UnityEngine;

namespace LibOracle;

public class Hooks {
    public static void Apply() {
        On.SaveState.LoadGame += On_SaveState_LoadGame;
        On.SaveState.SaveToString += On_SaveState_SaveToString;
        On.OracleGraphics.SkinColor += On_OracleGraphics_SkinColor;
        On.OracleGraphics.Gown.Color += On_Gown_Color;
        On.OracleGraphics.InitiateSprites += On_OracleGraphics_InitiateSprites;
    }
    

    public static void Unapply() {
        On.SaveState.LoadGame -= On_SaveState_LoadGame;
        On.SaveState.SaveToString -= On_SaveState_SaveToString;
        On.OracleGraphics.SkinColor -= On_OracleGraphics_SkinColor;
        On.OracleGraphics.Gown.Color -= On_Gown_Color;
        On.OracleGraphics.InitiateSprites -= On_OracleGraphics_InitiateSprites;
    }
    
    private static void On_OracleGraphics_InitiateSprites(On.OracleGraphics.orig_InitiateSprites orig, OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
        orig(self, sLeaser, rCam);
        if (self.owner is not OracleWrapper wrapper) return;
        for (int i = 0; i < 2; i++) sLeaser.sprites[self.EyeSprite(i)].color = wrapper.EyeColor;
    }

    private static Color On_Gown_Color(On.OracleGraphics.Gown.orig_Color orig, OracleGraphics.Gown self, float f) =>
        self.owner.owner is OracleWrapper wrapper
            ? Color.Lerp(wrapper.RobeColor, wrapper.RobeFadeColor, f)
            : orig(self, f);

    private static Color On_OracleGraphics_SkinColor(On.OracleGraphics.orig_SkinColor orig, OracleGraphics self) =>
        self.owner is OracleWrapper wrapper ? wrapper.SkinColor : orig(self);

    private static string On_SaveState_SaveToString(On.SaveState.orig_SaveToString orig, SaveState self) {
        string output = orig(self);
        string json = JsonConvert.SerializeObject(Plugin.Memories);
        output += $"<svB>{json}<svA>";
        return output;
    }

    private static void On_SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game) {
        orig(self, str, game);
        string[] split = Regex.Split(str, "<svA>");
        string jajajaja = split.FirstOrDefault(x => x.StartsWith("ORACLEMEMORY"));
        if (jajajaja == null) return;
        string json = Regex.Split(jajajaja, "<svB>")[1];
        Plugin.Memories = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
    }
}