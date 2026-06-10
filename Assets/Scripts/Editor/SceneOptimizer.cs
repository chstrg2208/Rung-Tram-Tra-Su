using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

namespace RungTramTraSu
{
    public class SceneOptimizer : EditorWindow
    {
        [MenuItem("Tools/Optimize Phase 1 Scene")]
        public static void OptimizePhase1()
        {
            // Verify active scene name is Phase1_GrandpaHouse
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!activeScene.name.Equals("Phase1_GrandpaHouse"))
            {
                if (!EditorUtility.DisplayDialog("Warning", "The active scene is not Phase1_GrandpaHouse. Do you want to run optimization anyway?", "Yes", "No"))
                {
                    return;
                }
            }

            int optimizedFoliageCount = 0;
            int deletedGameObjectCount = 0;
            int deletedDuckCount = 0;

            // 1. Optimize Foliage under NatureDecorations
            GameObject natureDecorations = GameObject.Find("NatureDecorations");
            if (natureDecorations != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(natureDecorations, "Optimize Foliage");

                // Get all direct children of NatureDecorations
                List<Transform> children = new List<Transform>();
                for (int i = 0; i < natureDecorations.transform.childCount; i++)
                {
                    children.Add(natureDecorations.transform.GetChild(i));
                }

                foreach (var child in children)
                {
                    if (child == null) continue;

                    LODGroup lodGroup = child.GetComponent<LODGroup>();
                    MeshRenderer mr = child.GetComponent<MeshRenderer>();
                    MeshFilter mf = child.GetComponent<MeshFilter>();

                    if (lodGroup != null)
                    {
                        // Find LOD1 child, or fallback to LOD0, or first child
                        Transform targetLOD = null;
                        for (int i = 0; i < child.childCount; i++)
                        {
                            var subChild = child.GetChild(i);
                            if (subChild.name.EndsWith("_LOD1") || subChild.name.Contains("LOD1"))
                            {
                                targetLOD = subChild;
                                break;
                            }
                        }

                        if (targetLOD == null)
                        {
                            for (int i = 0; i < child.childCount; i++)
                            {
                                var subChild = child.GetChild(i);
                                if (subChild.name.EndsWith("_LOD0") || subChild.name.Contains("LOD0"))
                                {
                                    targetLOD = subChild;
                                    break;
                                }
                            }
                        }

                        if (targetLOD == null && child.childCount > 0)
                        {
                            targetLOD = child.GetChild(0);
                        }

                        if (targetLOD != null)
                        {
                            var targetMF = targetLOD.GetComponent<MeshFilter>();
                            var targetMR = targetLOD.GetComponent<MeshRenderer>();

                            if (targetMF != null && targetMF.sharedMesh != null)
                            {
                                if (mf == null) mf = child.gameObject.AddComponent<MeshFilter>();
                                mf.sharedMesh = targetMF.sharedMesh;
                            }

                            if (targetMR != null)
                            {
                                if (mr == null) mr = child.gameObject.AddComponent<MeshRenderer>();
                                mr.sharedMaterials = targetMR.sharedMaterials;
                            }
                        }

                        // Destroy children of this foliage object
                        List<GameObject> childrenToDestroy = new List<GameObject>();
                        for (int i = 0; i < child.childCount; i++)
                        {
                            childrenToDestroy.Add(child.GetChild(i).gameObject);
                        }

                        foreach (var childGo in childrenToDestroy)
                        {
                            deletedGameObjectCount++;
                            Undo.DestroyObjectImmediate(childGo);
                        }

                        // Destroy LODGroup component
                        Undo.DestroyObjectImmediate(lodGroup);
                        optimizedFoliageCount++;
                    }

                    // Refresh mesh renderer references
                    mr = child.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        // Turn off shadow casting
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        mr.receiveShadows = true;
                    }

                    // Mark as Static
                    GameObjectUtility.SetStaticEditorFlags(child.gameObject, 
                        StaticEditorFlags.BatchingStatic | 
                        StaticEditorFlags.OccludeeStatic | 
                        StaticEditorFlags.OccluderStatic | 
                        StaticEditorFlags.ReflectionProbeStatic);
                }
            }
            else
            {
                Debug.LogWarning("NatureDecorations GameObject not found!");
            }

            // 2. Remove all ducks (Snowy White Duck & DUKVKVKV)
            var allGo = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            List<GameObject> ducksToDestroy = new List<GameObject>();
            foreach (var go in allGo)
            {
                if (go.name.Contains("Snowy White Duck") || go.name.Contains("DUKVKVKV"))
                {
                    ducksToDestroy.Add(go);
                }
            }

            foreach (var duck in ducksToDestroy)
            {
                deletedDuckCount++;
                Undo.DestroyObjectImmediate(duck);
            }

            // 3. Optimize URP settings (Shadow Cascades = 2)
            string urpAssetPath = "Assets/Settings/PC_RPAsset.asset";
            UniversalRenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpAssetPath);
            if (urpAsset != null)
            {
                urpAsset.shadowCascadeCount = 2;
                EditorUtility.SetDirty(urpAsset);
                AssetDatabase.SaveAssetIfDirty(urpAsset);
                Debug.Log("URP Asset Shadow Cascades set to 2.");
            }
            else
            {
                Debug.LogWarning("URP Asset not found at path: " + urpAssetPath);
            }

            // Mark active scene dirty and save it
            if (natureDecorations != null)
            {
                EditorUtility.SetDirty(natureDecorations);
            }
            
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);

            AssetDatabase.SaveAssets();

            string report = $"Phase 1 Optimization Completed Successfully!\n" +
                            $"- Optimized foliage objects (LODs flattened): {optimizedFoliageCount}\n" +
                            $"- Deleted child LOD GameObjects: {deletedGameObjectCount}\n" +
                            $"- Deleted duck GameObjects: {deletedDuckCount}\n" +
                            $"- Shadow casting disabled on all {optimizedFoliageCount} foliage objects\n" +
                            $"- URP Shadow Cascades set to 2\n" +
                            $"Scene has been saved.";
            
            EditorUtility.DisplayDialog("Optimization Complete", report, "OK");
            Debug.Log(report);
        }
    }
}
