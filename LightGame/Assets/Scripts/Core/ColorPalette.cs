using UnityEngine;

public static class ColorPalette
{
    public static readonly Color Wall = new Color32(0x1a, 0x1a, 0x2e, 0xFF);
    public static readonly Color Floor = new Color32(0x0f, 0x0f, 0x1a, 0xFF);
    public static readonly Color Player = new Color32(0x4e, 0xcd, 0xc4, 0xFF);
    public static readonly Color LightSource = new Color32(0xf9, 0xa8, 0x25, 0xFF);
    public static readonly Color BeamEmitter = new Color32(0xff, 0x6f, 0x00, 0xFF);
    public static readonly Color MirrorSurface = new Color32(0xb0, 0xbe, 0xc5, 0xFF);
    public static readonly Color ObjectiveUnlit = new Color(0x4a / 255f, 0x14 / 255f, 0x8c / 255f, 0.25f);
    public static readonly Color ObjectiveLit = new Color32(0xff, 0xd6, 0x00, 0xFF);
    public static readonly Color Beam = new Color32(0xff, 0xf5, 0x9d, 0xFF);
    public static readonly Color Fog = Color.black;
}
