using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceLogistics.UI
{
    public class CameraControls : MonoBehaviour
    {
        [Header("Zoom Settings")]
        public float ZoomSensitivity = 0.1f; // Scroll value is ~120 on windows
        public float MinZoom = 1.0f;
        public float MaxZoom = 1000.0f;

        [Header("Pan Settings")]
        public float PanSensitivity = 1.0f;

        private Camera _cam;
        private Vector3 _dragOrigin;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null)
            {
                Debug.LogError("CameraControls requires a Camera component.");
                enabled = false;
            }
        }

        private void Update()
        {
            if (Mouse.current == null) return;

            HandleZoom();
            HandlePan();
        }

        private void HandleZoom()
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                if (_cam.orthographic)
                {
                    float targetSize = _cam.orthographicSize - scroll * ZoomSensitivity * _cam.orthographicSize;
                    _cam.orthographicSize = Mathf.Clamp(targetSize, MinZoom, MaxZoom);
                }
            }
        }

        private void HandlePan()
        {
            // Middle Mouse Button (usually button 2)
            if (Mouse.current.middleButton.wasPressedThisFrame)
            {
                _dragOrigin = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            }

            if (Mouse.current.middleButton.isPressed)
            {
                Vector3 currentPos = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                Vector3 diff = _dragOrigin - currentPos;
                
                // Z位置は維持
                diff.z = 0;
                
                transform.position += diff;
            }
        }
        
        // 外部からズームを設定する
        public void SetZoom(float size)
        {
            if (_cam != null) _cam.orthographicSize = Mathf.Clamp(size, MinZoom, MaxZoom);
        }
    }
}
