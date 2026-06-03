using System.Collections;
using UnityEngine;

namespace AlgorithmOfDelivery.Maze
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 50f;
        [SerializeField] private float _zoomSpeed = 10f;
        [SerializeField] private float _minZoom = 5f;
        [SerializeField] private float _maxZoom = 4000f;
        [SerializeField] private float _focusZoom = 420f;

        private Camera _camera;
        private Coroutine _zoomCoroutine;
        private float _worldMinX, _worldMaxX, _worldMinY, _worldMaxY;
        private bool _hasWorldBounds;

        public bool IsZooming { get; private set; }

        private void Start()
        {
            EnsureCamera();
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

        public void ConfigureWorldBounds(float minX, float maxX, float minY, float maxY, float zoomPadding = 1.25f, float focusZoom = -1f)
        {
            SetWorldBounds(minX, maxX, minY, maxY);

            Camera cam = EnsureCamera();
            if (cam == null)
                return;

            float width = Mathf.Max(0.01f, maxX - minX);
            float height = Mathf.Max(0.01f, maxY - minY);
            float fitSize = Mathf.Max(height / 2f, width / (2f * cam.aspect));
            float maxZoom = fitSize * Mathf.Max(1f, zoomPadding);
            SetZoomLimits(_minZoom, maxZoom, focusZoom);

            Vector3 centerPos = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, transform.position.z);
            transform.position = centerPos;

            float startZoom = cam.orthographicSize;
            if (focusZoom > 0f)
                startZoom = Mathf.Min(maxZoom, Mathf.Max(_minZoom, focusZoom * 1.5f));

            cam.orthographicSize = Mathf.Clamp(startZoom, _minZoom, _maxZoom);
        }

        public void SetZoomLimits(float minZoom, float maxZoom, float focusZoom = -1f)
        {
            _minZoom = Mathf.Max(0.01f, minZoom);
            _maxZoom = Mathf.Max(_minZoom, maxZoom);
            if (focusZoom > 0f)
                _focusZoom = Mathf.Clamp(focusZoom, _minZoom, _maxZoom);

            Camera cam = EnsureCamera();
            if (cam != null)
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, _minZoom, _maxZoom);
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
            Camera cam = EnsureCamera();
            if (cam == null)
            {
                IsZooming = false;
                yield break;
            }

            Vector3 startPos = transform.position;
            float startSize = cam.orthographicSize;
            float targetSize = Mathf.Clamp(_focusZoom, _minZoom, _maxZoom);
            Vector3 targetPos = new Vector3(targetWorld.x, targetWorld.y, -10f);

            if (_hasWorldBounds)
                targetPos = ClampPosition(targetPos);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            transform.position = targetPos;
            cam.orthographicSize = targetSize;
            IsZooming = false;
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 movement = new Vector3(horizontal, vertical, 0f) * _moveSpeed * Time.unscaledDeltaTime;
            Vector3 nextPosition = transform.position + movement;

            if (_hasWorldBounds)
                nextPosition = ClampPosition(nextPosition);

            transform.position = nextPosition;
        }

        private void HandleZoom()
        {
            Camera cam = EnsureCamera();
            if (cam == null)
                return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float newSize = cam.orthographicSize - scroll * _zoomSpeed;
                cam.orthographicSize = Mathf.Clamp(newSize, _minZoom, _maxZoom);
            }
        }

        private void ClampToWorldBounds()
        {
            if (!_hasWorldBounds) return;

            Camera cam = EnsureCamera();
            if (cam == null)
                return;

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            Vector3 pos = transform.position;
            pos.x = ClampAxis(pos.x, _worldMinX, _worldMaxX, halfW);
            pos.y = ClampAxis(pos.y, _worldMinY, _worldMaxY, halfH);
            transform.position = pos;
        }

        private Vector3 ClampPosition(Vector3 pos)
        {
            if (!_hasWorldBounds) return pos;

            Camera cam = EnsureCamera();
            if (cam == null)
                return pos;

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            pos.x = ClampAxis(pos.x, _worldMinX, _worldMaxX, halfW);
            pos.y = ClampAxis(pos.y, _worldMinY, _worldMaxY, halfH);
            return pos;
        }

        public void SetBounds(float width, float height)
        {
            SetBounds(width, height, new Vector3(width / 2f, height / 2f, -10f));
        }

        public void SetBounds(float width, float height, Vector3 centerPos)
        {
            Camera cam = EnsureCamera();
            if (cam == null)
                return;

            float aspect = cam.aspect;
            float neededSizeY = height / 2f;
            float neededSizeX = width / (2f * aspect);
            cam.orthographicSize = Mathf.Max(neededSizeY, neededSizeX);
            transform.position = centerPos;
        }

        public float Aspect
        {
            get
            {
                Camera cam = EnsureCamera();
                return cam != null ? cam.aspect : 16f / 9f;
            }
        }

        private Camera EnsureCamera()
        {
            if (_camera == null)
                _camera = Camera.main;
            return _camera;
        }

        private float ClampAxis(float value, float min, float max, float halfExtent)
        {
            float low = min + halfExtent;
            float high = max - halfExtent;

            if (low > high)
                return (min + max) * 0.5f;

            return Mathf.Clamp(value, low, high);
        }
    }
}
