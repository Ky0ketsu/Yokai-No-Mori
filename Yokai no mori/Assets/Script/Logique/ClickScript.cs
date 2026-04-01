using UnityEngine;
using static UnityEngine.Audio.ProcessorInstance;

public class ClickScript : MonoBehaviour
{
    void OnTileClicked(Vector2Int pos)
    {
        Pawn pawn = ServicesLocator.Get<GridService>().GetPawn(pos);

        if (pawn != null)
        {
            var moves = MoveGenerator.GetMoves(pawn, ServicesLocator.Get<GridService>() );

            // ajouter methode d'affichage du mouvement
        }
    }
}
