using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RungTramTraSu
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Raycast Settings")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float interactDistance = 3.5f;
        [SerializeField] private LayerMask interactableLayer;

        private PlayerInput playerInput;
        private InputAction interactAction;
        private IInteractable currentInteractable;

        // Sự kiện dùng để cập nhật UI
        public static event Action<string> OnInteractableFound;
        public static event Action OnInteractableLost;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            interactAction = playerInput.actions["Interact"];

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            CheckInteractable();

            // Nếu nhấn nút Tương tác (E) và có đối tượng tương tác
            if (interactAction != null && interactAction.triggered)
            {
                TryInteract();
            }
        }

        private void CheckInteractable()
        {
            if (cameraTransform == null) return;

            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit;

            // Bắn tia raycast
            if (Physics.Raycast(ray, out hit, interactDistance, interactableLayer))
            {
                // Kiểm tra xem đối tượng có Component triển khai IInteractable không
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                
                // Nếu là đối tượng mới
                if (interactable != null)
                {
                    if (interactable != currentInteractable)
                    {
                        currentInteractable = interactable;
                        // Phát sự kiện tìm thấy để UI hiển thị thông báo gợi ý
                        OnInteractableFound?.Invoke(currentInteractable.GetInteractPrompt());
                    }
                    return; // Thoát sớm nếu vẫn đang nhìn đối tượng đó
                }
            }

            // Nếu không trúng gì hoặc trúng vật thể không thể tương tác
            if (currentInteractable != null)
            {
                currentInteractable = null;
                // Phát sự kiện mất đối tượng để UI ẩn đi thông báo gợi ý
                OnInteractableLost?.Invoke();
            }
        }

        private void TryInteract()
        {
            if (currentInteractable != null)
            {
                currentInteractable.Interact();
            }
        }
    }
}
