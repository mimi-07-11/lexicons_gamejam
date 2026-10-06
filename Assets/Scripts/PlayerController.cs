using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float speed = 4f;
    Rigidbody2D rb; 
    InputAction move; 
    Vector2 input;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        move = new InputAction("Move", InputActionType.Value);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddBinding("<Gamepad>/leftStick");
    }

    void OnEnable()  => move.Enable();
    void OnDisable() => move.Disable();
    void Update()
{
    input = move.ReadValue<Vector2>();
    
}

void FixedUpdate()
{
    rb.linearVelocity = input * speed;
    
}
}