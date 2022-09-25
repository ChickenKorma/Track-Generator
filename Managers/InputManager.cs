using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private PlayerInputActions playerInputActions;

    public static Action<float> throttleEvent = delegate { };
    public static Action<float> steerEvent = delegate { };

    public static Action handbrakeEvent = delegate { };
    public static Action exitEvent = delegate { };

    private void OnEnable()
    {
        playerInputActions = new PlayerInputActions();
        playerInputActions.Enable();

        playerInputActions.Gameplay.Throttle.started += OnThrottle;
        playerInputActions.Gameplay.Throttle.performed += OnThrottle;
        playerInputActions.Gameplay.Throttle.canceled += OnThrottle;

        playerInputActions.Gameplay.Steer.started += OnSteer;
        playerInputActions.Gameplay.Steer.performed += OnSteer;
        playerInputActions.Gameplay.Steer.canceled += OnSteer;

        playerInputActions.Gameplay.Handbrake.performed += OnHandbrake;

        playerInputActions.Gameplay.Exit.performed += OnExit;
    }

    private void OnDisable()
    {
        playerInputActions.Gameplay.Throttle.started -= OnThrottle;
        playerInputActions.Gameplay.Throttle.performed -= OnThrottle;
        playerInputActions.Gameplay.Throttle.canceled -= OnThrottle;

        playerInputActions.Gameplay.Steer.started -= OnSteer;
        playerInputActions.Gameplay.Steer.performed -= OnSteer;
        playerInputActions.Gameplay.Steer.canceled -= OnSteer;

        playerInputActions.Gameplay.Handbrake.performed -= OnHandbrake;

        playerInputActions.Gameplay.Exit.performed -= OnExit;

        playerInputActions.Disable();
    }

    private void OnThrottle(InputAction.CallbackContext context)
    {
        throttleEvent.Invoke(context.ReadValue<float>());
    }

    private void OnSteer(InputAction.CallbackContext context)
    {
        steerEvent.Invoke(context.ReadValue<float>());
    }

    private void OnHandbrake(InputAction.CallbackContext context)
    {
        handbrakeEvent.Invoke();
    }

    private void OnExit(InputAction.CallbackContext context)
    {
        exitEvent.Invoke();
    }
}
