using LibOracle;

namespace TemplateIterator;

public class TemplateOracle : OracleWrapper {
    public TemplateOracle(AbstractPhysicalObject obj, Room room) : base(obj, room) { }
    [OracleMemory]
    public int Encounters = 0;

    public override void Update(bool eu) {
        base.Update(eu);
        if (State == OracleState.ID.Idle) {
            // your idle code here
            
        }
    }
}