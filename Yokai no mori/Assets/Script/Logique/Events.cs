using System;
using UnityEngine;

public class Events
{
    public static event Action<int, int, float, Vector3, GameObject> OnGenerateGridVisual;
    public static void InvokeGenerateGridVisual(int width, int height, float cellSize, Vector3 origin, GameObject prefab) { Debug.Log("Generate Grid visual"); OnGenerateGridVisual?.Invoke(width, height, cellSize, origin, prefab); }

}
