using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using System.IO;

namespace RungTramTraSu
{
    public class Phase1Setup : EditorWindow
    {
        [MenuItem("Rung Tram Tra Su/Setup Phase 1 Scene")]
        public static void CreatePhase1Scene()
        {
            // 0. Buộc Unity quét lại thư mục và import tất cả file .glb cùng Texture mới được sao chép
            AssetDatabase.Refresh();

            // 1. Tạo layer "Interactable" nếu chưa có
            CreateLayer("Interactable");

            // 2. Tạo Scene mới
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Xóa Main Camera mặc định để tự tạo Player Camera tối ưu hơn
            GameObject defaultCam = GameObject.FindWithTag("MainCamera");
            if (defaultCam != null) DestroyImmediate(defaultCam);

            // Tìm đèn Directional Light mặc định để xoay góc sáng và nhuộm nắng sớm vàng ấm áp
            GameObject dirLight = GameObject.Find("Directional Light");
            if (dirLight != null)
            {
                dirLight.transform.rotation = Quaternion.Euler(20, -55, 0); // Góc nắng chiếu xiên tạo bóng dài thơ mộng
                var lightComp = dirLight.GetComponent<Light>();
                if (lightComp != null)
                {
                    lightComp.color = new Color(1.0f, 0.95f, 0.82f); // Ánh nắng vàng nhạt buổi sớm
                    lightComp.intensity = 1.35f;                    // Tăng độ sáng nắng sớm
                    lightComp.shadows = LightShadows.Soft;           // Bóng đổ mềm mại
                    lightComp.shadowStrength = 0.85f;                // Độ đậm bóng đổ vừa phải, tự nhiên
                }
            }

            // --- THIẾT LẬP BỐ CỤC ĐỊA HÌNH SÔNG NƯỚC RỪNG TRÀM ---

            // Tải texture đất cỏ và bèo tấm
            Texture2D grassTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/grass_dirt_texture.png");
            Texture2D waterTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/duckweed_water_texture.png");

            // A1. Bờ Đất Trái (Sân nhà và hiên xuất phát) - Chạy từ X = -45 đến X = 15 (Dày 10m chống lún)
            GameObject groundLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            groundLeft.name = "GroundLeft_Bank";
            groundLeft.transform.position = new Vector3(-15f, -5.0f, 0f); // Mặt trên tại Y = 0f
            groundLeft.transform.localScale = new Vector3(60f, 10.0f, 80f);
            
            Material grassMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (grassTex != null)
            {
                grassMat.mainTexture = grassTex;
                grassMat.mainTextureScale = new Vector2(15f, 20f);
            }
            else
            {
                grassMat.color = new Color(0.15f, 0.32f, 0.18f);
            }
            // Đất cỏ nhám không phản bóng (Smoothness thấp)
            if (grassMat.HasProperty("_Smoothness")) grassMat.SetFloat("_Smoothness", 0.05f);
            else if (grassMat.HasProperty("_Glossiness")) grassMat.SetFloat("_Glossiness", 0.05f);
            groundLeft.GetComponent<Renderer>().sharedMaterial = grassMat;

            // A2. Bờ Đất Phải (Bờ đối diện) - Chạy từ X = 35 đến X = 55 (Dày 10m chống lún)
            GameObject groundRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            groundRight.name = "GroundRight_Bank";
            groundRight.transform.position = new Vector3(45f, -5.0f, 0f); // Mặt trên tại Y = 0f
            groundRight.transform.localScale = new Vector3(20f, 10.0f, 80f);
            groundRight.GetComponent<Renderer>().sharedMaterial = grassMat;

            // B. Con kênh bèo tấm ở chính giữa - Chạy từ X = 15 đến X = 35 (Sâu -1.0m)
            GameObject river = GameObject.CreatePrimitive(PrimitiveType.Plane);
            river.name = "RiverWater_Canal";
            river.transform.position = new Vector3(25f, -1.0f, 0f); // Mặt nước sông bèo ở Y = -1.0m
            river.transform.localScale = new Vector3(2f, 1f, 8f);   // Kích thước 20m x 80m
            DestroyImmediate(river.GetComponent<MeshCollider>());

            Material waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (waterTex != null)
            {
                waterMat.mainTexture = waterTex;
                waterMat.mainTextureScale = new Vector2(4f, 16f);
            }
            else
            {
                waterMat.color = new Color(0.08f, 0.22f, 0.18f);
            }
            // Mặt nước nhẵn lấp lánh phản chiếu mặt trời cực tốt
            if (waterMat.HasProperty("_Smoothness")) waterMat.SetFloat("_Smoothness", 0.85f);
            else if (waterMat.HasProperty("_Glossiness")) waterMat.SetFloat("_Glossiness", 0.85f);
            waterMat.SetFloat("_Metallic", 0.15f);
            river.GetComponent<Renderer>().sharedMaterial = waterMat;

            // B-Extra: Tạo Đáy sông vật lý dày hẳn 10m để người chơi nhảy xuống sông không bao giờ bị lọt void
            GameObject riverBed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            riverBed.name = "RiverBed_Collider";
            riverBed.transform.position = new Vector3(25f, -6.5f, 0f); // Mặt trên đáy sông vẫn ở Y = -1.5f (ngập nước 0.5m)
            riverBed.transform.localScale = new Vector3(20f, 10.0f, 80f);
            // Ẩn hiển thị hình khối đáy sông (chỉ lấy va chạm collider)
            DestroyImmediate(riverBed.GetComponent<MeshRenderer>());

            // C. Cầu Tàu Gỗ (Wooden Pier) bắc từ bờ trái (đất cao Y=0) nhô ra kênh sông
            GameObject pierContainer = new GameObject("WoodenPier");
            
            GameObject plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = "PierPlank";
            plank.transform.SetParent(pierContainer.transform);
            plank.transform.position = new Vector3(15f, 0.15f, 8f); // Bắc từ X = 13.25 ra X = 16.75
            plank.transform.localScale = new Vector3(3.5f, 0.08f, 1.3f);
            
            GameObject post1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post1.name = "PierPost_1";
            post1.transform.SetParent(pierContainer.transform);
            post1.transform.position = new Vector3(16.2f, -0.8f, 7.3f); // Cọc gỗ dài 2m cắm qua lòng sông bèo
            post1.transform.localScale = new Vector3(0.15f, 2.0f, 0.15f);

            GameObject post2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post2.name = "PierPost_2";
            post2.transform.SetParent(pierContainer.transform);
            post2.transform.position = new Vector3(16.2f, -0.8f, 8.7f);
            post2.transform.localScale = new Vector3(0.15f, 2.0f, 0.15f);

            Material woodMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            woodMat.color = new Color(0.32f, 0.2f, 0.11f);
            if (woodMat.HasProperty("_Smoothness")) woodMat.SetFloat("_Smoothness", 0.1f);
            plank.GetComponent<Renderer>().sharedMaterial = woodMat;
            post1.GetComponent<Renderer>().sharedMaterial = woodMat;
            post2.GetComponent<Renderer>().sharedMaterial = woodMat;


            // --- IMPORT VÀ SẮP ĐẶT MÔ HÌNH CHÍNH (KEY MODELS) ---

            // 1. Ngôi nhà sàn (Stilt House) nhô ra bờ sông bên sườn trái
            GameObject house = LoadAndInstantiate("Assets/Models/VietnameseHouse/thatched+stilt+house+3d+model.glb", "Stilt House", new Vector3(-5f, 3.8f, 0f), Quaternion.identity);
            if (house != null)
            {
                house.transform.localScale = new Vector3(18f, 18f, 18f);
                // Tự động gán MeshCollider cho các mesh con của ngôi nhà để người chơi có thể đi vào nhà, bước lên bậc thang
                AddMeshCollidersRecursively(house);
            }

            // 2. Ông Ngoại (Grandpa NPC) đứng ngay đầu bến cầu tàu gỗ (Y = 0.05f sát đất!)
            GameObject grandpa = LoadAndInstantiate("Assets/Models/VietnameseGrandpa/Meshy_AI_Old_Man_with_Open_Arm_biped/Meshy_AI_Old_Man_with_Open_Arm_biped_Character_output.glb", "Grandpa_NPC", new Vector3(13.0f, 0.05f, 8f), Quaternion.Euler(0, 90, 0));
            if (grandpa != null)
            {
                grandpa.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
                grandpa.layer = LayerMask.NameToLayer("Interactable");
                var col = grandpa.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0, 0.9f, 0);
                col.radius = 0.35f;
                col.height = 1.8f;
                grandpa.AddComponent<NPCGrandpa>();
            }

