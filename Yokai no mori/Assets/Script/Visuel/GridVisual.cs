using System.Collections.Generic;
using UnityEngine;

public class GridVisual : MonoBehaviour
{
    private List<GameObject> _spawnedTiles = new List<GameObject>();

    private void SpawnTiles()
    {
        ClearSpawnedTiles();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = grid.GetGridObject(x, y);
                Vector3 pos = grid.GetWorldPosition(x, y);
                GameObject obj = Instantiate(prefab, pos, Quaternion.identity, parent);
                TileInstance tileInstance = obj.AddComponent<TileInstance>();
                tileInstance.SetTile(tile);
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
