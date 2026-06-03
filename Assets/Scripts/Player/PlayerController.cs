using UnityEngine;
using UnityEngine.InputSystem;

namespace RungTramTraSu
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 3.0f;
        [SerializeField] private float crouchSpeed = 1.5f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Camera Settings")]
        [SerializeField] private Transform playerCamera;
        [SerializeField] private float mouseSensitivity = 0.1f;
        [SerializeField] private float upperLookLimit = 80.0f;
        [SerializeField] private float lowerLookLimit = -80.0f;

        [Header("Crouch Settings")]
        [SerializeField] private float standingHeight = 2.0f;
        [SerializeField] private float crouchHeight = 1.2f;
        [SerializeField] private float cameraStandingY = 0.8f;
        [SerializeField] private float cameraCrouchY = 0.2f;
        [SerializeField] private float crouchTransitionSpeed = 8.0f;

        private CharacterController characterController;
        private PlayerInput playerInput;

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction crouchAction;

        private Vector2 currentInput;
        private Vector3 moveDirection;
        private float verticalRotation = 0.0f;
        private float targetCameraY;
        private bool isCrouching = false;
        private bool isFrozen = false;
        private bool isMovementLocked = false;

        public bool IsCrouching => isCrouching;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();

            // Lấy các action từ Input Action Asset đã gán trên PlayerInput
            moveAction = playerInput.actions["Move"];
            lookAction = playerInput.actions["Look"];
            crouchAction = playerInput.actions["Crouch"];
            
            targetCameraY = cameraStandingY;

            // Tự động tìm kiếm camera nếu bị trống do lỗi liên kết ở các Phase sau
            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }
        }

        private void OnEnable()
        {
            if (crouchAction != null)
            {
                crouchAction.started += OnCrouchStarted;
            }
        }

        private void OnDisable()
        {
            if (crouchAction != null)
            {
                crouchAction.started -= OnCrouchStarted;
            }
        }

        private void Start()
        {
            // Khóa chuột vào tâm màn hình
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (isFrozen)
            {
                // Áp dụng trọng lực ngay cả khi đứng im
                if (characterController != null && characterController.enabled && !characterController.isGrounded)
                {
                    moveDirection.y += gravity * Time.deltaTime;
                    characterController.Move(moveDirection * Time.deltaTime);
                }
                return;
            }

            if (!isMovementLocked)
            {
                HandleMovement();
            }
            else
            {
                // Áp dụng trọng lực khi khóa di chuyển (nếu CharacterController bật)
                if (characterController != null && characterController.enabled && !characterController.isGrounded)
                {
                    moveDirection = new Vector3(0, gravity, 0);
                    characterController.Move(moveDirection * Time.deltaTime);
                }
            }

            HandleMouseLook();
            HandleCrouchTransition();
        }

        private void HandleMovement()
        {
            // Đọc di chuyển từ WASD
            Vector2 rawInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            currentInput = rawInput;

            float speed = isCrouching ? crouchSpeed : walkSpeed;
            float movementDirectionY = moveDirection.y;

            // Tính toán hướng di chuyển dựa trên góc nhìn của người chơi
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);

            moveDirection = (forward * currentInput.y) + (right * currentInput.x);
            moveDirection.Normalize();
            moveDirection *= speed;

            // Áp dụng trọng lực
            if (characterController.isGrounded)
            {
                moveDirection.y = -0.5f; // Giữ nhân vật bám đất
            }
            else
            {
                moveDirection.y = movementDirectionY + (gravity * Time.deltaTime);
            }

            // Di chuyển CharacterController
            characterController.Move(moveDirection * Time.deltaTime);
        }

        private void HandleMouseLook()
        {
            // Đọc độ dời chuột (Look)
            Vector2 lookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;

            // Xoay ngang (Player Body)
            transform.Rotate(Vector3.up * lookInput.x * mouseSensitivity);

            // Xoay dọc (Camera)
            verticalRotation -= lookInput.y * mouseSensitivity;
            verticalRotation = Mathf.Clamp(verticalRotation, lowerLookLimit, upperLookLimit);

            if (playerCamera != null)
            {
                playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
            }
        }

        private void OnCrouchStarted(InputAction.CallbackContext context)
        {
            if (isFrozen) return;

            // Đảo trạng thái Crouch khi nhấn phím C
            isCrouching = !isCrouching;

            // Thiết lập mục tiêu chiều cao và camera
            characterController.height = isCrouching ? crouchHeight : standingHeight;
            targetCameraY = isCrouching ? cameraCrouchY : cameraStandingY;

            // Điều chỉnh vị trí của CharacterController center để không bị lún đất
            characterController.center = new Vector3(0, characterController.height / 2f, 0);
        }

        private void HandleCrouchTransition()
        {
            if (playerCamera == null) return;

            // Tạo hiệu ứng chuyển đổi mượt mà cho camera khi cúi người
            Vector3 camPos = playerCamera.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, targetCameraY, Time.deltaTime * crouchTransitionSpeed);
            playerCamera.localPosition = camPos;
        }

        /// <summary>
        /// Khóa hoặc mở khóa di chuyển của người chơi (dùng khi thoại, chuyển cảnh)
        /// </summary>
        public void SetFrozen(bool frozen)
        {
            isFrozen = frozen;
            if (frozen)
            {
                moveDirection = Vector3.zero;
                // Mở khóa chuột khi hội thoại để tương tác UI nếu cần
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        /// <summary>
        /// Khóa hoặc mở khóa di chuyển WASD của người chơi nhưng vẫn cho phép xoay chuột nhìn
        /// </summary>
        public void SetMovementLocked(bool locked)
        {
            isMovementLocked = locked;
            if (locked)
            {
                moveDirection = Vector3.zero;
            }
        }
    }
}
