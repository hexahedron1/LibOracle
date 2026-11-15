using System;
using System.Runtime.CompilerServices;
using HUD;
using JetBrains.Annotations;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LibOracle;

public abstract class OracleWrapper : Oracle, Conversation.IOwnAConversation {
    public Vector2 Center => new((ArmArea.left + ArmArea.right) / 2f, (ArmArea.bottom + ArmArea.top) / 2f);
    public Vector2 BottomCenter => new(Center.x, ArmArea.bottom);
    public Data PData => (Data)PObject.data;
    protected OracleWrapper(PlacedObject pobj, Room room) : base(new AbstractPhysicalObject(room.world, AbstractPhysicalObject.AbstractObjectType.Oracle, null, new WorldCoordinate(room.abstractRoom.index, (int)pobj.pos.x, (int)pobj.pos.y, -1), room.game.GetNewID()), room) {
        PObject = pobj;
        for (int i = 0; i < bodyChunks.Length; ++i) {
            bodyChunks[i] = new BodyChunk(this, i, Center - new Vector2(0, 20), 6f, 0.5f);
        }
        /*for (int index = 0; index < bodyChunkConnections.Length; ++index)
            bodyChunkConnections[index] = new BodyChunkConnection(bodyChunks[index], bodyChunks[index + 1], 9f, BodyChunkConnection.Type.Normal, 1f, 0.5f);
        bodyChunkConnections = new BodyChunkConnection[bodyChunks.Length - 1];
        airFriction = 0.99f;
        gravity = 0.0f;
        bounce = 0.1f;
        surfaceFriction = 0.17f;
        collisionLayer = 1;
        waterFriction = 0.92f;
        buoyancy = 0.95f;*/
        var data = (Data)pobj.data;
        if (!PData.Broken) {
            room.AddObject(myScreen = new OracleProjectionScreen(room, null)); // the behavior argument is not used whatsoever. thanks videocult
            room.gravity = 0f;
        }
        Plugin.Logger.LogInfo($"New oracle in {room.abstractRoom.name}");
        currentGetTo = firstChunk.pos;
        oracleBehavior = new Behavior(this);
        behavior = (Behavior)oracleBehavior;
        if (PData.Broken) foreach (var t in room.updateList) {
            if (t is not AntiGravity antiGravity) continue;
            antiGravity.active = false;
            break;
        }
        arm = new Arm(this);
        arm.isActive = true;
        foreach (var field in GetType().GetFields()) {
            if (!Attribute.IsDefined(field, typeof(OracleMemoryAttribute))) continue;
            if (!field.IsStatic) {
                Plugin.Logger.LogWarning($"Field {field.Name} is not static! Skipping...");
                continue;
            }
            if (field.DeclaringType is null) {
                Plugin.Logger.LogWarning($"Unable to find declaring type for {field.Name}??? I'm right here. Did a cosmic ray hit your computer?");
                continue;
            }
            string id = $"{field.DeclaringType.Assembly.GetName().Name}:{field.DeclaringType.Namespace}.{field.DeclaringType.Name}:{field.Name}";
            if (!Plugin.MemoryFields.Contains(id)) Plugin.MemoryFields.Add(id);
        }
        for (int index = 0; index < bodyChunks.Length; ++index) {
            bodyChunks[index] = new BodyChunk(this, index, Center, 6f, 0.5f);
        } for (int index = 0; index < bodyChunkConnections.Length; ++index)
            bodyChunkConnections[index] = new BodyChunkConnection(bodyChunks[index], bodyChunks[index + 1], 9f, BodyChunkConnection.Type.Normal, 1f, 0.5f);
        Plugin.Logger.LogDebug(Center);
        
        lastPos = Center;
        LookPos = Center;
        mySwarmers?.Clear();
        State = new(this) {
            Id = OracleState.ID.Idle
        };
        ID = new OracleID("Wrapper", true);
    }
    internal bool Crashed;
    //protected void SetPearlCollectionStatus(DataPearl.AbstractDataPearl.DataPearlType type, bool discussed) => Plugin.PearlCollectionData[type][ID] = discussed;
    
