using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

namespace RungTramTraSu
{
    public class Phase2Manager : MonoBehaviour
    {
        public static Phase2Manager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Transform boat;
        [SerializeField] private Transform player;
        [SerializeField] private TextMeshProUGUI objectiveText;
        [SerializeField] private PhotoCamera photoCamera;
        [SerializeField] private Transform sunRayTarget;
        [SerializeField] private Transform storkTarget;
        [SerializeField] private GameObject storksFlock;

        [Header("Movement Settings")]
        [SerializeField] private float boatSpeed = 3.5f;
        [SerializeField] private float rotationSpeed = 3.0f;

        private List<Vector3> waypoints = new List<Vector3>();
        private int currentWaypointIndex = 0;
        private bool isTravelling = true;
        private bool photoCaptured = false;
        private bool event1Triggered = false;
        private bool event2Triggered = false;

        private bool isAtCheckpoint = false;
        private int birdsCapturedAtCurrentCheckpoint = 0;
        private List<GameObject> activeBirds = new List<GameObject>();
        private int currentCheckpoint = 0; // 0, 1, 2, 3
        private Coroutine flightCoroutine;
        private bool checkpoint1Triggered = false;
        private bool checkpoint2Triggered = false;
        private bool checkpoint3Triggered = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // Generate winding waypoints based on the canal curve formula
            float zStart = -60f;
            float zEnd = 58f;
            float step = 2.0f;
            for (float z = zStart; z <= zEnd; z += step)
            {
                float x = 25f + Mathf.Sin(z * 0.08f) * 5f;
                // Height of boat sits on water (Y = -0.82f)
                waypoints.Add(new Vector3(x, -0.82f, z));
            }

            // Put player on the boat
            if (player != null && boat != null)
            {
                player.SetParent(boat);
                
                // Bù trừ tỷ lệ scale của thuyền để player không bị phóng to và bay lên trời
                Vector3 boatScale = boat.localScale;
                player.localScale = new Vector3(1f / boatScale.x, 1f / boatScale.y, 1f / boatScale.z);
                player.localPosition = new Vector3(0f, 0.3f / boatScale.y, -1.0f / boatScale.z);
                player.localRotation = Quaternion.identity;
                
                var controller = player.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.SetFrozen(false);
                    controller.SetMovementLocked(true);
                }

                // Tắt CharacterController để player di chuyển theo Parent (thuyền) một cách chính xác
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
            }

            if (storksFlock != null) storksFlock.SetActive(false);