            // 3. Chiếc xuồng ba lá (Sampan Boat) neo sát mép cầu tàu
            // - Đặt Y = -0.82f để chìm nhẹ xuống mặt nước bèo tấm Y = -1.0f cho tự nhiên.
            // - Gán script WaterFloat để tự bập bênh bơi bập bùng hữu cơ.
            GameObject boat = LoadAndInstantiate("Assets/Models/VietnameseBoat/mô+hình+thuyền+sampan+gỗ+3d.glb", "Sampan Boat", new Vector3(17.2f, -0.82f, 8f), Quaternion.Euler(0f, 5f, 0f));
            GameObject boatTriggerZone = null;
            if (boat != null)
            {
                boat.transform.localScale = new Vector3(5f, 5f, 5f);
                
                // Thiết lập hệ thống va chạm vững chắc cho thuyền
                SetupPerfectBoatCollider(boat);

                // Thêm script bập bênh nước chân thật
                boat.AddComponent<WaterFloat>();
                
                // Vùng nhận diện người chơi bước lên xuồng để kích hoạt chuyển Phase
                boatTriggerZone = new GameObject("BoatTriggerZone");
                boatTriggerZone.transform.SetParent(boat.transform, false);
                boatTriggerZone.transform.localPosition = new Vector3(0, 0.3f, 0);
                
                var triggerCol = boatTriggerZone.AddComponent<BoxCollider>();
                triggerCol.isTrigger = true;
                triggerCol.size = new Vector3(1.2f, 0.8f, 3.0f);
                
                boatTriggerZone.AddComponent<BoatTrigger>();
            }