    public override void HitByExplosion(float hitFac, Explosion explosion, int hitChunk) {
        base.HitByExplosion(hitFac, explosion, hitChunk);
        Vector2 dir = (bodyChunks[hitChunk].pos - explosion.pos).normalized;
        bodyChunks[hitChunk].vel += dir * explosion.force * 10;
        Plugin.Logger.LogDebug("ow xplosion");
        InGameEvent(IngameEvent.OracleHitByExplosion, explosion.pos, new Tuple<Explosion>(explosion));
    }
    public override void HitByWeapon(Weapon weapon) {
        base.HitByWeapon(weapon);
        InGameEvent(IngameEvent.OracleHitByWeapon, weapon.firstChunk.pos, new Tuple<Weapon>(weapon));
    }
    private readonly PlacedObject PObject;
    private IntRect ArmArea => new((int)(PObject.pos.x/20f)*20, (int)(PObject.pos.y/20f)*20, (int)(PObject.pos.x/20f)*20+((Data)PObject.data).ArmArea.x*20, (int)(PObject.pos.y/20f)*20+((Data)PObject.data).ArmArea.y*20);
    #region Properties
    public Color SkinColor { get; set; } = new(1f, 1f, 1f);
    public Color RobeColor { get; set; } = new(0.2f, 0.2f, 0.2f);
    public Color RobeFadeColor { get; set; } = new(0.01f, 0.01f, 0.01f);
    public Color EyeColor { get; set; } = new(0.01f, 0.01f, 0.01f);
    public Color LightColor { get; set; } = new(1f, 1f, 1f);
    //public static string CollectionIcon { get; set; } = "GuidancePebbles";
    //public static Color CollectionColor { get; set; } = new(1f, 1f, 1f);
    public float LightAlpha { get; set; } = 0.5f;
    // TODO: implement these
    //[CanBeNull] public string ThirdEyeSprite { get; set; } = null;
    //public Color ThirdEyeColor { get; set; } = new(0.01f, 0.01f, 0.01f);
    //public float ThirdEyeScale { get; set; } = 1f;
    public float CrawlSpeed { get; set; }
    public bool InSitPosition { get; set; }
    public bool HoldKnees { get; set; }
    public OracleState State { get; set; }
    public Vector2 LookPos { get; set; }
    public bool EyesOpen { get; set; } = true;
    //public readonly Vector2?[] HandDirections = new Vector2?[2];
    #endregion Properties
    public new virtual void SetUpSwarmers() { }
    public new virtual void SetUpMarbles() { }
    public static bool IsPlayerInEntrance(Player player) => player.bodyMode == Player.BodyModeIndex.CorridorClimb;
    public virtual void StateSwitch(OracleState.ID newState) { }
    public new virtual void GlowerEaten() {}
    protected Vector2 nextPos;
    protected Vector2 lastPos;
    protected Vector2 currentGetTo;
    protected Vector2 lastPosHandle;
    protected Vector2 nextPosHandle;
    protected float pathProgression;
    protected float investigateAngle;
    public float Angle { get; set; }
    private Vector2 baseIdeal;
    private Behavior behavior;
    protected Conversation Conversation;
    public bool Conscious { get; set; } = true;
    public new bool Consious => Conscious;
    protected virtual void ConversationEvents(ConversationWrapper convo) { }

