using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;

namespace LibOracle;

public static class ExtensionMethods {
    public static void IngameEvent(this Room room, IngameEvent e, Vector2 position, [CanBeNull] ITuple eventData = null) {
        foreach (var objs in room.physicalObjects) {
            foreach (var obj in objs) {
                if (obj is OracleWrapper wrapper) {
                    wrapper.InGameEvent(e, position, eventData);
                }
            }
        }
    }
}