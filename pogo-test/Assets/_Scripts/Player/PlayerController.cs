// A simplified and reorganized version of your player controller
// written in a beginner-friendly style, with clear comments

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // Movement Settings
    public float moveSpeed = 12f;
    public float acceleration = 20f;
    public float gravity = -30f;
    public float jumpForce = 12f;
    public float airControlMultiplier = 0.2f;

    // References
    public Transform orientation;
    public Transform cameraTransform;
    public LayerMask pogoLayers;

    // Pogo Settings
    public float pogoForce = 16f;
    public float maxPogoRange = 2f;
    public float pogoCooldown = 0.1f;
    public float perfectWindow = 0.2f;
    public float perfectBoost = 1.5f;

    // Internal State
    public CharacterController controller;
    private PlayerInputActions input;

    private Vector2 moveInput;
    private Vector3 currentMoveVelocity;
    private Vector3 velocity;

    private bool isGrounded;
    private bool jumpPressed;
    private bool pogoPrimed;
    private bool hasPogoed;

    private float lastPogoTime = -999f;
    private float pogoPressTime = -999f;
    private float timeSinceLeftGround = 999f;

    public float currentSpeed;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = new PlayerInputActions();
    }

    void OnEnable()
    {
        input.Enable();
        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        input.Player.Jump.performed += _ => jumpPressed = true;
        input.Player.Pogo.performed += _ => {
            pogoPrimed = true;
            pogoPressTime = Time.time;
        };
        input.Player.Pogo.canceled += _ => pogoPrimed = false;
    }

    void OnDisable()
    {
        input.Disable();
    }

    void Update()
    {
        // Check if grounded
        isGrounded = controller.isGrounded;
        timeSinceLeftGround = isGrounded ? 0f : timeSinceLeftGround + Time.deltaTime;

        // Reset pogo state on landing
        if (isGrounded && hasPogoed)
        {
            hasPogoed = false;
        }

        TryPerfectPogo();
        TryNormalPogo();
        ApplyGravityAndJump();
        DampHorizontalVelocityIfGrounded();
        HandleMovementInput();

        // Apply Final Movement
        controller.Move(currentMoveVelocity * Time.deltaTime);
        controller.Move(velocity * Time.deltaTime);

        currentSpeed = currentMoveVelocity.magnitude;
    }

    void TryPerfectPogo()
    {
        bool canPerfectPogo = timeSinceLeftGround > 0.05f && Time.time - pogoPressTime <= perfectWindow;

        if (canPerfectPogo)
        {
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, maxPogoRange, pogoLayers))
            {
                if (hit.distance < 0.3f) return; // Too close to be considered midair

                Vector3 baseDir = -cameraTransform.forward.normalized;
                Vector3 surfaceNormal = hit.normal;
                Vector3 bounceDir = (baseDir * 0.8f + surfaceNormal * 0.2f).normalized;

                velocity = bounceDir * pogoForce * perfectBoost;
                lastPogoTime = Time.time;
                pogoPressTime = -999f;
                hasPogoed = true;
            }
        }
    }

    void TryNormalPogo()
    {
        bool canPogo = pogoPrimed && Time.time - lastPogoTime > pogoCooldown;

        if (canPogo)
        {
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, maxPogoRange, pogoLayers))
            {
                Vector3 baseDir = -cameraTransform.forward.normalized;
                Vector3 surfaceNormal = hit.normal;
                Vector3 bounceDir = (baseDir * 0.8f + surfaceNormal * 0.2f).normalized;

                velocity = bounceDir * pogoForce;
                lastPogoTime = Time.time;
                hasPogoed = true;
            }
        }
    }

    void ApplyGravityAndJump()
    {
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else if (velocity.y < 0f)
        {
            velocity.y = -2f;
            if (jumpPressed)
            {
                velocity.y = jumpForce;
                jumpPressed = false;
            }
        }
    }

    void DampHorizontalVelocityIfGrounded()
    {
        if (isGrounded && Time.time - lastPogoTime > 0.05f)
        {
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            horizontal = Vector3.Lerp(horizontal, Vector3.zero, Time.deltaTime * 5f);
            velocity.x = horizontal.x;
            velocity.z = horizontal.z;
        }
    }

    void HandleMovementInput()
    {
        Vector3 target = (orientation.right * moveInput.x + orientation.forward * moveInput.y).normalized;
        float control = (!isGrounded && hasPogoed) ? airControlMultiplier : 1f;

        currentMoveVelocity = Vector3.Lerp(
            currentMoveVelocity,
            target * moveSpeed * control,
            acceleration * Time.deltaTime
        );
    }
}