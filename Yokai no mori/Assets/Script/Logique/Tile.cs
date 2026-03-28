using UnityEngine;

public class Tile : MonoBehaviour
{
    private GridSystem<Tile> _grid;
    private int x;
    private int y;

    public Tile(GridSystem<Tile> grid, int x, int y)
    {
        _grid = grid;
        this.x = x;
        this.y = y;
    }
}
