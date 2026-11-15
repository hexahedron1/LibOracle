using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using Newtonsoft.Json;
using Noise;
using On.Menu;
using RWCustom;
using UnityEngine;

namespace LibOracle;

public class Hooks {
    public static void Apply() {
        try {
            On.SaveState.LoadGame += On_SaveState_LoadGame;
            On.SaveState.SaveToString += On_SaveState_SaveToString;
            On.OracleGraphics.SkinColor += On_OracleGraphics_SkinColor;
            On.OracleGraphics.Gown.Color += On_Gown_Color;
            On.OracleGraphics.ApplyPalette += On_OracleGraphics_ApplyPalette;
            On.OracleGraphics.Update += On_OracleGraphics_Update;
            On.Oracle.SetUpSwarmers += On_Oracle_SetUpSwarmers;
            On.OracleGraphics.UbilicalCord.SetStuckSegments += On_UbilicalCord_SetStuckSegments;
            On.OracleGraphics.Halo.Update += On_Halo_Update;
            On.OracleGraphics.DrawSprites += On_OracleGraphics_DrawSprites;
            #region IngameEvent
            On.Player.Die += (orig, self) => {
                orig(self);
                self.room.IngameEvent(IngameEvent.PlayerDied, self.DangerPos, new Tuple<Player>(self));
            };
            On.Player.SlugcatGrab += (orig, self, o, used) => {
                orig(self, o, used);
                self.room.IngameEvent(IngameEvent.PlayerGrabObject, self.DangerPos, new Tuple<Player, PhysicalObject>(self, o));
            };
            On.Player.ReleaseObject += (orig, self, grasp, eu) => {
                self.room.IngameEvent(IngameEvent.PlayerReleaseObject, self.DangerPos, new Tuple<Player, PhysicalObject>(self, self.grasps[grasp].grabbed));
                orig(self, grasp, eu);
            };
            On.Player.ThrowObject += (orig, self, grasp, eu) => {
                self.room.IngameEvent(IngameEvent.PlayerThrowObject, self.DangerPos, new Tuple<Player, PhysicalObject>(self, self.grasps[grasp].grabbed));
                orig(self, grasp, eu);
            };
            On.Player.SwallowObject += (orig, self, grasp) => {
                orig(self, grasp);
                self.room.IngameEvent(IngameEvent.PlayerSwallowObject, self.DangerPos, new Tuple<Player, PhysicalObject>(self, self.grasps[grasp].grabbed));
            };
            On.Player.Regurgitate += (orig, self) => {
                orig(self);
                self.room.IngameEvent(IngameEvent.PlayerRegurgitateObject, self.DangerPos, new Tuple<Player>(self));
            };
            On.Player.BiteEdibleObject += (orig, self, eu) => {
                orig(self, eu);
                self.room.IngameEvent(IngameEvent.PlayerBiteObject, self.DangerPos, new Tuple<Player>(self));
            };
            On.Player.ObjectEaten += (orig, self, edible) => {
                orig(self, edible);
                self.room.IngameEvent(IngameEvent.PlayerEatObject, self.DangerPos, new Tuple<Player>(self));
            };
            On.Weapon.HitSomething += (orig, self, result, eu) => {
                var ret = orig(self, result, eu);
                if (ret && result.obj is Player player) self.room.IngameEvent(IngameEvent.PlayerHit, result.collisionPoint, new Tuple<Player, Weapon, Creature>(player, self, self.thrownBy));
                return ret;
            };
            On.Player.Jump += (orig, self) => {
                orig(self);
                self.room.IngameEvent(IngameEvent.PlayerJump, self.DangerPos);
            };
            On.PlayerGraphics.TailSpeckles.setSpearProgress += (orig, self, f) => {
                orig(self, f);
                if (Mathf.Approximately(f, 1.0f)) self.pGraphics.owner.room.IngameEvent(IngameEvent.SpearMake, self.pGraphics.player.DangerPos, new Tuple<Player>(self.pGraphics.player));
            };
            // TODO: IL hook arti jump
            IL.Player.ClassMechanicsArtificer += IL_Player_ClassMechanicsArtificer;
            On.Player.SpearOnBack.SpearToBack += (orig, self, spr) => {
                orig(self, spr);
                self.owner.room.IngameEvent(IngameEvent.SpearPutOnBack, spr.firstChunk.pos, new Tuple<Player, Spear>(self.owner, spr));
            };
            On.Player.SpearOnBack.SpearToHand += (orig, self, eu) => {
                orig(self, eu);
                self.owner.room.IngameEvent(IngameEvent.SpearPutOnBack, self.spear.firstChunk.pos, new Tuple<Player, Spear>(self.owner, self.spear));
            };
            On.Player.SpitUpCraftedObject += (orig, self) => {
                orig(self);
                self.room.IngameEvent(IngameEvent.CraftObject, self.DangerPos, new Tuple<Player>(self));
            };
            On.Player.ActivateAscension += (orig, self) => {
                orig(self);
                self.room.IngameEvent(IngameEvent.AscensionActivate, self.DangerPos, new Tuple<Player>(self));
            };
            On.Player.DeactivateAscension += (orig, self) => {
                orig(self);
                self.room.IngameEvent(IngameEvent.AscensionDeactivate, self.DangerPos, new Tuple<Player>(self));
            };
            IL.Player.ClassMechanicsSaint += IL_Player_ClassMechanicsSaint;
            On.Player.Tongue.AttachToTerrain += (orig, self, pos) => {
                orig(self, pos);
                self.player.room.IngameEvent(IngameEvent.TongueAttach, pos, new Tuple<Player, bool>(self.player, false));
            };
            On.Player.Tongue.AttachToChunk += (orig, self, chunk) => {
                orig(self, chunk);
                self.player.room.IngameEvent(IngameEvent.TongueAttach, chunk.pos, new Tuple<Player, bool>(self.player, true));
            };
            On.Player.Tongue.Release += (orig, self) => {
                orig(self);
                self.player.room.IngameEvent(IngameEvent.TongueRetract, self.pos, new Tuple<Player>(self.player));
            };
            On.Explosion.ctor += (orig, self, room, sourceObject, pos, time, rad, force, damage, stun, deafen, holder, factor, minStun, noise) => {
                orig(self, room, sourceObject, pos, time, rad, force, damage, stun, deafen, holder, factor, minStun, noise);
                room.IngameEvent(IngameEvent.Explosion, self.pos, new Tuple<Explosion>(self));
            };
            On.Room.InGameNoise += (orig, self, noise) => {
                orig(self, noise);
                self.IngameEvent(IngameEvent.Noise, noise.pos, new Tuple<InGameNoise>(noise));
            };
            On.Room.Update += (orig, self) => {
                orig(self);
                var cycle = self.game.world.rainCycle;
                if (cycle.timer == cycle.cycleLength - 800) self.IngameEvent(IngameEvent.CycleEndSoon, Vector2.zero);
                if (cycle.timer == cycle.cycleLength) self.IngameEvent(IngameEvent.CycleEnd, Vector2.zero);
                if (cycle.preTimer == 1) self.IngameEvent(IngameEvent.PrecycleEnd, Vector2.zero);
                if (cycle.dayNightCounter == 2640) self.IngameEvent(IngameEvent.NightStart, Vector2.zero);
            };
            On.MoreSlugcats.SingularityBomb.Thrown += (orig, self, by, pos, fromPos, dir, frc, eu) => {
                orig(self, by, pos, fromPos, dir, frc, eu);
                self.room.IngameEvent(IngameEvent.SingularityBombThrown, self.thrownPos, new Tuple<Creature, SingularityBomb>(by, self));
            };

            #endregion
            // evil hooks don't interact
            IL.Oracle.OracleArm.Joint.Update += IL_Joint_Update;
            IL.OracleGraphics.Gown.InitiateSprite += IL_Gown_InitiateSprite;
            IL.OracleGraphics.ctor += IL_OracleGraphics_ctor;
            IL.OracleGraphics.InitiateSprites += IL_OracleGraphics_InitiateSprites;
        }
        catch (Exception e) {
            Plugin.Logger.LogError("Shit has hit the fan");
            Plugin.Logger.LogError(e);
        }
    }
    // this was originally an On hook until i needed to add the forehead sprite and you apparently CANNOT run the code strictly before or after the original code if you want new sprites. 
    private static void IL_OracleGraphics_InitiateSprites(ILContext il) {
        try {
            Plugin.Logger.LogInfo("ILn't hooking OracleGraphics.InitiateSprites");
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.Before, x => x.MatchCallvirt(typeof(GraphicsModule).GetMethod(nameof(GraphicsModule.AddToContainer))));
            c.GotoNext(MoveType.Before, x => x.MatchLdarg(0));
            for (int i = 0; i < 3; i++) c.Emit(OpCodes.Ldarg, i);
            c.EmitDelegate(On_OracleGraphics_InitiateSprites);
            Plugin.Logger.LogInfo("Yay");
        } catch (Exception e) {
            Plugin.Logger.LogError("FAIL");
            Plugin.Logger.LogError(e);
        }
    }
    private static void IL_OracleGraphics_ctor(ILContext il) {
        try {
            Plugin.Logger.LogInfo("IL hooking OracleGraphics.ctor");
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After, x => x.MatchCall(
                typeof(OracleGraphics).GetMethod("get_IsMoon")));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((bool isMoon, OracleGraphics self) => {
                bool ejeje = isMoon || self.oracle is OracleWrapper wrapper && wrapper.PData.DisconnectedCord;
                Plugin.Logger.LogDebug("checking if has broken cord: " + ejeje);
                return ejeje;
            });
            /*
            for (int i = 0; i < 3; i++)
                c.GotoNext(MoveType.After, x => x.MatchCall(
                    typeof(OracleGraphics).GetMethod("get_IsMoon")));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((bool isMoon, OracleGraphics self) => {
                bool ejeje = isMoon || self.oracle is OracleWrapper { ThirdEyeSprite: not null };
                Plugin.Logger.LogDebug("checking if has 3rd eye: " + ejeje);
                return ejeje;
            });*/
            Plugin.Logger.LogInfo("Yay");
        } catch (Exception e) {
            Plugin.Logger.LogError("FAIL");
            Plugin.Logger.LogError(e);
        }
    }
    private static void On_OracleGraphics_DrawSprites(On.OracleGraphics.orig_DrawSprites orig, OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
        orig(self, sLeaser, rCam, timeStacker, camPos);
        if (self.oracle is not OracleWrapper wrapper || wrapper.ThirdEyeSprite == null) return;
        Vector2 vector2_1 = Vector2.Lerp(self.owner.firstChunk.lastPos, self.owner.firstChunk.pos, timeStacker);
        Vector2 vector2_4 = Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker);
        Vector2 v2 = Custom.DirVec(vector2_4, vector2_1);
        Vector2 vector2_5 = Custom.PerpendicularVector(v2);
        Vector2 vector2_6 = self.RelativeLookDir(timeStacker);
        Vector2 p1 = vector2_4 + vector2_5 * (vector2_6.x * 2.5f) + v2 * (float) (-2.0 - vector2_6.y * 1.5);
        sLeaser.sprites[self.MoonThirdEyeSprite].x = p1.x - camPos.x;
        sLeaser.sprites[self.MoonThirdEyeSprite].y = p1.y - camPos.y;
        sLeaser.sprites[self.MoonThirdEyeSprite].rotation = Custom.AimFromOneVectorToAnother(p1, vector2_4 - v2 * 10f);
        sLeaser.sprites[self.MoonThirdEyeSprite].scaleX = Mathf.Lerp(wrapper.ThirdEyeScale, wrapper.ThirdEyeScale*0.75f, Mathf.Abs(vector2_6.x));
        sLeaser.sprites[self.MoonThirdEyeSprite].scaleY = Custom.LerpMap(vector2_6.y, 0.0f, 1f, wrapper.ThirdEyeScale, wrapper.ThirdEyeScale*0.25f);
    }
    private static void IL_Player_ClassMechanicsArtificer(ILContext il) {
        try {
            Plugin.Logger.LogInfo("IL hooking Player.ClassMechanicsArtificer");
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After, x => x.MatchNewobj<Explosion.ExplosionLight>());
            c.GotoNext(MoveType.After, x => x.MatchCallvirt(typeof(Room).GetMethod(nameof(Room.AddObject))));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Player self) => {
                self.room.IngameEvent(IngameEvent.BombJump, self.firstChunk.pos);
            });
            Plugin.Logger.LogInfo("Yay");
        } catch (Exception e) {
            Plugin.Logger.LogError("FAIL");
            Plugin.Logger.LogError(e);
        }
    }
    public class PobjData {
        public bool ascended = false;
    }
    public static readonly ConditionalWeakTable<PhysicalObject, PobjData> pobjCWT = new();
    public static bool TryGetData(PhysicalObject key, out PobjData data) {
        data = key != null ? pobjCWT.GetOrCreateValue(key) : null;
        return data != null;
    }
    private static void IL_Player_ClassMechanicsSaint(ILContext il) {
        try {
            Plugin.Logger.LogInfo("IL hooking Player.ClassMechanicsSaint");
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After, x => x.MatchIsinst<Creature>());
            int physicalObject = 0;
            c.GotoPrev(MoveType.Before, x => x.MatchLdloc(out physicalObject));
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, physicalObject);
            c.EmitDelegate((Player self, PhysicalObject obj) => {
                if (!TryGetData(obj, out PobjData data) || data.ascended) return;
                Plugin.Logger.LogDebug($"Tried to ascend {obj.GetType().Name}");
                data.ascended = true;
                switch (obj) {
                    case Creature creature:
                        self.room.IngameEvent(IngameEvent.CreatureAscended, creature.firstChunk.pos, new Tuple<Player, Creature>(self, creature));
                        break;
                    case Oracle oracle:
                        self.room.IngameEvent(IngameEvent.OracleAscended, oracle.firstChunk.pos, new Tuple<Player, Oracle>(self, oracle));
                        break;
                }
            });
            c.GotoNext(MoveType.After, x => x.MatchIsinst<STOracleBehavior>());
            c.GotoNext(MoveType.After, x => x.MatchBlt(out _));
            c.Emit(OpCodes.Ldloc, physicalObject);
            c.EmitDelegate((PhysicalObject obj) => {
                if (!TryGetData(obj, out PobjData data) || !data.ascended) return;
                data.ascended = false;
            });
            Plugin.Logger.LogInfo("Yay");
        } catch (Exception e) {
            Plugin.Logger.LogError("FAIL");
            Plugin.Logger.LogError(e);
        }
    }

    private static void IL_Gown_InitiateSprite(ILContext il) {
        try {
            Plugin.Logger.LogInfo("IL hooking Gown.InitiateSprite");
            ILCursor c = new ILCursor(il);
            ILLabel labele = null;
            c.GotoNext(MoveType.After, x => x.MatchBrtrue(out labele));
            c.Emit(OpCodes.Ldarg, 0);
            c.EmitDelegate((OracleGraphics.Gown gown) => gown.owner.owner is OracleWrapper wrapper && wrapper.PData.Broken);
            c.Emit(OpCodes.Brtrue, labele);
            Plugin.Logger.LogInfo("Yay");
        } catch (Exception e) {
            Plugin.Logger.LogError("FAIL");
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
        On.OracleGraphics.ApplyPalette -= On_OracleGraphics_ApplyPalette;
        On.OracleGraphics.Update -= On_OracleGraphics_Update;
        On.OracleGraphics.DrawSprites -= On_OracleGraphics_DrawSprites;
        On.Oracle.SetUpSwarmers -= On_Oracle_SetUpSwarmers;
        On.OracleGraphics.UbilicalCord.SetStuckSegments -= On_UbilicalCord_SetStuckSegments;
        On.OracleGraphics.Halo.Update -= On_Halo_Update;
        IL.Oracle.OracleArm.Joint.Update -= IL_Joint_Update;
        IL.OracleGraphics.Gown.InitiateSprite -= IL_Gown_InitiateSprite;
        IL.OracleGraphics.ctor -= IL_OracleGraphics_ctor;
        IL.OracleGraphics.InitiateSprites -= IL_OracleGraphics_InitiateSprites;
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
                GenericBodyPart foot = self.feet[i];
                foot.vel = foot.vel + Vector2.ClampMagnitude(mysteriousVector.Value - self.feet[i].pos, 10f) / 2f;
            }
            if (wrapper.HoldKnees) {
                GenericBodyPart hand = self.hands[i];
                hand.vel += Vector2.ClampMagnitude(self.knees[i, 0] - self.hands[i].pos, 10f) / 3f;
            } else if (wrapper.HandDirections[i] != null) {
                GenericBodyPart hand = self.hands[i];
                hand.vel += Custom.DirVec(hand.pos, wrapper.HandDirections[i].Value);
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

    private static void On_OracleGraphics_InitiateSprites(OracleGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
        if (self.owner is OracleWrapper wrapper) {
            for (int i = 0; i < 2; i++) sLeaser.sprites[self.EyeSprite(i)].color = wrapper.EyeColor;
            if (wrapper.ThirdEyeSprite != null && false) {
                sLeaser.sprites[self.MoonThirdEyeSprite] = new FSprite(wrapper.ThirdEyeSprite) {
                    color = wrapper.ThirdEyeColor
                };
            }
            sLeaser.sprites[self.fadeSprite].color = wrapper.LightColor;
            sLeaser.sprites[self.fadeSprite].alpha = wrapper.LightAlpha;
        }
    }

    private static Color On_Gown_Color(On.OracleGraphics.Gown.orig_Color orig, OracleGraphics.Gown self, float f) =>
        self.owner.owner is OracleWrapper wrapper
            ? Color.Lerp(wrapper.RobeColor, wrapper.RobeFadeColor, f)
            : orig(self, f);

    private static Color On_OracleGraphics_SkinColor(On.OracleGraphics.orig_SkinColor orig, OracleGraphics self) =>
        self.owner is OracleWrapper wrapper ? wrapper.SkinColor : orig(self);

    private static string On_SaveState_SaveToString(On.SaveState.orig_SaveToString orig, SaveState self) {
        string output = orig(self);
        Plugin.Logger.LogInfo($"Saving oracle memories for save file {self.currentTimelinePosition.value}...");
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
                memoryData[memoryField] = innerJson;
                Plugin.Logger.LogDebug(memoryField);
            } catch (Exception e) {
                Plugin.Logger.LogError($"FAILED TO SAVE MEMORY FIELD: {memoryField}");
                Plugin.Logger.LogError(e);
            }
        }
        string json = JsonConvert.SerializeObject(memoryData);
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

    private static Dictionary<string, string> memoryData = [];
    
    private static void On_SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game) {
        orig(self, str, game);
        string[] split = Regex.Split(str, "<svA>");
        string jajajaja = split.FirstOrDefault(x => x.StartsWith("ORACLEMEMORY"));
        if (jajajaja == null) return;
        Plugin.Logger.LogInfo($"Loading oracle memories for save file {self.currentTimelinePosition.value}...");
        string json = Regex.Split(jajajaja, "<svB>")[1];
        Plugin.Logger.LogInfo(json);
        memoryData = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        List<string> notFound = [];
        foreach (var pair in memoryData) {
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