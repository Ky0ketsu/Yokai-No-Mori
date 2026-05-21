using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using YokaiNoMori.Enumeration;
using YokaiNoMori.Interface;
using YokaiNoMori.Struct;

namespace YokaiNoMori.BackEnd
{
    /// <summary>
    /// Table de transposition
    /// Zobrist
    /// Multi-threading
    /// Killer Moves
    /// Heuristic d'historique
    /// parallélisation
    /// Quiescence
    /// MVV-LVA
    /// Null move pruning
    /// </summary>


    // Intelligence Artificielle "ShaCauchemar"

    public class AICompetitor : MonoBehaviour, ICompetitor
    {
        private IGameManager _gameManager;
        private ECampType _camp;
        private string _name = "Absolute_Yokai_Engine_Pro";

        // Valeur heuristique pour l'évaluation du plateau
        private const int VAL_KING = 1000000;
        private const int VAL_SAMURAI = 1200;
        private const int VAL_TANUKI = 600;
        private const int VAL_KITSUNE = 550;
        private const int VAL_KODAMA = 200;
        private const int VAL_MOBILITY = 10;

        // Entrèe de la TT pour mémoriser les calculs
        private enum Bound { Exact, Upper, Lower }
        private struct TTEntry
        {
            public int Depth;      // Profondeur du calcul mémorisé
            public int Value;      // Score de la position
            public Bound Bound;    // Type de précision du score
            public SimMove BestMove; // Meilleur coup trouvé pour cette position
        }

        private static readonly object _ttLock = new object(); // Verrou pour le multi-threading sur la TT

        private static Dictionary<ulong, TTEntry> _transpositionTable = new Dictionary<ulong, TTEntry>();
        private SimMove[,] _killerMoves = new SimMove[64, 2]; // "Killer moves" : coups ayant causé des coupes Alpha-Beta
        private int[,] _historyHeuristic = new int[6, 14];   // Heuristique d'historique pour l'ordre des coups
        private HashSet<ulong> _pathStack = new HashSet<ulong>(); // Pour éviter les cycles (répétitions)

        public void Init(IGameManager igameManager, float timerForAI, ECampType currentCamp)
        {
            _gameManager = igameManager;
            _camp = currentCamp;
            Zobrist.Init(); // Initialisation des clés de hachage unique pour chaque position
        }

        public string GetName() => _name;
        public ECampType GetCamp() => _camp;
        public void GetDatas() { }
        public void StopTurn() { }

        public void StartTurn()
        {
            // Définit le délai visuel avant de jouer
            float delay = (FindFirstObjectByType<YokaiNoMori.FrontEnd.GameController>()?.IsAIVsAI() ?? false) ? 0.3f : 0.5f;
            // Calcule le temps de réflexion autorisé (4s max total)
            StartCoroutine(AIDelayRoutine(delay, 4.0f - delay - 0.2f));
        }

        private IEnumerator AIDelayRoutine(float delay, float timeout)
        {
            yield return new WaitForSeconds(delay);

            SimMove best = null;
            bool calculationFinished = false;

            // Lancement du calcul dans un thread séparé pour ne pas figer Unity (Parallélisme)
            Task.Run(() =>
            {
                try
                {
                    best = FindBestMove(timeout);
                }
                catch (Exception exception)
                {
                    // Erreur silencieuse
                }
                calculationFinished = true;
            });

            // Attente de la fin du thread
            while (!calculationFinished) yield return null;

            if (best != null)
            {
                // passage du coup simulé en action réelle sur le GameManager
                IPawn target = (best.ActionType == EActionType.PARACHUTE) ?
                    _gameManager.GetReservePawnsByPlayer(_camp).FirstOrDefault(pawn => pawn.GetPawnType() == best.PawnType) :
                    _gameManager.GetPawnsOnBoard(_camp).FirstOrDefault(pawn => pawn.GetCurrentPosition() == best.FromPos && pawn.GetPawnType() == best.PawnType);

                if (target != null) _gameManager.DoAction(target, best.ToPos, best.ActionType);
            }
        }




