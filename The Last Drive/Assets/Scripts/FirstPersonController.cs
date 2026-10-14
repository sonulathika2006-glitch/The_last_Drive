
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float gravity = -20f;
    public Transform playerCamera;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraPitch;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Mouse look: left and right
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(0f, mouseX, 0f);

        // Mouse look: up and down
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -85f, 85f);
        
        if (playerCamera != null)
        {
            playerCamera.localRotation =
                Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        // Walk forward and backward
        float vertical = 0f;

        if (Input.GetKey(KeyCode.UpArrow) ||
            Input.GetKey(KeyCode.W))
            vertical -= 1f;

        if (Input.GetKey(KeyCode.DownArrow) ||
            Input.GetKey(KeyCode.S))
            vertical += 1f;

        // Optional sideways movement
        float horizontal = 0f;

        if (Input.GetKey(KeyCode.A))
            horizontal += 1f;

        if (Input.GetKey(KeyCode.D))
            horizontal -= 1f;

        Vector3 movement =
            (transform.forward * vertical +
             transform.right * horizontal).normalized;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        movement *= moveSpeed;
        movement.y = verticalVelocity;

        controller.Move(movement * Time.deltaTime);

        // Escape releases the mouse
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Click to lock the mouse again
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
