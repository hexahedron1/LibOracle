using System;
using System.CodeDom;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LibOracle;

public abstract class OracleWrapper : Oracle { // TODO: convert to placed object
    public OracleID WrappedOracle = new("WrappedOracle", true);
    public Vector2 Center => new((ArmArea.left + ArmArea.right) / 2f, (ArmArea.bottom + ArmArea.top) / 2f);
    protected OracleWrapper(PlacedObject pobj, Room room) : base(new AbstractPhysicalObject(room.world, AbstractPhysicalObject.AbstractObjectType.Oracle, null, new WorldCoordinate(room.abstractRoom.index, (int)pobj.pos.x, (int)pobj.pos.y, -1), room.game.GetNewID()), room) {
        var data = (Data)pobj.data;
        PObject = pobj;
        if (!data.Broken) {
            room.AddObject(myScreen = new OracleProjectionScreen(room, null)); // the behavior argument is not used whatsoever. thanks videocult
        }
        Plugin.Logger.LogInfo($"New oracle in {room.abstractRoom.name}");
        currentGetTo = firstChunk.pos;
        oracleBehavior = new Behavior(this);
        arm = new Arm(this);
        foreach (var field in GetType().GetFields()) {
            if (!Attribute.IsDefined(field, typeof(OracleMemoryAttribute))) continue;
            if (!field.IsStatic) {
                Plugin.Logger.LogWarning($"Field {field.Name} is not static! Skipping");
                continue;
            }
            string id = $"{field.DeclaringType?.Name ?? "UNKNOWN CLASS???"}.{field.Name}";
            Plugin.Logger.LogInfo($"Registering oracle memory: {id}");
            if (Plugin.Memories.TryGetValue(id, out var memory)) {
                field.SetValue(null, memory);
            } else {
                Plugin.Memories.Add(id, field.GetValue(null));
            }
        }
        ID = WrappedOracle;
        for (int index = 0; index < bodyChunks.Length; ++index) {
            bodyChunks[index] = new BodyChunk(this, index, Center, 6f, 0.5f);
        } for (int index = 0; index < bodyChunkConnections.Length; ++index)
            bodyChunkConnections[index] = new BodyChunkConnection(bodyChunks[index], bodyChunks[index + 1], 9f, BodyChunkConnection.Type.Normal, 1f, 0.5f);
        Plugin.Logger.LogDebug(Center);
        lastPos = Center;
        TargetPos = Center;
        foreach (OracleSwarmer oracleSwarmer in mySwarmers) {
            oracleSwarmer.Destroy();
        }
        mySwarmers.Clear();
        State = new(this) {
            Id = OracleState.ID.Idle
        };
    }

    private readonly PlacedObject PObject;
    private IntRect ArmArea => new((int)(PObject.pos.x/20f)*20, (int)(PObject.pos.y/20f)*20, (int)(PObject.pos.x/20f)*20+((Data)PObject.data).ArmArea.x*20, (int)(PObject.pos.y/20f)*20+((Data)PObject.data).ArmArea.y*20);
    public Color SkinColor { get; set; } = new(1f, 1f, 1f);
    public Color RobeColor { get; set; } = new(0.2f, 0.2f, 0.2f);
    public Color RobeFadeColor { get; set; } = new(0.01f, 0.01f, 0.01f);
    public Color EyeColor { get; set; } = new(0.01f, 0.01f, 0.01f);
    public OracleState State { get; set; }
    public Vector2 LookPos { get; set; }

    public new virtual void SetUpSwarmers() { }
    public new virtual void SetUpMarbles() { }
    public virtual void StateSwitch(OracleState.ID newState) {
        
    }

    private Vector2 nextPos;
    private Vector2 lastPos;
    private Vector2 currentGetTo;
    private Vector2 lastPosHandle;
    private Vector2 nextPosHandle;
    private float pathProgression;
    public float Angle { get; set; }
    private Vector2 baseIdeal;
    private Behavior behavior;
    public Vector2 TargetPos {
        get => nextPos;
        set {
            nextPos = value;
            nextPosHandle = -Custom.DegToVec(Angle) * (Mathf.Lerp(0.3f, 0.65f, Random.value) * Vector2.Distance(lastPos, nextPos));
            if (!Mathf.Approximately(pathProgression, 1f)) {
                lastPosHandle = Custom.RNV() *
                                (Mathf.Lerp(0.3f, 0.65f, Random.value) * Vector2.Distance(lastPos, nextPos));
                lastPos = currentGetTo;
                pathProgression = 0.0f;
            }
            Plugin.Logger.LogDebug($"New pos: {value.x}, {value.y}");
        }
    }

