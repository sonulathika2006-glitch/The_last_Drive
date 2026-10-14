
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TaxiPlayerController : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform taxi;
    public Transform driverSeat;
    public Transform driverCameraPoint;
    public Transform exitPoint;

    [Header("Walking")]
    public float walkSpeed = 4f;
    public float mouseSensitivity = 2f;

    [Header("Taxi Entry")]
    public float enterDistance = 15f;

    [Header("Natural Car Movement")]
    public float driveSpeed = 12f;
    public float reverseSpeed = 5f;
    public float acceleration = 4f;
    public float braking = 8f;
    public float coastingDeceleration = 1.5f;

    [Header("Natural Steering")]
    public float turnSpeed = 55f;
    public float steeringSmoothness = 2f;
    public float lowSpeedSteering = 0.35f;

    [Tooltip("Enable this if W moves the taxi backward.")]
    public bool reverseTaxiForward = true;

    [Tooltip("Enable this if A and D steer the wrong way.")]
    public bool reverseSteering = false;

    [Header("Driving Camera")]
    public Vector3 cameraRotationOffset =
        new Vector3(0f, 180f, 0f);

    public float drivingLookLimit = 65f;

    private CharacterController controller;
    private bool insideTaxi;

    private float verticalVelocity;
    private float cameraPitch;
    private float drivingLookYaw;

    private float currentSpeed;
    private float currentSteer;

    private Vector3 walkingCameraLocalPosition;
    private Quaternion walkingCameraLocalRotation;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        if (playerCamera != null)
        {
            walkingCameraLocalPosition =
                playerCamera.transform.localPosition;

            walkingCameraLocalRotation =
                playerCamera.transform.localRotation;
        }

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

        if (insideTaxi)
        {
            DriveTaxi();
            UpdateDrivingCamera();
        }
        else
        {
            LookAroundWalking();
            Walk();
        }
    }

    void LookAroundWalking()
    {
        if (playerCamera == null)
            return;

        float mouseX =
            Input.GetAxis("Mouse X") * mouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") * mouseSensitivity;

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
            transform.right * x +
            transform.forward * z;

        movement = Vector3.ClampMagnitude(movement, 1f);

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity +=
            Physics.gravity.y * Time.deltaTime;

        Vector3 velocity =
            movement * walkSpeed +
            Vector3.up * verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    void TryEnterTaxi()
    {
        if (playerCamera == null ||
            taxi == null ||
            driverSeat == null ||
            driverCameraPoint == null ||
            exitPoint == null)
        {
            Debug.LogError(
                "Assign all five references in the Inspector."
            );
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            driverSeat.position
        );

        if (distance > enterDistance)
        {
            Debug.Log("Move closer to the driver's door.");
            return;
        }

        insideTaxi = true;
        controller.enabled = false;

        cameraPitch = 0f;
        drivingLookYaw = 0f;
        currentSteer = 0f;

        Debug.Log("Entered taxi.");
    }

    void UpdateDrivingCamera()
    {
        if (playerCamera == null ||
            driverCameraPoint == null)
            return;

        float mouseX =
            Input.GetAxis("Mouse X") * mouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") * mouseSensitivity;

        drivingLookYaw += mouseX;

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(
            cameraPitch,
            -drivingLookLimit,
            drivingLookLimit
        );

        playerCamera.transform.position =
            driverCameraPoint.position;

        Quaternion baseRotation =
            driverCameraPoint.rotation *
            Quaternion.Euler(cameraRotationOffset);

        playerCamera.transform.rotation =
            baseRotation *
            Quaternion.Euler(
                cameraPitch,
                drivingLookYaw,
                0f
            );
    }

    void DriveTaxi()
    {
        if (taxi == null)
            return;

        float moveInput = Input.GetAxisRaw("Vertical");
        float steerInput = Input.GetAxisRaw("Horizontal");

        float direction = reverseTaxiForward ? -1f : 1f;

        // Choose a target speed without changing instantly.
        float targetSpeed = 0f;

        if (moveInput > 0f)
            targetSpeed = moveInput * driveSpeed;
        else if (moveInput < 0f)
            targetSpeed = moveInput * reverseSpeed;

        // Smooth acceleration, braking and reversing.
        float rate;

        if (Mathf.Abs(moveInput) < 0.01f)
        {
            rate = coastingDeceleration;
            targetSpeed = 0f;
        }
        else
        {
            bool slowingDown =
                Mathf.Abs(targetSpeed) < Mathf.Abs(currentSpeed) ||
                currentSpeed * targetSpeed < 0f;

            rate = slowingDown ? braking : acceleration;
        }

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            rate * Time.deltaTime
        );

        // Smooth the steering input instead of snapping.
        currentSteer = Mathf.MoveTowards(
            currentSteer,
            steerInput,
            steeringSmoothness * Time.deltaTime
        );

        // Move the car in its local forward direction.
        taxi.Translate(
            Vector3.forward *
            currentSpeed *
            direction *
            Time.deltaTime,
            Space.Self
        );

        // Reduce steering at low speeds.
        float speedFactor = Mathf.Clamp01(
            Mathf.Abs(currentSpeed) /
            Mathf.Max(driveSpeed, 0.01f)
        );

        float steeringFactor = Mathf.Lerp(
            lowSpeedSteering,
            1f,
            speedFactor
        );

        float steeringDirection =
            reverseSteering ? -1f : 1f;

        // Preserve the left/right direction of your working code.
        // Steering also works while coasting.
        if (Mathf.Abs(currentSpeed) > 0.1f)
        {
            float turnAmount =
                currentSteer *
                steeringDirection *
                turnSpeed *
                steeringFactor *
                Mathf.Sign(currentSpeed) *
                Time.deltaTime;

            taxi.Rotate(
                Vector3.up * turnAmount,
                Space.Self
            );
        }
    }

    void ExitTaxi()
    {
        if (exitPoint == null ||
            taxi == null ||
            playerCamera == null)
            return;

        insideTaxi = false;

        transform.SetPositionAndRotation(
            exitPoint.position,
            Quaternion.Euler(
                0f,
                taxi.eulerAngles.y,
                0f
            )
        );

        playerCamera.transform.localPosition =
            walkingCameraLocalPosition;

        playerCamera.transform.localRotation =
            walkingCameraLocalRotation;

        controller.enabled = true;

        verticalVelocity = 0f;
        cameraPitch = 0f;
        drivingLookYaw = 0f;
        currentSteer = 0f;
        currentSpeed = 0f;

        Debug.Log("Exited taxi.");
    }
}
