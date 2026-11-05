using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LibOracle;
using On.Watcher;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TemplateIterator;

public class TemplateOracle : OracleWrapper {
    public TemplateOracle(PlacedObject pobj, Room room) : base(pobj, room) {
        if (!PData.Broken) State.Movement = MoveRandomly;
        else {
            LightAlpha = 0f;
            for (int i = 0; i < 5; i++) {
                SLOracleSwarmer swarmer = new(
                    new AbstractPhysicalObject(room.world, AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer, null,
                        room.GetWorldCoordinate(Center + new Vector2(0, 100) + Custom.RNV()), room.game.GetNewID()), room.world);
                room.AddObject(swarmer);
                swarmers.Add(swarmer);
            }
        }
    }
    private List<SLOracleSwarmer> swarmers = [];
    public static readonly OracleState.MovementBehavior MoveRandomly = new(nameof(MoveRandomly), true);
    [OracleMemory] public static int Encounters;
    private bool awareOfPlayer;
    private int time = 0;
    private int timeBeenLookingAtBug;
    private CosmeticInsect CosmeticInsectOfInterest;
    private bool seenSpearMake;
    private int lookAtPlayerCounter = 0;
    public override void InGameEvent(IngameEvent e, Vector2 position, ITuple eventData = null) {
        if (e == IngameEvent.SpearMake && !seenSpearMake) {
            seenSpearMake = true;
            Say("That's disgusting");
            lookAtPlayerCounter = 80;
        } else if (e == IngameEvent.OracleHitByWeapon && ((Tuple<Weapon>)eventData)!.Item1.thrownBy is Player) {
            Say("Ow");
        } else if (e == IngameEvent.PlayerDied && ((Tuple<Player>)eventData)!.Item1 == Player) {
            Say("My condolences");
            lookAtPlayerCounter = 800;
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
        } else if (State.Movement == MoveRandomly && Mathf.Approximately(pathProgression, 1f) && State.Time%30 == 0) {
            TargetPos = Center + Custom.RNV() * Random.value * 240f;
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
            if (lookAtPlayerCounter > 0) {
                LookPos = Player.DangerPos;
                lookAtPlayerCounter--;
            } else if (CosmeticInsectOfInterest != null) {
                LookPos = CosmeticInsectOfInterest.pos;
                timeBeenLookingAtBug++;
                if (!SharedPhysics.RayTraceTilesForTerrain(room, firstChunk.pos, CosmeticInsectOfInterest.pos)) CosmeticInsectOfInterest = null;
            }
            if (!awareOfPlayer && !IsPlayerInEntrance(Player)) {
                awareOfPlayer = true;
            }
            if (awareOfPlayer) {
                if (Player.room != room) awareOfPlayer = false;
                if (time == 400) Say("Sorry little creature, i am too invested in the bugs to talk to you.");
                time++;
            }
        } else { 
            if (Player.room != room) return;
            if (!awareOfPlayer && !IsPlayerInEntrance(Player)) {
                awareOfPlayer = true;
                Encounters++;
                Say($"Hello! I have seen you {Encounters} times.");
            }
            if (awareOfPlayer) {
                LookPos = Player.DangerPos;
                time++;
            }
            else time = 0;
            if (Player.room != room)
                awareOfPlayer = false;
        }
    }
}