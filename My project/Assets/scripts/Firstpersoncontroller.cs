using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform cameraTransform;
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float acceleration = 14f;
    [SerializeField] private float fallResetHeight = -20f;
    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 85f;
    private CharacterController controller;
    private float verticalVelocity, pitch;
    private Vector3 horizontalVelocity, spawnPosition;
    public Transform View => cameraTransform;
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null)
        {
            Camera child = GetComponentInChildren<Camera>();
            if (child == null) child = Camera.main;
            if (child != null) cameraTransform = child.transform;
        }
        if (cameraTransform == null) { Debug.LogError("Assign the player's camera to FirstPersonController.", this); enabled = false; return; }
        spawnPosition = transform.position;
        if (CowDatingGame.Instance == null) new GameObject("Cow Dating Game").AddComponent<CowDatingGame>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void Update()
    {
        if (CowDatingGame.Blocked || Cursor.lockState != CursorLockMode.Locked)
        { horizontalVelocity = Vector3.zero; return; }
        Vector2 look = GameInput.Look * mouseSensitivity * CowDatingGame.LookScale;
        transform.Rotate(Vector3.up * look.x);
        pitch = Mathf.Clamp(pitch - look.y, -maxLookAngle, maxLookAngle);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0, 0);
        Vector2 input = GameInput.Movement;
        Vector3 target = (transform.right * input.x + transform.forward * input.y) * (GameInput.Sprint ? sprintSpeed : walkSpeed);
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, Mathf.Max(0.1f, acceleration) * Time.deltaTime);
        float safeGravity = -Mathf.Max(0.1f, Mathf.Abs(gravity));
        if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
        if (controller.isGrounded && GameInput.Pressed(KeyCode.Space)) verticalVelocity = Mathf.Sqrt(Mathf.Max(0, jumpHeight) * -2f * safeGravity);
        verticalVelocity += safeGravity * Time.deltaTime;
        controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);
        if (transform.position.y < fallResetHeight)
        {
            controller.enabled = false;
            transform.position = spawnPosition;
            controller.enabled = true;
            verticalVelocity = 0;
        }
    }
}