        // Structure représentant un coup pour le simulateur de l'IA.
        internal class SimMove
        {
            public EPawnType PawnType;
            public Vector2Int FromPos;
            public Vector2Int ToPos;
            public EActionType ActionType;
            public int OrderScore;
        }


        // Cherche le meilleur
        // Explore de plus en plus profond tant que le temps imparti le permet
        private SimMove FindBestMove(float timeoutSeconds)
        {
            Board board = CaptureBoard();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            long timeoutMs = (long)(timeoutSeconds * 1000);
            SimMove best = null;
            int depthReached = 0;
            int bestValue = 0;

            for (int depth = 1; depth <= 20; depth++)
            {
                var moves = OrderMoves(board, board.Gen(_camp), depth);
                SimMove currentBest = null;
                int alpha = -VAL_KING * 2, beta = VAL_KING * 2;


                // parallélisation
                // Explore les branches principales sur plusieur thread pour exploiter le CPU
                object searchLock = new object();
                Parallel.ForEach(moves, (move, state) =>
                {
                    if (watch.ElapsedMilliseconds > timeoutMs) state.Stop();
                    if (state.IsStopped) return;

                    Board next = board.Clone();
                    next.Move(move, _camp);

                    int valueFound = -Negamax(next, depth - 1, -beta, -alpha, GetOpponent(_camp), watch, timeoutMs, true, new HashSet<ulong>());
                    lock (searchLock)
                    {
                        if (valueFound > alpha)
                        {
                            alpha = valueFound;
                            bestValue = valueFound;
                            currentBest = move;
                        }
                    }
                });

                if (watch.ElapsedMilliseconds > timeoutMs) break;
                best = currentBest;
                depthReached = depth;
                if (alpha > VAL_KING / 2) break;
            }

            Debug.Log($"Depth: {depthReached} | Score: {bestValue} | Time: {watch.ElapsedMilliseconds / 1000f:F2}s");
            return best;
        }

        // Algorithme Negamax avec Alpha-Beta

        private int Negamax(Board board, int depth, int alpha, int beta, ECampType turn, System.Diagnostics.Stopwatch watch, long timeout, bool allowNMP, HashSet<ulong> path)
        {
            // Conditions de fin de branche : victoire ou profondeur limite
            if (board.IsOver) return board.Winner == turn ? VAL_KING : -VAL_KING;
            if (depth <= 0) return Quiescence(board, alpha, beta, turn);

            // Sécurité temporelle et détection de cycle
            if (watch.ElapsedMilliseconds > timeout) return board.Eval(turn);
            if (path.Contains(board.Hash)) return 0;

            int oldAlpha = alpha;

            // Recherche via la table de transposition en memoire
            lock (_ttLock)
            {
                if (_transpositionTable.TryGetValue(board.Hash, out var entry) && entry.Depth >= depth)
                {
                    if (entry.Bound == Bound.Exact) return entry.Value;
                    if (entry.Bound == Bound.Lower) alpha = Math.Max(alpha, entry.Value);
                    else if (entry.Bound == Bound.Upper) beta = Math.Min(beta, entry.Value);
                    if (alpha >= beta) return entry.Value; // Coupure Beta immédiate grâce au cache
                }
            }

            // Null move pruning
            if (allowNMP && depth >= 3 && !board.InCheck(turn))
            {
                Board nmBoard = board.Clone();
                int nmVal = -Negamax(nmBoard, depth - 3, -beta, -beta + 1, GetOpponent(turn), watch, timeout, false, path);
                if (nmVal >= beta) return beta;
            }

            var moves = OrderMoves(board, board.Gen(turn), depth);
            if (moves.Count == 0) return -VAL_KING;

            path.Add(board.Hash);
            SimMove bestMoveFound = null; int bestValueFound = int.MinValue;
            int moveCount = 0;

            foreach (var move in moves)
            {
                moveCount++;
                Board next = board.Clone(); next.Move(move, turn);
                int valueFound;
                if (depth >= 3 && moveCount > 4 && !board.IsCapture(move))
                {
                    valueFound = -Negamax(next, depth - 2, -beta, -alpha, GetOpponent(turn), watch, timeout, true, path);
                }
                else
                {
                    valueFound = -Negamax(next, depth - 1, -beta, -alpha, GetOpponent(turn), watch, timeout, true, path);
                }
                if (valueFound > bestValueFound)
                {
                    bestValueFound = valueFound;
                    bestMoveFound = move;
                }
                alpha = Math.Max(alpha, valueFound);
                if (alpha >= beta)
                {
                    lock (_killerMoves) { _killerMoves[depth % 64, 0] = move; }
                    break;
                }
            }
            path.Remove(board.Hash);

            lock (_ttLock)
            {
                _transpositionTable[board.Hash] = new TTEntry
                {
                    Depth = depth,
                    Value = bestValueFound,
                    BestMove = bestMoveFound,
                    Bound = (bestValueFound <= oldAlpha) ? Bound.Upper : (bestValueFound >= beta) ? Bound.Lower : Bound.Exact
                };
            }
            return bestValueFound;
        }

