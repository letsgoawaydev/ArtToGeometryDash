using GeometryDashAPI.Attributes;
using GeometryDashAPI.Levels.GameObjects.Default;
public class HSVBlock : ColorBlock
{
    [GameProperty("41", "1", false, KeyOverride = 41)]
    public string Unknown1 { get; set; }

    [GameProperty("43", "-2147483648a0a1a0a0", false, KeyOverride = 43)]
    public string HSVString { get; set; }

    public HSVBlock()
    {
    }

    public HSVBlock(int id)
        : base(id)
    {
    }
}