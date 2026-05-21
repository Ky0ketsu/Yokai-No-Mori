using UnityEngine;
using YokaiNoMori.Interface;

namespace YokaiNoMori.FrontEnd
{
    public class TileView : MonoBehaviour
    {
        public Vector2Int Position { get; private set; }
        private IBoardCase _boardCase;
        private MeshRenderer _renderer;
        private Color _originalColor;

        public void Init(IBoardCase boardCase)
        {
            _boardCase = boardCase;
            Position = boardCase.GetPosition();
            _renderer = GetComponent<MeshRenderer>();
            if (_renderer != null)
                _originalColor = _renderer.material.color;
        }

        public void SetHighlight(bool highlight, Color color = default)
        {
            if (_renderer == null) return;
            _renderer.material.color = highlight ? color : _originalColor;
        }
    }
}
