using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YokaiNoMori.Enumeration;
using YokaiNoMori.Interface;
using YokaiNoMori.Struct;

namespace YokaiNoMori.BackEnd
{

    // Gestionnaire principal de jeu
    // Gère les règles, les tours et les conditions de victoire.

    public class YokaiGameManager : IGameManager
    {
        // Événement déclenché a chaque changement d'état du plateau pour mettre à jour la vue
        public Action OnStateChanged;


        private BoardCase[,] _board = new BoardCase[3, 4];
        private List<IPawn> _allPawns = new List<IPawn>();
        private List<ICompetitor> _competitors = new List<ICompetitor>(); 
        private SAction _lastAction; 
        private int _currentPlayerIndex = 0; 
        private bool _isGameOver = false;
        private string _winnerName = "";

        public YokaiGameManager()
        {
            // Initialisation du plateau vide
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    _board[x, y] = new BoardCase(new Vector2Int(x, y));
                }
            }
        }


        // Configure la partie avec deux joueur et place les pièces initiales.

        public void SetupGame(ICompetitor p1, ICompetitor p2)
        {
            _competitors.Add(p1);
            _competitors.Add(p2);
            p1.Init(this, 5f, ECampType.PLAYER_ONE);
            p2.Init(this, 5f, ECampType.PLAYER_TWO);

            // Placement initial des pieces
            CreateAndPlacePawn(EPawnType.Kitsune, p1, new Vector2Int(0, 0));
            CreateAndPlacePawn(EPawnType.Koropokkuru, p1, new Vector2Int(1, 0));
            CreateAndPlacePawn(EPawnType.Tanuki, p1, new Vector2Int(2, 0));
            CreateAndPlacePawn(EPawnType.Kodama, p1, new Vector2Int(1, 1));

            CreateAndPlacePawn(EPawnType.Tanuki, p2, new Vector2Int(0, 3));
            CreateAndPlacePawn(EPawnType.Koropokkuru, p2, new Vector2Int(1, 3));
            CreateAndPlacePawn(EPawnType.Kitsune, p2, new Vector2Int(2, 3));
            CreateAndPlacePawn(EPawnType.Kodama, p2, new Vector2Int(1, 2));
        }

        private void CreateAndPlacePawn(EPawnType type, ICompetitor owner, Vector2Int pos)
        {
            BasePawn pawn = new BasePawn(type, owner, pos);
            _allPawns.Add(pawn);
            _board[pos.x, pos.y].SetPawn(pawn);
            pawn.SetBoardCase(_board[pos.x, pos.y]);
        }

        public List<IPawn> GetAllPawn() => _allPawns;

        public List<IBoardCase> GetAllBoardCase()
        {
            List<IBoardCase> cases = new List<IBoardCase>();
            foreach (var bc in _board) cases.Add(bc);
            return cases;
        }


        // Exécute une action (Mouvement ou Parachutage) demandée par un joueur
       
        public void DoAction(IPawn pawnTarget, Vector2Int position, EActionType actionType)
        {
            if (_isGameOver) return;

            BasePawn basePawn = pawnTarget as BasePawn;
            if (basePawn == null) return;

            Vector2Int startPos = basePawn.GetCurrentPosition();
            IPawn capturedPawn = null;

            if (actionType == EActionType.MOVE)
            {
                IBoardCase targetCase = GetBoardCaseAt(position);
                if (targetCase == null) return;

                // Si la case cible est occupée, c'est une capture
                if (targetCase.IsBusy())
                {
                    capturedPawn = targetCase.GetPawnOnIt();
                    // Empêcher de capturer ses propre pièce
                    if (capturedPawn.GetCurrentOwner().GetCamp() == basePawn.GetCurrentOwner().GetCamp()) return;
                    HandleCapture(capturedPawn);
                }

                // Libère l'ancienne case
                if (basePawn.GetCurrentBoardCase() != null)
                    (basePawn.GetCurrentBoardCase() as BoardCase).SetPawn(null);
                
                // Déplace la piéce sur la nouvelle case
                basePawn.SetPosition(position);
                BoardCase newCase = _board[position.x, position.y];
                newCase.SetPawn(basePawn);
                basePawn.SetBoardCase(newCase);

                // Promotion du Kodama s'il atteint la dernière ligne adverse
                if (basePawn.GetPawnType() == EPawnType.Kodama)
                {
                    int lastRow = basePawn.GetCurrentOwner().GetCamp() == ECampType.PLAYER_ONE ? 3 : 0;
                    if (position.y == lastRow) basePawn.SetPawnType(EPawnType.KodamaSamurai);
                }
            }
            else if (actionType == EActionType.PARACHUTE)
            {
                // Un parachutage ne peut se faire que sur une case vide
                IBoardCase targetCase = GetBoardCaseAt(position);
                if (targetCase == null || targetCase.IsBusy()) return;

                basePawn.SetPosition(position);
                BoardCase newCase = _board[position.x, position.y];
                newCase.SetPawn(basePawn);
                basePawn.SetBoardCase(newCase);
            }

            // Enregistre la dernière action
            _lastAction.SetAction(basePawn.GetCurrentOwner().GetCamp(), basePawn.GetPawnType(), actionType, startPos, position, capturedPawn);
            
            // Notifie la vue pour le rafraîchissement visuel
            OnStateChanged?.Invoke();

            // Condition de victoire immédiate par capture du Roi adverse
            if (capturedPawn != null && capturedPawn.GetPawnType() == EPawnType.Koropokkuru)
            {
                EndGame(basePawn.GetCurrentOwner());
                return;
            }

            NextTurn();
        }


        // Gère la capture d'une pièce, change de propriétaire et l'envoie en réserve.

        private void HandleCapture(IPawn captured)
        {
            BasePawn bp = captured as BasePawn;
            (bp.GetCurrentBoardCase() as BoardCase).SetPawn(null);
            bp.SetBoardCase(null);
            bp.SetPosition(new Vector2Int(-1, -1)); // Position spéciale "Réserve"
            
            // La pièce capturèe appartient désormais à l'adversaire
            ICompetitor newOwner = _competitors.First(c => c != bp.GetCurrentOwner());
            bp.SetOwner(newOwner);

            // Si un Kodama promu est capturé, il perd sa promotion
            if (bp.GetPawnType() == EPawnType.KodamaSamurai)
                bp.SetPawnType(EPawnType.Kodama);
        }

        // Filtre les pièce en réserve
        public List<IPawn> GetReservePawnsByPlayer(ECampType campType) => _allPawns.Where(p => p.GetCurrentOwner().GetCamp() == campType && p.GetCurrentPosition().x == -1).ToList();
        // Filtre les pièces sur le plateau
        public List<IPawn> GetPawnsOnBoard(ECampType campType) => _allPawns.Where(p => p.GetCurrentOwner().GetCamp() == campType && p.GetCurrentPosition().x != -1).ToList();
        public SAction GetLastAction() => _lastAction;

 
        // Passe au joueur suivant et vérifie la victoire par percer

        private void NextTurn()
        {
            _competitors[_currentPlayerIndex].StopTurn();
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _competitors.Count;
            
            // Vérifie si le joueur dont le tour commence a déjà gagné par Percer
            CheckPerceeWin();

            if (!_isGameOver)
            {
                _competitors[_currentPlayerIndex].GetDatas();
                _competitors[_currentPlayerIndex].StartTurn();
            }
        }

        private void CheckPerceeWin()
        {
            // Un joueur gagne par Percer s'il commence son tour avec son Roi sur la dernière ligne adverse
            ICompetitor currentPlayer = _competitors[_currentPlayerIndex];
            IPawn king = GetPawnsOnBoard(currentPlayer.GetCamp()).FirstOrDefault(p => p.GetPawnType() == EPawnType.Koropokkuru);
            
            if (king != null)
            {
                int targetRow = currentPlayer.GetCamp() == ECampType.PLAYER_ONE ? 3 : 0;
                if (king.GetCurrentPosition().y == targetRow)
                {
                    EndGame(currentPlayer);
                }
            }
        }

        public bool IsGameOver() => _isGameOver;
        public string GetWinnerName() => _winnerName;

        private void EndGame(ICompetitor winner)
        {
            if (_isGameOver) return;
            _isGameOver = true;
            _winnerName = winner.GetName();
            Debug.Log($"[GAME OVER] Gagnant : {winner.GetName()} ({winner.GetCamp()})");
            OnStateChanged?.Invoke();
        }

        public ECampType GetCurrentPlayerCamp() => _competitors[_currentPlayerIndex].GetCamp();
        public IBoardCase GetBoardCaseAt(Vector2Int pos) => (pos.x >= 0 && pos.x < 3 && pos.y >= 0 && pos.y < 4) ? _board[pos.x, pos.y] : null;

        public void StartGame()
        {
            _competitors[_currentPlayerIndex].GetDatas();
            _competitors[_currentPlayerIndex].StartTurn();
        }
        }
        }
