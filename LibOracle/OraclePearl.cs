using UnityEngine;

namespace LibOracle;

public class OraclePearl
    : PebblesPearl {
    public Color BaseColor { get; set; }
    public Color HighlightColor { get; set; }
    public OraclePearl(AbstractPhysicalObject abstractPhysicalObject, World world, Color @base, Color highlight, AbstractDataPearl.DataPearlType type) : base(abstractPhysicalObject, world) {
        BaseColor = @base;
        HighlightColor = highlight;
        AbstractPearl.dataPearlType = type;
    }
}