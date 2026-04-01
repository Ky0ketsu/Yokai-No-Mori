using UnityEngine;
using YokaiNoMori.Enumeration;

public class Pawn : MonoBehaviour
{
    public EPawnType type;
    public Vector2Int position;
    public int owner; // joueur 0 ou 1

    public Pawn(EPawnType type, Vector2Int pos, int owner)
    {
        this.type = type;
        this.position = pos;
        this.owner = owner;
    }
}
