using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using YokaiNoMori.BackEnd;
using YokaiNoMori.Enumeration;
using YokaiNoMori.Interface;

namespace YokaiNoMori.FrontEnd
{
    public enum PlayerType { Human, AI }

    /// <summary>
    /// Contrôleur principal de l'interface utilisateur et des interactions.
    /// Fait le lien entre la logique du GameManager et le rendu visuel.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [Header("Préfabs")]
        public GameObject tilePrefab;
        public GameObject piecePrefab;

        [Header("Paramètres")]
        public float spacing = 1.1f; // Espacement entre les cases
        
        [Header("Configuration du Match")]
        public PlayerType player1Type = PlayerType.Human;
        public PlayerType player2Type = PlayerType.AI;

        private YokaiGameManager _gameManager;
        private Dictionary<IBoardCase, TileView> _tileViews = new Dictionary<IBoardCase, TileView>();
        private Dictionary<IPawn, PieceView> _pieceViews = new Dictionary<IPawn, PieceView>();

        private IPawn _selectedPawn;
        private List<Vector2Int> _validMoves = new List<Vector2Int>();
        private PieceView _currentHoveredPiece;

        private bool _gameStarted = false;

        private void Start()
        {
            StartNewGame();
        }


        // Réinitialise et lance une nouvelle partie selon la configuration actuel

        public void StartNewGame()
        {
            if (_gameManager != null)
            {
                // Nettoyage des objets de la partie précédente
                foreach (var pv in _pieceViews.Values) Destroy(pv.gameObject);
                foreach (var tv in _tileViews.Values) Destroy(tv.gameObject);
                _pieceViews.Clear();
                _tileViews.Clear();
            }

            _gameManager = new YokaiGameManager();
            // Abonnement à l'événement de mise à jour pour synchroniser le visuel
            _gameManager.OnStateChanged += SyncVisuals;
            
            ICompetitor p1 = CreateCompetitor(player1Type, "Player 1", ECampType.PLAYER_ONE);
            ICompetitor p2 = CreateCompetitor(player2Type, "Player 2", ECampType.PLAYER_TWO);

            _gameManager.SetupGame(p1, p2);
            GenerateBoard();
            GeneratePieces();
            
            _gameManager.StartGame();
            _gameStarted = true;
        }

        private ICompetitor CreateCompetitor(PlayerType type, string name, ECampType camp)
        {
            if (type == PlayerType.AI)
            {
                GameObject aiObj = new GameObject(name + "_AI");
                aiObj.transform.SetParent(transform);
                return aiObj.AddComponent<AICompetitor>();
            }
            return new HumanCompetitor(name);
        }

        public bool IsAIVsAI() => player1Type == PlayerType.AI && player2Type == PlayerType.AI;

        public bool IsHumanTurn()
        {
            ECampType currentCamp = _gameManager.GetCurrentPlayerCamp();
            if (currentCamp == ECampType.PLAYER_ONE) return player1Type == PlayerType.Human;
            if (currentCamp == ECampType.PLAYER_TWO) return player2Type == PlayerType.Human;
            return false;
        }

        private void GenerateBoard()
        {
            foreach (var boardCase in _gameManager.GetAllBoardCase())
            {
                Vector2Int pos = boardCase.GetPosition();
                Vector3 worldPos = new Vector3(pos.x * spacing, 0, pos.y * spacing);
                GameObject tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
                TileView tv = tileObj.AddComponent<TileView>();
                tv.Init(boardCase);
                _tileViews[boardCase] = tv;
            }
        }

        private void GeneratePieces()
        {
            foreach (var pawn in _gameManager.GetAllPawn())
            {
                GameObject pieceObj = Instantiate(piecePrefab, transform);
                PieceView pv = pieceObj.AddComponent<PieceView>();
                pv.Init(pawn);
                _pieceViews[pawn] = pv;
                UpdateSinglePiecePosition(pawn, true);
            }
        }

