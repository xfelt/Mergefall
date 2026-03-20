using UnityEngine;
using UnityEngine.UI;

public class BoardBackgroundView : MonoBehaviour
{
    public BoardBackgroundCatalog catalog;
    public Image backgroundImage;

    public void SetBoard(int boardIndex)
    {
        var sprite = catalog.GetBackground(boardIndex);

        if (sprite != null)
            backgroundImage.sprite = sprite;
    }
}