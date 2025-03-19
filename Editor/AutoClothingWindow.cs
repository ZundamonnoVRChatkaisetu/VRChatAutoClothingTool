using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace VRChatAutoClothingTool
{
    public class AutoClothingWindow : EditorWindow
    {
        // ウィンドウスクロール位置
        private Vector2 scrollPosition;
        
        // アバターと衣装のGameObject
        private GameObject avatarObject;
        private GameObject clothingObject;
        
        // ボーン対応マッピングを保存するリスト
        private List<BoneMapping> boneMappings = new List<BoneMapping>();
        
        // スケーリング設定
        private float globalScaleFactor = 1.0f;
        private bool maintainProportions = true;
        private Vector3 customScaleFactor = Vector3.one;
        
        // 処理ステータス
        private string statusMessage = "";
        private bool isProcessing = false;
        
        [MenuItem("ずん解/衣装自動調整ツール")]
        public static void ShowWindow()
        {
            GetWindow<AutoClothingWindow>("ずん解 衣装自動調整ツール")]");
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            GUILayout.Label("VRChat Auto Clothing Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            DrawObjectSelectionSection();
            EditorGUILayout.Space(10);
            
            DrawBoneMappingSection();
            EditorGUILayout.Space(10);
            
            DrawScalingSection();
            EditorGUILayout.Space(10);
            
            DrawButtonsSection();
            EditorGUILayout.Space(10);
            
            DrawStatusSection();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawObjectSelectionSection()
        {
            GUILayout.Label("アバターと衣装の選択", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("アバター");
            avatarObject = (GameObject)EditorGUILayout.ObjectField(avatarObject, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("衣装");
            clothingObject = (GameObject)EditorGUILayout.ObjectField(clothingObject, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("ボーン構造を分析"))
            {
                AnalyzeBoneStructure();
            }
        }
        
        private void DrawBoneMappingSection()
        {
            GUILayout.Label("ボーンマッピング", EditorStyles.boldLabel);
            
            if (boneMappings.Count == 0)
            {
                EditorGUILayout.HelpBox("アバターと衣装を選択して「ボーン構造を分析」ボタンを押してください。", MessageType.Info);
                return;
            }
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            foreach (var mapping in boneMappings)
            {
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField(mapping.BoneName, GUILayout.Width(150));
                
                EditorGUILayout.LabelField("アバター:", GUILayout.Width(60));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(mapping.AvatarBone, typeof(Transform), true, GUILayout.Width(150));
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.LabelField("衣装:", GUILayout.Width(60));
                mapping.ClothingBone = (Transform)EditorGUILayout.ObjectField(mapping.ClothingBone, typeof(Transform), true, GUILayout.Width(150));
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawScalingSection()
        {
            GUILayout.Label("スケーリング設定", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            maintainProportions = EditorGUILayout.Toggle("プロポーションを維持", maintainProportions);
            
            if (maintainProportions)
            {
                globalScaleFactor = EditorGUILayout.Slider("グローバルスケール", globalScaleFactor, 0.1f, 3.0f);
            }
            else
            {
                EditorGUILayout.LabelField("カスタムスケール");
                EditorGUI.indentLevel++;
                customScaleFactor.x = EditorGUILayout.Slider("X", customScaleFactor.x, 0.1f, 3.0f);
                customScaleFactor.y = EditorGUILayout.Slider("Y", customScaleFactor.y, 0.1f, 3.0f);
                customScaleFactor.z = EditorGUILayout.Slider("Z", customScaleFactor.z, 0.1f, 3.0f);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawButtonsSection()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !isProcessing && avatarObject != null && clothingObject != null;
            
            if (GUILayout.Button("衣装を自動調整", GUILayout.Height(30)))
            {
                AutoAdjustClothing();
            }
            
            if (GUILayout.Button("プレビュー", GUILayout.Height(30)))
            {
                PreviewAdjustment();
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawStatusSection()
        {
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }
        }
        
        private void AnalyzeBoneStructure()
        {
            if (avatarObject == null || clothingObject == null)
            {
                statusMessage = "アバターと衣装の両方を選択してください。";
                return;
            }
            
            statusMessage = "ボーン構造を分析中...";
            isProcessing = true;
            
            // ボーン構造分析の実装
            boneMappings.Clear();
            
            // アバターのTransformを取得
            var avatarTransforms = avatarObject.GetComponentsInChildren<Transform>();
            
            // 衣装のTransformを取得
            var clothingTransforms = clothingObject.GetComponentsInChildren<Transform>();
            
            // 基本的なボーン名のリスト（VRChatの標準的なボーン名）
            var commonBoneNames = new List<string>
            {
                "Hips", "Spine", "Chest", "UpperChest", "Neck", "Head",
                "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
                "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
                "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes",
                "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes"
            };
            
            // 指のボーン名を追加
            var fingerNames = new List<string> { "Thumb", "Index", "Middle", "Ring", "Little" };
            var jointNames = new List<string> { "Proximal", "Intermediate", "Distal" };
            
            foreach (var hand in new[] { "Left", "Right" })
            {
                foreach (var finger in fingerNames)
                {
                    foreach (var joint in jointNames)
                    {
                        commonBoneNames.Add($"{hand}{finger}{joint}");
                    }
                }
            }
            
            // アバターのボーンを検索
            var avatarBones = new Dictionary<string, Transform>();
            foreach (var boneTransform in avatarTransforms)
            {
                var boneName = boneTransform.name;
                if (commonBoneNames.Contains(boneName) || commonBoneNames.Any(b => boneName.Contains(b)))
                {
                    avatarBones[boneName] = boneTransform;
                }
            }
            
            // 衣装のボーンを検索
            var clothingBones = new Dictionary<string, Transform>();
            foreach (var boneTransform in clothingTransforms)
            {
                var boneName = boneTransform.name;
                if (commonBoneNames.Contains(boneName) || commonBoneNames.Any(b => boneName.Contains(b)))
                {
                    clothingBones[boneName] = boneTransform;
                }
            }
            
            // マッピングリストを作成
            foreach (var avatarBone in avatarBones)
            {
                var boneName = avatarBone.Key;
                var avatarTransform = avatarBone.Value;
                
                // 同じ名前の衣装ボーンを探す
                Transform clothingTransform = null;
                clothingBones.TryGetValue(boneName, out clothingTransform);
                
                // マッピングを追加
                boneMappings.Add(new BoneMapping
                {
                    BoneName = boneName,
                    AvatarBone = avatarTransform,
                    ClothingBone = clothingTransform
                });
            }
            
            isProcessing = false;
            statusMessage = $"{boneMappings.Count}個のボーンをマッピングしました。手動でマッピングを調整できます。";
            
            Repaint();
        }
        
        private void AutoAdjustClothing()
        {
            if (avatarObject == null || clothingObject == null)
            {
                statusMessage = "アバターと衣装の両方を選択してください。";
                return;
            }
            
            if (boneMappings.Count == 0)
            {
                statusMessage = "先にボーン構造を分析してください。";
                return;
            }
            
            statusMessage = "衣装を調整中...";
            isProcessing = true;
            
            // Undo登録
            Undo.RegisterFullObjectHierarchyUndo(clothingObject, "Auto Adjust Clothing");
            
            // 衣装を一時的にアバターの子オブジェクトにする
            clothingObject.transform.parent = avatarObject.transform;
            
            // グローバルスケールの適用
            Vector3 scaleFactor = maintainProportions 
                ? new Vector3(globalScaleFactor, globalScaleFactor, globalScaleFactor)
                : customScaleFactor;
            
            // ルート位置の調整
            clothingObject.transform.localPosition = Vector3.zero;
            clothingObject.transform.localRotation = Quaternion.identity;
            clothingObject.transform.localScale = Vector3.Scale(clothingObject.transform.localScale, scaleFactor);
            
            // 各ボーンのマッピングに基づいて調整
            foreach (var mapping in boneMappings)
            {
                if (mapping.AvatarBone != null && mapping.ClothingBone != null)
                {
                    // ボーンの位置と回転を合わせる
                    mapping.ClothingBone.position = mapping.AvatarBone.position;
                    mapping.ClothingBone.rotation = mapping.AvatarBone.rotation;
                    
                    // スケールの調整（オプション）
                    if (mapping.BoneName.Contains("Hips") || mapping.BoneName.Contains("Spine") || mapping.BoneName.Contains("Chest"))
                    {
                        mapping.ClothingBone.localScale = Vector3.Scale(mapping.ClothingBone.localScale, scaleFactor);
                    }
                }
            }
            
            // 衣装のメッシュレンダラーを取得し、スキンメッシュのバインドポーズを再計算
            var skinRenderers = clothingObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in skinRenderers)
            {
                if (renderer.sharedMesh != null)
                {
                    // メッシュを複製して編集可能にする
                    Mesh meshCopy = Instantiate(renderer.sharedMesh);
                    string assetPath = $"Assets/AdjustedMeshes/{clothingObject.name}_{renderer.name}_Adjusted.asset";
                    
                    // アセットフォルダの作成
                    string directory = System.IO.Path.GetDirectoryName(assetPath);
                    if (!System.IO.Directory.Exists(directory))
                    {
                        System.IO.Directory.CreateDirectory(directory);
                    }
                    
                    // メッシュをアセットとして保存
                    AssetDatabase.CreateAsset(meshCopy, assetPath);
                    AssetDatabase.SaveAssets();
                    
                    // レンダラーにメッシュを適用
                    renderer.sharedMesh = meshCopy;
                }
            }
            
            // 調整完了後に衣装を親から解除するオプション
            // clothingObject.transform.parent = null;
            
            isProcessing = false;
            statusMessage = "衣装の自動調整が完了しました。必要に応じて手動で微調整してください。";
            
            // UIの更新
            Repaint();
            
            // シーンビューの更新
            SceneView.RepaintAll();
        }
        
        private void PreviewAdjustment()
        {
            if (avatarObject == null || clothingObject == null)
            {
                statusMessage = "アバターと衣装の両方を選択してください。";
                return;
            }
            
            if (boneMappings.Count == 0)
            {
                statusMessage = "先にボーン構造を分析してください。";
                return;
            }
            
            // プレビュー用の一時的なオブジェクトを作成
            GameObject previewObject = Instantiate(clothingObject);
            previewObject.name = clothingObject.name + " (Preview)";
            
            // プレビューオブジェクトを一時的にアバターの子オブジェクトにする
            previewObject.transform.parent = avatarObject.transform;
            
            // スケールの適用
            Vector3 scaleFactor = maintainProportions 
                ? new Vector3(globalScaleFactor, globalScaleFactor, globalScaleFactor)
                : customScaleFactor;
            
            // ルート位置の調整
            previewObject.transform.localPosition = Vector3.zero;
            previewObject.transform.localRotation = Quaternion.identity;
            previewObject.transform.localScale = Vector3.Scale(previewObject.transform.localScale, scaleFactor);
            
            // プレビューオブジェクトのボーンを取得
            var previewTransforms = previewObject.GetComponentsInChildren<Transform>();
            
            // 各ボーンのマッピングに基づいて調整
            foreach (var mapping in boneMappings)
            {
                if (mapping.AvatarBone != null && mapping.ClothingBone != null)
                {
                    // プレビューオブジェクトの対応するボーンを検索
                    var previewBoneName = mapping.ClothingBone.name;
                    var previewBone = previewTransforms.FirstOrDefault(t => t.name == previewBoneName);
                    
                    if (previewBone != null)
                    {
                        // ボーンの位置と回転を合わせる
                        previewBone.position = mapping.AvatarBone.position;
                        previewBone.rotation = mapping.AvatarBone.rotation;
                        
                        // スケールの調整（オプション）
                        if (mapping.BoneName.Contains("Hips") || mapping.BoneName.Contains("Spine") || mapping.BoneName.Contains("Chest"))
                        {
                            previewBone.localScale = Vector3.Scale(previewBone.localScale, scaleFactor);
                        }
                    }
                }
            }
            
            // プレビューオブジェクトに半透明マテリアルを適用
            var renderers = previewObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    // 元のマテリアルを複製
                    var material = new Material(materials[i]);
                    
                    // 半透明設定を適用
                    material.color = new Color(material.color.r, material.color.g, material.color.b, 0.5f);
                    if (material.HasProperty("_Mode"))
                    {
                        material.SetFloat("_Mode", 3); // Transparent
                    }
                    material.renderQueue = 3000;
                    
                    // マテリアルの各種設定
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
            
            // 30秒後にプレビューを自動削除するコルーチン
            EditorApplication.delayCall += () =>
            {
                // 30秒後に実行
                EditorApplication.delayCall += () =>
                {
                    if (previewObject != null)
                    {
                        DestroyImmediate(previewObject);
                        statusMessage = "プレビューを終了しました。";
                        Repaint();
                    }
                };
            };
            
            statusMessage = "プレビューを表示しています。30秒後に自動的に削除されます。";
            
            // シーンビューにフォーカス
            SceneView.lastActiveSceneView.FrameSelected();
        }
    }
    
    // ボーンマッピング用のクラス
    [System.Serializable]
    public class BoneMapping
    {
        public string BoneName;
        public Transform AvatarBone;
        public Transform ClothingBone;
    }
}
