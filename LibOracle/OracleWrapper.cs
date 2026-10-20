using System;
using System.CodeDom;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;
#pragma warning disable CS0108, CS0114

namespace LibOracle;

public abstract class OracleWrapper : Oracle {
    public OracleID WrappedOracle = new("WrappedOracle", true);
    protected OracleWrapper(AbstractPhysicalObject obj, Room room, bool broken = false) : base(obj, room) {
        Broken = broken;
        if (!broken) {
            room.AddObject(myScreen = new OracleProjectionScreen(room, null)); // the behavior argument is not used whatsoever. thanks videocult
        }
        Plugin.Logger.LogInfo($"New oracle in {room.abstractRoom.name}");
        currentGetTo = firstChunk.pos;
        oracleBehavior = new Behavior(this);
        arm = new Arm(this);
        foreach (var field in GetType().GetFields()) {
            Plugin.Logger.LogInfo($"Checking field: {field.Name}");
            if (!Attribute.IsDefined(field, typeof(OracleMemoryAttribute))) continue;
            string id = $"{field.DeclaringType?.Name ?? "UNKNOWN CLASS???"}.{field.Name}";
            Plugin.Logger.LogInfo($"Registering oracle memory: {id}");
            if (Plugin.Memories.TryGetValue(id, out var memory)) {
                field.SetValue(this, memory);
            } else {
                Plugin.Memories.Add(id, field.GetValue(this));
            }
        }
        ID = WrappedOracle;
        var pos = new Vector2(350f, 350f);
        for (int index = 0; index < bodyChunks.Length; ++index) {
            bodyChunks[index] = new BodyChunk(this, index, pos, 6f, 0.5f);
        } for (int index = 0; index < bodyChunkConnections.Length; ++index)
            bodyChunkConnections[index] = new BodyChunkConnection(bodyChunks[index], bodyChunks[index + 1], 9f, BodyChunkConnection.Type.Normal, 1f, 0.5f);
        lastPos = pos;
        TargetPos = pos;
        mySwarmers.Clear();
    }
    public Color SkinColor { get; set; } = new(1f, 1f, 1f);
    public Color RobeColor { get; set; } = new(0.2f, 0.2f, 0.2f);
    public Color RobeFadeColor { get; set; } = new(0f, 0f, 0f);
    public Color EyeColor { get; set; } = new(0f, 0f, 0f);
    public bool Broken { get; set; }
    public OracleState State { get; set; }
    public Vector2 LookPos { get; set; }

    public virtual void SetUpSwarmers() { }
    public virtual void SetUpMarbles() { }
    public virtual void StateSwitch(OracleState.ID newState) {
        
    }

    private Vector2 nextPos;
    private Vector2 lastPos;
    private Vector2 currentGetTo;
    private Vector2 lastPosHandle;
    private Vector2 nextPosHandle;
    private float pathProgression;
    private float investigateAngle;
    private Vector2 baseIdeal;
    private Behavior behavior;
    public Vector2 TargetPos {
        get => nextPos;
        set {
            lastPos = currentGetTo;
            nextPos = value;
            lastPosHandle = Custom.RNV() * (Mathf.Lerp(0.3f, 0.65f, Random.value) * Vector2.Distance(lastPos, nextPos));
            nextPosHandle = -Custom.DegToVec(investigateAngle) * (Mathf.Lerp(0.3f, 0.65f, Random.value) * Vector2.Distance(lastPos, nextPos));
            pathProgression = 0.0f;
        }
    }

