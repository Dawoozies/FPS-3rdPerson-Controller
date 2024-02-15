using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float sensitivity;
    public Transform cameraContainer;
    Camera mainCamera;
    Vector2 angles;
    [Range(0f,90f)]
    public float yRotationLimit;
    public Vector3 offsetFromPlayer;
    public float moveSpeed;
    Rigidbody rb;
    public float jumpSpeed;
    public LayerMask groundLayerMask;
    public float groundCheckDistance;
    public bool grounded;
    void Start()
    {
        mainCamera = Camera.main;
        InputManager.RegisterMouseInputCallback(MouseInputHandler);
        InputManager.RegisterMoveInputCallback(MoveInputHandler);
        InputManager.RegisterJumpInputCallback(JumpInputHandler);
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        cameraContainer.transform.position = transform.position;
        mainCamera.transform.localPosition = offsetFromPlayer;
        grounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayerMask);
    }
    void MouseInputHandler(Vector2 mouseScreenPos, Vector2 mouseWorldPos, Vector2 mouseDelta)
    {
        angles.x += mouseDelta.x * sensitivity;
        angles.y += mouseDelta.y * sensitivity;
        angles.y = Mathf.Clamp(angles.y, -yRotationLimit, yRotationLimit);
        Quaternion xRotation = Quaternion.AngleAxis(angles.x, Vector3.up);
        Quaternion yRotation = Quaternion.AngleAxis(angles.y, Vector3.left);
        cameraContainer.transform.localRotation = xRotation * yRotation;
    }
    void MoveInputHandler(Vector2 moveInput)
    {
        if(moveInput.magnitude > 0)
        {
            Vector3 playerForward = cameraContainer.transform.forward;
            playerForward.y = 0;
            transform.forward = playerForward;
            Vector3 currentYVelocity = new(0f, rb.velocity.y, 0f);
            rb.velocity = moveInput.y*transform.forward * moveSpeed + moveInput.x*transform.right*moveSpeed + currentYVelocity;
        }
    }
    void JumpInputHandler(float heldTime)
    {
        if (heldTime > 0 && grounded)
        {
            rb.velocity = new(rb.velocity.x, jumpSpeed, rb.velocity.z);
        }
    }
}
