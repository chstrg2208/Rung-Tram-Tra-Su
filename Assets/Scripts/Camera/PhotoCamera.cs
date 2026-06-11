using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace RungTramTraSu
{
    public class PhotoCamera : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float normalFOV = 60f;
        [SerializeField] private float zoomFOV = 30f;
        [SerializeField] private float zoomSpeed = 8f;

        [Header("UI Canvas References")]
        [SerializeField] private GameObject viewfinderCanvas; // UI Ống ngắm (vạch kẻ)
        [SerializeField] private Image flashImage;             // UI Đèn flash (nháy trắng)
        [SerializeField] private float flashDuration = 0.2f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip shutterSound;

        [Header("Quest Validation")]
        [SerializeField] private Transform questTarget;        // Đối tượng cần chụp (Cây xoài/Con mèo)
        [SerializeField] private LayerMask occlusionLayers;     // Các layer cản tầm nhìn

        private bool hasCamera = false;
        private bool isZooming = false;
        private float targetFOV;
        private bool isTakingPhoto = false;
        private string currentPhotoCategory = "General";

        public bool HasCamera => hasCamera;
        public bool IsZooming => isZooming;

        private void Awake()
        {
            if (playerCamera == null) playerCamera = Camera.main;
            targetFOV = normalFOV;
            
            // Tự động mở khóa camera ở các Phase sau Phase 2 (Phase 3, 4, 5)
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName.Contains("Phase3") || sceneName.Contains("Phase4") || sceneName.Contains("Phase5"))
            {
                hasCamera = true;
            }
            else
            {
                hasCamera = false;
            }
            
            if (viewfinderCanvas != null) viewfinderCanvas.SetActive(false);
            if (flashImage != null)
            {
                flashImage.gameObject.SetActive(false);
                // Thiết lập màu trắng đục nhưng trong suốt
                flashImage.color = new Color(1, 1, 1, 0);
            }
        }

        private void Update()
        {
            if (!hasCamera || isTakingPhoto) return;

            HandleZoom();
            UpdateDynamicCategory();
            HandleCapture();
        }

        private void HandleZoom()
        {
            // Kiểm tra giữ chuột phải
            if (Mouse.current.rightButton.isPressed)
            {
                isZooming = true;
                targetFOV = zoomFOV;
                if (viewfinderCanvas != null) viewfinderCanvas.SetActive(true);
            }
            else
            {
                isZooming = false;
                targetFOV = normalFOV;
                if (viewfinderCanvas != null) viewfinderCanvas.SetActive(false);
            }

            // Lerp FOV mượt mà
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
            }
        }

        private void HandleCapture()
        {
            // Nhấn chuột trái để chụp ảnh khi đang ngắm (zoom)
            if (isZooming && Mouse.current.leftButton.wasPressedThisFrame)
            {
                StartCoroutine(TakePhotoRoutine());
            }
        }

        private IEnumerator TakePhotoRoutine()
        {
            isTakingPhoto = true;

            // 1. Phát âm thanh chụp
            if (audioSource != null && shutterSound != null)
            {
                audioSource.PlayOneShot(shutterSound);
            }

            // 2. Chớp đèn Flash
            if (flashImage != null)
            {
                flashImage.gameObject.SetActive(true);
                // Fade-in trắng xóa nhanh
                float elapsed = 0;
                while (elapsed < flashDuration * 0.3f)
                {
                    elapsed += Time.deltaTime;
                    flashImage.color = new Color(1, 1, 1, Mathf.Lerp(0, 1, elapsed / (flashDuration * 0.3f)));
                    yield return null;
                }
            }

            // 3. Chụp màn hình (Chụp sạch - ẩn UI)
            if (viewfinderCanvas != null) viewfinderCanvas.SetActive(false);
            
            // Chờ kết thúc frame để chụp chính xác render camera
            yield return new WaitForEndOfFrame();

            int width = Screen.width;
            int height = Screen.height;
            Texture2D capturedTex = new Texture2D(width, height, TextureFormat.RGB24, false);
            capturedTex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            capturedTex.Apply();

            // Khôi phục lại UI
            if (isZooming && viewfinderCanvas != null) viewfinderCanvas.SetActive(true);

            // Lưu ảnh vào PersistentGameManager hoặc Phase1Manager
            if (PersistentGameManager.Instance != null)
            {
                PersistentGameManager.Instance.SavePhoto(currentPhotoCategory, capturedTex);
            }
            if (Phase1Manager.Instance != null)
            {
                Phase1Manager.Instance.SavePhoto(capturedTex);
            }

            // 4. Kiểm tra xem mục tiêu nhiệm vụ có trong khung hình không
            ValidatePhotoContent();

            // 5. Fade-out đèn Flash
            if (flashImage != null)
            {
                float elapsed = 0;
                while (elapsed < flashDuration * 0.7f)
                {
                    elapsed += Time.deltaTime;
                    flashImage.color = new Color(1, 1, 1, Mathf.Lerp(1, 0, elapsed / (flashDuration * 0.7f)));
                    yield return null;
                }
                flashImage.gameObject.SetActive(false);
            }

            isTakingPhoto = false;
        }

        private void ValidatePhotoContent()
        {
            if (questTarget == null) return;

            // Determine the actual visual center of the target (using its Collider bounds center if available)
            Vector3 targetPosition = questTarget.position;
            Collider targetCollider = questTarget.GetComponent<Collider>();
            if (targetCollider == null)
            {
                targetCollider = questTarget.GetComponentInChildren<Collider>();
            }
            if (targetCollider != null)
            {
                targetPosition = targetCollider.bounds.center;
            }

            // Chuyển vị trí mục tiêu từ tọa độ World sang Viewport của Camera
            Vector3 viewportPoint = playerCamera.WorldToViewportPoint(targetPosition);

            // Kiểm tra xem mục tiêu:
            // - Có nằm ở phía trước camera hay không (z > 0)
            // - Có nằm trong phạm vi hiển thị màn hình hay không (x, y từ 0.0 đến 1.0)
            // - Để ảnh đẹp, yêu cầu mục tiêu nằm ở vùng trung tâm (x, y từ 0.2 đến 0.8)
            bool isVisible = viewportPoint.z > 0 && 
                             viewportPoint.x >= 0.2f && viewportPoint.x <= 0.8f && 
                             viewportPoint.y >= 0.2f && viewportPoint.y <= 0.8f;

            if (isVisible)
            {
                // Kiểm tra xem mục tiêu có bị vật cản (như tường, nhà) che mất không
                RaycastHit hit;
                Vector3 directionToTarget = targetPosition - playerCamera.transform.position;
                if (Physics.Raycast(playerCamera.transform.position, directionToTarget, out hit, directionToTarget.magnitude + 1f, occlusionLayers))
                {
                    // Nếu va chạm trúng vật khác trước mục tiêu
                    if (hit.transform != questTarget && !hit.transform.IsChildOf(questTarget))
                    {
                        Debug.Log("Mục tiêu bị che mất bởi: " + hit.collider.name);
                        return; // Bị che khuất
                    }
                }

                // Chụp ảnh thành công! Báo về Phase1Manager
                Debug.Log("Chụp ảnh mục tiêu thành công!");
                if (Phase1Manager.Instance != null) Phase1Manager.Instance.OnPhotoQuestCompleted();
                if (Phase2Manager.Instance != null) Phase2Manager.Instance.OnPhotoQuestCompleted();
                if (Phase3Manager.Instance != null) Phase3Manager.Instance.OnPhotoQuestCompleted();
                if (Phase4Manager.Instance != null) Phase4Manager.Instance.OnPhotoQuestCompleted();
                if (Phase5Manager.Instance != null) Phase5Manager.Instance.OnPhotoQuestCompleted();
            }
            else
            {
                Debug.Log("Mục tiêu nằm ngoài tầm ngắm trung tâm. Tọa độ Viewport: " + viewportPoint);
            }
        }

        /// <summary>
        /// Kích hoạt máy ảnh khi nhận được từ Ông Ngoại
        /// </summary>
        public void UnlockCamera()
        {
            hasCamera = true;
        }

        /// <summary>
        /// Gán mục tiêu cần chụp cho nhiệm vụ hiện tại
        /// </summary>
        public void SetQuestTarget(Transform target)
        {
            questTarget = target;
        }

        public void SetPhotoCategory(string category)
        {
            currentPhotoCategory = category;
        }

        private void UpdateDynamicCategory()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName.Contains("Phase4"))
            {
                AnimalAI[] animals = FindObjectsByType<AnimalAI>(FindObjectsSortMode.None);
                AnimalAI bestAnimal = null;
                float bestDist = float.MaxValue;
                foreach (var animal in animals)
                {
                    if (animal == null || animal.HasFled) continue;
                    Vector3 vp = playerCamera.WorldToViewportPoint(animal.transform.position);
                    if (vp.z > 0 && vp.x >= 0.1f && vp.x <= 0.9f && vp.y >= 0.1f && vp.y <= 0.9f)
                    {
                        float distToCenter = Vector2.Distance(new Vector2(vp.x, vp.y), new Vector2(0.5f, 0.5f));
                        if (distToCenter < bestDist)
                        {
                            bestDist = distToCenter;
                            bestAnimal = animal;
                        }
                    }
                }
                if (bestAnimal != null)
                {
                    currentPhotoCategory = "Phase4_" + bestAnimal.Type.ToString();
                }
            }
            else if (sceneName.Contains("Phase5"))
            {
                currentPhotoCategory = "Phase5_Sunset";
            }
            else if (sceneName.Contains("Phase1"))
            {
                currentPhotoCategory = "Phase1_Mango";
            }
        }
    }
}