            // 4. Cây Xoài nhiệm vụ đặt ở góc vườn thoáng
            GameObject mangoTree = LoadAndInstantiate("Assets/Models/TeaTree/low-poly+tree+3d+model.glb", "Mango_Tree_Target", new Vector3(-3f, 3.5f, 14f), Quaternion.identity);
            if (mangoTree != null)
            {
                mangoTree.transform.localScale = new Vector3(12f, 12f, 12f);
                mangoTree.layer = LayerMask.NameToLayer("Interactable");
                
                // Thêm hiệu ứng đung đưa theo gió
                mangoTree.AddComponent<WindSway>();
                
                // Tạo một GameObject con để làm Trunk Collider với scale nghịch đảo để triệt tiêu scale 12 của cây xoài,
                // tránh tạo ra bức tường vô hình rộng 18m chặn đường đi của người chơi.
                GameObject trunkCol = new GameObject("TrunkCollider");
                trunkCol.transform.SetParent(mangoTree.transform, false);
                trunkCol.transform.localPosition = Vector3.zero;
                trunkCol.transform.localScale = new Vector3(1f/12f, 1f/12f, 1f/12f);
                
                var col = trunkCol.AddComponent<CapsuleCollider>();
                col.center = new Vector3(0f, 1.5f, 0f);
                col.radius = 0.6f;  // Bán kính 0.6m ở world space
                col.height = 4.0f;  // Chiều cao 4.0m ở world space
            }


