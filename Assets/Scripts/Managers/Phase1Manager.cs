using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

namespace RungTramTraSu
{
    public class Phase1Manager : MonoBehaviour
    {
        public static Phase1Manager Instance { get; private set; }

        public enum Phase1State
        {
            Intro,             // Đi tới gặp Ông Ngoại
            TakingPhoto,       // Đang chụp ảnh Cây Xoài
            PhotoTaken,        // Chụp ảnh thành công, nói chuyện lại với Ông
            TalkedAgain,       // Ông giục xuống xuồng
            OnBoat             // Đã bước xuống xuồng, đang chuyển màn
        }

        [Header("State")]
        [SerializeField] private Phase1State currentState = Phase1State.Intro;

        [Header("References")]
        [SerializeField] private PhotoCamera photoCamera;          // Cơ chế máy ảnh trên camera
        [SerializeField] private GameObject cameraHandModel;       // Model máy ảnh trên tay người chơi
        [SerializeField] private Transform mangoTreeTarget;        // Cây xoài cần chụp
        [SerializeField] private GameObject boatTriggerZone;      // Vùng trigger trên xuồng gỗ
        
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI objectiveText;    // Giao diện hiển thị mục tiêu

        [Header("Transition Settings")]
        [SerializeField] private string nextSceneName = "Phase2_Canal";

        // Biến lưu trữ tấm ảnh chụp tạm thời
        private Texture2D capturedPhoto;

        public Phase1State CurrentState => currentState;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            currentState = Phase1State.Intro;

            // Khởi đầu ẩn model máy ảnh trên tay người chơi và tắt vùng trigger lên xuồng
            if (cameraHandModel != null) cameraHandModel.SetActive(false);
            if (boatTriggerZone != null) boatTriggerZone.SetActive(false);

            UpdateObjectiveText("Mục tiêu: Dùng W, A, S, D để đi tới bến nước và nói chuyện với Ông Ngoại (Nhấn E).");
        }

        /// <summary>
        /// Được gọi từ NPCGrandpa sau khi hội thoại đầu kết thúc
        /// </summary>
        public void GiveCameraToPlayer()
        {
            currentState = Phase1State.TakingPhoto;
            
            // Hiện máy ảnh trên tay người chơi và mở khóa cơ chế ngắm/chụp
            if (cameraHandModel != null) cameraHandModel.SetActive(true);
            if (photoCamera != null)
            {
                photoCamera.UnlockCamera();
                photoCamera.SetQuestTarget(mangoTreeTarget);
            }

            UpdateObjectiveText("Mục tiêu: Giữ Chuột Phải để ngắm, nhấn Chuột Trái để chụp ảnh Cây Xoài to trong vườn.");
        }

        /// <summary>
        /// Được gọi từ PhotoCamera khi chụp thành công mục tiêu
        /// </summary>
        public void OnPhotoQuestCompleted()
        {
            if (currentState != Phase1State.TakingPhoto) return;

            currentState = Phase1State.PhotoTaken;
            UpdateObjectiveText("Mục tiêu: Đưa ảnh cho Ông Ngoại xem (Đi tới nói chuyện với ông bằng phím E).");
        }

        /// <summary>
        /// Được gọi từ NPCGrandpa sau khi khen ảnh chụp xong
        /// </summary>
        public void SetReadyForBoat()
        {
            currentState = Phase1State.TalkedAgain;

            // Kích hoạt collider trigger trên xuồng gỗ để người chơi có thể bước xuống
            if (boatTriggerZone != null) boatTriggerZone.SetActive(true);

            UpdateObjectiveText("Mục tiêu: Bước xuống chiếc xuồng gỗ đậu ở bến nước để bắt đầu khởi hành.");
        }

        /// <summary>
        /// Được gọi khi người chơi đi vào Collider của xuồng gỗ
        /// </summary>
        public void BoardBoat()
        {
            if (currentState != Phase1State.TalkedAgain) return;

            currentState = Phase1State.OnBoat;
            UpdateObjectiveText("Đang chuẩn bị xuất phát...");

            // Khóa di chuyển người chơi
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) player.SetFrozen(true);

            // Chuyển màn hình tối dần và load màn chơi Phase 2
            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.StartFadeOut(2.0f, () => {
                    // Load màn chơi tiếp theo (Scene Phase 2)
                    Debug.Log("Chuyển cảnh sang: " + nextSceneName);
                    // Ở đây nếu Scene chưa được Add vào Build Settings sẽ log lỗi, 
                    // nhưng code đã sẵn sàng để chuyển cảnh khi Scene Phase 2 được xây dựng.
                    SceneManager.LoadScene(nextSceneName);
                });
            }
            else
            {
                // Nếu không có Fader thì load scene luôn
                SceneManager.LoadScene(nextSceneName);
            }
        }

        /// <summary>
        /// Lưu ảnh chụp màn hình từ PhotoCamera
        /// </summary>
        public void SavePhoto(Texture2D tex)
        {
            capturedPhoto = tex;
            Debug.Log("GameManager đã lưu tấm ảnh chụp từ máy ảnh (Kích thước: " + tex.width + "x" + tex.height + ")");
        }

        public Texture2D GetCapturedPhoto()
        {
            return capturedPhoto;
        }

        private void UpdateObjectiveText(string text)
        {
            if (objectiveText != null)
            {
                objectiveText.text = text;
            }
        }
    }
}
