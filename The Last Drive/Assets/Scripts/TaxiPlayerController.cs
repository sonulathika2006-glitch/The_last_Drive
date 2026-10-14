
using UnityEngine;

public class TaxiPlayerController : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform taxi;
    public Transform driverSeat;
    public Transform exitPoint;

    [Header("Walking")]
    public float walkSpeed = 4f;
    public float mouseSensitivity = 2f;

    [Header("Taxi")]
    public float enterDistance = 5f;
    public float driveSpeed = 12f;
    public float reverseSpeed = 5f;
    public float turnSpeed = 65f;

    [Header("Camera")]
    public float cameraHeight = 1.6f;
    public Vector3 cameraOffset = Vector3.zero;

    private CharacterController controller;
    private bool insideTaxi;
    private float verticalVelocity;
    private float cameraPitch;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (insideTaxi)
                ExitTaxi();
            else
                TryEnterTaxi();
        }

        LookAround();

        if (insideTaxi)
        {
            FollowDriverSeat();
            DriveTaxi();
        }
        else
        {
            Walk();
        }
    }

    void LookAround()
    {
        if (playerCamera == null)
            return;

        float mouseX =
            Input.GetAxis("Mouse X") * mouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (insideTaxi && taxi != null)
            taxi.Rotate(Vector3.up * mouseX);
        else
            transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);

        playerCamera.transform.localRotation =
            Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    void Walk()
    {
        if (controller == null || !controller.enabled)
            return;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 movement =
            transform.right * x + transform.forward * z;

        movement = Vector3.ClampMagnitude(movement, 1f);

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += Physics.gravity.y * Time.deltaTime;

        controller.Move(
            (movement * walkSpeed +
            Vector3.up * verticalVelocity) * Time.deltaTime
        );
    }

    void TryEnterTaxi()
    {
        if (taxi == null || driverSeat == null ||
            exitPoint == null || playerCamera == null)
        {
            Debug.LogError("Assign all taxi references in the Inspector.");
            return;
        }

        float distance =
            Vector3.Distance(transform.position, driverSeat.position);

        Debug.Log("Distance to seat: " + distance);

        if (distance > enterDistance)
        {
            Debug.Log("Move closer to the driver's door.");
            return;
        }

        insideTaxi = true;

        if (controller != null)
            controller.enabled = false;

        // Keep Player unparented to avoid inheriting Taxi scale.
        transform.SetPositionAndRotation(
            driverSeat.position,
            Quaternion.Euler(0f, taxi.eulerAngles.y, 0f)
        );

        cameraPitch = 0f;
        playerCamera.transform.localRotation = Quaternion.identity;

        Debug.Log("Entered taxi.");
    }

    void FollowDriverSeat()
    {
        if (driverSeat == null)
            return;

        // Follow the seat in world space while the taxi moves.
        transform.position = driverSeat.position;
    }

    void DriveTaxi()
    {
        if (taxi == null)
            return;

        float move = Input.GetAxisRaw("Vertical");
        float turn = Input.GetAxisRaw("Horizontal");

        float speed = move >= 0f ? driveSpeed : reverseSpeed;

        taxi.Translate(
            Vector3.forward * move * speed * Time.deltaTime,
            Space.Self
        );

        if (Mathf.Abs(move) > 0.01f)
        {
            taxi.Rotate(
                Vector3.up * turn * turnSpeed *
                Time.deltaTime * Mathf.Sign(move)
            );
        }
    }

    void ExitTaxi()
    {
        if (exitPoint == null || taxi == null)
            return;

        insideTaxi = false;

        transform.SetPositionAndRotation(
            exitPoint.position,
            Quaternion.Euler(0f, taxi.eulerAngles.y, 0f)
        );

        if (controller != null)
            controller.enabled = true;

        verticalVelocity = 0f;

        Debug.Log("Exited taxi.");
    }
}