            // --- SINH RỪNG TRÀM DỌC HAI BỜ KÊNH (PROCEDURAL FOREST ON BOTH BANKS) ---
            
            Random.InitState(12345);
            GameObject forestContainer = new GameObject("TeaTree_Forest");
            for (int i = 0; i < 45; i++)
            {
                Vector3 treePos = Vector3.zero;
                if (i < 20)
                {
                    // Rừng bờ trái - CHỈ sinh ở X < -15 (rìa bên trái xa) để chừa lối đi X từ -15 đến 15
                    treePos = new Vector3(Random.Range(-35f, -15f), 3.5f, Random.Range(-35f, 35f));
                }
                else
                {
                    // Rừng bờ phải (bờ đối diện sông) - Sinh từ X = 36 trở đi (tránh sông)
                    treePos = new Vector3(Random.Range(36f, 48f), 3.5f, Random.Range(-35f, 35f));
                }

                GameObject forestTree = LoadAndInstantiate("Assets/Models/TeaTree/low-poly+tree+3d+model.glb", "TeaTree_" + i, treePos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
                if (forestTree != null)
                {
                    forestTree.transform.SetParent(forestContainer.transform);
                    float rndScale = Random.Range(9f, 14f);
                    forestTree.transform.localScale = new Vector3(rndScale, rndScale, rndScale);
                    
                    // Thêm chuyển động đung đưa theo gió cho cây rừng
                    forestTree.AddComponent<WindSway>();

                    // Thêm Trunk Collider độc lập cho từng cây tràm trong rừng
                    GameObject trunkCol = new GameObject("TrunkCollider");
                    trunkCol.transform.SetParent(forestTree.transform, false);
                    trunkCol.transform.localPosition = Vector3.zero;
                    trunkCol.transform.localScale = new Vector3(1f/rndScale, 1f/rndScale, 1f/rndScale);
                    
                    var col = trunkCol.AddComponent<CapsuleCollider>();
                    col.center = new Vector3(0f, 1.5f, 0f);
                    col.radius = 0.5f;
                    col.height = 4.0f;
                }
            }


            // --- THIẾT LẬP NHÂN VẬT CHƠI (PLAYER & CAMERA) ---

            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(6f, 1.0f, -10f); // Xuất phát từ sân trống hướng nhìn ra nhà và sông

            var charController = player.AddComponent<CharacterController>();
            charController.height = 2f;
            charController.radius = 0.4f;
            charController.center = new Vector3(0, 1f, 0);

            var playerInput = player.AddComponent<PlayerInput>();
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (inputAsset != null)
            {
                playerInput.actions = inputAsset;
                playerInput.defaultActionMap = "Player";
            }

            var playerCtrl = player.AddComponent<PlayerController>();
            var playerInteract = player.AddComponent<PlayerInteraction>();

            // Camera mắt nhìn
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(player.transform, false);
            camObj.transform.localPosition = new Vector3(0, 1.8f, 0);
            var camera = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();

            // Kích hoạt Hậu kỳ điện ảnh (Post-Processing) trên Camera URP
            var cameraData = camObj.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;

            var photoCam = camObj.AddComponent<PhotoCamera>();

            SerializedObject serCtrl = new SerializedObject(playerCtrl);
            serCtrl.FindProperty("playerCamera").objectReferenceValue = camObj.transform;
            serCtrl.ApplyModifiedProperties();

            SerializedObject serInteract = new SerializedObject(playerInteract);
            serInteract.FindProperty("cameraTransform").objectReferenceValue = camObj.transform;
            serInteract.FindProperty("interactableLayer").intValue = 1 << LayerMask.NameToLayer("Interactable");
            serInteract.ApplyModifiedProperties();

            GameObject cameraHandModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cameraHandModel.name = "CameraHandModel";
            cameraHandModel.transform.SetParent(camObj.transform, false);
            cameraHandModel.transform.localPosition = new Vector3(0.2f, -0.25f, 0.4f);
            cameraHandModel.transform.localRotation = Quaternion.identity;
            cameraHandModel.transform.localScale = new Vector3(0.12f, 0.08f, 0.1f);
            DestroyImmediate(cameraHandModel.GetComponent<BoxCollider>());


            // --- XÂY DỰNG GIAO DIỆN (UI CANVASES) ---

            GameObject gameUI = new GameObject("GameUI");
            var canvas = gameUI.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameUI.AddComponent<CanvasScaler>();
            gameUI.AddComponent<GraphicRaycaster>();
            
            var interactUI = gameUI.AddComponent<InteractionUI>();

            GameObject promptPanel = new GameObject("InteractionPromptPanel");
            promptPanel.transform.SetParent(gameUI.transform, false);
            var promptRect = promptPanel.AddComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0.5f);
            promptRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptRect.pivot = new Vector2(0.5f, 0.5f);
            promptRect.anchoredPosition = new Vector2(0, -60);
            promptRect.sizeDelta = new Vector2(400, 50);

