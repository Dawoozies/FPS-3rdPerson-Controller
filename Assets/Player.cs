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
    public float wallCheckDistance;
    public bool atWall;
    public float wallJumpTime;
    public int wallJumpsLeft;
    public int afterTimeWallJumpsLeft;
    Vector3 scaleVel;
    public Transform weaponParent;
    public GameObject shotgunPrefab;
    GameObject shotgun;
    public Vector3 weaponOffset;
    ParticleSystem weaponSystem;
    void Start()
    {
        mainCamera = Camera.main;
        InputManager.RegisterMouseInputCallback(MouseInputHandler);
        InputManager.RegisterMoveInputCallback(MoveInputHandler);
        InputManager.RegisterJumpInputCallback(JumpInputHandler);
        rb = GetComponent<Rigidbody>();
        GameObject shotgun = Instantiate(shotgunPrefab, weaponParent);
        shotgunPrefab.transform.forward = -Vector3.forward;
        weaponSystem = weaponParent.GetComponentInChildren<ParticleSystem>();
        InputManager.RegisterMouseLeftClickCallback(MouseLeftClickHandler);
        weaponSystem.transform.parent = shotgun.transform;
        weaponSystem.transform.localPosition = Vector3.zero;
        weaponSystem.transform.localPosition -= 16f*weaponSystem.transform.forward;
    }
    void Update()
    {
        cameraContainer.transform.position = transform.position;
        mainCamera.transform.localPosition = offsetFromPlayer;
        grounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayerMask);
        atWall = Physics.Raycast(transform.position, transform.forward, wallCheckDistance, groundLayerMask);
        if(wallJumpTime > 0f)
        {
            wallJumpTime -= Time.deltaTime;
        }
        if(grounded)
        {
            wallJumpsLeft = 1;
            afterTimeWallJumpsLeft = 0;
        }
        else
        {
            rb.velocity -= new Vector3(0f, 8f * Time.deltaTime, 0f);
        }
        transform.localScale = Vector3.SmoothDamp(transform.localScale, Vector3.one, ref scaleVel, 0.25f);
        weaponParent.transform.position = mainCamera.transform.position + weaponOffset.x * mainCamera.transform.right + weaponOffset.y * mainCamera.transform.up + weaponOffset.z * mainCamera.transform.forward;
        weaponParent.transform.rotation = mainCamera.transform.rotation;
    }
    void MouseLeftClickHandler(float heldTime)
    {
        if(heldTime > 0 && weaponSystem.isStopped)
            weaponSystem.Play(true);
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
        if(moveInput.magnitude > 0 && wallJumpTime <= 0)
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
            transform.localScale = Vector3.one * 1.5f;
        }
        if (heldTime > 0 && atWall && afterTimeWallJumpsLeft > 0 && wallJumpTime <= 0)
        {
            afterTimeWallJumpsLeft--;
            wallJumpsLeft++;
            transform.localScale = Vector3.one * 1.5f;
        }
        if (heldTime > 0 && atWall && wallJumpsLeft > 0)
        {
            rb.velocity *= -1;
            rb.velocity = new(rb.velocity.x, jumpSpeed, rb.velocity.z);
            wallJumpTime = 0.5f;
            wallJumpsLeft--;
            afterTimeWallJumpsLeft++;
            transform.localScale = Vector3.one * 1.5f;
        }

    }
}