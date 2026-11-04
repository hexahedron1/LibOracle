using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Newtonsoft.Json;
using On.Menu;
using RWCustom;
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
        //On.Oracle.ctor += On_Oracle_ctor;
        IL.Oracle.OracleArm.Joint.Update += IL_Joint_Update;
        //IL.Oracle.OracleArm.Update += IL_OracleArm_Update;
        //On.OracleGraphics.Halo.DrawSprites += On_Halo_DrawSprites;
        On.Oracle.SetUpSwarmers += On_Oracle_SetUpSwarmers;
        On.OracleGraphics.UbilicalCord.SetStuckSegments += On_UbilicalCord_SetStuckSegments;
        On.OracleGraphics.Halo.Update += On_Halo_Update;
        IL.OracleGraphics.Gown.InitiateSprite += IL_Gown_InitiateSprite;
    }
    
    private static void IL_Gown_InitiateSprite(ILContext il) {
        try {
            ILCursor c = new ILCursor(il);
            ILLabel labele = null;
            c.GotoNext(MoveType.After, x => x.MatchBrtrue(out labele));
            c.Emit(OpCodes.Ldarg, 0);
            c.EmitDelegate((OracleGraphics.Gown gown) => gown.owner.owner is OracleWrapper wrapper && wrapper.PData.Broken);
            c.Emit(OpCodes.Brtrue, labele);
            Plugin.Logger.LogInfo("Successfully IL hooked Gown.InitiateSprite");
        } catch (Exception e) {
            Plugin.Logger.LogError("FAILED TO IL HOOK Gown.InitiateSprite");
            Plugin.Logger.LogError(e);
        }
    }
    private static void On_Halo_Update(On.OracleGraphics.Halo.orig_Update orig, OracleGraphics.Halo self) {
        if (self.owner.owner is OracleWrapper wrapper && wrapper.PData.Broken) return;
        orig(self);
    }
    private static void On_UbilicalCord_SetStuckSegments(On.OracleGraphics.UbilicalCord.orig_SetStuckSegments orig, OracleGraphics.UbilicalCord self) {
        orig(self);
        if (self.owner.owner is not OracleWrapper wrapper) return;
        self.coord[0, 0] = wrapper.BottomCenter - new Vector2(0, 10);
    }
    public static void Unapply() {
        On.SaveState.LoadGame -= On_SaveState_LoadGame;
        On.SaveState.SaveToString -= On_SaveState_SaveToString;
        On.OracleGraphics.SkinColor -= On_OracleGraphics_SkinColor;
        On.OracleGraphics.Gown.Color -= On_Gown_Color;
        On.OracleGraphics.InitiateSprites -= On_OracleGraphics_InitiateSprites;
        On.OracleGraphics.ApplyPalette -= On_OracleGraphics_ApplyPalette;
        On.OracleGraphics.Update -= On_OracleGraphics_Update;
        //On.Oracle.ctor -= On_Oracle_ctor;
        IL.Oracle.OracleArm.Joint.Update -= IL_Joint_Update;
        //IL.Oracle.OracleArm.Update -= IL_OracleArm_Update;
        On.Oracle.SetUpSwarmers -= On_Oracle_SetUpSwarmers;
        On.OracleGraphics.UbilicalCord.SetStuckSegments -= On_UbilicalCord_SetStuckSegments;
        On.OracleGraphics.Halo.Update -= On_Halo_Update;
        IL.OracleGraphics.Gown.InitiateSprite -= IL_Gown_InitiateSprite;
    }
    private static void On_Oracle_SetUpSwarmers(On.Oracle.orig_SetUpSwarmers orig, Oracle self) {
        if (self is OracleWrapper) return;
        orig(self);
    }

    private static void On_Halo_DrawSprites(On.OracleGraphics.Halo.orig_DrawSprites orig, OracleGraphics.Halo self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (self.owner.owner is not OracleWrapper wrapper) return;
        foreach (var sprite in sLeaser.sprites) {
            sprite.alpha = wrapper.Conscious ? sprite.alpha : 0f;
        }
    }

    private static void IL_Joint_Update(ILContext il) {
        ILCursor ilCursor = new ILCursor(il);
        if (ilCursor.TryGotoNext(MoveType.After, x => x.MatchLdsfld<Oracle.OracleID>("SS"), x => x.MatchCall(out MethodReference _)))
            ilCursor.Emit(OpCodes.Ldarg_0).EmitDelegate((Func<bool, Oracle.OracleArm.Joint, bool>) ((flag, self) => flag || self.arm is OracleWrapper.Arm));
        else
            Plugin.Logger.LogError("Couldn't ILHook Oracle.OracleArm.Joint.Update!");
    }
    private static void IL_OracleArm_Update(ILContext il) {
        ILCursor ilCursor = new ILCursor(il);
        for (int index = 1; index <= 3; ++index) {
            if (ilCursor.TryGotoNext(MoveType.After, x => x.MatchLdsfld<Oracle.OracleID>("SS"), x => x.MatchCall(out MethodReference _)))
                ilCursor.Emit(OpCodes.Ldarg_0).EmitDelegate((Func<bool, Oracle.OracleArm, bool>) ((flag, self) => flag || self is OracleWrapper.Arm));
            else
                Plugin.Logger.LogError($"Couldn't ILHook Oracle.OracleArm.Update (part {index})!");
        }
    }

    private static void On_Oracle_ctor(On.Oracle.orig_ctor orig, Oracle self, AbstractPhysicalObject abstractPhysicalObject, Room room) {
        if (self is OracleWrapper) return;
        orig(self, abstractPhysicalObject, room);
    }

    private static void On_OracleGraphics_ApplyPalette(On.OracleGraphics.orig_ApplyPalette orig, OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {
        orig(self, sLeaser, rCam, palette);
        if (self.owner is not OracleWrapper wrapper || wrapper.ThirdEyeSprite == null) return;
        sLeaser.sprites[self.MoonThirdEyeSprite].color = Color.Lerp(wrapper.ThirdEyeColor, self.SkinColor(), 0.5f);
    }
    // please excuse the copypasted shitcode from decompiled OracleGraphics
    private static void On_OracleGraphics_Update(On.OracleGraphics.orig_Update orig, OracleGraphics self) {
        orig(self);
        if (self.owner is not OracleWrapper wrapper) return;
        if (self.lightsource != null) {
            if (wrapper.PData.Broken) self.lightsource.alpha = 0f;
            self.lightsource.color = wrapper.LightColor;
        }
        self.lookDir = Vector2.ClampMagnitude(wrapper.LookPos - wrapper.firstChunk.pos, 100f) / 100f;
        self.lookDir = Vector2.ClampMagnitude(self.lookDir + self.randomTalkVector * (self.averageVoice * 0.3f), 1f);
        self.eyesOpen = wrapper.EyesOpen ? 1f : 0f;
        for (int i = 0; i < 2; i++) {
            // idk how to name this variable lmao
            Vector2? mysteriousVector = !wrapper.HoldKnees 
                ? SharedPhysics.ExactTerrainRayTracePos(wrapper.room, wrapper.bodyChunks[wrapper.InSitPosition ? 0 : 1].pos, wrapper.bodyChunks[wrapper.InSitPosition ? 0 : 1].pos + new Vector2(i == 0 ? -54f : -24f, -40f) * (2f * wrapper.CrawlSpeed)) 
                : SharedPhysics.ExactTerrainRayTracePos(wrapper.room, wrapper.firstChunk.pos, wrapper.firstChunk.pos + new Vector2(i == 0 ? -24f : -14f, -40f));
            if (mysteriousVector.HasValue) {
                GenericBodyPart foot5 = self.feet[i];
                foot5.vel = foot5.vel + Vector2.ClampMagnitude(mysteriousVector.Value - self.feet[i].pos, 10f) / 2f;
            }
            if (wrapper.HoldKnees) {
                GenericBodyPart hand12 = self.hands[i];
                hand12.vel += Vector2.ClampMagnitude(self.knees[i, 0] - self.hands[i].pos, 10f) / 3f;
            }
            Vector2 vector2_6;
            if (mysteriousVector.HasValue)
            {
                Vector2 p2 = self.feet[i].pos + Custom.DirVec(wrapper.bodyChunks[1].pos, wrapper.bodyChunks[0].pos) * 15f;
                vector2_6 = Vector2.Lerp(p2 + Custom.DirVec(wrapper.firstChunk.pos, p2) * 5f, (self.feet[i].pos + wrapper.bodyChunks[1].pos) / 2f, Mathf.InverseLerp(7f, 14f, Vector2.Distance(self.feet[i].pos, wrapper.bodyChunks[1].pos)));
            }
            else
                vector2_6 = (self.feet[i].pos + wrapper.bodyChunks[1].pos) / 2f + Custom.PerpendicularVector(wrapper.bodyChunks[1].pos, wrapper.bodyChunks[0].pos) * (float) ((i == 0 ? -1.0 : 1.0) * 5.0);
            self.knees[i, 0] = Vector2.Lerp(self.knees[i, 0], vector2_6, 0.4f);
            if (!Custom.DistLess(self.knees[i, 0], vector2_6, 15f))
                self.knees[i, 0] = vector2_6 + Custom.DirVec(vector2_6, self.knees[i, 0]);
        }
    }

    private static void On_OracleGraphics_InitiateSprites(On.OracleGraphics.orig_InitiateSprites orig, OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
        orig(self, sLeaser, rCam);
        if (self.owner is not OracleWrapper wrapper) return;
        for (int i = 0; i < 2; i++) sLeaser.sprites[self.EyeSprite(i)].color = wrapper.EyeColor;
        if (wrapper.ThirdEyeSprite != null) sLeaser.sprites[self.MoonThirdEyeSprite] = new FSprite(wrapper.ThirdEyeSprite);
        sLeaser.sprites[self.fadeSprite].color = wrapper.LightColor;
        sLeaser.sprites[self.fadeSprite].alpha = wrapper.LightAlpha;
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
                    notFound.Add(split[1]);
                    continue;
                }
                FieldInfo crashed = type?.GetField(nameof(OracleWrapper.Crashed));
                if (crashed != null && (bool)crashed.GetValue(null)) {
                    Plugin.Logger.LogWarning($"Declaring type of a memory has suffered a crash: {split[1]}. Skipping...");
                    notFound.Add(split[1]);
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
        //Plugin.Logger.LogDebug($"Searching for {name}");
        var asms = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in asms) {
            //Plugin.Logger.LogDebug(assembly.GetName().Name);
            if (assembly.GetName().Name == name) {
                //Plugin.Logger.LogDebug("! FOUND");
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