using UnityEngine;

namespace RungTramTraSu
{
    public class NPCGrandpa : MonoBehaviour, IInteractable
    {
        [Header("Dialogue Sets")]
        [SerializeField]
        private string[] introDialogue = new string[]
        {
            "Dậy rồi đó hả con? Đêm qua ngủ ngon giấc không?",
            "Sáng nay trời mát mẻ dữ lắm. Ông ngoại chuẩn bị xuồng sẵn rồi, lát hai ông cháu mình đi chơi rừng tràm Trà Sư nghe.",
            "Mà nè, ông ngoại có cái này cho con...",
            "Đây là chiếc máy ảnh phim cũ của ông hồi xưa. Con giữ lấy đi.",
            "Đi chơi rừng đẹp lắm, con cầm máy theo chụp lại làm kỷ niệm.",
            "Nè, con thử cầm lên, bấm chuột phải để ngắm ngốc rồi chụp thử cây xoài to đằng kia cho ông xem coi có hoạt động tốt không nha!"
        };

        [SerializeField]
        private string[] waitingForPhotoDialogue = new string[]
        {
            "Con cứ từ từ thử xem. Nhấn chuột phải để ngắm (Zoom) và click chuột trái để chụp cây xoài to đằng kia kìa.",
            "Thử chụp một tấm đẹp đẹp cho ông coi thử coi."
        };

        [SerializeField]
        private string[] photoTakenDialogue = new string[]
        {
            "Đâu, đưa ông coi tấm hình thử... Ừa! Đẹp lắm con, máy ảnh cũ vậy chứ chụp vẫn bén ngót hà.",
            "Thôi, chuẩn bị đồ đạc rồi hai ông cháu mình xuống xuồng đi con. Đứng đây nắng lên nóng lắm.",
            "Bước xuống chiếc xuồng dưới bến nước kia kìa, ông chèo đưa đi."
        };

        [SerializeField]
        private string[] finalWaitingDialogue = new string[]
        {
            "Mau xuống xuồng đi con, ông chèo đưa con đi sâu vào rừng tràm mát lắm."
        };

        public string GetInteractPrompt()
        {
            return "Nói chuyện với Ông Ngoại";
        }

        public void Interact()
        {
            Phase1Manager manager = Phase1Manager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("Không tìm thấy Phase1Manager. Không thể kích hoạt thoại!");
                return;
            }

            switch (manager.CurrentState)
            {
                case Phase1Manager.Phase1State.Intro:
                    // Bắt đầu hội thoại lần đầu, sau đó kích hoạt nhận máy ảnh
                    DialogueManager.Instance.ShowDialogue("Ông Ngoại", introDialogue, () => {
                        manager.GiveCameraToPlayer();
                    });
                    break;

                case Phase1Manager.Phase1State.TakingPhoto:
                    // Đang chờ chụp ảnh
                    DialogueManager.Instance.ShowDialogue("Ông Ngoại", waitingForPhotoDialogue);
                    break;

                case Phase1Manager.Phase1State.PhotoTaken:
                    // Đã chụp ảnh xong, nói chuyện để chuyển sang bước lên xuồng
                    DialogueManager.Instance.ShowDialogue("Ông Ngoại", photoTakenDialogue, () => {
                        manager.SetReadyForBoat();
                    });
                    break;

                case Phase1Manager.Phase1State.TalkedAgain:
                case Phase1Manager.Phase1State.OnBoat:
                    // Đã nhắc xuống xuồng
                    DialogueManager.Instance.ShowDialogue("Ông Ngoại", finalWaitingDialogue);
                    break;
            }
        }

        private bool isWalkingToBoat = false;
        private float walkSpeed = 1.8f;
        private int currentWaypointIndex = 0;
        private Vector3[] walkWaypoints = new Vector3[0];

        public void WalkToBoat()
        {
            isWalkingToBoat = true;
            currentWaypointIndex = 0;

            // Build waypoints dynamically from the generated stepping stones
            GameObject stonesContainer = GameObject.Find("SteppingStonesPath");
            System.Collections.Generic.List<Vector3> pointsList = new System.Collections.Generic.List<Vector3>();

            if (stonesContainer != null)
            {
                foreach (Transform child in stonesContainer.transform)
                {
                    pointsList.Add(child.position);
                }
                // Sort stones by X coordinate so Grandpa walks from house to pier
                pointsList.Sort((a, b) => a.x.CompareTo(b.x));
            }

            // Add intermediate deck entrance waypoint to snap Grandpa up to deck level
            pointsList.Add(new Vector3(13.0f, 1.03f, 8.0f));

            // Add the pier target waypoint at the end
            pointsList.Add(new Vector3(17.5f, 1.03f, 8.0f));

            walkWaypoints = pointsList.ToArray();
        }

        private void Update()
        {
            if (isWalkingToBoat && walkWaypoints.Length > 0)
            {
                Vector3 targetPos = walkWaypoints[currentWaypointIndex];
                float step = walkSpeed * Time.deltaTime;
                Vector3 targetDir = targetPos - transform.position;
                targetDir.y = 0; // Rotate horizontal only

                if (Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(targetPos.x, 0f, targetPos.z)) > 0.2f)
                {
                    if (targetDir.magnitude > 0.05f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDir), 8.0f * Time.deltaTime);
                    }
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, step);
                }
                else
                {
                    // Move to next waypoint or finish
                    if (currentWaypointIndex < walkWaypoints.Length - 1)
                    {
                        currentWaypointIndex++;
                    }
                    else
                    {
                        isWalkingToBoat = false;
                        // Parent to boat and sit down
                        GameObject boatObj = GameObject.Find("Sampan Boat");
                        if (boatObj != null)
                        {
                            transform.SetParent(boatObj.transform, true);
                            transform.localPosition = new Vector3(0f, 0.06f, 0.3f); // puts him at world (21.0, -0.7, 8.0)
                            transform.localRotation = Quaternion.identity;
                        }
                    }
                }
            }
        }
    }
}
