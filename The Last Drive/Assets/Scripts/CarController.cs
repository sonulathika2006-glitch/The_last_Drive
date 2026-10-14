
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    public Transform player;
    public Transform seatPoint;
    public Transform exitPoint;

    public float acceleration = 12f;
    public float reverseSpeed = 5f;
    public float turnSpeed = 70f;
    public float interactionDistance = 3f;

    private Rigidbody rb;
    private FirstPersonController fps;
    private CharacterController characterController;
    private Renderer[] playerRenderers;

    private bool isDriving;
    private float moveInput;
    private float turnInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.constraints |= RigidbodyConstraints.FreezeRotationX
                        | RigidbodyConstraints.FreezeRotationZ;

        if (player != null)
        {
            fps = player.GetComponent<FirstPersonController>();
            characterController =
                player.GetComponent<CharacterController>();
            playerRenderers =
                player.GetComponentsInChildren<Renderer>(true);
        }
    }

    void Update()
    {
        if (player == null || seatPoint == null ||
            exitPoint == null)
            return;

        if (!isDriving)
        {
            if (Vector3.Distance(player.position, transform.position)
                <= interactionDistance &&
                Input.GetKeyDown(KeyCode.F))
            {
                EnterCar();
            }

            return;
        }

        moveInput = 0f;
        turnInput = 0f;

        if (Input.GetKey(KeyCode.UpArrow))
            moveInput -= 1f;

        if (Input.GetKey(KeyCode.DownArrow))
            moveInput += 1f;

        if (Input.GetKey(KeyCode.LeftArrow))
            turnInput += 1f;

        if (Input.GetKey(KeyCode.RightArrow))
            turnInput -= 1f;

        if (Input.GetKeyDown(KeyCode.F))
            ExitCar();
    }

    void FixedUpdate()
    {
        if (!isDriving) return;

        float speed = moveInput >= 0f
            ? acceleration
            : reverseSpeed;

        Vector3 movement = transform.forward
            * moveInput * speed * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + movement);

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            float reverseDirection = moveInput > 0f ? 1f : -1f;

            Quaternion rotation = Quaternion.Euler(
                0f,
                turnInput * turnSpeed * reverseDirection
                    * Time.fixedDeltaTime,
                0f
            );

            rb.MoveRotation(rb.rotation * rotation);
        }
    }

    void EnterCar()
    {
        isDriving = true;

        if (fps != null)
            fps.enabled = false;

        if (characterController != null)
            characterController.enabled = false;

        foreach (Renderer r in playerRenderers)
            r.enabled = false;

        player.SetParent(seatPoint, false);
        player.localPosition = Vector3.zero;
        player.localRotation = Quaternion.identity;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void ExitCar()
    {
        isDriving = false;

        player.SetParent(null, true);
        player.SetPositionAndRotation(
            exitPoint.position,
            exitPoint.rotation
        );

        foreach (Renderer r in playerRenderers)
            r.enabled = true;

        if (characterController != null)
            characterController.enabled = true;

        if (fps != null)
            fps.enabled = true;

        moveInput = 0f;
        turnInput = 0f;
    }
}
