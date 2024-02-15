using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static List<Action<Vector2, Vector2, Vector2>> mouseInputActions = new();
    public static List<Action<Vector2>> moveInputActions = new();
    public static List<Action<float>> mouseLeftClickActions = new();
    public static List<Action<float>> jumpInputActions = new();
    float leftClickHeldTime = 0f;
    float jumpHeldTime = 0f;
    void Update()
    {
        Vector2 mouseScreenPos = Input.mousePosition;
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 mouseDelta = new(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        foreach (Action<Vector2,Vector2,Vector2> action in mouseInputActions)
        {
            action(mouseScreenPos, mouseWorldPos, mouseDelta);
        }
        Vector2 moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        foreach (Action<Vector2> action in moveInputActions)
        {
            action(moveInput);
        }
        if(Input.GetMouseButton(0))
        {
            leftClickHeldTime += Time.deltaTime;
        }
        else
        {
            leftClickHeldTime = 0f;
        }
        foreach (Action<float> action in mouseLeftClickActions)
        {
            action(leftClickHeldTime);
        }
        if(Input.GetButton("Jump"))
        {
            jumpHeldTime += Time.deltaTime;
        }
        else
        {
            jumpHeldTime = 0f;
        }
        foreach (Action<float> action in jumpInputActions)
        {
            action(jumpHeldTime);
        }
    }
    public static void RegisterMouseInputCallback(Action<Vector2, Vector2, Vector2> action)
    {
        mouseInputActions.Add(action);
    }
    public static void RegisterMoveInputCallback(Action<Vector2> action)
    {
        moveInputActions.Add(action);
    }
    public static void RegisterMouseLeftClickCallback(Action<float> action)
    {
        mouseLeftClickActions.Add(action);
    }
    public static void RegisterJumpInputCallback(Action<float> action)
    {
        jumpInputActions.Add(action);
    }
}