    public class Behavior(OracleWrapper o) : OracleBehavior(o) {
        private OracleWrapper wrapper => (OracleWrapper)oracle;
        public override Vector2 BaseGetToPos => wrapper.baseIdeal;
        public override Vector2 OracleGetToPos => wrapper.currentGetTo;
        public override Vector2 GetToDir => Custom.DegToVec(wrapper.Angle);

        public override void Update(bool eu) {
            base.Update(eu);
        }
    }
    public Player player { get; set; }
    public override void Update(bool eu) {
        base.Update(eu);
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
                oracleBehavior.consistentBasePosCounter = 0;
            }
        }
        else
            baseIdeal = nextPos;

        if (State.Movement == OracleState.MovementBehavior.Static && TargetPos != Center) {
            TargetPos = Center;
            Angle = 180f;
        }
        ((Arm)arm).Update();
    }
    protected virtual float BasePosScore(Vector2 tryPos) => Mathf.Abs(Vector2.Distance(nextPos, tryPos) - 200f) + Custom.LerpMap(Vector2.Distance(player.DangerPos, tryPos), 40f, 300f, 800f, 0.0f);
    
    protected virtual Vector2 ClampVectorInRoom(Vector2 v) {
        Vector2 vector2 = v;
        vector2.x = Mathf.Clamp(vector2.x, arm.cornerPositions[0].x + 10f, arm.cornerPositions[1].x - 10f);
        vector2.y = Mathf.Clamp(vector2.y, arm.cornerPositions[2].y + 10f, arm.cornerPositions[1].y - 10f);
        return vector2;
    }

    public class Data(PlacedObject obj) : Pom.Pom.ManagedData(obj, null) {
        [Pom.Pom.IntVector2Field("armArea", 28, 28, Pom.Pom.IntVector2Field.IntVectorReprType.rect)]
        public IntVector2 ArmArea;
        [Pom.Pom.BooleanField("broken", false, displayName: "Broken")]
        public bool Broken;
    }
    public class Arm : OracleArm {
        public Arm(OracleWrapper oracle) : base(oracle) {
            baseMoveSoundLoop = new StaticSoundLoop(SoundID.SS_AI_Base_Move_LOOP, oracle.firstChunk.pos, oracle.room, 1f, 1f);
        }
        public new virtual void Update() {
            cornerPositions[0] = new Vector2(((OracleWrapper)oracle).ArmArea.left, ((OracleWrapper)oracle).ArmArea.top);
            cornerPositions[1] = new Vector2(((OracleWrapper)oracle).ArmArea.right, ((OracleWrapper)oracle).ArmArea.top);
            cornerPositions[2] = new Vector2(((OracleWrapper)oracle).ArmArea.right, ((OracleWrapper)oracle).ArmArea.bottom);
            cornerPositions[3] = new Vector2(((OracleWrapper)oracle).ArmArea.left, ((OracleWrapper)oracle).ArmArea.bottom);
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
    public static void RegisterOracle<TOracle>(string category) where TOracle : OracleWrapper {
        Plugin.Logger.LogInfo($"Registering new oracle: {typeof(TOracle).Name} in {category}");
        Pom.Pom.RegisterManagedObject<TOracle, Data, Pom.Pom.ManagedRepresentation>(typeof(TOracle).Name, category);
    }
    public static void RegisterOracle<TOracle, TData>(string category) where TOracle : OracleWrapper where TData : Data {
        Plugin.Logger.LogInfo($"Registering new oracle: {typeof(TOracle).Name} in {category}");
        Pom.Pom.RegisterManagedObject<TOracle, TData, Pom.Pom.ManagedRepresentation>(typeof(TOracle).Name, category);
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

    public MovementBehavior Movement { get; set; } = MovementBehavior.Static;
    public class ID(string value, bool register) : ExtEnum<ID>(value, register) {
        public static readonly ID Idle = new("Idle", true);
        public static readonly ID Welcome = new("Idle", true);
    }
    public class MovementBehavior(string value, bool register) : ExtEnum<MovementBehavior>(value, register) {
        public static readonly MovementBehavior Static = new("Static", true);
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