        //  continue d'explorer les captures au-delà de la profondeur limite
        // ne s'arrête que sur des positions stable sans capture immédiate possible
        private int Quiescence(Board board, int alpha, int beta, ECampType turn)
        {
            int standPat = board.Eval(turn);
            if (standPat >= beta) return beta;
            alpha = Math.Max(alpha, standPat);


            var captures = board.Gen(turn).Where(move => board.IsCapture(move)).OrderByDescending(move => board.EvalPiece(move.PawnType)).ToList();
            foreach (var move in captures)
            {
                Board next = board.Clone(); next.Move(move, turn);
                int valueFound = -Quiescence(next, -beta, -alpha, GetOpponent(turn));
                if (valueFound >= beta) return beta;
                alpha = Math.Max(alpha, valueFound);
            }
            return alpha;
        }

        //maximise les chances de coupures Alpha-Beta rapides.
        private List<SimMove> OrderMoves(Board board, List<SimMove> moves, int depth)
        {
            foreach (var move in moves)
            {
                int scoreValue = 0;
                if (board.IsCapture(move))
                {
                    // MVV-LVA : Most Valuable Victim - Least Valuable Aggressor.
                    // On privilégie la capture d'une grosse pièce
                    scoreValue = 10000 + (board.EvalPiece(board.GetPieceAt(move.ToPos).Type) * 10) - board.EvalPiece(move.PawnType);
                }
                else if (move.PawnType == EPawnType.Kodama && move.ToPos.y == board.GetOpponentRow(board.GetOwnerAt(move.FromPos)))
                {
                    scoreValue = 9000; // Priorité élevée à la promotion du Kodama.
                }
                else if (_killerMoves[depth % 64, 0]?.ToPos == move.ToPos && _killerMoves[depth % 64, 0]?.PawnType == move.PawnType)
                {
                    scoreValue = 8000; // Killer
                }
                move.OrderScore = scoreValue;
            }
            // Trie par score décroissant pour traiter les meilleur en premier.
            return moves.OrderByDescending(move => move.OrderScore).ToList();
        }


        // Renvoie le camp opposé.
        private ECampType GetOpponent(ECampType camp) => camp == ECampType.PLAYER_ONE ? ECampType.PLAYER_TWO : ECampType.PLAYER_ONE;


        // Convertit l'état actuel du jeu en une structure optimisée pour la simulation.
        private Board CaptureBoard()
        {
            Board board = new Board();
            foreach (var pawn in _gameManager.GetAllPawn()) board.Pieces.Add(new Board.Piece { Type = pawn.GetPawnType(), Owner = pawn.GetCurrentOwner().GetCamp(), Position = pawn.GetCurrentPosition() });
            board.UpdateHash();
            return board;
        }


