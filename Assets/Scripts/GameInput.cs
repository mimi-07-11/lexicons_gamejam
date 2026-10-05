using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Works with BOTH the old Input Manager and the new Input System, so no setup needed.
public static class GameInput
{
    public static Vector2 Move
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }
    }

    public static bool AttackPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current; var k = Keyboard.current;
            return (m != null && m.leftButton.wasPressedThisFrame) || (k != null && k.jKey.wasPressedThisFrame);
#else
            return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J);
#endif
        }
    }

    public static bool DodgePressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }

    public static Vector2 MousePosition
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }
    }
}
