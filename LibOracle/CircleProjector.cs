using CoralBrain;
using UnityEngine;

namespace LibOracle;

public class CircleProjector(Vector2 pos, int index = 0, float radius = 180f) : UpdatableAndDeletable, IOwnProjectedCircles {
    public ProjectedCircle Circle { get; private set; }
    public float Radius { get; set; } = radius;
    public int Index { get; set; } = index;
    public override void Update(bool eu) {
        if (Circle != null) {
            Circle.index = Index;
            Circle.rad = Radius;
            if (!Circle.slatedForDeletetion) return;
            Circle = null;
            Destroy();
        } else {
            Circle = new ProjectedCircle(room, this, Index, Radius);
            room.AddObject(Circle);
        }
    }
    public Vector2 Position { get; set; } = pos;
    public Vector2 CircleCenter(int index, float timeStacker) => Position;
    public Room HostingCircleFromRoom() => room;
    public bool CanHostCircle() => !slatedForDeletetion;
}