        private void Update()
        {
            if (!_gameStarted || _gameManager.IsGameOver()) return;

            HandleHover(); // le survol des pièces avec la souris

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleClick(); // sélection et mouvement
            }
        }

        private void HandleHover()
        {
            if (Mouse.current == null) return;
            
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            PieceView targetPiece = null;

            if (Physics.Raycast(ray, out RaycastHit hit)) targetPiece = hit.collider.GetComponentInParent<PieceView>();

            if (targetPiece != _currentHoveredPiece)
            {
                if (_currentHoveredPiece != null) _currentHoveredPiece.SetHover(false);
                _currentHoveredPiece = targetPiece;
                if (_currentHoveredPiece != null) _currentHoveredPiece.SetHover(true);
            }
        }

        private void HandleClick()
        {
            if (!IsHumanTurn()) return;

            ECampType currentCamp = _gameManager.GetCurrentPlayerCamp();
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit[] hits = Physics.RaycastAll(ray);
            if (hits.Length == 0) return;


            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            
            TileView targetTile = hits.Select(h => h.collider.GetComponent<TileView>()).FirstOrDefault(t => t != null);
            PieceView targetPiece = hits.Select(h => h.collider.GetComponentInParent<PieceView>()).FirstOrDefault(p => p != null);

            if (_selectedPawn == null)
            {
                if (targetPiece != null && targetPiece.Pawn.GetCurrentOwner().GetCamp() == currentCamp) SelectPawn(targetPiece.Pawn);
            }
            else
            {
                Vector2Int targetPos = new Vector2Int(-99, -99);
                if (targetPiece != null && targetPiece.Pawn.GetCurrentPosition().x != -1)
                {
                    targetPos = targetPiece.Pawn.GetCurrentPosition();
                }
                else if (targetTile != null)
                { 
                    targetPos = targetTile.Position;
                }

                // Validation du mouvement
                if (_validMoves.Contains(targetPos))
                {
                    EActionType actionType = (_selectedPawn.GetCurrentPosition().x == -1) ? EActionType.PARACHUTE : EActionType.MOVE;
                    _gameManager.DoAction(_selectedPawn, targetPos, actionType);
                    ClearSelection();
                }
                else
                {
                    // change la sélection
                    if (targetPiece != null && targetPiece.Pawn.GetCurrentOwner().GetCamp() == currentCamp && targetPiece.Pawn != _selectedPawn)
                    {
                        SelectPawn(targetPiece.Pawn);
                    }
                    else
                    {
                        ClearSelection();
                    }
                }
            }
        }

        private void SelectPawn(IPawn pawn)
        {
            ClearSelection();
            _selectedPawn = pawn;
            if (_pieceViews.ContainsKey(pawn)) _pieceViews[pawn].SetSelected(true);

            if (pawn.GetCurrentPosition().x == -1)
            {
                _validMoves = _gameManager.GetAllBoardCase().Where(boardCase => !boardCase.IsBusy()).Select(boardCase => boardCase.GetPosition()).ToList();
            }
            else
            {
                foreach (var dir in pawn.GetDirections())
                {
                    Vector2Int targetPos = pawn.GetCurrentPosition() + dir;
                    IBoardCase targetCase = _gameManager.GetAllBoardCase().FirstOrDefault(boardCase => boardCase.GetPosition() == targetPos);
                    if (targetCase != null && (!targetCase.IsBusy() || targetCase.GetPawnOnIt().GetCurrentOwner().GetCamp() != pawn.GetCurrentOwner().GetCamp())) _validMoves.Add(targetPos);
                }
            }

            // Mise en surbrillance des cases de destination possibles
            foreach (var move in _validMoves)
            {
                IBoardCase boardCase = _gameManager.GetAllBoardCase().FirstOrDefault(@case => @case.GetPosition() == move);
                if (boardCase != null) _tileViews[boardCase].SetHighlight(true, Color.yellow);
            }
        }

        private void ClearSelection()
        {
            if (_selectedPawn != null && _pieceViews.ContainsKey(_selectedPawn)) _pieceViews[_selectedPawn].SetSelected(false);
            _selectedPawn = null;
            _validMoves.Clear();
            foreach (var tv in _tileViews.Values) tv.SetHighlight(false);
        }

        private void SyncVisuals()
        {
            // Met à jour la position de chaque pièce sur le plateau
            foreach (var pawn in _gameManager.GetAllPawn())
                UpdateSinglePiecePosition(pawn, false);
        }

        private void UpdateSinglePiecePosition(IPawn pawn, bool teleport)
        {
            if (!_pieceViews.ContainsKey(pawn)) return;
            PieceView pv = _pieceViews[pawn];
            Vector2Int pos = pawn.GetCurrentPosition();
            
            Vector3 targetPos;
            if (pos.x == -1) // Pièce en réserve
            {
                float offset = (pawn.GetCurrentOwner().GetCamp() == ECampType.PLAYER_ONE) ? -2f : 5f;
                targetPos = new Vector3(pawn.GetPawnType() == EPawnType.Kodama ? 0.5f : 1.5f, 0, offset);
            }
            else // Pièce sur le plateau
            {
                targetPos = new Vector3(pos.x * spacing, 0.1f, pos.y * spacing);
            }

            if (teleport) pv.MoveTo(targetPos);
            else pv.MoveToAnimated(targetPos);

            pv.UpdateVisuals();
        }

        private void OnGUI()
        {
            if (_gameManager == null) return;

            // configuration du jeu
            GUILayout.BeginArea(new Rect(10, 10, 250, 150), "Configuration", GUI.skin.box);
            GUILayout.Space(20);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Joueur 1 :");
            if (GUILayout.Button(player1Type.ToString())) player1Type = (player1Type == PlayerType.Human) ? PlayerType.AI : PlayerType.Human;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Joueur 2 :");
            if (GUILayout.Button(player2Type.ToString())) player2Type = (player2Type == PlayerType.Human) ? PlayerType.AI : PlayerType.Human;
            GUILayout.EndHorizontal();

            if (GUILayout.Button("REDÉMARRER / APPLIQUER")) StartNewGame();
            GUILayout.EndArea();

            if (_gameManager.IsGameOver())
            {
                GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter };
                style.normal.textColor = Color.yellow;
                GUI.Label(new Rect(Screen.width / 2 - 300, Screen.height / 2 - 100, 600, 200), $"VICTOIRE : {_gameManager.GetWinnerName()}", style);
            }
            else if (_gameStarted)
            {
                GUILayout.BeginArea(new Rect(10, 170, 200, 50));
                GUILayout.Label($"Tour de : {_gameManager.GetCurrentPlayerCamp()}");
                GUILayout.EndArea();
            }
        }
    }
}
