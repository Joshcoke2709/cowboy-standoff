
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

public static class PauseShortcut
{
    [Shortcut("Cowboy Standoff/Toggle Pause", KeyCode.F8)]
    private static void TogglePause()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPaused = !EditorApplication.isPaused;
        }
    }
}