    public SaveState SaveState => ((StoryGameSession)room.game.session).saveState;
    public void Say(string line, int extraLinger = 0, bool interrupt = false) {
        if (SaveState.deathPersistentSaveData.theMark || SaveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint) {
            if (interrupt) {
                DialogBox.Interrupt(line, extraLinger);
            } else {
                DialogBox.NewMessage(line, extraLinger);
            }
        }
        else Voice();
    }
    public virtual void Voice(int? line = null) {
        room.PlaySound((line ?? Random.Range(0, 5)) switch {
            1 => SoundID.SS_AI_Talk_2,
            2 => SoundID.SS_AI_Talk_3,
            3 => SoundID.SS_AI_Talk_4,
            4 => SoundID.SS_AI_Talk_5,
            _ => SoundID.SS_AI_Talk_1
        }, firstChunk);
    }
    public void StartConversation(Conversation.ID id) {
        //Plugin.Logger.LogDebug("Starting convo: " + id.value);
        Conversation?.Destroy();
        Conversation = new ConversationWrapper(this, id, DialogBox) {
            colorMode = ModManager.MSC
        };
    }
    public virtual string ReplaceParts(string s) => s;
    public virtual void SpecialEvent(string eventName) { }
    public virtual void InGameEvent(IngameEvent e, Vector2 position, [CanBeNull] ITuple eventData = null) { }
    public DialogBox DialogBox {
        get {
            if (room.game.cameras[0].hud.dialogBox == null) {
                room.game.cameras[0].hud.InitDialogBox();
                room.game.cameras[0].hud.dialogBox!.defaultYPos = -10f;
            }
            return room.game.cameras[0].hud.dialogBox;
        }
    }
    public Vector2 TargetPos {
        get => nextPos;
        set {
            if (nextPos == value) return;
            nextPos = value;
            nextPosHandle = -Custom.DegToVec(Angle) * (Mathf.Lerp(0.3f, 0.65f, Random.value) * Vector2.Distance(lastPos, nextPos));
            if (!Mathf.Approximately(pathProgression, 1f)) {
                lastPosHandle = Custom.RNV() *
                                (Mathf.Lerp(0.3f, 0.65f, Random.value) * Vector2.Distance(lastPos, nextPos));
                lastPos = currentGetTo;
                pathProgression = 0.0f;
            }
            //Plugin.Logger.LogDebug($"New pos: {value.x}, {value.y}");
        }
    }

    public Player Player { get; set; }
    protected internal void SetPalette(int palA, int palB, float blend) {
        foreach (var t in room.game.cameras) {
            if (t.room == room && !t.AboutToSwitchRoom)
                t.ChangeBothPalettes(palA, palB, blend);
        }
    }
    private BlackScreenOfDeath bsod;
    protected virtual void Move() {
        currentGetTo = Custom.Bezier(lastPos, ClampVectorInRoom(lastPos + lastPosHandle), nextPos,
            ClampVectorInRoom(nextPos + nextPosHandle), pathProgression);
        pathProgression = Mathf.Min(1f, pathProgression + 1f / Mathf.Lerp((float)(40.0 + pathProgression * 80.0),
            Vector2.Distance(lastPos, nextPos) / 5f, 0.5f));
        behavior.consistentBasePosCounter++;
        if (room.readyForAI && !PData.Broken) {
            Vector2 vector2 = new Vector2(Random.value * room.PixelWidth, Random.value * room.PixelHeight);
            if (!(room.GetTile(vector2).Solid || BasePosScore(vector2) + 40.0 >= BasePosScore(baseIdeal))) {
                baseIdeal = vector2;
                behavior.consistentBasePosCounter = 0;
            }
        }
        else baseIdeal = baseIdeal = PData.BrokenArmBasePos + PObject.pos;
    }
    public override void Update(bool eu) {
        if (Player == null && room.game.AlivePlayers.Count > 0)
            Player = room.game.FirstAlivePlayer.realizedCreature as Player;
        if (Player != null && Player.room != room) Player = null;
        behavior.lookPoint = LookPos;
        base.Update(eu);
        if (bsod != null) {
            if (bsod.room == null) room.AddObject(bsod);
            Conscious = false;
            EyesOpen = false;
            return;
        }
        try {
            if (graphicsModule != null && !((OracleGraphics)graphicsModule).initiated) {
                ((OracleGraphics)graphicsModule).initiated = true;
                for (int index = 0; index < 100; ++index)
                    ((OracleGraphics)graphicsModule).Update();
            }
            Move();
            try {
                Update();
            } catch (Exception e) {
                bsod = new(new Vector2(240f, 580f), this, e, false);
                Plugin.Logger.LogError($"ERROR IN {GetType().Name} UPDATE()");
                Plugin.Logger.LogError(e.ToString());
            }
            ((Arm)arm).Update();
            Conversation?.Update();
        }
        catch (Exception e) {
            bsod = new(new Vector2(240f, 580f), this, e, true);
            Plugin.Logger.LogError("ERROR IN ORACLEWRAPPER UPDATE()");
            Plugin.Logger.LogError(e.ToString());
        }
    }
    // A separate method is used so that it can be wrapped in try-catch in the actual update
    // this is what is intended to be overridden by an extending class
    protected virtual void Update() {
        if (State.Movement == OracleState.MovementBehavior.Center) {
            if (PData.Broken) {
                TargetPos = BottomCenter + new Vector2(0, 40f);
            } else {
                if (TargetPos != Center) {
                    TargetPos = Center;
                }
            }
            Angle = 0f;
        }
    }
    protected virtual float BasePosScore(Vector2 tryPos) => Mathf.Abs(Vector2.Distance(nextPos, tryPos) - 200f) + (Player == null ? 0 : Custom.LerpMap(Vector2.Distance(Player.DangerPos, tryPos), 40f, 300f, 800f, 0.0f));
    public void MoveAroundTarget(Vector2 pos, float minDist, float maxDist, float changeChance = 0f) {
        if (!(Custom.Dist(pos, nextPos) > maxDist || Custom.Dist(pos, nextPos) < minDist || room.aimap.getTerrainProximity(nextPos) < 2.0 || Random.value < changeChance)) return;
        Angle = 180f;
        Vector2 vector2 = pos + Custom.DegToVec(investigateAngle) * Random.Range(minDist, maxDist);
        if (room.aimap.getTerrainProximity(vector2) >= 2.0) {
            TargetPos = vector2;
        }
    }
    protected virtual Vector2 ClampVectorInRoom(Vector2 v) {
        Vector2 vector2 = v;
        vector2.x = Mathf.Clamp(vector2.x, arm.cornerPositions[0].x + 10f, arm.cornerPositions[1].x - 10f);
        vector2.y = Mathf.Clamp(vector2.y, arm.cornerPositions[2].y + 10f, arm.cornerPositions[1].y - 10f);
        return vector2;
    }
    public static void RegisterOracle<TOracle>(string category) 
        where TOracle : OracleWrapper {
        Plugin.Logger.LogInfo($"Registering new oracle: {typeof(TOracle).Name} in {category}");
        Pom.Pom.RegisterManagedObject<TOracle, Data, Pom.Pom.ManagedRepresentation>(typeof(TOracle).Name, category);
    }
    public static void RegisterOracle<TOracle, TData>(string category) 
        where TOracle : OracleWrapper 
        where TData : Data {
        Plugin.Logger.LogInfo($"Registering new oracle: {typeof(TOracle).Name} in {category}");
        Pom.Pom.RegisterManagedObject<TOracle, TData, Pom.Pom.ManagedRepresentation>(typeof(TOracle).Name, category);
    }
    #region Subclasses