            var promptText = promptPanel.AddComponent<TextMeshProUGUI>();
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.fontSize = 20;
            promptText.color = Color.yellow;
            promptText.text = "[E] Nói chuyện";

            SerializedObject serIntUI = new SerializedObject(interactUI);
            serIntUI.FindProperty("promptPanel").objectReferenceValue = promptPanel;
            serIntUI.FindProperty("promptText").objectReferenceValue = promptText;
            serIntUI.ApplyModifiedProperties();

            GameObject objTextObj = new GameObject("ObjectiveText");
            objTextObj.transform.SetParent(gameUI.transform, false);
            var objRect = objTextObj.AddComponent<RectTransform>();
            objRect.anchorMin = new Vector2(0.5f, 1f);
            objRect.anchorMax = new Vector2(0.5f, 1f);
            objRect.pivot = new Vector2(0.5f, 1f);
            objRect.anchoredPosition = new Vector2(0, -40);
            objRect.sizeDelta = new Vector2(800, 60);

            var objText = objTextObj.AddComponent<TextMeshProUGUI>();
            objText.alignment = TextAlignmentOptions.Center;
            objText.fontSize = 22;
            objText.color = Color.white;
            objText.text = "Mục tiêu: Đang cập nhật...";

            GameObject viewfinderCanvas = new GameObject("ViewfinderCanvas");
            viewfinderCanvas.transform.SetParent(gameUI.transform, false);
            var vfRect = viewfinderCanvas.AddComponent<RectTransform>();
            vfRect.anchorMin = Vector2.zero;
            vfRect.anchorMax = Vector2.one;
            vfRect.sizeDelta = Vector2.zero;

            GameObject borderObj = new GameObject("ViewfinderBorder");
            borderObj.transform.SetParent(viewfinderCanvas.transform, false);
            var borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = Vector2.zero;
            var borderImg = borderObj.AddComponent<Image>();
            borderImg.color = new Color(0, 0, 0, 0.4f);

            GameObject recTextObj = new GameObject("RECText");
            recTextObj.transform.SetParent(viewfinderCanvas.transform, false);
            var recRect = recTextObj.AddComponent<RectTransform>();
            recRect.anchorMin = new Vector2(0.1f, 0.9f);
            recRect.anchorMax = new Vector2(0.1f, 0.9f);
            recRect.anchoredPosition = Vector2.zero;
            recRect.sizeDelta = new Vector2(100, 30);
            var recText = recTextObj.AddComponent<TextMeshProUGUI>();
            recText.text = "● REC";
            recText.color = Color.red;
            recText.fontSize = 20;

            GameObject flashObj = new GameObject("FlashImage");
            flashObj.transform.SetParent(gameUI.transform, false);
            var flashRect = flashObj.AddComponent<RectTransform>();
            flashRect.anchorMin = Vector2.zero;
            flashRect.anchorMax = Vector2.one;
            flashRect.sizeDelta = Vector2.zero;
            var flashImg = flashObj.AddComponent<Image>();
            flashImg.color = new Color(1, 1, 1, 0);

