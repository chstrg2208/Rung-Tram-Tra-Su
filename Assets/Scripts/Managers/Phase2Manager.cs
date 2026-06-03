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

            // Event 1: Sunshine Ray through canopy (z around -20m)
            if (!event1Triggered && z >= -22f && z < 0f)
            {
                event1Triggered = true;
                if (photoCamera != null)
                {
                    photoCamera.UnlockCamera();
                    photoCamera.SetQuestTarget(sunRayTarget);
                }
                photoCaptured = false;
                UpdateObjectiveText("Nhiệm vụ: Chụp ảnh Vạt nắng vàng rực rỡ xuyên qua tán lá tràm phía trước.");
                DialogueManager.Instance.ShowDialogue("Ông Ngoại", new string[] {
                    "Ngước lên coi kìa con! Vạt nắng vàng xuyên qua kẽ lá nhìn đẹp quá trời đất kìa!",
                    "Con lấy máy ra bấm chụp một tấm kỷ niệm đi con!"
                });
            }

            // Event 2: Storks fly (z around 15m)
            if (!event2Triggered && z >= 15f && z < 35f)
            {
                event2Triggered = true;
                if (storksFlock != null) storksFlock.SetActive(true);
                // Start storks flight animation
                StartCoroutine(StorksFlightRoutine());
                
                if (photoCamera != null)
                {
                    photoCamera.UnlockCamera();
                    photoCamera.SetQuestTarget(storkTarget);
                }
                photoCaptured = false;
                UpdateObjectiveText("Nhiệm vụ: Nhanh tay chụp ảnh Đàn cò trắng đang bay lướt qua bầu trời kênh.");
                DialogueManager.Instance.ShowDialogue("Ông Ngoại", new string[] {
                    "Kìa! Đàn cò trắng kìa con! Tụi nó bay lướt ngang kênh đó. Chụp lẹ tay đi con!"
                });
            }
        }

        private IEnumerator StorksFlightRoutine()
        {
            float elapsed = 0f;
            float duration = 12f;
            Vector3 startPos = new Vector3(8f, 10f, 40f);
            Vector3 endPos = new Vector3(45f, 15f, 15f);
            if (storksFlock != null)
            {
                storksFlock.transform.position = startPos;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    storksFlock.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                    storksFlock.transform.Rotate(Vector3.up * 5f * Time.deltaTime);
                    yield return null;
                }
            }
        }

        public void OnPhotoQuestCompleted()
        {
            if (photoCaptured) return;
            photoCaptured = true;

            if (event2Triggered)
            {
                UpdateObjectiveText("Chụp ảnh Đàn cò trắng thành công!");
                DialogueManager.Instance.ShowDialogue("Ông Ngoại", new string[] {
                    "Bén quá con ơi! Đất lành chim đậu, đàn cò trắng về đông như vầy là rừng mình thanh bình lắm đó."
                });
            }
            else if (event1Triggered)
            {
                UpdateObjectiveText("Chụp ảnh Vạt nắng vàng thành công!");
                DialogueManager.Instance.ShowDialogue("Ông Ngoại", new string[] {
                    "Chụp được rồi hả con? Đẹp quá chừng! Ánh sáng này chiếu xiên xiên qua lá nhìn thơ mộng ghê."
                });
            }
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