        // Représentation légère du plateau

        internal class Board
        {
            public struct Piece { public EPawnType Type; public ECampType Owner; public Vector2Int Position; }
            public List<Piece> Pieces = new List<Piece>();
            public ulong Hash;
            public bool IsOver;
            public ECampType Winner;

            public Board Clone() => new Board { Pieces = new List<Piece>(Pieces), IsOver = IsOver, Winner = Winner, Hash = Hash };


            // Simule l'exécution d'un mouvement
            // Gère les captures, les promotions et l'envoi en réserve.

            public void Move(SimMove move, ECampType actor)
            {
                int index = (move.ActionType == EActionType.PARACHUTE) ? Pieces.FindIndex(pawn => pawn.Owner == actor && pawn.Position.x == -1 && pawn.Type == move.PawnType) : Pieces.FindIndex(pawn => pawn.Owner == actor && pawn.Position == move.FromPos && pawn.Type == move.PawnType);
                if (index == -1) return;
                int targetIndex = Pieces.FindIndex(pawn => pawn.Position == move.ToPos && pawn.Position.x != -1);
                if (targetIndex != -1)
                {
                    var victim = Pieces[targetIndex];
                    if (victim.Type == EPawnType.Koropokkuru)
                    {
                        IsOver = true;
                        Winner = actor;
                    }
                    victim.Owner = actor; victim.Position = new Vector2Int(-1, -1);
                    if (victim.Type == EPawnType.KodamaSamurai) victim.Type = EPawnType.Kodama;
                    Pieces[targetIndex] = victim;
                }
                var actorPiece = Pieces[index];
                actorPiece.Position = move.ToPos;
                if (actorPiece.Type == EPawnType.Kodama && move.ActionType == EActionType.MOVE && move.ToPos.y == GetOpponentRow(actor)) actorPiece.Type = EPawnType.KodamaSamurai;
                Pieces[index] = actorPiece;
                UpdateHash();
            }


            // Génère la liste de tous les coups possibles pour un camp

            public List<SimMove> Gen(ECampType actor)
            {
                var moves = new List<SimMove>();
                foreach (var pawn in Pieces.Where(pieceInfo => pieceInfo.Owner == actor))
                {
                    if (pawn.Position.x == -1)
                    {
                        for (int xCoord = 0; xCoord < 3; xCoord++)
                        {
                            for (int yCoord = 0; yCoord < 4; yCoord++)
                            {
                                if (!Pieces.Any(pieceInfo => pieceInfo.Position == new Vector2Int(xCoord, yCoord)))
                                {
                                    moves.Add(new SimMove { PawnType = pawn.Type, FromPos = pawn.Position, ToPos = new Vector2Int(xCoord, yCoord), ActionType = EActionType.PARACHUTE });
                                }
                            }
                        }

                    }
                    else
                    {
                        foreach (var direction in GetDirs(pawn.Type, actor))
                        {
                            Vector2Int targetPos = pawn.Position + direction;
                            if (targetPos.x >= 0 && targetPos.x < 3 && targetPos.y >= 0 && targetPos.y < 4 && Pieces.FirstOrDefault(pieceInfo => pieceInfo.Position == targetPos).Owner != actor)
                            {
                                moves.Add(new SimMove { PawnType = pawn.Type, FromPos = pawn.Position, ToPos = targetPos, ActionType = EActionType.MOVE });
                            }
                        }
                    }
                }
                return moves;
            }


            // calcule l'avantage d'un joueur.

            public int Eval(ECampType actor)
            {
                int scoreSum = 0; ECampType opponent = actor == ECampType.PLAYER_ONE ? ECampType.PLAYER_TWO : ECampType.PLAYER_ONE;
                foreach (var pawn in Pieces)
                {
                    int pieceValue = EvalPiece(pawn.Type);
                    if (pawn.Position.x != -1)
                    {
                        int targetRow = pawn.Owner == ECampType.PLAYER_ONE ? 3 : 0;
                        pieceValue += (3 - Math.Abs(pawn.Position.y - targetRow)) * 50;
                        if (pawn.Type == EPawnType.Koropokkuru && pawn.Position.y == targetRow) pieceValue += 50000;
                    }
                    if (pawn.Owner == actor) scoreSum += pieceValue; else if (pawn.Owner == opponent) scoreSum -= pieceValue;
                }
                return scoreSum;
            }