            SerializedObject serCam = new SerializedObject(photoCam);
            serCam.FindProperty("viewfinderCanvas").objectReferenceValue = viewfinderCanvas;
            serCam.FindProperty("flashImage").objectReferenceValue = flashImg;
            serCam.FindProperty("normalFOV").floatValue = 60f;
            serCam.FindProperty("zoomFOV").floatValue = 30f;
            serCam.FindProperty("occlusionLayers").intValue = 1 << 0;
            serCam.ApplyModifiedProperties();

            GameObject managersObj = new GameObject("Managers");
            var diagManager = managersObj.AddComponent<DialogueManager>();

            GameObject diagPanel = new GameObject("DialoguePanel");
            diagPanel.transform.SetParent(gameUI.transform, false);
            var diagRect = diagPanel.AddComponent<RectTransform>();
            diagRect.anchorMin = new Vector2(0.5f, 0f);
            diagRect.anchorMax = new Vector2(0.5f, 0f);
            diagRect.pivot = new Vector2(0.5f, 0f);
            diagRect.anchoredPosition = new Vector2(0, 30);
            diagRect.sizeDelta = new Vector2(700, 160);
            var diagImg = diagPanel.AddComponent<Image>();
            diagImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            GameObject speakerTextObj = new GameObject("SpeakerNameText");
            speakerTextObj.transform.SetParent(diagPanel.transform, false);
            var spkRect = speakerTextObj.AddComponent<RectTransform>();
            spkRect.anchorMin = new Vector2(0f, 1f);
            spkRect.anchorMax = new Vector2(0f, 1f);
            spkRect.pivot = new Vector2(0f, 1f);
            spkRect.anchoredPosition = new Vector2(15, -10);
            spkRect.sizeDelta = new Vector2(200, 30);
            var spkText = speakerTextObj.AddComponent<TextMeshProUGUI>();
            spkText.fontSize = 20;
            spkText.color = Color.green;
            spkText.text = "Ông Ngoại";

            GameObject dialogueTextObj = new GameObject("DialogueText");
            dialogueTextObj.transform.SetParent(diagPanel.transform, false);
            var textRect = dialogueTextObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(15, 15);
            textRect.offsetMax = new Vector2(-15, -45);
            var diagTextComp = dialogueTextObj.AddComponent<TextMeshProUGUI>();
            diagTextComp.fontSize = 18;
            diagTextComp.color = Color.white;
            diagTextComp.text = "Đang chạy lời thoại...";

            GameObject continueIndicator = new GameObject("ContinueIndicator");
            continueIndicator.transform.SetParent(diagPanel.transform, false);
            var cntRect = continueIndicator.AddComponent<RectTransform>();
            cntRect.anchorMin = new Vector2(1f, 0f);
            cntRect.anchorMax = new Vector2(1f, 0f);
            cntRect.pivot = new Vector2(1f, 0f);
            cntRect.anchoredPosition = new Vector2(-15, 10);
            cntRect.sizeDelta = new Vector2(120, 25);
            var cntText = continueIndicator.AddComponent<TextMeshProUGUI>();
            cntText.fontSize = 14;
            cntText.color = Color.gray;
            cntText.text = "[Click / Space] Tiếp tục";

            SerializedObject serDiag = new SerializedObject(diagManager);
            serDiag.FindProperty("dialoguePanel").objectReferenceValue = diagPanel;
            serDiag.FindProperty("speakerNameText").objectReferenceValue = spkText;
            serDiag.FindProperty("dialogueText").objectReferenceValue = diagTextComp;
            serDiag.FindProperty("continueIndicator").objectReferenceValue = continueIndicator;
            serDiag.ApplyModifiedProperties();

