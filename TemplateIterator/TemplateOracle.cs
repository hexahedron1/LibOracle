using LibOracle;

namespace TemplateIterator;

public class TemplateOracle : OracleWrapper {
    public TemplateOracle(PlacedObject pobj, Room room) : base(pobj, room) { }
    [OracleMemory] public static int Encounters;
    private bool awareOfPlayer;
    public override void Update() {
        if (Player.room != room) return;
        if (!awareOfPlayer && !IsPlayerInEntrance(Player)) {
            awareOfPlayer = true;
            Encounters++;
            Say($"Hello! I have seen you {Encounters} times.");
        }
        if (awareOfPlayer) {
            LookPos = Player.DangerPos;
        }
        if (Player.room != room)
            awareOfPlayer = false;
    }
}