    protected class ConversationWrapper : Conversation {
        public ConversationWrapper(IOwnAConversation owner, ID id, DialogBox box) : base(owner, id, box) {
            Oracle.ConversationEvents(this);
        }
        protected OracleWrapper Oracle => (OracleWrapper)interfaceOwner;
        public void AddTextEvent(string text, int initialWait = 0, int extraLinger = 0) => events.Add(new TextEvent(this, initialWait, text, extraLinger));
        public void AddSpecialEvent(string eventName, int initialWait = 0) => events.Add(new SpecialEvent(this, initialWait, eventName));
        public void AddWaitEvent(int wait) => events.Add(new WaitEvent(this, wait));
        public void LoadEventsFromFile(string fileName, bool oneRandomLine = false, int randomSeed = 0) {
            if (Plugin.CrsLoaded) ModIntegrations.CrsIntegration.LoadEventsFromFile(this, fileName, Oracle.SaveState.saveStateNumber, oneRandomLine, randomSeed);
            else Plugin.Logger.LogWarning("Unable to load events from file: CRS must be enabled");
        }
    }
    public class Data(PlacedObject obj) : Pom.Pom.ManagedData(obj, null) {
        [Pom.Pom.IntVector2Field("armArea", 28, 28, Pom.Pom.IntVector2Field.IntVectorReprType.rect)]
        public IntVector2 ArmArea;
        [Pom.Pom.BooleanField("broken", false, displayName: "Broken")]
        public bool Broken;
        [Pom.Pom.BooleanField("disconnectedCord", false, displayName: "Disconnected cord")]
        public bool DisconnectedCord;
        [Pom.Pom.Vector2Field("brokenArmBasePos", 280f, 0f, Pom.Pom.Vector2Field.VectorReprType.none, "Broken arm base pos")]
        public Vector2 BrokenArmBasePos;
    }
    public class Behavior(OracleWrapper o) : OracleBehavior(o) {
        private OracleWrapper wrapper => (OracleWrapper)oracle;
        public override Vector2 BaseGetToPos => wrapper.baseIdeal;
        public override Vector2 OracleGetToPos => wrapper.currentGetTo;
        public override Vector2 GetToDir => Custom.DegToVec(wrapper.Angle);
        public override bool EyesClosed => !wrapper.EyesOpen;
    }
    public class Arm : OracleArm {
        public Arm(OracleWrapper oracle) : base(oracle) {
            baseMoveSoundLoop = new StaticSoundLoop(SoundID.SS_AI_Base_Move_LOOP, oracle.firstChunk.pos, oracle.room, 1f, 1f);
        }
        protected OracleWrapper wrapper => (OracleWrapper)oracle;
        public new virtual void Update() {
            cornerPositions[0] = new Vector2(((OracleWrapper)oracle).ArmArea.left, ((OracleWrapper)oracle).ArmArea.top);
            cornerPositions[1] = new Vector2(((OracleWrapper)oracle).ArmArea.right, ((OracleWrapper)oracle).ArmArea.top);
            cornerPositions[2] = new Vector2(((OracleWrapper)oracle).ArmArea.right, ((OracleWrapper)oracle).ArmArea.bottom);
            cornerPositions[3] = new Vector2(((OracleWrapper)oracle).ArmArea.left, ((OracleWrapper)oracle).ArmArea.bottom);
            if (isActive) {
                if (!wrapper.PData.Broken) {
                    if (oracle.Consious) {
                        float num = oracle.dazed <= 240.0 ? (float)(1.0 - oracle.dazed / 240.0) : 0.0f;
                        foreach (var t in oracle.bodyChunks) t.vel *= 0.4f;
                        oracle.bodyChunks[0].vel += Vector2.ClampMagnitude(oracle.oracleBehavior.OracleGetToPos - oracle.bodyChunks[0].pos, 100f) / 100f * (6.2f * num);
                        for (int index = 1; index < oracle.bodyChunks.Length; ++index)
                            oracle.bodyChunks[index].vel += Vector2.ClampMagnitude(oracle.oracleBehavior.OracleGetToPos - oracle.oracleBehavior.GetToDir * oracle.bodyChunkConnections[0].distance - oracle.bodyChunks[0].pos, 100f) / 100f * (3.2f * num);
                    }
                    Vector2 baseGetToPos = oracle.oracleBehavior.BaseGetToPos;
                    Vector2 vector2 = new Vector2(Mathf.Clamp(baseGetToPos.x, cornerPositions[0].x, cornerPositions[1].x), cornerPositions[0].y);
                    float num1 = Vector2.Distance(vector2, baseGetToPos);
                    float num2 = Mathf.InverseLerp(cornerPositions[0].x, cornerPositions[1].x, baseGetToPos.x);
                    for (int index = 1; index < 4; ++index) {
                        Vector2 a = index % 2 != 0 ? new Vector2(cornerPositions[index].x, Mathf.Clamp(baseGetToPos.y, cornerPositions[2].y, cornerPositions[0].y)) : (!ModManager.MSC || !Region.IsRubiconRegion(oracle.room.world.name) || !(oracle.ID == OracleID.SS) ? (!ModManager.MSC || !Region.IsRubiconRegion(oracle.room.world.name) || !(oracle.ID == MoreSlugcatsEnums.OracleID.DM) ? new Vector2(Mathf.Clamp(baseGetToPos.x, cornerPositions[0].x, cornerPositions[1].x), cornerPositions[index].y) : new Vector2(Mathf.Clamp(baseGetToPos.x, cornerPositions[0].x + (float)((cornerPositions[1].x - (double)cornerPositions[0].x) * 0.6000000238418579), cornerPositions[1].x), cornerPositions[index].y)) : new Vector2(Mathf.Clamp(baseGetToPos.x, cornerPositions[0].x, cornerPositions[0].x + (float)((cornerPositions[1].x - (double)cornerPositions[0].x) * 0.4000000059604645)), cornerPositions[index].y));
                        float num3 = Vector2.Distance(a, baseGetToPos);
                        if (num3 < (double)num1) {
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
                        framePos = Mathf.MoveTowardsAngle(framePos * 90f, num2 * 90f, 0.4f) / 90f;
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
                } else {
                    //joints[1].vel += Vector2.ClampMagnitude(new Vector2(1774f, 66f) - joints[1].pos, 50f) / 50f * 5f;
                    /*
                    oracle.bodyChunks[1].vel *= 0.9f;
                    oracle.bodyChunks[0].vel *= 0.9f;
                    if (wrapper.InSitPosition) {
                        oracle.bodyChunks[0].vel += Vector2.ClampMagnitude(oracle.oracleBehavior.OracleGetToPos - oracle.bodyChunks[0].pos, 100f) / 100f * 0.5f;
                        oracle.bodyChunks[1].vel += Vector2.ClampMagnitude(oracle.oracleBehavior.OracleGetToPos - oracle.oracleBehavior.GetToDir * oracle.bodyChunkConnections[0].distance - oracle.bodyChunks[0].pos, 100f) / 100f * 0.2f;
                    }
                    */
                    Vector2 baseGetToPos = oracle.oracleBehavior.BaseGetToPos;
                    Vector2 vector2 = new Vector2(Mathf.Clamp(baseGetToPos.x, cornerPositions[0].x, cornerPositions[1].x), cornerPositions[0].y);
                    float num1 = Vector2.Distance(vector2, baseGetToPos);
                    float num2 = Mathf.InverseLerp(cornerPositions[0].x, cornerPositions[1].x, baseGetToPos.x);
                    for (int index = 1; index < 4; ++index) {
                        Vector2 a = index % 2 != 0 ? new Vector2(cornerPositions[index].x, Mathf.Clamp(baseGetToPos.y, cornerPositions[2].y, cornerPositions[0].y)) : (!ModManager.MSC || !Region.IsRubiconRegion(oracle.room.world.name) || !(oracle.ID == OracleID.SS) ? (!ModManager.MSC || !Region.IsRubiconRegion(oracle.room.world.name) || !(oracle.ID == MoreSlugcatsEnums.OracleID.DM) ? new Vector2(Mathf.Clamp(baseGetToPos.x, cornerPositions[0].x, cornerPositions[1].x), cornerPositions[index].y) : new Vector2(Mathf.Clamp(baseGetToPos.x, cornerPositions[0].x + (float)((cornerPositions[1].x - (double)cornerPositions[0].x) * 0.6000000238418579), cornerPositions[1].x), cornerPositions[index].y)) : new Vector2(Mathf.Clamp(baseGetToPos.x, cornerPositions[0].x, cornerPositions[0].x + (float)((cornerPositions[1].x - (double)cornerPositions[0].x) * 0.4000000059604645)), cornerPositions[index].y));
                        float num3 = Vector2.Distance(a, baseGetToPos);
                        if (num3 < (double)num1) {
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
                    framePos = num2;
                }
            }
            foreach (var t in joints) t.Update();
        }
    }
    #endregion Subclasses
}
#pragma warning disable CS0660
public class OracleState(OracleWrapper owner) {
    private ID id = ID.Idle;
    public ID Id {
        get => id;
        set {
            if (id == value) return;
            id = value;
            SwitchTime = Owner.room.game.timeInRegionThisCycle;
            Progress = 0;
            Owner.StateSwitch(value);
        } 
    }

    public MovementBehavior Movement { get; set; } = MovementBehavior.Center;
    public class ID(string value, bool register) : ExtEnum<ID>(value, register) {
        public static readonly ID Idle = new(nameof(Idle), true);
        public static readonly ID Welcome = new(nameof(Welcome), true);
    }
    public class MovementBehavior(string value, bool register) : ExtEnum<MovementBehavior>(value, register) {
        public static readonly MovementBehavior Center = new(nameof(Center), true);
    }
    public OracleWrapper Owner { get; set; } = owner;
    public int SwitchTime { get; set; }
    public int Time => Owner.room.game.timeInRegionThisCycle - SwitchTime;
    public int Progress { get; set; }
    public static bool operator ==(OracleState left, ID right) => left?.Id == right;
    public static bool operator ==(OracleState left, MovementBehavior right) => left?.Movement == right;
    public static bool operator !=(OracleState left, ID right) => !(left == right);
    public static bool operator !=(OracleState left, MovementBehavior right) => !(left == right);
}
[AttributeUsage(AttributeTargets.Field)]
public class OracleMemoryAttribute : Attribute;