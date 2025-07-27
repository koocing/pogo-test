// Simplified and beginner-friendly version of CameraController
// Includes clear comments and organization

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Look Settings")]
    public Transform orientation;
    public Transform cameraTarget; // The camera to bob
    public float sensitivity = 1.5f;
    public float rollIntensity = 10f;
    public float rollSpeed = 5f;

    [Header("Headbob Settings")]
    public float bobFrequency = 8f;
    public float bobAmplitude = 0.05f;
    public float speedThreshold = 0.1f; // Min speed to trigger bob

    private PlayerInputActions input;
    private Vector2 lookInput;
    private float pitch;
    private float bobTimer;
    private Vector3 cameraStartLocalPos;

    private PlayerController player; // Reference to PlayerController

    void Awake()
    {
        input = new PlayerInputActions();
        player = orientation.GetComponentInParent<PlayerController>();
        cameraStartLocalPos = cameraTarget.localPosition;
    }

    void OnEnable()
    {
        input.Enable();
        input.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        input.Player.Look.canceled += _ => lookInput = Vector2.zero;
    }

    void OnDisable()
    {
        input.Disable();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleHeadbob();
    }

    void HandleMouseLook()
    {
        float mouseX = lookInput.x * sensitivity;
        float mouseY = lookInput.y * sensitivity;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        orientation.Rotate(Vector3.up * mouseX);

        float targetRoll = -mouseX * rollIntensity;
        float currentRoll = Mathf.LerpAngle(transform.localEulerAngles.z, targetRoll, Time.deltaTime * rollSpeed);

        transform.localRotation = Quaternion.Euler(pitch, 0f, currentRoll);
    }

    void HandleHeadbob()
    {
        if (player != null && player.controller.isGrounded && player.currentSpeed > speedThreshold)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude;
            Vector3 targetPos = cameraStartLocalPos + new Vector3(0, bobOffset, 0);
            cameraTarget.localPosition = targetPos;
        }
        else
        {
            bobTimer = 0f;
            cameraTarget.localPosition = Vector3.Lerp(cameraTarget.localPosition, cameraStartLocalPos, Time.deltaTime * 5f);
        }
    }
}
