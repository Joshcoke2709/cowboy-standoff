using UnityEngine;

/// <summary>
/// Stores the cosmetic preset chosen for each player. These values affect only sprite tint.
/// </summary>
public static class CharacterColorSettings
{
    private const string PlayerOneKey = "CowboyStandoff.PlayerOneColor";
    private const string PlayerTwoKey = "CowboyStandoff.PlayerTwoColor";

    private static readonly Color[] Palette =
    {
        new Color32(130, 153, 181, 255), // Dusty Blue #8299B5
        new Color32(145, 165, 138, 255), // Sage Green #91A58A
        new Color32(194, 167, 125, 255), // Warm Sand #C2A77D
        new Color32(120, 159, 155, 255), // Muted Teal #789F9B
        new Color32(164, 147, 173, 255), // Dusty Lavender #A493AD
        new Color32(133, 140, 150, 255)  // Slate Grey #858C96
    };

    private static readonly string[] PaletteNames =
    {
        "DUSTY BLUE", "SAGE GREEN", "WARM SAND", "MUTED TEAL", "DUSTY LAVENDER", "SLATE GREY"
    };

    public static int PaletteCount => Palette.Length;

    public static int GetColorIndex(bool playerOne)
    {
        string key = playerOne ? PlayerOneKey : PlayerTwoKey;
        int defaultIndex = playerOne ? 0 : 1;
        return Mathf.Clamp(PlayerPrefs.GetInt(key, defaultIndex), 0, Palette.Length - 1);
    }

    public static Color GetColor(bool playerOne)
    {
        return Palette[GetColorIndex(playerOne)];
    }

    public static Color GetColorAt(int index)
    {
        return Palette[Mathf.Clamp(index, 0, Palette.Length - 1)];
    }

    public static string GetColorNameAt(int index)
    {
        return PaletteNames[Mathf.Clamp(index, 0, PaletteNames.Length - 1)];
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    public static void SetColor(bool playerOne, int colorIndex)
    {
        string key = playerOne ? PlayerOneKey : PlayerTwoKey;
        PlayerPrefs.SetInt(key, Mathf.Clamp(colorIndex, 0, Palette.Length - 1));
        PlayerPrefs.Save();
    }
}
