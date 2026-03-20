using UnityEngine;

[CreateAssetMenu(
    fileName = "BoardBackgroundCatalog",
    menuName = "Merge Survivor/Board Background Catalog"
)]
public class BoardBackgroundCatalog : ScriptableObject
{
    public BoardBackgroundEntry[] boards;

    public Sprite GetBackground(int boardIndex)
    {
        foreach (var b in boards)
            if (b.boardIndex == boardIndex)
                return b.background;

        return null;
    }
}

[System.Serializable]
public class BoardBackgroundEntry
{
    public int boardIndex;
    public Sprite background;
}