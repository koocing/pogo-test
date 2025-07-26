using System.Collections;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 12f;
    public float acceleration = 20f;
    public float gravity = -30f;
    public float jumpForce = 12f;
    public float airControlMultiplier = 0.2f; // tweak to feel

    [Header("References")]
    public Transform orientation;

    [HideInInspector] public float currentSpeed;
    [HideInInspector] public CharacterController controller;

    private PlayerInputActions input;
    private Vector2 moveInput;
    private Vector3 velocity;
    private Vector3 currentMoveVelocity;
    private bool isGrounded;
    private bool jumpPressed;

    [Header("Pogo Settings")]
    public float pogoForce = 16f;
    public float pogoCooldown = 0.1f;
    public float maxPogoRange = 2f;
    public Transform cameraTransform;

    private bool pogoPrimed;
    private bool hasPogoed;
    private float lastPogoTime = -999f;

    public float perfectWindow = 0.2f;
    public float perfectBoost = 1.5f;

    private float pogoPressTime = -999f;

    private float timeSinceLeftGround = 999f;

    public LayerMask pogoLayers;

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
        input.Player.Pogo.performed += _ =>
        {
            pogoPrimed = true;
            pogoPressTime = Time.time;
        };

        input.Player.Pogo.canceled += _ => pogoPrimed = false;
    }

    void OnDisable() => input.Disable();

    void Update()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded)
            timeSinceLeftGround = 0f;
        else
            timeSinceLeftGround += Time.deltaTime;

        if (isGrounded && hasPogoed)
        {
            hasPogoed = false;
        }

        bool canPerfectPogo = timeSinceLeftGround > 0.05f && Time.time - pogoPressTime <= perfectWindow;

        if (canPerfectPogo)
        {
            Vector3 rayOrigin = cameraTransform.position;
            Vector3 rayDirection = cameraTransform.forward;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, maxPogoRange, pogoLayers, QueryTriggerInteraction.Ignore))
            {
                Debug.Log($"Perfect Pogo Check: grounded={isGrounded}, hit={hit.collider.name}, distance={hit.distance}");

                // Optional: prevent pogo if ray hit is very close
                if (hit.distance < 0.3f)
                {
                    Debug.Log("Skipped perfect pogo: too close to surface (probably grounded)");
                    return;
                }

                Debug.DrawLine(rayOrigin, hit.point, Color.yellow, 1f);
                Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.magenta, 1f);

                Vector3 baseDir = -rayDirection.normalized;
                Vector3 surfaceNormal = hit.normal;
                Vector3 blended = (baseDir * 0.8f + surfaceNormal * 0.2f).normalized;

                velocity = blended * pogoForce * perfectBoost;

                hasPogoed = true;
                lastPogoTime = Time.time;
                pogoPressTime = -999f; // prevent repeat perfects

                Debug.Log("Perfect Pogo Performed");
            }
        }

        if (pogoPrimed && Time.time - lastPogoTime > pogoCooldown)
        {
            Vector3 rayOrigin = cameraTransform.position;
            Vector3 rayDirection = cameraTransform.forward;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, maxPogoRange, pogoLayers, QueryTriggerInteraction.Ignore))
            {
                Debug.Log($"Pogo Hit: {hit.collider.name}");
                Debug.DrawLine(rayOrigin, hit.point, Color.green, 1f);
                Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.cyan, 1f);

                Vector3 baseDir = -rayDirection.normalized;
                Vector3 surfaceNormal = hit.normal;
                Vector3 blended = (baseDir * 0.8f + surfaceNormal * 0.2f).normalized;

                velocity = blended * pogoForce;

                lastPogoTime = Time.time;

                hasPogoed = true;
            }
        }

        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else if (velocity.y < 0)
        {
            velocity.y = -2f; // Stick to ground
            if (jumpPressed)
            {
                velocity.y = jumpForce;
                jumpPressed = false;
            }
        }

        // Smoothly decelerate pogo momentum when grounded
        if (isGrounded && Time.time - lastPogoTime > 0.05f)
        {
            Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);
            horizontalVel = Vector3.Lerp(horizontalVel, Vector3.zero, Time.deltaTime * 5f);
            velocity.x = horizontalVel.x;
            velocity.z = horizontalVel.z;
        }

        // Movement input
        Vector3 targetMove = (orientation.right * moveInput.x + orientation.forward * moveInput.y).normalized;
        float controlFactor = 1f;
        if (!isGrounded)
        {
            controlFactor = hasPogoed ? airControlMultiplier : 1f;
        }

        currentMoveVelocity = Vector3.Lerp(
            currentMoveVelocity,
            targetMove * moveSpeed * controlFactor,
            acceleration * Time.deltaTime
        );

        controller.Move(currentMoveVelocity * Time.deltaTime);
        controller.Move(velocity * Time.deltaTime);

        currentSpeed = currentMoveVelocity.magnitude;
    }
}

