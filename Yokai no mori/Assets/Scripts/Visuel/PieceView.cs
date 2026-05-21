using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YokaiNoMori.Enumeration;
using YokaiNoMori.Interface;

namespace YokaiNoMori.FrontEnd
{
    public class PieceView : MonoBehaviour
    {
        public IPawn Pawn { get; private set; }
        private SpriteRenderer _spriteRenderer;
        private Coroutine _moveCoroutine;
        private Coroutine _shakeCoroutine;
        private Transform _visualChild;

        private Vector3 _baseScale = new Vector3(0.6f, 0.6f, 0.6f);
        private float _hoverScaleFactor = 1.25f;
        private bool _isHovered = false;

        public void Init(IPawn pawn)
        {
            Pawn = pawn;
            
            _visualChild = transform.Find("Visual");
            if (_visualChild == null)
            {
                GameObject visualObj = new GameObject("Visual");
                visualObj.transform.SetParent(transform);
                visualObj.transform.localPosition = new Vector3(0, 0.15f, 0);
                visualObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
                visualObj.transform.localScale = _baseScale;
                _visualChild = visualObj.transform;
            }

            _spriteRenderer = _visualChild.GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = _visualChild.gameObject.AddComponent<SpriteRenderer>();
            }
            
            foreach (Transform child in transform)
            {
                if (child != _visualChild)
                {
                    if (child.name.Contains("Label") || child.GetComponent<TMPro.TextMeshPro>() != null) Destroy(child.gameObject);
                }
            }

            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (_spriteRenderer != null)
            {
                string path = GetSpritePath(Pawn.GetPawnType());
                Sprite sprite = LoadSprite(path);
                if (sprite != null)
                {
                    _spriteRenderer.sprite = sprite;
                }

                var camp = Pawn.GetCurrentOwner().GetCamp();
                Color tint = (camp == ECampType.PLAYER_ONE) ? new Color(0.7f, 0.7f, 1f) : new Color(1f, 0.7f, 0.7f);
                _spriteRenderer.color = tint;
                
                if (camp == ECampType.PLAYER_TWO) transform.rotation = Quaternion.Euler(0, 180, 0);
                else transform.rotation = Quaternion.identity;
            }
        }

        public void SetHover(bool hovered)
        {
            if (_isHovered == hovered) return;
            _isHovered = hovered;
            _visualChild.localScale = _isHovered ? _baseScale * _hoverScaleFactor : _baseScale;
        }

        private string GetSpritePath(EPawnType type)
        {
            switch (type)
            {
                case EPawnType.Kitsune: return "Assets/Sprite/Pawn/Kitsune.jpeg";
                case EPawnType.Kodama: return "Assets/Sprite/Pawn/Kodama.jpeg";
                case EPawnType.KodamaSamurai: return "Assets/Sprite/Pawn/KodamaSamutai.jpeg";
                case EPawnType.Koropokkuru: return "Assets/Sprite/Pawn/Koropokkuru.jpeg";
                case EPawnType.Tanuki: return "Assets/Sprite/Pawn/Tanuki.jpeg";
                default: return "";
            }
        }

        private Sprite LoadSprite(string path)
        {
        #if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        #else
            return null; 
        #endif
        }

        public void MoveToAnimated(Vector3 targetPosition, float duration = 0.2f)
{
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(MoveRoutine(targetPosition, duration));
        }

        private IEnumerator MoveRoutine(Vector3 target, float duration)
        {
            Vector3 start = transform.position;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0, 1, t);
                transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }
            transform.position = target;
            _moveCoroutine = null;
        }

        public void SetSelected(bool selected)
        {
            if (selected)
            {
                if (_shakeCoroutine == null) _shakeCoroutine = StartCoroutine(ShakeRoutine());
            }
            else
            {
                if (_shakeCoroutine != null)
                {
                    StopCoroutine(_shakeCoroutine);
                    _shakeCoroutine = null;
                    if (_visualChild != null) _visualChild.localPosition = new Vector3(0, 0.15f, 0);
                }
            }
        }

        private IEnumerator ShakeRoutine()
        {
            float intensity = 0.03f;
            while (true)
            {
                Vector3 offset = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)) * intensity;
                if (_visualChild != null) _visualChild.localPosition = new Vector3(0, 0.15f, 0) + offset;
                yield return new WaitForSeconds(0.15f);
            }
        }

        public void MoveTo(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }
    }
}
