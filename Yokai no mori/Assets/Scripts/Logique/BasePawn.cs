using System.Collections.Generic;
using UnityEngine;
using YokaiNoMori.Enumeration;
using YokaiNoMori.Interface;

namespace YokaiNoMori.BackEnd
{
    public class BasePawn : IPawn
    {
        private EPawnType _pawnType;
        private ICompetitor _owner;
        private IBoardCase _currentBoardCase;
        private Vector2Int _position;

        public BasePawn(EPawnType type, ICompetitor owner, Vector2Int initialPosition)
        {
            _pawnType = type;
            _owner = owner;
            _position = initialPosition;
        }

        public List<Vector2Int> GetDirections()
        {
            List<Vector2Int> directions = new List<Vector2Int>();
            bool isPlayerOne = _owner.GetCamp() == ECampType.PLAYER_ONE;
            int forward = isPlayerOne ? 1 : -1;

            switch (_pawnType)
            {
                case EPawnType.Koropokkuru:
                    directions.AddRange(new[] {
                        new Vector2Int(0, 1), new Vector2Int(0, -1),
                        new Vector2Int(1, 0), new Vector2Int(-1, 0),
                        new Vector2Int(1, 1), new Vector2Int(1, -1),
                        new Vector2Int(-1, 1), new Vector2Int(-1, -1)
                    });
                    break;
                case EPawnType.Kitsune:
                    directions.AddRange(new[]
                    {
                        new Vector2Int(1, 1), new Vector2Int(1, -1),
                        new Vector2Int(-1, 1), new Vector2Int(-1, -1)
                    });
                    break;
                case EPawnType.Tanuki:
                    directions.AddRange(new[]
                    {
                        new Vector2Int(0, 1), new Vector2Int(0, -1),
                        new Vector2Int(1, 0), new Vector2Int(-1, 0)
                    });
                    break;
                case EPawnType.Kodama:
                    directions.Add(new Vector2Int(0, forward));
                    break;
                case EPawnType.KodamaSamurai:
                    directions.AddRange(new[] {
                        new Vector2Int(0, forward),     // Forward
                        new Vector2Int(0, -forward),    // Backward
                        new Vector2Int(1, 0),           // Right
                        new Vector2Int(-1, 0),          // Left
                        new Vector2Int(1, forward),     // Forward-Right
                        new Vector2Int(-1, forward)     // Forward-Left
                    });
                    break;
            }
            return directions;
        }

        public ICompetitor GetCurrentOwner()
        {
            return _owner;
        }
        public IBoardCase GetCurrentBoardCase()
        {
            return _currentBoardCase;
        }
        public Vector2Int GetCurrentPosition()
        {
            return _position;
        }
        public EPawnType GetPawnType()
        {
            return _pawnType;
        }


        public void SetOwner(ICompetitor newOwner)
        {
            _owner = newOwner;
        }
        public void SetPosition(Vector2Int newPosition)
        {
            _position = newPosition;
        }
        public void SetBoardCase(IBoardCase newBoardCase)
        {
            _currentBoardCase = newBoardCase;
        }
        public void SetPawnType(EPawnType newType)
        {
            _pawnType = newType;
        }
    }
}