    public class Behavior(OracleWrapper o) : OracleBehavior(o) {
        public override Vector2 BaseGetToPos => ((OracleWrapper)oracle).baseIdeal;
    }
    public Player player { get; set; }
    public override void Update(bool eu) { 
        // please forgive me for what you are about to witness
        var ptr = typeof(PhysicalObject).GetMethod(nameof(PhysicalObject.Update))?.MethodHandle.GetFunctionPointer();
        var pobjUpdate = (Action<bool>)Activator.CreateInstance(typeof(Action<bool>), this, ptr);
        pobjUpdate(eu);
        // base.Update() has a lot of implicit behavior that fucks up my work so i can't use it and i hate it and i want to have a friendly talk with videcult about it
        if (ID == WrappedOracle) return;
        player = room.game.AlivePlayers.Count > 0
            ? room.game.FirstAlivePlayer.realizedCreature as Player
            : null;
        // Movement
        currentGetTo = Custom.Bezier(lastPos, ClampVectorInRoom(lastPos + lastPosHandle), nextPos, ClampVectorInRoom(nextPos + nextPosHandle), pathProgression);
        pathProgression = Mathf.Min(1f, pathProgression + 1f / Mathf.Lerp((float)(40.0 + pathProgression * 80.0),
            Vector2.Distance(lastPos, nextPos) / 5f, 0.5f));
        oracleBehavior.consistentBasePosCounter++;
        if (room.readyForAI) { 
            Vector2 vector2 = new Vector2(Random.value * room.PixelWidth, Random.value * room.PixelHeight);
            if (!(room.GetTile(vector2).Solid || BasePosScore(vector2) + 40.0 >= BasePosScore(baseIdeal))) {
                baseIdeal = vector2;
                behavior.consistentBasePosCounter = 0;
            }
        }
        else
            baseIdeal = nextPos;
        ((Arm)arm).Update();
    }
    protected virtual float BasePosScore(Vector2 tryPos) => Mathf.Abs(Vector2.Distance(nextPos, tryPos) - 200f) + Custom.LerpMap(Vector2.Distance(player.DangerPos, tryPos), 40f, 300f, 800f, 0.0f);
    
    protected virtual Vector2 ClampVectorInRoom(Vector2 v) {
        Vector2 vector2 = v;
        vector2.x = Mathf.Clamp(vector2.x, arm.cornerPositions[0].x + 10f, arm.cornerPositions[1].x - 10f);
        vector2.y = Mathf.Clamp(vector2.y, arm.cornerPositions[2].y + 10f, arm.cornerPositions[1].y - 10f);
        return vector2;
    }

