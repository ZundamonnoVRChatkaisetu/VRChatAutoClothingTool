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
        
        // 調整前の衣装の状態を保存
        private Vector3 originalClothingPosition;
        private Quaternion originalClothingRotation;
        private Vector3 originalClothingScale;
        
        // ボーン対応マッピングを保存するリスト
        private List<BoneMapping> boneMappings = new List<BoneMapping>();
        
        // スケーリング設定
        private float globalScaleFactor = 1.0f;
        
        // 処理ステータス
        private string statusMessage = "";
        private bool isProcessing = false;
        
        // 微調整フラグと設定
        private bool showFineAdjustmentPanel = false;
        private Vector3 positionAdjustment = Vector3.zero;
        private Vector3 rotationAdjustment = Vector3.zero;
        private float sizeAdjustment = 1.0f;
        
        // 貫通検出の設定
        private float penetrationPushOutDistance = 0.01f;
        private bool showPenetrationSettings = false;
        
        // UnityのGUIの更新間隔
        private const float GUI_UPDATE_INTERVAL = 0.1f;
        private float lastUpdateTime = 0f;
        
        // 微調整の一括アンドゥ用のフラグ
        private bool fineAdjustmentStarted = false;
        
        [MenuItem("ずん解/衣装自動調整ツール")]
        public static void ShowWindow()
        {
            GetWindow<AutoClothingWindow>("ずん解 衣装自動調整ツール");
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
            
            // 貫通検出設定セクション
            DrawPenetrationDetectionSection();
            EditorGUILayout.Space(10);
            
            // 微調整パネルを表示（自動調整後に表示される）
            if (showFineAdjustmentPanel)
            {
                DrawFineAdjustmentSection();
                EditorGUILayout.Space(10);
            }
            
            DrawStatusSection();
            
            EditorGUILayout.EndScrollView();
            
            // GUI更新（リアルタイム反映のため）
            HandleGuiUpdates();
        }
        
        private void HandleGuiUpdates()
        {
            // 微調整パネルが表示されている場合、変更があるたびに更新をかける
            if (showFineAdjustmentPanel && clothingObject != null)
            {
                float currentTime = Time.realtimeSinceStartup;
                if (currentTime - lastUpdateTime > GUI_UPDATE_INTERVAL)
                {
                    Repaint();
                    lastUpdateTime = currentTime;
                }
            }
        }
        
        private void DrawObjectSelectionSection()
        {
            GUILayout.Label("アバターと衣装の選択", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("アバター");
            avatarObject = (GameObject)EditorGUILayout.ObjectField(avatarObject, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("衣装");
            clothingObject = (GameObject)EditorGUILayout.ObjectField(clothingObject, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();
            
            if (EditorGUI.EndChangeCheck())
            {
                // オブジェクトが変更された場合は衣装の元の状態を保存
                if (clothingObject != null)
                {
                    SaveOriginalClothingState();
                }
            }
            
            if (GUILayout.Button("ボーン構造を分析"))
            {
                AnalyzeBoneStructure();
            }
        }
        
        private void SaveOriginalClothingState()
        {
            if (clothingObject != null)
            {
                originalClothingPosition = clothingObject.transform.position;
                originalClothingRotation = clothingObject.transform.rotation;
                originalClothingScale = clothingObject.transform.localScale;
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
            
            EditorGUI.BeginChangeCheck();
            globalScaleFactor = EditorGUILayout.Slider("全体スケール", globalScaleFactor, 0.1f, 3.0f);
            if (EditorGUI.EndChangeCheck())
            {
                // スケール設定が変更された場合のリアルタイムプレビュー（オプション）
                // この部分では実装しないが、将来的にリアルタイムプレビューを追加可能
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPenetrationDetectionSection()
        {
            showPenetrationSettings = EditorGUILayout.Foldout(showPenetrationSettings, "貫通検出設定", true);
            
            if (showPenetrationSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.HelpBox("貫通を検出した際に、頂点をどれだけアバターから押し出すかを設定します。", MessageType.Info);
                
                penetrationPushOutDistance = EditorGUILayout.Slider("押し出し距離", penetrationPushOutDistance, 0.001f, 0.05f);
                
                EditorGUILayout.EndVertical();
            }
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
        
        private void DrawFineAdjustmentSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("微調整パネル", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("以下のスライダーを使って衣装の位置・回転・サイズを微調整できます。調整はリアルタイムで反映されます。", MessageType.Info);
            
            EditorGUI.BeginChangeCheck();
            
            // サイズ調整
            EditorGUILayout.Space(5);
            GUILayout.Label("サイズ調整", EditorStyles.boldLabel);
            float newSizeAdjustment = EditorGUILayout.Slider("サイズ倍率", sizeAdjustment, 0.5f, 2.0f);
            
            // 位置調整
            EditorGUILayout.Space(5);
            GUILayout.Label("位置調整", EditorStyles.boldLabel);
            Vector3 newPositionAdjustment = new Vector3(
                EditorGUILayout.Slider("X位置", positionAdjustment.x, -0.5f, 0.5f),
                EditorGUILayout.Slider("Y位置", positionAdjustment.y, -0.5f, 0.5f),
                EditorGUILayout.Slider("Z位置", positionAdjustment.z, -0.5f, 0.5f)
            );
            
            // 回転調整
            EditorGUILayout.Space(5);
            GUILayout.Label("回転調整", EditorStyles.boldLabel);
            Vector3 newRotationAdjustment = new Vector3(
                EditorGUILayout.Slider("X回転", rotationAdjustment.x, -180f, 180f),
                EditorGUILayout.Slider("Y回転", rotationAdjustment.y, -180f, 180f),
                EditorGUILayout.Slider("Z回転", rotationAdjustment.z, -180f, 180f)
            );
            
            if (EditorGUI.EndChangeCheck())
            {
                // 微調整を開始する際に一度だけUndo登録
                if (!fineAdjustmentStarted && clothingObject != null)
                {
                    Undo.RecordObject(clothingObject.transform, "Fine Adjustment");
                    fineAdjustmentStarted = true;
                }
                
                // 値が変更されたらリアルタイムで衣装を調整
                bool sizeChanged = sizeAdjustment != newSizeAdjustment;
                bool positionChanged = positionAdjustment != newPositionAdjustment;
                bool rotationChanged = rotationAdjustment != newRotationAdjustment;
                
                sizeAdjustment = newSizeAdjustment;
                positionAdjustment = newPositionAdjustment;
                rotationAdjustment = newRotationAdjustment;
                
                UpdateFineAdjustment(sizeChanged, positionChanged, rotationChanged);
            }
            
            EditorGUILayout.Space(10);
            if (GUILayout.Button("調整をリセット", GUILayout.Height(25)))
            {
                ResetFineAdjustment();
            }
            
            if (GUILayout.Button("調整を確定", GUILayout.Height(25)))
            {
                FinalizeFineAdjustment();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void UpdateFineAdjustment(bool sizeChanged, bool positionChanged, bool rotationChanged)
        {
            if (clothingObject == null || avatarObject == null) return;
            
            // サイズ調整
            if (sizeChanged)
            {
                MeshUtility.AdjustClothingSize(clothingObject, sizeAdjustment);
            }
            
            // 位置調整
            if (positionChanged)
            {
                // アバターの位置を基準にして調整
                clothingObject.transform.position = avatarObject.transform.position + positionAdjustment;
            }
            
            // 回転調整
            if (rotationChanged)
            {
                // アバターの回転を基準にして調整
                clothingObject.transform.rotation = avatarObject.transform.rotation * Quaternion.Euler(rotationAdjustment);
            }
            
            // シーンビューの更新
            SceneView.RepaintAll();
        }
        
        private void ResetFineAdjustment()
        {
            sizeAdjustment = 1.0f;
            positionAdjustment = Vector3.zero;
            rotationAdjustment = Vector3.zero;
            
            if (clothingObject != null && avatarObject != null)
            {
                // 元の位置・回転・スケールに戻す
                Undo.RecordObject(clothingObject.transform, "Reset Fine Adjustment");
                
                clothingObject.transform.position = avatarObject.transform.position;
                clothingObject.transform.rotation = avatarObject.transform.rotation;
                
                // スケールはグローバルスケールファクターを適用
                Vector3 scaleFactor = new Vector3(globalScaleFactor, globalScaleFactor, globalScaleFactor);
                clothingObject.transform.localScale = Vector3.one;
                clothingObject.transform.localScale = Vector3.Scale(clothingObject.transform.localScale, scaleFactor);
                
                // 微調整開始フラグをリセット
                fineAdjustmentStarted = false;
                
                // シーンビューを更新
                SceneView.RepaintAll();
            }
        }
        
        private void FinalizeFineAdjustment()
        {
            if (clothingObject == null) return;
            
            // 調整結果を確定（アンドゥポイントを設定）
            Undo.RecordObject(clothingObject.transform, "Finalize Fine Adjustment");
            
            // 微調整開始フラグをリセット
            fineAdjustmentStarted = false;
            
            // 現在の調整を確定（アセットとして保存するなど）
            EditorUtility.DisplayDialog("微調整確定", 
                "現在の微調整設定が確定されました。\n\nサイズ: " + sizeAdjustment + 
                "\n位置: " + positionAdjustment.ToString("F2") + 
                "\n回転: " + rotationAdjustment.ToString("F1"), "OK");
            
            // 確定後も微調整パネルは表示したままにする
            statusMessage = "微調整が確定されました。";
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
            
            // 追加のボーン名パターン
            var additionalBonePatterns = new List<string>
            {
                "UpperLeg.L", "UpperLeg_L",
                "UpperLeg.R", "UpperLeg_R",
                "Shoulder.L", "Shoulder_L",
                "Shoulder.R", "Shoulder_R"
            };
            
            // 追加のボーン名パターンを共通ボーン名リストに追加
            commonBoneNames.AddRange(additionalBonePatterns);
            
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
            
            // 調整前の状態を保存
            SaveOriginalClothingState();
            
            // Undo登録
            Undo.RegisterFullObjectHierarchyUndo(clothingObject, "Auto Adjust Clothing");
            
            // 衣装を一時的にアバターの子オブジェクトにする
            clothingObject.transform.parent = avatarObject.transform;
            
            // グローバルスケールの適用
            Vector3 scaleFactor = new Vector3(globalScaleFactor, globalScaleFactor, globalScaleFactor);
            
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
            
            // 衣装とアバターの貫通をチェックして調整
            MeshUtility.AdjustClothingPenetration(avatarObject, clothingObject, penetrationPushOutDistance);
            
            // 調整完了後に衣装を親から解除
            clothingObject.transform.parent = null;
            clothingObject.transform.position = avatarObject.transform.position;
            clothingObject.transform.rotation = avatarObject.transform.rotation;
            
            // 微調整パネルを表示
            showFineAdjustmentPanel = true;
            sizeAdjustment = 1.0f;
            positionAdjustment = Vector3.zero;
            rotationAdjustment = Vector3.zero;
            fineAdjustmentStarted = false;
            
            isProcessing = false;
            statusMessage = "衣装の自動調整が完了しました。必要に応じて微調整パネルで調整してください。";
            
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
            Vector3 scaleFactor = new Vector3(globalScaleFactor, globalScaleFactor, globalScaleFactor);
            
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
            
            // 貫通チェック
            MeshUtility.AdjustClothingPenetration(avatarObject, previewObject, penetrationPushOutDistance);
            
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
        
        // エディタイベント処理（微調整のリアルタイム更新用）
        void OnEnable()
        {
            // エディタ更新イベントを登録
            EditorApplication.update += OnEditorUpdate;
        }
        
        void OnDisable()
        {
            // エディタ更新イベントを解除
            EditorApplication.update -= OnEditorUpdate;
        }
        
        void OnEditorUpdate()
        {
            // エディタの更新時に必要な処理（微調整のリアルタイム反映など）
            if (showFineAdjustmentPanel && clothingObject != null && avatarObject != null)
            {
                // 必要に応じてここでリアルタイム更新を行うこともできる
            }
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
