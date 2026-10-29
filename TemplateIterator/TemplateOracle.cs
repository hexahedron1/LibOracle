using System;
using LibOracle;

namespace TemplateIterator;

public class TemplateOracle : OracleWrapper {
    public TemplateOracle(PlacedObject pobj, Room room) : base(pobj, room) { }
    [OracleMemory] public static int Encounters;
    private bool awareOfPlayer;
    private int time = 0;
    public override void Update() {
        if (Player.room != room) return;
        if (!awareOfPlayer && !IsPlayerInEntrance(Player)) {
            awareOfPlayer = true;
            Encounters++;
            Say($"Hello! I have seen you {Encounters} times.");
        }
        if (time == 400) Say("I'm gonna sneeze");
        if (time == 520) throw new Exception("i sneezed too hard"); 
        if (awareOfPlayer) {
            LookPos = Player.DangerPos;
            time++;
        }
        else time = 0;
        if (Player.room != room)
            awareOfPlayer = false;
    }
}