using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using LibOracle;
using UnityEngine;

namespace TemplateIterator;

public class TemplateOracle : OracleWrapper {
    public TemplateOracle(PlacedObject pobj, Room room) : base(pobj, room) {
        ID = new OracleID("TMIR", true);
    }
    protected override void InitChamberObjects() {
        if (PData.Broken) return;
        AddPearl(Center, new Color(1f, 1f, 1f), this, 1, 20f, TemplateOraclePearl);
        AddPearl(new Vector2(220f, 80f), new Color(0.5f, 0.5f, 0.5f), TemplateOraclePearl);

    }
    private readonly DataPearl.AbstractDataPearl.DataPearlType TemplateOraclePearl = new (nameof(TemplateOraclePearl), true);
    public override void InGameEvent(IngameEvent e, Vector2 position, ITuple eventData = null) {
        if (e == IngameEvent.PlayerGrabObject) {
            var data = (Tuple<Player,PhysicalObject>)eventData!;
            if (data.Item2 is SLOracleSwarmer) {
                Say("Please let go. Now.");
            }
        }
    }
}