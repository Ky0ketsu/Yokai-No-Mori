using System.Collections.Generic;
using UnityEngine;
using YokaiNoMori.Enumeration;

public class MoveGenerator : MonoBehaviour
{
    public static List<Vector2Int> GetMoves(Pawn piece, GridService board)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        switch (piece.type)
        {
            case EPawnType.Koropokkuru:
                AddStepMoves(piece, board, moves, new Vector2Int[]
                {
                    Vector2Int.up,
                    Vector2Int.down,
                    Vector2Int.left,
                    Vector2Int.right,
                    new Vector2Int(1,1),
                    new Vector2Int(-1,1),
                    new Vector2Int(1,-1),
                    new Vector2Int(-1,-1)
                });
                break;

            case EPawnType.Kodama:
                int dir = piece.owner == 0 ? 1 : -1;
                AddStepMoves(piece, board, moves, new Vector2Int[]
                {
                    new Vector2Int(0, dir)
                });
                break;

            case EPawnType.Tanuki:
                AddStepMoves(piece, board, moves, new Vector2Int[]
                {
                    Vector2Int.up,
                    Vector2Int.left,
                    Vector2Int.right
                });
                break;

            case EPawnType.Kitsune:
                AddStepMoves(piece, board, moves, new Vector2Int[]
                {
                    new Vector2Int(1,1),
                    new Vector2Int(-1,1),
                    new Vector2Int(1,-1),
                    new Vector2Int(-1,-1)
                });
                break;
        }

        return moves;
    }

    private static void AddStepMoves( Pawn piece, GridService board, List<Vector2Int> moves, Vector2Int[] directions)
    {
        foreach (var dir in directions)
        {
            Vector2Int target = piece.position + dir;

            if (!board.grid.GetGridObject(target.x ,target.y))
                continue;

            Pawn targetPawn = board.GetPawn(target);

            // Case vide
            if (targetPawn == null)
            {
                moves.Add(target);
            }
            // Ennemi
            else if (targetPawn.owner != piece.owner)
            {
                moves.Add(target);
            }
            // Alliée → interdit
        }
    }
}
