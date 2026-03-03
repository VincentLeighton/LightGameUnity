using UnityEngine;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "LightGame/Color Palette")]
public class ColorPalette : ScriptableObject
{
    private static ColorPalette _default;

    public static ColorPalette Default
    {
        get
        {
            if (_default == null)
            {
                _default = Resources.Load<ColorPalette>("ColorPalette");
                if (_default == null)
                {
                    _default = CreateInstance<ColorPalette>();
                }
            }
            return _default;
        }
    }

    [Header("Environment")]
    public Color wall = new Color32(26, 26, 46, 255);
    public Color floor = new Color32(15, 15, 26, 255);
    public Color fog = Color.black;

    [Header("Entities")]
    public Color player = new Color32(78, 205, 196, 255);
    public Color lightSource = new Color32(249, 168, 37, 255);
    public Color beamEmitter = new Color32(255, 111, 0, 255);
    public Color mirrorSurface = new Color32(176, 190, 197, 255);

    [Header("Objectives")]
    public Color objectiveUnlit = new Color(74 / 255f, 20 / 255f, 140 / 255f, 0.25f);
    public Color objectiveLit = new Color32(255, 214, 0, 255);

    [Header("Beam")]
    public Color beam = new Color32(255, 245, 157, 255);

    // Static accessors for backward compatibility
    public static Color Wall => Default.wall;
    public static Color Floor => Default.floor;
    public static Color Fog => Default.fog;
    public static Color Player => Default.player;
    public static Color LightSource => Default.lightSource;
    public static Color BeamEmitter => Default.beamEmitter;
    public static Color MirrorSurface => Default.mirrorSurface;
    public static Color ObjectiveUnlit => Default.objectiveUnlit;
    public static Color ObjectiveLit => Default.objectiveLit;
    public static Color Beam => Default.beam;
}
