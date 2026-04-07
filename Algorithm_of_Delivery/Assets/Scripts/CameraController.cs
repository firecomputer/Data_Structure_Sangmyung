using UnityEngine;

namespace AlgorithmOfDelivery.Maze
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 50f;
        [SerializeField] private float _zoomSpeed = 10f;
        [SerializeField] private float _minZoom = 5f;
        [SerializeField] private float _maxZoom = 200f;

        private Camera _camera;

        private void Start()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            HandleMovement();
            HandleZoom();
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 movement = new Vector3(horizontal, vertical, 0f) * _moveSpeed * Time.deltaTime;
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

        public void SetBounds(float width, float height)
        {
            float aspect = _camera.aspect;
            float neededSizeY = height / 2f;
            float neededSizeX = width / (2f * aspect);
            _camera.orthographicSize = Mathf.Max(neededSizeY, neededSizeX);
            transform.position = new Vector3(width / 2f, height / 2f, -10f);
        }
    }
}
