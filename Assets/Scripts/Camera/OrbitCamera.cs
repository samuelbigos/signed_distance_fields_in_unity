using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitCamera : MonoBehaviour
{
    public float MinDistance = 25.0f;
    public float MaxDistance = 200.0f;
    public float ZoomSpeed = 1.0f;
    public float ZoomDeceleration = 20.0f;
    public float PanSpeed = 0.1f;
    public float Deceleration = 5.0f;
    public float MinZoom = 1.5f;
    public float MaxZoom = 10.0f;

    public GameObject GimbalH;
    public GameObject GimbalV;

    private Vector2 _velocity;
    private float _zoomVelocity;
    private bool _mouseDown;

    void Start()
    {

    }

    void Update()
    {
        if (_mouseDown)
        {
            _velocity += Pointer.current.delta.ReadValue();
        }

        GimbalH.transform.Rotate(GimbalH.transform.up, _velocity.x * PanSpeed);
        GimbalV.transform.Rotate(new Vector3(1.0f, 0.0f, 0.0f), _velocity.y * -PanSpeed);
        _velocity = Vector2.Lerp(_velocity, new Vector2(0.0f, 0.0f), Time.deltaTime * Deceleration);

        Vector3 newPos = transform.localPosition;
        newPos += new Vector3(0.0f, 0.0f, _zoomVelocity * Mathf.Abs(transform.localPosition.z));
        newPos.z = Mathf.Clamp(newPos.z, -MaxZoom, -MinZoom);
        transform.localPosition = newPos;
        _zoomVelocity = Mathf.Lerp(_zoomVelocity, 0.0f, Time.deltaTime * ZoomDeceleration);
    }

    public void MouseMove(InputAction.CallbackContext context)
    {
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
            default:
                break;
        }
    }

    public void MouseWheel(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            Vector2 value = context.ReadValue<Vector2>();
            value = Vector2.ClampMagnitude(value, 1.0f);
            _zoomVelocity += value.y * ZoomSpeed;
        }
        
    }
}