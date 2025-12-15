using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private Camera targetCamera;

    [Header("Zoom Settings")]
    [SerializeField] private float minFOV = 20f;
    [SerializeField] private float maxFOV = 90f;
    [SerializeField] private float defaultFOV = 60f;

    [Header("Zoom Speed")]
    [SerializeField] private float scrollSensitivity = 10f;
    [SerializeField] private float smoothSpeed = 5f;

    [Header("Input Settings")]
    [SerializeField] private bool useMouseScroll = true;
    [SerializeField] private bool useKeyboard = false;
    [SerializeField] private Key zoomInKey = Key.E;
    [SerializeField] private Key zoomOutKey = Key.Q;
    [SerializeField] private float keyboardZoomSpeed = 20f;

    private float targetFOV;
    private float currentFOV;

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        if (targetCamera != null)
        {
            currentFOV = targetCamera.fieldOfView;
            targetFOV = currentFOV;
        }
        else
        {
            Debug.LogError("No camera found for CameraZoomController!");
        }
    }

    private void Update()
    {
        if (targetCamera == null) return;

        HandleZoomInput();
        SmoothZoom();
    }

    private void HandleZoomInput()
    {
        if (useMouseScroll)
        {
            float scrollInput = Mouse.current?.scroll.ReadValue().y ?? 0f;
            if (scrollInput != 0f)
            {
                targetFOV -= scrollInput * scrollSensitivity * Time.deltaTime;
                targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
            }
        }

        if (useKeyboard)
        {
            if (Keyboard.current[zoomInKey].isPressed)
            {
                targetFOV -= keyboardZoomSpeed * Time.deltaTime;
                targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
            }

            if (Keyboard.current[zoomOutKey].isPressed)
            {
                targetFOV += keyboardZoomSpeed * Time.deltaTime;
                targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
            }
        }
    }

    private void SmoothZoom()
    {
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, smoothSpeed * Time.deltaTime);
        targetCamera.fieldOfView = currentFOV;
    }

    public void ZoomIn(float amount)
    {
        targetFOV = Mathf.Clamp(targetFOV - amount, minFOV, maxFOV);
    }

    public void ZoomOut(float amount)
    {
        targetFOV = Mathf.Clamp(targetFOV + amount, minFOV, maxFOV);
    }

    public void SetZoom(float fov)
    {
        targetFOV = Mathf.Clamp(fov, minFOV, maxFOV);
    }

    public void ResetZoom()
    {
        targetFOV = defaultFOV;
    }

    public void SetZoomInstant(float fov)
    {
        targetFOV = Mathf.Clamp(fov, minFOV, maxFOV);
        currentFOV = targetFOV;
        targetCamera.fieldOfView = currentFOV;
    }
}
