using System;
using System.Linq;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace LibOracle;
// this was pretty unnecessary but i thought it'd look cool
public class BlackScreenOfDeath : UpdatableAndDeletable, IDrawable {
    OracleWrapper Oracle;
    private ProjectedImage image;
    public BlackScreenOfDeath(Vector2 pos, OracleWrapper owner, Exception exc, bool myFault) {
        Tick = 0;
        Exception = exc;
        Pos = pos;
        Oracle = owner;
        Plugin.Logger.LogDebug($"BSOD: {Oracle}");
        if (!Oracle.PData.Broken) {
            TitleLabel = new GlyphLabel(pos, GlyphLabel.RandomString(5, exc.GetHashCode(), false)) {
                scale = 3f
            };
            // Labels[0] = new FLabel(Custom.GetFont(),
            GlyphLabels[0] = new GlyphLabel(Vector2.zero, GlyphLabel.RandomString(25, 4, false)); // choosen by a fair dice roll. guaranteed to be random. not sponsored by eMudhra
            GlyphLabels[1] = new GlyphLabel(Vector2.zero, GlyphLabel.RandomString(23, 82243, false));
            GlyphLabels[2] = new GlyphLabel(Vector2.zero, GlyphLabel.RandomString(18, 67578, false));
            GlyphLabels[3] = new GlyphLabel(Vector2.zero, GlyphLabel.RandomString(17, 15313, false));
            GlyphLabels[4] = new GlyphLabel(Vector2.zero, GlyphLabel.RandomString(25, 43223, false));
            image = Oracle.myScreen.AddImage(myFault ? "liboracle_bsod_myfault" : "liboracle_bsod");
        }
    } 
    public Vector2 Pos { get; set; }
    public int Tick { get; set; }
    public Exception Exception { get; set; }
    public GlyphLabel TitleLabel { get; set; }
    public int TitleBlink { get; set; }
    public GlyphLabel[] GlyphLabels = new GlyphLabel[5];
    private float prevGrav;
    public override void Update(bool eu) {
        base.Update(eu);
        Oracle.Conscious = false;
        if (Tick == 0) {
            Oracle.stun = int.MaxValue;
            Oracle.Crashed = true;
            if (!Oracle.PData.Broken) {
                Oracle.setGravity(0.9f);
                if (room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.DarkenLights) == null)
                    room.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.DarkenLights, 0.0f, false));
                if (room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Contrast) == null)
                    room.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Contrast, 0.0f, false));
                room.AddObject(TitleLabel);
                room.PlaySound(SoundID.Broken_Anti_Gravity_Switch_Off, 0.0f, 1f, 1f);
                room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Moon_Panic_Attack, 0.0f, 1f, 1f);
                foreach (var label in GlyphLabels) {
                    room.AddObject(label);
                }
                prevGrav = room.gravity;
            }
        }
        if (Tick == 3) {
            Vector2 rnv = Custom.RNV() * 5;
            Vector2 pos = Oracle.firstChunk.pos + rnv;
            room.AddObject(new SootMark(room, pos, 80f, true));
            room.AddObject(new Explosion(room, Oracle, pos, 7, 150f, 6.2f, 1f, 80f, 0.1f, Oracle.Player, 0.7f, 160f, 1f));
            room.AddObject(new Explosion.ExplosionLight(pos, 280f, 1f, 7, Oracle.SkinColor));
            room.AddObject(new Explosion.ExplosionLight(pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
            room.AddObject(new ExplosionSpikes(room, pos, 14, 30f, 9f, 7f, 170f, Oracle.SkinColor));
            room.AddObject(new ShockWave(pos, 330f, 0.045f, 5));
            room.PlaySound(SoundID.Bomb_Explode, pos, Oracle.abstractPhysicalObject);
            Oracle.firstChunk.vel += -rnv;
            Oracle.arm.isActive = false;
        }
        if (!Oracle.PData.Broken) {
            TitleLabel.pos = Pos;
            image.pos = Pos + new Vector2(250, -280);
            image.alpha = Tick >= 250 ? 1f : 0f;
            if (Tick == 250) room.PlaySound(SoundID.SS_AI_Image);
            for (int i = 0; i < GlyphLabels.Length; i++) {
                GlyphLabels[i].pos = Pos - new Vector2(0, 30 + 15 * i);
                if (Tick == 160 + i * 3)
                    room.PlaySound(SoundID.SS_AI_Text);
                GlyphLabels[i].visibleGlyphs = Tick >= 160 + i * 3 && Tick < 250 ? GlyphLabels[i].glyphs.Length : 0;
            }
            TitleLabel.visibleGlyphs = Tick > 92 && TitleBlink <= 0 ? 5 : 0;
            if (TitleBlink > 0) TitleBlink--;
            TitleBlink = Tick switch {
                95 or 102 or 109 or 116 or 123 or 130 or 137 => 4,
                _ => TitleBlink
            };
            float t = Mathf.Min(Tick / 90f, 1f);
            Oracle.SetPalette(25, 28, t);
            room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.DarkenLights).amount = Mathf.Lerp(0.0f, 0.8f, t);
            room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Contrast).amount = Mathf.Lerp(0.0f, 1f, t);
            if (Tick < 300) room.gravity = Mathf.Lerp(prevGrav, 1, t);
            
            if (Tick is > 300 and <= 480) {
                float t2 = (Tick - 300) / 180f;
                room.gravity = Mathf.Lerp(1, 0, t2);
            }
        }
        Tick++;
    }

    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
        
    }

    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
        /*foreach (var t in Labels) {
            t?.Redraw(false, false);
        }*/
    }

    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {
    }

    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) {
    }
}