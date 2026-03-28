using UnityEngine;

public class TileInstance : MonoBehaviour
{
    private Tile _tile;
    public Tile tile {  get { return _tile; } }

    public void SetTile(Tile tile)
    {
        _tile = tile;
    }
}