            public int EvalPiece(EPawnType type) { switch (type) { case EPawnType.Koropokkuru: return VAL_KING; case EPawnType.KodamaSamurai: return VAL_SAMURAI; case EPawnType.Tanuki: return VAL_TANUKI; case EPawnType.Kitsune: return VAL_KITSUNE; default: return VAL_KODAMA; } }
            public bool IsCapture(SimMove move) => Pieces.Any(pawn => pawn.Position == move.ToPos && pawn.Position.x != -1);
            public Piece GetPieceAt(Vector2Int position) => Pieces.FirstOrDefault(pawn => pawn.Position == position);
            public ECampType GetOwnerAt(Vector2Int position) => Pieces.FirstOrDefault(pawn => pawn.Position == position).Owner;
            public int GetOpponentRow(ECampType owner) => owner == ECampType.PLAYER_ONE ? 3 : 0;


            // Vérifie si le Roi d'un camp est sous la menace d'une pièce adverse.
            public bool InCheck(ECampType team)
            {
                var king = Pieces.FirstOrDefault(pawn => pawn.Owner == team && pawn.Type == EPawnType.Koropokkuru);
                if (king.Position.x == -1) return false;
                ECampType opponent = team == ECampType.PLAYER_ONE ? ECampType.PLAYER_TWO : ECampType.PLAYER_ONE;
                return Gen(opponent).Any(move => move.ToPos == king.Position);
            }

            // Met à jour Zobrist
            public void UpdateHash() { Hash = 0; foreach (var pawn in Pieces) Hash ^= Zobrist.Get(pawn.Type, pawn.Owner, pawn.Position); }

            // Renvoie les vecteurs de direction de déplacement pour chaque type de Yokai.
            private Vector2Int[] GetDirs(EPawnType type, ECampType owner)
            {
                int forward = owner == ECampType.PLAYER_ONE ? 1 : -1;
                switch (type)
                {
                    case EPawnType.Koropokkuru: return new[] { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) };
                    case EPawnType.Kitsune: return new[] { new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) };
                    case EPawnType.Tanuki: return new[] { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0) };
                    case EPawnType.Kodama: return new[] { new Vector2Int(0, forward) };
                    default: return new[] { new Vector2Int(0, forward), new Vector2Int(0, -forward), new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(1, forward), new Vector2Int(-1, forward) };
                }
            }
        }



        // génération de la signature numérique unique 

        internal static class Zobrist
        {
            private static ulong[,,] Table = new ulong[6, 3, 14];
            // Initialise la table avec des nombres 64 bits aléatoires.
            public static void Init()
            {
                if (Table[1, 1, 1] != 0) return;
                var rnd = new System.Random(42);
                for (int iIndex = 0; iIndex < 6; iIndex++)
                {
                    for (int jIndex = 0; jIndex < 3; jIndex++)
                    {
                        for (int kIndex = 0; kIndex < 14; kIndex++)
                        {
                            byte[] buffer = new byte[8]; rnd.NextBytes(buffer);
                            Table[iIndex, jIndex, kIndex] = BitConverter.ToUInt64(buffer, 0);
                        }
                    }
                }
            }

            // Renvoie la valeur pré-calculée pour une pièce donnée à une position donnée.

            public static ulong Get(EPawnType type, ECampType owner, Vector2Int position)
            {
                int posIdx = (position.x == -1) ? 12 + (owner == ECampType.PLAYER_ONE ? 0 : 1) : position.x + position.y * 3;
                return Table[(int)type, (int)owner, posIdx];
            }
        }
    }
}