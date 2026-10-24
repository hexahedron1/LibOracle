using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        On.OracleGraphics.ApplyPalette += On_OracleGraphics_ApplyPalette;
        On.OracleGraphics.Update += On_OracleGraphics_Update;
    }

    private static void On_OracleGraphics_ApplyPalette(On.OracleGraphics.orig_ApplyPalette orig, OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {
        orig(self, sLeaser, rCam, palette);
        if (self.owner is not OracleWrapper wrapper || wrapper.ThirdEyeSprite == null) return;
        sLeaser.sprites[self.MoonThirdEyeSprite].color = Color.Lerp(wrapper.ThirdEyeColor, self.SkinColor(), 0.5f);
    }


    public static void Unapply() {
        On.SaveState.LoadGame -= On_SaveState_LoadGame;
        On.SaveState.SaveToString -= On_SaveState_SaveToString;
        On.OracleGraphics.SkinColor -= On_OracleGraphics_SkinColor;
        On.OracleGraphics.Gown.Color -= On_Gown_Color;
        On.OracleGraphics.InitiateSprites -= On_OracleGraphics_InitiateSprites;
        On.OracleGraphics.ApplyPalette -= On_OracleGraphics_ApplyPalette;
        On.OracleGraphics.Update -= On_OracleGraphics_Update;
    }

    private static void On_OracleGraphics_Update(On.OracleGraphics.orig_Update orig, OracleGraphics self) {
        orig(self);
        if (self.owner is not OracleWrapper wrapper) return;
        self.lookDir = Vector2.ClampMagnitude(wrapper.LookPos - wrapper.firstChunk.pos, 100f) / 100f;
        self.lookDir = Vector2.ClampMagnitude(self.lookDir + self.randomTalkVector * (self.averageVoice * 0.3f), 1f);
    }

    private static void On_OracleGraphics_InitiateSprites(On.OracleGraphics.orig_InitiateSprites orig, OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
        orig(self, sLeaser, rCam);
        if (self.owner is not OracleWrapper wrapper) return;
        for (int i = 0; i < 2; i++) sLeaser.sprites[self.EyeSprite(i)].color = wrapper.EyeColor;
        if (wrapper.ThirdEyeSprite != null) sLeaser.sprites[self.MoonThirdEyeSprite] = new FSprite(wrapper.ThirdEyeSprite);
    }

    private static Color On_Gown_Color(On.OracleGraphics.Gown.orig_Color orig, OracleGraphics.Gown self, float f) =>
        self.owner.owner is OracleWrapper wrapper
            ? Color.Lerp(wrapper.RobeColor, wrapper.RobeFadeColor, f)
            : orig(self, f);

    private static Color On_OracleGraphics_SkinColor(On.OracleGraphics.orig_SkinColor orig, OracleGraphics self) =>
        self.owner is OracleWrapper wrapper ? wrapper.SkinColor : orig(self);

    private static string On_SaveState_SaveToString(On.SaveState.orig_SaveToString orig, SaveState self) {
        string output = orig(self);
        Plugin.Logger.LogInfo("Saving oracle memories...");
        List<string> notFound = [];
        foreach (var memoryField in Plugin.MemoryFields) {
            try {
                string[] split = memoryField.Split(':');
                if (notFound.Contains(split[0]) || notFound.Contains(split[1])) continue;
                Assembly asm = GetAssembly(split[0]);
                if (asm == null) {
                    Plugin.Logger.LogWarning($"Assembly of a field wasn't found: {split[0]}. Skipping...");
                    notFound.Add(split[0]);
                    continue;
                }
                Type type = asm.GetType(split[1], false, false);
                if (type == null) {
                    Plugin.Logger.LogWarning($"Declaring type of a memory wasn't found: {split[1]}. Skipping...");
                    notFound.Add(split[0]);
                    continue;
                }
                FieldInfo field = type.GetField(split[2]);
                if (field == null) {
                    Plugin.Logger.LogWarning($"Memory field not found: {memoryField}. Skipping...");
                    continue;
                }
                if (!field.IsStatic) {
                    Plugin.Logger.LogWarning($"Memory field is not static: {memoryField}. Skipping...");
                    continue;
                }

                string innerJson = JsonConvert.SerializeObject(field.GetValue(null));
                data[memoryField] = innerJson;
                Plugin.Logger.LogDebug(memoryField);
            } catch (Exception e) {
                Plugin.Logger.LogError($"FAILED TO SAVE MEMORY FIELD: {memoryField}");
                Plugin.Logger.LogError(e);
            }
        }
        string json = JsonConvert.SerializeObject(data);
        Plugin.Logger.LogInfo(json);
        string stuff = $"ORACLEMEMORY<svB>{json}";
        string[] spli2 = Regex.Split(output, "<svA>");
        string existing = spli2.FirstOrDefault(x => x.StartsWith("ORACLEMEMORY"));
        if (existing != null) {
            spli2[spli2.IndexOf(existing)] = stuff;
            output = string.Join("<svA>", spli2);
        } else output += $"{stuff}<svA>";
        Plugin.Logger.LogInfo(output);
        return output;
    }

    private static Assembly GetAssembly(string name) {
        Plugin.Logger.LogDebug($"Searching for {name}");
        var asms = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in asms) {
            Plugin.Logger.LogDebug(assembly.GetName().Name);
            if (assembly.GetName().Name == name) {
                Plugin.Logger.LogDebug("! FOUND");
                return assembly;
            }
        }
        return null;
    }

    private static Dictionary<string, string> data = [];
    
    private static void On_SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game) {
        orig(self, str, game);
        string[] split = Regex.Split(str, "<svA>");
        string jajajaja = split.FirstOrDefault(x => x.StartsWith("ORACLEMEMORY"));
        if (jajajaja == null) return;
        Plugin.Logger.LogInfo("Loading oracle memories...");
        Plugin.Logger.LogInfo(jajajaja);
        string json = Regex.Split(jajajaja, "<svB>")[1];
        Plugin.Logger.LogInfo(json);
        data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        List<string> notFound = [];
        foreach (var pair in data) {
            Plugin.Logger.LogInfo($"{pair.Key}: {pair.Value}");
            try {
                string[] spli2 = pair.Key.Split(':');
                if (notFound.Contains(spli2[0]) || notFound.Contains(spli2[1])) continue;
                Assembly asm = GetAssembly(spli2[0]);
                if (asm == null) {
                    Plugin.Logger.LogInfo($"Assembly of a field wasn't found: {spli2[0]}. If the mod is not loaded, this is normal.");
                    notFound.Add(spli2[0]);
                    continue;
                }
                Type type = asm.GetType(spli2[1], false, false);
                if (type == null) {
                    Plugin.Logger.LogInfo($"Declaring type of a field wasn't found: {spli2[1]}. If the mod is not loaded, this is normal.");
                    notFound.Add(spli2[0]);
                    continue;
                }
                FieldInfo field = type.GetField(spli2[2]);
                if (field == null) {
                    Plugin.Logger.LogWarning($"Field not found: {pair.Key}. Skipping...");
                    continue;
                }
                if (!field.IsStatic) {
                    Plugin.Logger.LogWarning($"Field is not static: {pair.Key}. Skipping...");
                    continue;
                }
                field.SetValue(null, JsonConvert.DeserializeObject(pair.Value, field.FieldType));
                Plugin.Logger.LogDebug(pair.Key);
            } catch (Exception e) {
                Plugin.Logger.LogError($"FAILED TO LOAD MEMORY FIELD: {pair.Key}");
                Plugin.Logger.LogError(e);
            }
        }
    }
}