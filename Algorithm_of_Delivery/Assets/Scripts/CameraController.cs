using System.Collections;
using UnityEngine;

namespace AlgorithmOfDelivery.Maze
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 50f;
        [SerializeField] private float _zoomSpeed = 10f;
        [SerializeField] private float _minZoom = 5f;
        [SerializeField] private float _maxZoom = 1350f;

        private Camera _camera;
        private Coroutine _zoomCoroutine;
        private float _worldMinX, _worldMaxX, _worldMinY, _worldMaxY;
        private bool _hasWorldBounds;

        public bool IsZooming { get; private set; }

        private void Start()
        {
            _camera = Camera.main;
        }

        private void LateUpdate()
        {
            if (!IsZooming)
            {
                HandleMovement();
                HandleZoom();
                ClampToWorldBounds();
            }
        }

        public void SetWorldBounds(float minX, float maxX, float minY, float maxY)
        {
            _worldMinX = minX;
            _worldMaxX = maxX;
            _worldMinY = minY;
            _worldMaxY = maxY;
            _hasWorldBounds = true;
        }

        public void ZoomTo(Vector2 worldPosition, float duration = 0.5f)
        {
            if (_zoomCoroutine != null)
                StopCoroutine(_zoomCoroutine);
            _zoomCoroutine = StartCoroutine(ZoomToCoroutine(worldPosition, duration));
        }

        private IEnumerator ZoomToCoroutine(Vector2 targetWorld, float duration)
        {
            IsZooming = true;
            Vector3 startPos = transform.position;
            float startSize = _camera.orthographicSize;
            float targetSize = Mathf.Lerp(_minZoom, _maxZoom, 0.3f);
            Vector3 targetPos = new Vector3(targetWorld.x, targetWorld.y, -10f);

            if (_hasWorldBounds)
                targetPos = ClampPosition(targetPos);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                _camera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            transform.position = targetPos;
            _camera.orthographicSize = targetSize;
            IsZooming = false;
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 movement = new Vector3(horizontal, vertical, 0f) * _moveSpeed * Time.unscaledDeltaTime;
            transform.position += movement;
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float newSize = _camera.orthographicSize - scroll * _zoomSpeed;
                _camera.orthographicSize = Mathf.Clamp(newSize, _minZoom, _maxZoom);
            }
        }

        private void ClampToWorldBounds()
        {
            if (!_hasWorldBounds) return;

            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;

            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, _worldMinX + halfW, _worldMaxX - halfW);
            pos.y = Mathf.Clamp(pos.y, _worldMinY + halfH, _worldMaxY - halfH);
            transform.position = pos;
        }

        private Vector3 ClampPosition(Vector3 pos)
        {
            if (!_hasWorldBounds) return pos;

            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;

            pos.x = Mathf.Clamp(pos.x, _worldMinX + halfW, _worldMaxX - halfW);
            pos.y = Mathf.Clamp(pos.y, _worldMinY + halfH, _worldMaxY - halfH);
            return pos;
        }

        public void SetBounds(float width, float height)
        {
            SetBounds(width, height, new Vector3(width / 2f, height / 2f, -10f));
        }

        public void SetBounds(float width, float height, Vector3 centerPos)
        {
            float aspect = _camera.aspect;
            float neededSizeY = height / 2f;
            float neededSizeX = width / (2f * aspect);
            _camera.orthographicSize = Mathf.Max(neededSizeY, neededSizeX);
            transform.position = centerPos;
        }
    }
}