    public class Arm : OracleArm {
        public Arm(OracleWrapper oracle) : base(oracle) {
            baseMoveSoundLoop = new StaticSoundLoop(SoundID.SS_AI_Base_Move_LOOP, oracle.firstChunk.pos, oracle.room, 1f, 1f);
            cornerPositions[0] = oracle.room.MiddleOfTile(10, 31);
            cornerPositions[1] = oracle.room.MiddleOfTile(38, 31);
            cornerPositions[2] = oracle.room.MiddleOfTile(38, 3);
            cornerPositions[3] = oracle.room.MiddleOfTile(10, 3);
        }
        public new virtual void Update() {
            base.Update();
            if (oracle.Consious) {
                float num = 1f;
                if (ModManager.MSC)
                    num = oracle.dazed <= 240.0 ? (float) (1.0 - oracle.dazed / 240.0) : 0.0f; 
                foreach (var t in oracle.bodyChunks)
                    t.vel *= 0.4f;
                oracle.bodyChunks[0].vel += Vector2.ClampMagnitude(oracle.oracleBehavior.OracleGetToPos - oracle.bodyChunks[0].pos, 100f) / 100f * (6.2f * num);
                for (int index = 1; index < oracle.bodyChunks.Length; ++index)
                    oracle.bodyChunks[index].vel += Vector2.ClampMagnitude(oracle.oracleBehavior.OracleGetToPos - oracle.oracleBehavior.GetToDir * oracle.bodyChunkConnections[0].distance - oracle.bodyChunks[0].pos, 100f) / 100f * (3.2f * num);
            }
            Vector2 baseGetToPos = oracle.oracleBehavior.BaseGetToPos;
            Vector2 vector2 = new Vector2(Mathf.Clamp(baseGetToPos.x, cornerPositions[0].x, cornerPositions[1].x), cornerPositions[0].y);
            float num1 = Vector2.Distance(vector2, baseGetToPos);
            float num2 = Mathf.InverseLerp(cornerPositions[0].x, cornerPositions[1].x, baseGetToPos.x);
            for (int index = 1; index < 4; ++index) {
                Vector2 a = index % 2 != 0 
                    ? new Vector2(cornerPositions[index].x, Mathf.Clamp(baseGetToPos.y, cornerPositions[2].y, cornerPositions[0].y))
                    : new Vector2(Mathf.Clamp(baseGetToPos.x, cornerPositions[0].x, cornerPositions[1].x), cornerPositions[index].y);
                float num3 = Vector2.Distance(a, baseGetToPos);
                if (num3 < num1) {
                    vector2 = a;
                    num1 = num3;
                    switch (index) {
                        case 1:
                            num2 = index + Mathf.InverseLerp(cornerPositions[0].y, cornerPositions[2].y, baseGetToPos.y);
                            continue;
                        case 2:
                            num2 = index + Mathf.InverseLerp(cornerPositions[1].x, cornerPositions[0].x, baseGetToPos.x);
                            continue;
                        case 3:
                            num2 = index + Mathf.InverseLerp(cornerPositions[2].y, cornerPositions[0].y, baseGetToPos.y);
                            continue;
                        default:
                            continue;
                    }
                }
            }
            baseMoving = Vector2.Distance(BasePos(1f), vector2) > (baseMoving ? 50.0 : 350.0) && oracle.oracleBehavior.consistentBasePosCounter > 30;
            lastFramePos = framePos;
            if (baseMoving) {
                framePos = Mathf.MoveTowardsAngle(framePos * 90f, num2 * 90f, 0.3f) / 90f;
                if (baseMoveSoundLoop != null) {
                    baseMoveSoundLoop.volume = Mathf.Min(baseMoveSoundLoop.volume + 0.1f, 1f);
                    baseMoveSoundLoop.pitch = Mathf.Min(baseMoveSoundLoop.pitch + 0.025f, 1f);
                }
            }
            else if (baseMoveSoundLoop != null) {
                baseMoveSoundLoop.volume = Mathf.Max(baseMoveSoundLoop.volume - 0.1f, 0.0f);
                baseMoveSoundLoop.pitch = Mathf.Max(baseMoveSoundLoop.pitch - 0.025f, 0.5f);
            }
            if (baseMoveSoundLoop != null) {
                baseMoveSoundLoop.pos = BasePos(1f);
                baseMoveSoundLoop.Update();
                if (ModManager.MSC)
                    baseMoveSoundLoop.volume *= 1f - oracle.noiseSuppress;
            }
        }
    }
}

public class OracleState(OracleWrapper owner) {
    private ID id = ID.Idle;
    public ID Id {
        get => id;
        set {
            id = value;
            SwitchTime = Owner.room.game.timeInRegionThisCycle;
            Progress = 0;
            Owner.StateSwitch(value);
        } 
    }
    public class ID(string value, bool register) : ExtEnum<ID>(value, register) {
        public static readonly ID Idle = new("Idle", true);
        public static readonly ID Welcome = new("Idle", true);
    }
    public class MovementBehavior(string value, bool register) : ExtEnum<MovementBehavior>(value, register) {
        public static readonly ID Static = new("Static", true);
    }
    public OracleWrapper Owner { get; set; } = owner;
    public int SwitchTime { get; set; }
    public int Time => Owner.room.game.timeInRegionThisCycle - SwitchTime;
    public int Progress { get; set; }
    public static bool operator ==(OracleState left, ID right) => left?.Id == right;
    public static bool operator !=(OracleState left, ID right) => !(left == right);
}

[AttributeUsage(AttributeTargets.Field)]
public class OracleMemoryAttribute : Attribute;