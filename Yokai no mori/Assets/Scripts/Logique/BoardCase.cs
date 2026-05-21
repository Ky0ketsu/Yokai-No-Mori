using UnityEngine;
using YokaiNoMori.Interface;

namespace YokaiNoMori.BackEnd
{
    public class BoardCase : IBoardCase
    {
        private Vector2Int _position;
        private IPawn _pawnOnIt;

        public BoardCase(Vector2Int position)
        {
            _position = position;
        }

        public Vector2Int GetPosition() => _position;
        public IPawn GetPawnOnIt() => _pawnOnIt;
        public bool IsBusy() => _pawnOnIt != null;

        public void SetPawn(IPawn pawn)
        {
            _pawnOnIt = pawn;
        }
    }
}