            UpdateObjectiveText("Nhìn ngắm phong cảnh. Ông Ngoại đang chèo xuồng đưa bạn đi...");
            StartCoroutine(GrandpaIntroDialogue());
        }

        private IEnumerator GrandpaIntroDialogue()
        {
            yield return new WaitForSeconds(3f);
            string[] intro = new string[] {
                "Con thấy cảnh quan sông nước miền Tây mình rộng lớn không?",
                "Nước nổi lên là bèo tấm phủ xanh um hết trơn hà, nhìn giống như một thảm lụa vậy đó con.",
                "Hai bên bờ sông tràm mọc san sát nhau, che mát cả dòng kênh. Gió thổi bập bùng nghe sướng tai lạ lùng."
            };
            DialogueManager.Instance.ShowDialogue("Ông Ngoại", intro);
        }

        private void Update()
        {
            if (isTravelling)
            {
                MoveBoat();
                CheckEvents();
            }

            // Kiểm tra ngắm bắn chim khi đang dừng ở Checkpoint
            if (isAtCheckpoint && Mouse.current.leftButton.wasPressedThisFrame && photoCamera != null)
            {
                if (photoCamera.IsZooming)
                {
                    CheckBirdCapture();
                }
            }
        }

        private void MoveBoat()
        {
            if (waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count || boat == null)
            {
                // Reached the end wood pier!
                ReachEnd();
                return;
            }

            Vector3 targetPos = waypoints[currentWaypointIndex];
            boat.position = Vector3.MoveTowards(boat.position, targetPos, boatSpeed * Time.deltaTime);

            // Rotate smoothly towards the waypoint
            Vector3 direction = (targetPos - boat.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                boat.rotation = Quaternion.Slerp(boat.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            if (Vector3.Distance(boat.position, targetPos) < 0.5f)
            {
                currentWaypointIndex++;
            }
        }

        private void CheckEvents()
        {
            float z = boat.position.z;

            // Checkpoint 1: Z = -20f (Tốc độ Chậm)
            if (!checkpoint1Triggered && z >= -20f && z < -10f)
            {
                checkpoint1Triggered = true;
                TriggerCheckpoint(1, 1.8f, "Phase2_Ch1", "Nhấn chuột phải ngắm, trái chụp 3 con chim bay (Tốc độ CHẬM).");
            }

            // Checkpoint 2: Z = 15f (Tốc độ Vừa)
            if (!checkpoint2Triggered && z >= 15f && z < 25f)
            {
                checkpoint2Triggered = true;
                TriggerCheckpoint(2, 4.0f, "Phase2_Ch2", "Nhấn chuột phải ngắm, trái chụp 3 con chim bay (Tốc độ VỪA).");
            }

            // Checkpoint 3: Z = 40f (Tốc độ Nhanh)
            if (!checkpoint3Triggered && z >= 40f && z < 48f)
            {
                checkpoint3Triggered = true;
                TriggerCheckpoint(3, 7.5f, "Phase2_Ch3", "Nhấn chuột phải ngắm, trái chụp 3 con chim bay (Tốc độ NHANH).");
            }
        }

        private void TriggerCheckpoint(int number, float birdSpeed, string category, string instructionText)
        {
            isTravelling = false;
            isAtCheckpoint = true;
            currentCheckpoint = number;
            birdsCapturedAtCurrentCheckpoint = 0;

            if (photoCamera != null)
            {
                photoCamera.UnlockCamera();
                photoCamera.SetPhotoCategory(category);
            }

            UpdateObjectiveText($"Checkpoint {number}: {instructionText} (0/3)");

            string speedText = number == 1 ? "từ từ thong thả" : (number == 2 ? "bay hơi nhanh hơn chút" : "bay rất nhanh lướt qua");
            DialogueManager.Instance.ShowDialogue("Ông Ngoại", new string[] {
                $"Tới Checkpoint {number} rồi nè con. Chim sáp sửa bay ngang qua đó con.",
                $"Đợt này chim sẽ bay {speedText}. Con lấy máy ảnh ra sẵn đi, ngắm sẵn rồi chụp nghe!"
            });

            // Sinh đàn chim và bắt đầu bay
            SpawnBirdFlock(boat.position.z);
            if (flightCoroutine != null) StopCoroutine(flightCoroutine);
            flightCoroutine = StartCoroutine(FlightRoutine(birdSpeed, boat.position.z));
        }

        private void SpawnBirdFlock(float zCenter)
        {
            ClearActiveBirds();

            for (int i = 0; i < 6; i++)
            {
                GameObject bird = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bird.name = "Stork_Bird_" + i;
                bird.transform.position = new Vector3(8f + Random.Range(-2f, 2f), 8f + Random.Range(-1.5f, 1.5f), zCenter + 15f + Random.Range(-2f, 2f));
                bird.transform.localScale = new Vector3(0.7f, 0.15f, 0.5f);
                
                Material birdMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                birdMat.color = Color.white;
                bird.GetComponent<Renderer>().sharedMaterial = birdMat;
                
                // Set tag and remove collider to avoid collision but keep detectable
                bird.tag = "Interactable";
                DestroyImmediate(bird.GetComponent<Collider>());
                
                // Add simple sphere collider for viewport checks if needed, but not required
                activeBirds.Add(bird);
            }
        }

        private void ClearActiveBirds()
        {
            foreach (var bird in activeBirds)
            {
                if (bird != null) Destroy(bird);
            }
            activeBirds.Clear();
        }

        private IEnumerator FlightRoutine(float speed, float zCenter)
        {
            while (isAtCheckpoint)
            {
                float t = 0f;
                float duration = 28f / speed;

                // Reset positions to start (left side of canal)
                for (int i = 0; i < activeBirds.Count; i++)
                {
                    if (activeBirds[i] != null)
                    {
                        activeBirds[i].transform.position = new Vector3(10f - i * 0.8f, 7f + Mathf.PingPong(i, 2f), zCenter + 16f + Random.Range(-1f, 1f));
                        activeBirds[i].SetActive(true);
                    }
                }

                // Fly left-to-right crossing the canal in front of the boat
                while (t < duration && isAtCheckpoint)
                {
                    t += Time.deltaTime;
                    float progress = t / duration;

                    for (int i = 0; i < activeBirds.Count; i++)
                    {
                        if (activeBirds[i] != null)
                        {
                            float curX = Mathf.Lerp(10f - i * 0.8f, 38f, progress);
                            float curY = 7f + Mathf.PingPong(i + Time.time, 2.5f);
                            float curZ = zCenter + 16f - progress * 4f;
                            activeBirds[i].transform.position = new Vector3(curX, curY, curZ);
                        }
                    }
                    yield return null;
                }

                yield return new WaitForSeconds(0.8f); // Dừng ngắn trước khi lặp lại bay tiếp
            }
        }

        private void CheckBirdCapture()
        {
            if (activeBirds.Count == 0) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            int hits = 0;
            List<GameObject> capturedThisFrame = new List<GameObject>();

            foreach (var bird in activeBirds)
            {
                if (bird == null) continue;
                Vector3 vp = cam.WorldToViewportPoint(bird.transform.position);
                
                // Hỗ trợ chụp ở khu vực trung tâm ống ngắm (Viewport [0.3, 0.7])
                if (vp.z > 0 && vp.x >= 0.28f && vp.x <= 0.72f && vp.y >= 0.28f && vp.y <= 0.72f)
                {
                    hits++;
                    capturedThisFrame.Add(bird);
                }
            }

            if (hits > 0)
            {
                birdsCapturedAtCurrentCheckpoint += hits;
                foreach (var b in capturedThisFrame)
                {
                    activeBirds.Remove(b);
                    Destroy(b);
                }

                UpdateObjectiveText($"Checkpoint {currentCheckpoint}: Chụp ảnh đàn chim ({birdsCapturedAtCurrentCheckpoint}/3)");

                if (birdsCapturedAtCurrentCheckpoint >= 3)
                {
                    ClearCheckpoint();
                }
            }
        }

        private void ClearCheckpoint()
        {
            isAtCheckpoint = false;
            ClearActiveBirds();

            if (flightCoroutine != null) StopCoroutine(flightCoroutine);

            DialogueManager.Instance.ShowDialogue("Ông Ngoại", new string[] {
                "Ừa giỏi quá con ơi! Chụp dính rồi kìa.",
                "Được 3 tấm hình đẹp rồi đó. Ông cháu mình nổ máy đi tiếp nghen."
            }, () => {
                isTravelling = true;
                UpdateObjectiveText("Nhìn ngắm phong cảnh. Ông Ngoại đang chèo xuồng đưa bạn đi...");
            });
        }

        public void OnPhotoQuestCompleted()
        {
            // Handled internally in CheckBirdCapture
        }

        private void ReachEnd()
        {
            isTravelling = false;
            UpdateObjectiveText("Xuồng cập bến gỗ lõi rừng. Chuẩn bị lên bờ...");
            
            if (player != null)
            {
                player.SetParent(null);
                player.localScale = Vector3.one; // Khôi phục lại tỷ lệ kích thước chuẩn 1x1x1 cho người chơi
                
                var controller = player.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.SetMovementLocked(false);
                }

                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = true;
            }

            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.StartFadeOut(2.5f, () => {
                    SceneManager.LoadScene("Phase3_BambooBridge");
                });
            }
            else
            {
                SceneManager.LoadScene("Phase3_BambooBridge");
            }
        }

        private void UpdateObjectiveText(string text)
        {
            if (objectiveText != null) objectiveText.text = text;
        }
    }
}
