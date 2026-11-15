using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LibOracle;
using MoreSlugcats;
using On.Watcher;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TemplateIterator;

public class TemplateOracle : OracleWrapper {
    public TemplateOracle(PlacedObject pobj, Room room) : base(pobj, room) {
        if (PData.Broken)  {
            LightAlpha = 0f;
        }
        ID = new OracleID("TMIR", true);
        ThirdEyeSprite = "Circle20";
        ThirdEyeColor = RobeColor;
        ThirdEyeScale = 1f;
        if (PData.Broken) SetUpSwarmers();
    }
    public sealed override void SetUpSwarmers() {
        for (int i = 0; i < 5; i++) {
            SLOracleSwarmer swarmer = new(
                new AbstractPhysicalObject(room.world, AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer, null,
                    room.GetWorldCoordinate(Center + new Vector2(0, 100) + Custom.RNV()), room.game.GetNewID()), room.world);
            room.AddObject(swarmer);
            mySwarmers.Add(swarmer);
        }
    }
    public static readonly Conversation.ID TMIR_SaintIntro = new(nameof(TMIR_SaintIntro), true);
    public static readonly Conversation.ID TMIR_FileConvoTest = new(nameof(TMIR_FileConvoTest), true);
    [OracleMemory] public static int Encounters;
    private bool awareOfPlayer;
    private int time = 0;
    private int timeBeenLookingAtBug;
    private CosmeticInsect CosmeticInsectOfInterest;
    private bool seenSpearMake;
    private int lookAtPlayerCounter;
    protected override void ConversationEvents(ConversationWrapper convo) {
        Plugin.Logger.LogDebug("ConversationEvents");
        if (convo.id == TMIR_SaintIntro) {
            convo.AddTextEvent("Hello! this is a conversation sequence.");
            convo.AddTextEvent("If everything works, this should play out as a normal conversation.");
            convo.AddSpecialEvent("testEvent");
            convo.AddTextEvent("I should have received a special event.");
        } else if (convo.id == TMIR_FileConvoTest) convo.LoadEventsFromFile("FileConvoTest");
    }
    public override void SpecialEvent(string eventName) {
        Say("I received a special event: " + eventName);
    }
    public override void InGameEvent(IngameEvent e, Vector2 position, ITuple eventData = null) {
        Plugin.Logger.LogDebug("IngameEvent: " + e.value);
        if (e == IngameEvent.SpearMake && !seenSpearMake) {
            seenSpearMake = true;
            Say("That's disgusting");
            lookAtPlayerCounter = 80;
        } else if (e == IngameEvent.OracleHitByWeapon && ((Tuple<Weapon>)eventData)!.Item1.thrownBy is Player) {
            Say("Ow", interrupt: true);
        } else if (e == IngameEvent.PlayerDied && ((Tuple<Player>)eventData)!.Item1 == Player) {
            Say("My condolences");
            lookAtPlayerCounter = 800;
        } else if (e == IngameEvent.OracleAscended && ((Tuple<Player, Oracle>)eventData)!.Item2 == this) {
            Say("Nuh uh");
            lookAtPlayerCounter = 800;
        } else if (e == IngameEvent.CreatureAscended) {
            Say("Rip");
            lookAtPlayerCounter = 800;
        } else if (e == IngameEvent.BombJump) {
            Say("asfsgbndbnv");
            lookAtPlayerCounter = 123;
        } else if (e == IngameEvent.PlayerThrowObject && ((Tuple<Player, PhysicalObject>)eventData)!.Item2 is DataPearl pearl) {
            if (PData.Broken) Say("Sorry I'm illiterate");
            else { StartConversation(TMIR_FileConvoTest); }
        }
    }
    protected override void Update() {
        if (State.Movement == OracleState.MovementBehavior.Center) {
            if (PData.Broken) {
                TargetPos = BottomCenter + new Vector2(-200, 40f);
            } else {
                if (TargetPos != Center) {
                    TargetPos = Center;
                }
            }
            Angle = 0f;
        }
        if (PData.Broken) {
            InSitPosition = true;
            HoldKnees = true;
            // find bug
            if (timeBeenLookingAtBug > 10 || CosmeticInsectOfInterest == null) {
                var list = (from x in room.drawableObjects where x is CosmeticInsect insect && SharedPhysics.RayTraceTilesForTerrain(room, firstChunk.pos, insect.pos) select (CosmeticInsect)x).ToList();
                if (list.Count > 0) {
                    list.Sort((x, y) => (int)Mathf.Sign(Custom.Dist(firstChunk.pos, x.pos) - Custom.Dist(firstChunk.pos, y.pos)));
                    if (list.First() != CosmeticInsectOfInterest) timeBeenLookingAtBug = 0;
                    CosmeticInsectOfInterest = list.First();
                }
            }
            // look at bug
            if (lookAtPlayerCounter > 0 && Player != null) {
                LookPos = Player.DangerPos;
                lookAtPlayerCounter--;
            } else if (CosmeticInsectOfInterest != null) {
                LookPos = CosmeticInsectOfInterest.pos;
                timeBeenLookingAtBug++;
                if (!SharedPhysics.RayTraceTilesForTerrain(room, firstChunk.pos, CosmeticInsectOfInterest.pos)) CosmeticInsectOfInterest = null;
            }
            if (Player != null) {
                if (!awareOfPlayer && !IsPlayerInEntrance(Player)) {
                    awareOfPlayer = true;
                    if (Player.slugcatStats.name == MoreSlugcatsEnums.SlugcatStatsName.Saint) {
                        StartConversation(TMIR_SaintIntro);
                    }
                }
                if (awareOfPlayer && Player.slugcatStats.name != MoreSlugcatsEnums.SlugcatStatsName.Saint) {
                    if (time == 400) Say("Sorry little creature, i am too invested in the bugs to talk to you.");
                    time++;
                }
            } else awareOfPlayer = false;
        } else {
            if (Player == null) {
                awareOfPlayer = false;
                time = 0;
                return;
            }
            if (awareOfPlayer) {
                LookPos = Player.DangerPos;
                time++;
            }
            if (!awareOfPlayer && !IsPlayerInEntrance(Player)) {
                awareOfPlayer = true;
                Encounters++;
                Say($"Hello! I have seen you {Encounters} times.");
            }
        }
    }
}