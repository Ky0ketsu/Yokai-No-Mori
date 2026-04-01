using System.Collections.Generic;
using UnityEngine;

public class GridVisual : MonoBehaviour
{
    private List<GameObject> _spawnedTiles = new List<GameObject>();

    private void OnEnable()
    {
        Events.OnGenerateGridVisual += HandleGenerateGridVisual;
    }
    private void OnDisable()
    {
        Events.OnGenerateGridVisual -= HandleGenerateGridVisual;
    }

    private void HandleGenerateGridVisual(int width, int height, float cellSize, Vector3 origin, GameObject prefab)
    {
        SpawnTiles(width, height, cellSize, origin, prefab);
    }

    private void SpawnTiles(int width, int height, float cellSize, Vector3 origin, GameObject prefab)
    {
        ClearSpawnedTiles();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //Tile tile = grid.GetGridObject(x, y);
                Vector3 pos = new Vector3(x, y, 0) * cellSize;
                GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);
                TileInstance tileInstance = obj.AddComponent<TileInstance>();
                //tileInstance.SetTile(tile);
                _spawnedTiles.Add(obj);
            }
        }
    }

    private void ClearSpawnedTiles()
    {
        if (_spawnedTiles.Count > 0) return;

        foreach (GameObject obj in _spawnedTiles)
        {
            DestroyImmediate(obj);
        }
        _spawnedTiles.Clear();
    }
}
