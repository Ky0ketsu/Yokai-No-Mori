using UnityEngine;
using System.Collections.Generic;

public class GridService : MonoBehaviour
{
    public int width = 10;
    public int height = 10;
    public float cellSize = 1;
    public Vector3 origin = Vector3.zero;

    public GridSystem<Tile> grid;
    public GameObject prefab; 
    public Transform parent;


    public void Start()
    {
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        grid = new GridSystem<Tile>
            (
            width,
            height,
                (GridSystem<Tile> Grid, int x, int y) =>
                {
                    Tile tile = new Tile(Grid, x, y);
                    return tile;
                }
            );
    }
}
