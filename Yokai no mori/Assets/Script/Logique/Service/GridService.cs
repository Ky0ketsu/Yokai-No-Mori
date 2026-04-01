using UnityEngine;
using System.Collections.Generic;
using System;
using static UnityEngine.Audio.ProcessorInstance;

public class GridService : MonoBehaviour
{
    public int width = 10;
    public int height = 10;
    public float cellSize = 1;
    public Vector3 origin = Vector3.zero;

    public GridSystem<Tile> grid;
    public GameObject prefab; 
    public Transform parent;
    public Pawn[,] pawnPosition ;

    private void OnEnable()
    {
        ServicesLocator.Register(this);
    }
    private void OnDisable()
    {
        ServicesLocator.Unregister(this);
    }

    private void Start()
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
        Debug.Log("Logique grid généré");

        Events.InvokeGenerateGridVisual(width, height, cellSize, origin, prefab);
    }

    public Pawn GetPawn(Vector2Int pos)
    {
        if (!grid.GetGridObject(pos.x, pos.y)) return null;
        return pawnPosition[pos.x, pos.y];
    }

    public void MovePiece(Pawn pawn, Vector2Int target)
    {
        pawnPosition[pawn.position.x, pawn.position.y] = null;

        pawn.position = target;

        pawnPosition[target.x, target.y] = pawn;
    }
}
