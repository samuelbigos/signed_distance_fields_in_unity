using UnityEngine;
using UnityEngine.InputSystem;

public class WasdCameraController : MonoBehaviour
{
    public float Speed = 10.0f;
    public float LookSpeed = 0.5f;
    public float Deceleration = 5.0f;

    public Camera Camera;

    private Vector3 _velocity;
    private bool _mouseDown;
    private Transform _transform;
    private Transform _camTransform;

    void Start()
    {
        _transform = transform;
        _camTransform = Camera.transform;
        
        Camera.depthTextureMode = DepthTextureMode.Depth;
        _camTransform.position = _transform.position;
        _camTransform.rotation = _transform.rotation;
    }

    void Update()
    {
        Vector3 dir = new Vector3();

        if (_mouseDown)
        {
            Vector3 foward = _camTransform.forward;
            Vector3 right = _camTransform.right;

            if (Keyboard.current.wKey.isPressed)
                dir += foward;
            if (Keyboard.current.aKey.isPressed)
                dir -= right;
            if (Keyboard.current.sKey.isPressed)
                dir -= foward;
            if (Keyboard.current.dKey.isPressed)
                dir += right;
        }

        float velMod = 1.0f;
        if (Keyboard.current.leftShiftKey.isPressed)
        {
            velMod = 5.0f;
        }
        _velocity += dir.normalized * Time.deltaTime * Speed * velMod;
        _velocity = Vector3.Lerp(_velocity, new Vector3(0.0f, 0.0f, 0.0f), Mathf.Min(Time.deltaTime, 1.0f / 60.0f) * Deceleration);

        Vector3 newPos = transform.localPosition;
        _transform.localPosition = newPos + _velocity * Time.deltaTime * 120.0f;

        if (_mouseDown)
        {
            Vector2 mouse = Pointer.current.delta.ReadValue();
            transform.Rotate(new Vector3(0.0f, 1.0f, 0.0f), mouse.x * LookSpeed, Space.World);
            transform.Rotate(_transform.right, -mouse.y * LookSpeed, Space.World);
        }

        _camTransform.position = _transform.position;
        _camTransform.rotation = _transform.rotation;
    }
    
    public void RightMouse(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Started:
                _mouseDown = true;
                break;
            case InputActionPhase.Canceled:
                _mouseDown = false;
                break;
        }
    }
}