            GameObject faderCanvasObj = new GameObject("FaderCanvas");
            var faderCanvas = faderCanvasObj.AddComponent<Canvas>();
            faderCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            faderCanvas.sortingOrder = 999;
            faderCanvasObj.AddComponent<CanvasScaler>();
            faderCanvasObj.AddComponent<GraphicRaycaster>();
            var screenFader = faderCanvasObj.AddComponent<ScreenFader>();

            GameObject fadeImgObj = new GameObject("FadeImage");
            fadeImgObj.transform.SetParent(faderCanvasObj.transform, false);
            var fimgRect = fadeImgObj.AddComponent<RectTransform>();
            fimgRect.anchorMin = Vector2.zero;
            fimgRect.anchorMax = Vector2.one;
            fimgRect.sizeDelta = Vector2.zero;
            var fImg = fadeImgObj.AddComponent<Image>();
            fImg.color = Color.black;

            SerializedObject serFade = new SerializedObject(screenFader);
            serFade.FindProperty("fadeImage").objectReferenceValue = fImg;
            serFade.ApplyModifiedProperties();

            // 10. Tạo Phase1Manager
            var phaseManager = managersObj.AddComponent<Phase1Manager>();
            SerializedObject serPhase = new SerializedObject(phaseManager);
            serPhase.FindProperty("photoCamera").objectReferenceValue = photoCam;
            serPhase.FindProperty("cameraHandModel").objectReferenceValue = cameraHandModel;
            
            if (mangoTree != null)
            {
                serPhase.FindProperty("mangoTreeTarget").objectReferenceValue = mangoTree.transform;
            }
            if (boatTriggerZone != null)
            {
                serPhase.FindProperty("boatTriggerZone").objectReferenceValue = boatTriggerZone;
            }
            serPhase.FindProperty("objectiveText").objectReferenceValue = objText;
            serPhase.ApplyModifiedProperties();


            // --- CẤU HÌNH HẬU KỲ ĐIỆN ẢNH (POST-PROCESSING) & SƯƠNG MÙ (FOG) ---

            // 1. Cấu hình Fog (Sương mù buổi sáng vùng sông nước miền Tây)
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.62f, 0.72f, 0.66f); // Sương mù xám xanh lục nhạt
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.015f;

            // 2. Tạo đối tượng Global Volume chứa hậu kỳ
            GameObject volumeObj = new GameObject("Global PostProcess Volume");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;

            // Tạo Volume Profile mới
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();

            // - ACES Tonemapping (Cho dải tương phản cao, màu sắc điện ảnh sắc sảo)
            var tonemapping = profile.Add<Tonemapping>();
            tonemapping.active = true;
            tonemapping.mode.Override(TonemappingMode.ACES);

            // - Bloom (Nắng sớm tỏa sáng rực rỡ qua tán cây)
            var bloom = profile.Add<Bloom>();
            bloom.active = true;
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(1.6f);
            bloom.scatter.Override(0.7f);
            bloom.tint.Override(new Color(1f, 0.95f, 0.82f)); // Sắc vàng nắng sớm ấm áp

            // - Color Adjustments (Tăng tương phản và độ rực của lá cây, bèo sông nước)
            var colorAdjust = profile.Add<ColorAdjustments>();
            colorAdjust.active = true;
            colorAdjust.contrast.Override(20f);
            colorAdjust.saturation.Override(28f);
            colorAdjust.postExposure.Override(0.18f);

            // - Vignette (Tạo góc tối điện ảnh, tập trung góc nhìn)
            var vignette = profile.Add<Vignette>();
            vignette.active = true;
            vignette.intensity.Override(0.24f);
            vignette.smoothness.Override(0.38f);
            vignette.rounded.Override(true);

            volume.sharedProfile = profile;

            // Lưu profile dưới dạng asset trong Scenes để không bị mất khi đóng Scene
            string profilePath = "Assets/Scenes/Phase1_VolumeProfile.asset";
            AssetDatabase.CreateAsset(profile, profilePath);


