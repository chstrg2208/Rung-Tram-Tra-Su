using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

namespace RungTramTraSu
{
    public class Phase3Manager : MonoBehaviour
    {
        public static Phase3Manager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private GrandpaAI grandpa;
        [SerializeField] private Transform player;
        [SerializeField] private TextMeshProUGUI objectiveText;
        [SerializeField] private PhotoCamera photoCamera;
        [SerializeField] private Transform rootTarget;
        [SerializeField] private GameObject bridgeEndTrigger;

        private bool storyTriggered = false;
        private bool rootPhotographed = false;
        private bool transitionTriggered = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // Unlock player movement
            if (player != null)
            {
                var controller = player.GetComponent<PlayerController>();
                if (controller != null) controller.SetFrozen(false);
            }

            UpdateObjectiveText("Mục tiêu: Đi dọc theo cầu tre, bám sát Ông Ngoại để khám phá rừng sâu.");
            StartCoroutine(IntroTalk());
        }

        private IEnumerator IntroTalk()
        {
            yield return new WaitForSeconds(2f);
            DialogueManager.Instance.ShowDialogue("Ông Ngoại", new string[] {
                "Đi trên cầu tre con nhớ đi cẩn thận nha, nhìn xuống chân coi chừng trượt chân ngã.",
                "Rừng sâu ở đây yên tĩnh lắm con, ánh sáng mặt trời chen qua từng nhánh cây tạo tia nắng lung linh ghê chưa!"
            });
        }

        private void Update()
        {
            if (player == null || transitionTriggered) return;

            float playerZ = player.position.z;

            // Trigger breathing root story at Z = -12m
            if (!storyTriggered && playerZ >= -12f && playerZ < 5f)
            {
                storyTriggered = true;
                StartCoroutine(BreathingRootStoryRoutine());
            }

            // Check bridge completion
            if (grandpa != null && grandpa.ReachedEnd() && Vector3.Distance(player.position, grandpa.transform.position) < 3.5f)
            {
                TriggerSceneTransition();
            }
        }

        private IEnumerator BreathingRootStoryRoutine()
        {
            // Grandpa stops and faces player (GrandpaAI checks distance, we just trigger Dialogue)
            string[] dialogue = new string[] {
                "Con dừng lại dòm mấy cái rễ cây nhô lên mặt nước xung quanh chân cầu tre nè con.",
                "Mấy loài cây khác ngập nước lâu ngày là thối rễ chết ngắc hà, còn cây tràm này ngập nước bao lâu cũng trơ trơ ra đó.",
                "Là nhờ mấy cái rễ thở nhô ngược lên trời như chông này nè! Tụi nó hít oxy nuôi cây sống đó con. Kỳ diệu thiệt chớ!",
                "Con lấy máy ảnh chụp một tấm rễ thở của cây tràm cho ông ngoại xem đi!"
            };

            bool dialogueDone = false;
            DialogueManager.Instance.ShowDialogue("Ông Ngoại", dialogue, () => {
                dialogueDone = true;
            });

            yield return new WaitUntil(() => dialogueDone);

            // Unlock photo camera targeting roots
            if (photoCamera != null)
            {
                photoCamera.UnlockCamera();
                photoCamera.SetQuestTarget(rootTarget);
            }

            UpdateObjectiveText("Mục tiêu: Ngắm và chụp ảnh cụm Rễ Thở của cây tràm bên sườn cầu tre.");
        }

        public void OnPhotoQuestCompleted()
        {
            if (rootPhotographed) return;
            rootPhotographed = true;

            UpdateObjectiveText("Chụp ảnh Rễ Tràm thành công! Tiếp tục bám sát ông ngoại.");
            DialogueManager.Instance.ShowDialogue("Ông Ngoại", new string[] {
                "Hình nét ghê con! Nhìn rễ tràm nhấp nhô mộc mạc y chang ngoài đời vậy đó.",
                "Thôi, hai ông cháu mình đi tiếp ra khu đầm lầy bảo tồn chim chóc nghen con."
            });
        }

        public void ShowGrandpaWarning(string warning)
        {
            // Temporary message in objective text without freezing player
            StartCoroutine(ShowTemporaryMessage(warning, 4f));
        }

        private IEnumerator ShowTemporaryMessage(string msg, float duration)
        {
            string oldObjective = objectiveText.text;
            objectiveText.text = "Ông Ngoại kêu: \"" + msg + "\"";
            objectiveText.color = Color.yellow;
            yield return new WaitForSeconds(duration);
            objectiveText.text = oldObjective;
            objectiveText.color = Color.white;
        }

        private void TriggerSceneTransition()
        {
            transitionTriggered = true;
            UpdateObjectiveText("Chuẩn bị chuyển sang Khu bảo tồn đầm lầy...");
            
            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.StartFadeOut(2f, () => {
                    SceneManager.LoadScene("Phase4_Sanctuary");
                });
            }
            else
            {
                SceneManager.LoadScene("Phase4_Sanctuary");
            }
        }

        private void UpdateObjectiveText(string text)
        {
            if (objectiveText != null) objectiveText.text = text;
        }
    }
}
