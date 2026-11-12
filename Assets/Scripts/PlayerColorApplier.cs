using UnityEngine;

/// <summary>
/// Applies saved cosmetic colours to the base body renderers in the gameplay scene.
/// Clothing overlays and other renderers are intentionally left alone.
/// </summary>
public sealed class PlayerColorApplier : MonoBehaviour
{
    [SerializeField] private SpriteRenderer playerOneBody;
    [SerializeField] private SpriteRenderer playerTwoBody;

    private void Start()
    {
        if (playerOneBody != null)
            playerOneBody.color = CharacterColorSettings.GetColor(true);
        if (playerTwoBody != null)
            playerTwoBody.color = CharacterColorSettings.GetColor(false);
    }
}