            // 11. Lưu Scene và báo kết quả
            EditorSceneManager.SaveScene(newScene, "Assets/Scenes/Phase1_GrandpaHouse.unity");
            Debug.Log("==> SETUP DỰ ÁN THÀNH CÔNG! Đã lưu scene tại: Assets/Scenes/Phase1_GrandpaHouse.unity");
            
            EditorUtility.DisplayDialog("Thành công!", "Nâng cấp hình ảnh hậu kỳ, ánh sáng và gió động cành lá đã hoàn tất!", "Đồng ý");
        }

        private static GameObject LoadAndInstantiate(string path, string newName, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("Không tìm thấy mô hình tại đường dẫn: " + path);
                return null;
            }
            
            GameObject obj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (obj != null)
            {
                obj.name = newName;
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            return obj;
        }

        private static void AddMeshCollidersRecursively(GameObject obj)
        {
            MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;

                // Kiểm tra số lượng tam giác để tránh tạo MeshCollider trên mesh quá lớn gây lag và lỗi PhysX (> 2 triệu)
                int triCount = 0;
                if (mf.sharedMesh.subMeshCount > 0)
                {
                    triCount = (int)mf.sharedMesh.GetIndexCount(0) / 3;
                }

                if (triCount > 100000)
                {
                    Debug.LogWarning($"[Rung Tram Tra Su] Bo qua MeshCollider cho '{mf.name}' vi qua nang ({triCount} tam giac) de tranh loi PhysX va lag. Thay bang BoxCollider.");
                    if (mf.gameObject.GetComponent<Collider>() == null)
                    {
                        mf.gameObject.AddComponent<BoxCollider>();
                    }
                    continue;
                }

                if (mf.gameObject.GetComponent<Collider>() == null)
                {
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
        }

        private static void SetupPerfectBoatCollider(GameObject boat)
        {
            // 1. Thêm MeshCollider tự động cho các mesh con để đứng vững trong lòng thuyền
            AddMeshCollidersRecursively(boat);

            // 2. Tính toán Bounds cục bộ để tạo BoxCollider bệ đỡ ở đáy thuyền chống lún
            MeshRenderer[] renderers = boat.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            Vector3 worldCenter = bounds.center;
            Vector3 worldSize = bounds.size;

            Vector3 localCenter = boat.transform.InverseTransformPoint(worldCenter);
            Vector3 localScale = boat.transform.lossyScale;
            Vector3 localSize = new Vector3(
                worldSize.x / (localScale.x == 0 ? 1f : localScale.x),
                worldSize.y / (localScale.y == 0 ? 1f : localScale.y),
                worldSize.z / (localScale.z == 0 ? 1f : localScale.z)
            );

            // Thêm BoxCollider bệ đỡ ở đáy thuyền
            var solidCol = boat.AddComponent<BoxCollider>();
            
            // Đảm bảo Y local tối thiểu là 0.4f (scale 5 tương đương 2m thế giới) để làm bệ đỡ dày chống xuyên thủng
            float finalLocalY = Mathf.Max(localSize.y, 0.4f);
            solidCol.size = new Vector3(localSize.x, finalLocalY, localSize.z);

            // Hạ center của BoxCollider xuống một chút để không che hết khoang lòng thuyền của MeshCollider
            solidCol.center = new Vector3(localCenter.x, localCenter.y - (finalLocalY - localSize.y) / 2f, localCenter.z);
        }

        private static void CreateLayer(string layerName)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            
            bool exists = false;
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layerProp = layers.GetArrayElementAtIndex(i);
                if (layerProp.stringValue == layerName)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                for (int i = 8; i < layers.arraySize; i++)
                {
                    SerializedProperty layerProp = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(layerProp.stringValue))
                    {
                        layerProp.stringValue = layerName;
                        tagManager.ApplyModifiedProperties();
                        Debug.Log("Tự động tạo Layer: " + layerName);
                        break;
                    }
                }
            }
        }
    }
}
