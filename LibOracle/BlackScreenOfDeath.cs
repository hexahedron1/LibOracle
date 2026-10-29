using System;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace LibOracle;

public class BlackScreenOfDeath : UpdatableAndDeletable, IDrawable {
    OracleWrapper Oracle;
    public BlackScreenOfDeath(Vector2 pos, OracleWrapper owner, Exception exc, bool myFault) {
        Tick = 0;
        Exception = exc;
        Pos = pos;
        Oracle = owner;
        Plugin.Logger.LogDebug($"BSOD: {Oracle}");
        TitleLabel = new GlyphLabel(pos, GlyphLabel.RandomString(5, exc.GetHashCode(), false)) {
            scale = 3f
        };
        // Labels[0] = new FLabel(Custom.GetFont(),
        Labels[0] = new FLabel(Custom.GetFont(),$"A problem has been detected and {Oracle.Name} has been shut down.");
        GlyphLabels[0] = new GlyphLabel(Vector2.zero, GlyphLabel.RandomString(25, 12345, false));
        Labels[2] = new FLabel(Custom.GetFont(),$"A mistake was made by {(myFault ? "libOracle" : "the mod")}'s developer which lead to an uncaught exception.");
        GlyphLabels[1] = new GlyphLabel(Vector2.zero, GlyphLabel.RandomString(23, 82243, false));
        Labels[4] = new FLabel(Custom.GetFont(), "If this is the first time you've seen this stop error screen, hibernate.");
        Labels[5] = new FLabel(Custom.GetFont(), "If this screen appears again, follow these steps:");
        GlyphLabels[2] = new GlyphLabel(Vector2.zero, GlyphLabel.RandomString(18, 67578, false));
        Labels[7] = new FLabel(Custom.GetFont(), "Check for any incompatible mods that are installed");
        Labels[8] = new FLabel(Custom.GetFont(),$"If problems continue, contact {(myFault ? "libOracle" : "the mod")}'s developer for assistance or a fix.");
        GlyphLabels[3] = new GlyphLabel(Vector2.zero, GlyphLabel.RandomString(17, 15313, false));
        Labels[10] = new FLabel(Custom.GetFont(), $"Technical information:*** STOP: 0x{exc.GetHashCode():x8} ({exc.GetType().Name}: {exc.Message})");
        Labels[11] = new FLabel(Custom.GetFont(),"Consult BepInEx/LogOutput.txt for stack trace and prior logged events.");
        GlyphLabels[4] = new GlyphLabel(Vector2.zero, GlyphLabel.RandomString(25, 43223, false));
    } 
    public Vector2 Pos { get; set; }
    public int Tick { get; set; }
    public Exception Exception { get; set; }
    public GlyphLabel TitleLabel { get; set; }
    public int TitleBlink { get; set; }
    public int SwitchFontBlink { get; set; }
    public FLabel[] Labels = new FLabel[12];
    public GlyphLabel[] GlyphLabels = new GlyphLabel[5];
    private float prevGrav;
    public override void Update(bool eu) {
        base.Update(eu);
        Oracle.Conscious = false;
        if (Tick == 0) {
            Oracle.setGravity(0.9f);
            Oracle.stun = int.MaxValue;
            if (room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.DarkenLights) == null)
                room.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.DarkenLights, 0.0f, false));
            if (room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Contrast) == null)
                room.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Contrast, 0.0f, false));
            room.AddObject(TitleLabel);
            room.PlaySound(SoundID.Broken_Anti_Gravity_Switch_Off, 0.0f, 1f, 1f);
            room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Moon_Panic_Attack, 0.0f, 1f, 1f);
            room.AddObject(new SootMark(room, Oracle.firstChunk.pos, 80f, true));
            room.AddObject(new Explosion(room, Oracle, Oracle.firstChunk.pos + Custom.RNV()*5, 7, 150f, 6.2f, 1f, 80f, 0.1f, Oracle.Player, 0.7f, 160f, 1f));
            room.AddObject(new Explosion.ExplosionLight(Oracle.firstChunk.pos, 280f, 1f, 7, Oracle.SkinColor));
            room.AddObject(new Explosion.ExplosionLight(Oracle.firstChunk.pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
            room.AddObject(new ExplosionSpikes(room, Oracle.firstChunk.pos, 14, 30f, 9f, 7f, 170f, Oracle.SkinColor));
            room.AddObject(new ShockWave(Oracle.firstChunk.pos, 330f, 0.045f, 5));
            room.PlaySound(SoundID.Bomb_Explode, Oracle.firstChunk.pos, Oracle.abstractPhysicalObject);
            foreach (var label in GlyphLabels) {
                room.AddObject(label);
            }
            prevGrav = room.gravity;
        }
        TitleLabel.pos = Pos;
        for (int i = 0; i < Labels.Length; i++) {
            if (Labels[i] == null) continue;
            Labels[i].SetPosition(Pos - new Vector2(-200, 20*i));
            Labels[i].isVisible = SwitchFontBlink > 0 || Tick >= 240;
        }
        for (int i = 0; i < GlyphLabels.Length; i++) {
            GlyphLabels[i].pos = Pos - new Vector2(0, 30 + 15*i);
            if (Tick == 160 + i * 3)
                room.PlaySound(SoundID.SS_AI_Text);
            GlyphLabels[i].visibleGlyphs = Tick >= 160 + i*3 && SwitchFontBlink == 0 && Tick < 240 ? GlyphLabels[i].glyphs.Length : 0;
        }
        TitleLabel.visibleGlyphs = Tick > 92 && TitleBlink <= 0 ? 5 : 0;
        if (TitleBlink > 0) TitleBlink--;
        if (SwitchFontBlink > 0) SwitchFontBlink--;
        TitleBlink = Tick switch {
            95 or 102 or 109 or 116 or 123 or 130 or 137 => 4,
            _ => TitleBlink
        };
        if (Tick is 224 or 231 or 238) SwitchFontBlink = 4;

        if (Tick <= 90) {
            float t = Tick / 90f;
            Oracle.SetPalette(25, 28, t);
            room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.DarkenLights).amount = Mathf.Lerp(0.0f, 0.8f, t);
            room.roomSettings.GetEffect(RoomSettings.RoomEffect.Type.Contrast).amount = Mathf.Lerp(0.0f, 1f, t);
            room.gravity = Mathf.Lerp(prevGrav, 1, t);
        }

        if (Tick is > 300 and <= 480) {
            float t = (Tick-300) / 180f;
            room.gravity = Mathf.Lerp(1, 0, t);
        }
        Tick++;
    }

    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
        foreach (var t in Labels) {
            if (t == null) continue;
            t.alignment = FLabelAlignment.Left;
            t.shader = rCam.game.rainWorld.Shaders["Projection"];
            t.color = new Color(0.0f, 0.0f, 0.0f);
            t.scale = 1.5f;
        }
        AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Midground"));
    }

    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
        /*foreach (var t in Labels) {
            t?.Redraw(false, false);
        }*/
    }

    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {
    }

    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) {
        foreach (var t in Labels) {
            if (t == null) continue;
            t.RemoveFromContainer();
            newContatiner.AddChild(t);
        }
    }
}