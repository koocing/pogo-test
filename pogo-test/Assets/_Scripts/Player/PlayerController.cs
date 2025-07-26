using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float airControl = 0.5f;
    public float gravity = -30f;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity;
    private bool isGrounded;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        isGrounded = controller.isGrounded;

        // Apply gravity
        if (!isGrounded)
            velocity.y += gravity * Time.deltaTime;
        else if (velocity.y < 0)
            velocity.y = -2f; // Stick to ground

        Vector3 move = (cameraTransform.right * moveInput.x + cameraTransform.forward * moveInput.y);
        move.y = 0;
        controller.Move(move * moveSpeed * Time.deltaTime);

        controller.Move(velocity * Time.deltaTime);
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }
}
