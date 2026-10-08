using UnityEngine;

// Keeps all gameplay input compatible with either Unity input backend.
public static class GameInput
{
    public static bool Pressed(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return false;
        UnityEngine.InputSystem.Key mapped;
        switch (key)
        {
            case KeyCode.E: mapped = UnityEngine.InputSystem.Key.E; break;
            case KeyCode.Y: mapped = UnityEngine.InputSystem.Key.Y; break;
            case KeyCode.N: mapped = UnityEngine.InputSystem.Key.N; break;
            case KeyCode.Space: mapped = UnityEngine.InputSystem.Key.Space; break;
            case KeyCode.Escape: mapped = UnityEngine.InputSystem.Key.Escape; break;
            case KeyCode.R: mapped = UnityEngine.InputSystem.Key.R; break;
            case KeyCode.Alpha1: mapped = UnityEngine.InputSystem.Key.Digit1; break;
            case KeyCode.Alpha2: mapped = UnityEngine.InputSystem.Key.Digit2; break;
            default: return false;
        }
        return keyboard[mapped].wasPressedThisFrame;
#else
        return Input.GetKeyDown(key);
#endif
    }
    public static Vector2 Movement
    {
        get
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (k == null) return Vector2.zero;
            return Vector2.ClampMagnitude(new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0),
                (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0)), 1);
#else
            return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1);
#endif
        }
    }
    public static Vector2 Look
    {
        get
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var m = UnityEngine.InputSystem.Mouse.current;
            return m == null ? Vector2.zero : m.delta.ReadValue() * 0.08f;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
        }
    }
    public static bool Sprint
    {
        get
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var k = UnityEngine.InputSystem.Keyboard.current;
            return k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }
    }
}
