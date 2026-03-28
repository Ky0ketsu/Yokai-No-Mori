using Unity.Mathematics;
using UnityEngine;

public class GridSystem<TGridObject>
{
    private int _height;
    public int Height{ get { return _height; } }

    private int _width;
    private int Width { get { return _width; } }

    private TGridObject[,] _gridObjects;
    public TGridObject[,] GridObjects { get { return _gridObjects; } }

    public GridSystem(int height, int width, System.Func<GridSystem<TGridObject>, int, int, TGridObject> createGridObject)
    {
        _width = width;
        _height = height;
        _gridObjects = new TGridObject[height, width];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {

            }
        }
    }


    public TGridObject GetGridObject(int x, int y)
    {
        if(x >= 0 && y >= 0 && x < _width && y < _height) return _gridObjects[x, y];
        else
        {
            Debug.LogWarning("Coordonnée de la tuile hors range");
            return default;
        }
    }
}
