using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace VRChatAutoClothingTool
{
    /// <summary>
    /// VRChatアバター用の衣装自動調整ウィンドウ
    /// </summary>
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
        
        // 新規マッピング用の一時変数
        private string newBoneName = "";
        private Transform newAvatarBone;
        private Transform newClothingBone;
        private bool showAddMappingPanel = false;
        
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
        private float lastSizeAdjustment = 1.0f; // 前回のサイズ調整値を保存
        
        // 貫通検出の設定
        private float penetrationPushOutDistance = 0.001f; // デフォルト値を小さくする（0.001f）
        private bool showPenetrationSettings = false;
        private bool enablePenetrationCheck = true; // 貫通チェックの有効/無効を切り替えるフラグ
        private bool useAdvancedSampling = true;     // 高精度サンプリングの使用フラグ
        private bool preferBodyMeshes = true;        // ボディメッシュを優先する
        private float penetrationThreshold = 0.015f; // 貫通とみなす距離の閾値
        private bool preserveMeshShape = true;       // メッシュ形状を維持する
        private float preserveStrength = 0.5f;       // 形状維持の強度 (0-1)
        
        // リアルタイムプレビュー機能
        private bool isRealTimePreview = false;
        private GameObject currentPreviewObject = null;
        
        // UnityのGUIの更新間隔
        private const float GUI_UPDATE_INTERVAL = 0.1f;
        private float lastUpdateTime = 0f;
        
        // 微調整の一括アンドゥ用のフラグ
        private bool fineAdjustmentStarted = false;
        
        // 各機能のハンドラ
        private BoneStructureAnalyzer boneAnalyzer;
        private ClothingAdjuster clothingAdjuster;
        private FineAdjustmentHandler fineAdjustHandler;
        
        [MenuItem("ずん解/衣装自動調整ツール")]
        public static void ShowWindow()
        {
            GetWindow<AutoClothingWindow>("ずん解 衣装自動調整ツール");
        }
        
        private void OnEnable()
        {
            // エディタ更新イベントを登録
            EditorApplication.update += OnEditorUpdate;
            
            // 各機能のハンドラを初期化
            boneAnalyzer = new BoneStructureAnalyzer();
            clothingAdjuster = new ClothingAdjuster();
            fineAdjustHandler = new FineAdjustmentHandler();
        }
        
        private void OnDisable()
        {
            // エディタ更新イベントを解除
            EditorApplication.update -= OnEditorUpdate;
            
            // プレビューモードを終了
            EndPreviewMode();
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
            
            // リアルタイムプレビューが有効な場合も更新
            if (isRealTimePreview && avatarObject != null && clothingObject != null)
            {
                float currentTime = Time.realtimeSinceStartup;
                if (currentTime - lastUpdateTime > GUI_UPDATE_INTERVAL)
                {
                    UpdatePreview();
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
                    
                    // プレビューを終了する
                    EndPreviewMode();
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
                
                // 手動でボーンマッピングを追加するボタンを表示
                if (GUILayout.Button("マッピングを手動追加"))
                {
                    showAddMappingPanel = true;
                }
                
                if (showAddMappingPanel)
                {
                    DrawAddMappingPanel();
                }
                
                return;
            }
            
            // マッピングの追加ボタン
            if (GUILayout.Button("新しいマッピングを追加"))
            {
                showAddMappingPanel = true;
            }
            
            if (showAddMappingPanel)
            {
                DrawAddMappingPanel();
            }
            
            // マッピングリストを表示
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // テーブルヘッダー
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("ボーン名", GUILayout.Width(150));
            GUILayout.Label("アバター", GUILayout.Width(150));
            GUILayout.Label("衣装", GUILayout.Width(150));
            GUILayout.Label("操作", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            
            // リストを複製（削除処理中にコレクションが変更されることを防ぐため）
            List<BoneMapping> mappingsToRemove = new List<BoneMapping>();
            
            foreach (var mapping in boneMappings)
            {
                EditorGUILayout.BeginHorizontal();
                
                string displayName = mapping.BoneName;
                if (mapping.IsUnmapped)
                {
                    // Unmappedであることを示す特別な表示
                    GUI.color = new Color(0.8f, 0.8f, 1.0f);
                }
                
                EditorGUILayout.LabelField(displayName, GUILayout.Width(150));
                GUI.color = Color.white;
                
                EditorGUILayout.LabelField("アバター:", GUILayout.Width(60));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(mapping.AvatarBone, typeof(Transform), true, GUILayout.Width(150));
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.LabelField("衣装:", GUILayout.Width(60));
                mapping.ClothingBone = (Transform)EditorGUILayout.ObjectField(mapping.ClothingBone, typeof(Transform), true, GUILayout.Width(150));
                
                // 削除ボタン
                if (GUILayout.Button("Del", GUILayout.Width(40)))
                {
                    mappingsToRemove.Add(mapping);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            // マッピングの削除処理
            foreach (var mapping in mappingsToRemove)
            {
                boneMappings.Remove(mapping);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawAddMappingPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("新しいマッピングを追加", EditorStyles.boldLabel);
            
            newBoneName = EditorGUILayout.TextField("ボーン名", newBoneName);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("アバターボーン");
            newAvatarBone = (Transform)EditorGUILayout.ObjectField(newAvatarBone, typeof(Transform), true);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("衣装ボーン");
            newClothingBone = (Transform)EditorGUILayout.ObjectField(newClothingBone, typeof(Transform), true);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("追加"))
            {
                if (!string.IsNullOrEmpty(newBoneName) && newAvatarBone != null)
                {
                    boneMappings.Add(new BoneMapping
                    {
                        BoneName = newBoneName,
                        AvatarBone = newAvatarBone,
                        ClothingBone = newClothingBone,
                        SourceAnalyzer = boneAnalyzer
                    });
                    
                    // フィールドをクリア
                    newBoneName = "";
                    newAvatarBone = null;
                    newClothingBone = null;
                    
                    showAddMappingPanel = false;
                }
                else
                {
                    EditorUtility.DisplayDialog("入力エラー", "ボーン名とアバターボーンは必須です。", "OK");
                }
            }
            
            if (GUILayout.Button("キャンセル"))
            {
                // フィールドをクリア
                newBoneName = "";
                newAvatarBone = null;
                newClothingBone = null;
                
                showAddMappingPanel = false;
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawScalingSection()
        {
            GUILayout.Label("スケーリング設定", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // リアルタイムプレビューを提供する
            bool wasPreview = isRealTimePreview;
            isRealTimePreview = EditorGUILayout.Toggle("リアルタイムプレビュー", isRealTimePreview);
            
            // プレビューモードが変更された場合
            if (wasPreview != isRealTimePreview)
            {
                if (isRealTimePreview)
                {
                    StartPreviewMode();
                }
                else
                {
                    EndPreviewMode();
                }
            }
            
            EditorGUI.BeginChangeCheck();
            globalScaleFactor = EditorGUILayout.Slider("全体スケール", globalScaleFactor, 0.1f, 3.0f);
            bool scaleChanged = EditorGUI.EndChangeCheck();
            
            if (scaleChanged && isRealTimePreview)
            {
                UpdatePreview();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPenetrationDetectionSection()
        {
            showPenetrationSettings = EditorGUILayout.Foldout(showPenetrationSettings, "貫通検出設定", true);
            
            if (showPenetrationSettings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.HelpBox("衣装がアバターに貫通しないよう調整するための設定です。衣装が変形する場合は無効にするか、値を小さくしてください。", MessageType.Info);
                
                // 貫通チェックの有効/無効を切り替えるトグル
                enablePenetrationCheck = EditorGUILayout.Toggle("貫通チェックを有効化", enablePenetrationCheck);
                
                // 有効な場合のみ詳細設定を表示
                if (enablePenetrationCheck)
                {
                    EditorGUI.indentLevel++;
                    
                    penetrationPushOutDistance = EditorGUILayout.Slider("押し出し距離", penetrationPushOutDistance, 0.0001f, 0.01f);
                    penetrationThreshold = EditorGUILayout.Slider("貫通閾値", penetrationThreshold, 0.001f, 0.03f);
                    
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("詳細設定", EditorStyles.boldLabel);
                    
                    preserveMeshShape = EditorGUILayout.Toggle("メッシュ形状を維持", preserveMeshShape);
                    
                    // 形状維持が有効な場合は強度スライダーを表示
                    if (preserveMeshShape)
                    {
                        EditorGUI.indentLevel++;
                        preserveStrength = EditorGUILayout.Slider("形状維持強度", preserveStrength, 0f, 1f);
                        EditorGUI.indentLevel--;
                        
                        EditorGUILayout.HelpBox("強度が高いほど元の形状を維持しますが、貫通修正が不十分になる場合があります。", MessageType.Info);
                    }
                    
                    useAdvancedSampling = EditorGUILayout.Toggle("高精度サンプリング", useAdvancedSampling);
                    preferBodyMeshes = EditorGUILayout.Toggle("ボディメッシュを優先", preferBodyMeshes);
                    
                    EditorGUI.indentLevel--;
                }
                
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
            fineAdjustHandler.DrawFineAdjustmentPanel(
                clothingObject,
                avatarObject,
                ref sizeAdjustment,
                ref positionAdjustment,
                ref rotationAdjustment,
                ref lastSizeAdjustment,
                ref fineAdjustmentStarted,
                globalScaleFactor,
                out statusMessage
            );
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
            
            // プレビューモードを終了
            EndPreviewMode();
            
            // ボーン構造分析の実行
            boneMappings = boneAnalyzer.AnalyzeBones(avatarObject, clothingObject);
            
            // BoneMapping.SourceAnalyzerを設定
            foreach (var mapping in boneMappings)
            {
                mapping.SourceAnalyzer = boneAnalyzer;
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
            
            // プレビューモードを終了
            EndPreviewMode();
            
            // 調整前の状態を保存
            SaveOriginalClothingState();
            
            // 衣装の自動調整を実行
            bool success = clothingAdjuster.AdjustClothing(
                avatarObject,
                clothingObject,
                boneMappings,
                globalScaleFactor,
                enablePenetrationCheck,
                penetrationPushOutDistance,
                penetrationThreshold,
                useAdvancedSampling,
                preferBodyMeshes,
                preserveMeshShape,
                preserveStrength
            );
            
            // 微調整パネルを表示
            showFineAdjustmentPanel = true;
            sizeAdjustment = 1.0f;
            lastSizeAdjustment = 1.0f;
            positionAdjustment = Vector3.zero;
            rotationAdjustment = Vector3.zero;
            fineAdjustmentStarted = false;
            
            isProcessing = false;
            statusMessage = success 
                ? "衣装の自動調整が完了しました。必要に応じて微調整パネルで調整してください。" 
                : "衣装の調整中にエラーが発生しました。";
            
            // UIの更新
            Repaint();
            
            // シーンビューの更新
            SceneView.RepaintAll();
        }
        
        private void StartPreviewMode()
        {
            if (avatarObject == null || clothingObject == null)
            {
                statusMessage = "アバターと衣装の両方を選択してください。";
                isRealTimePreview = false;
                return;
            }
            
            if (boneMappings.Count == 0)
            {
                // ボーン構造を分析
                boneMappings = boneAnalyzer.AnalyzeBones(avatarObject, clothingObject);
                
                foreach (var mapping in boneMappings)
                {
                    mapping.SourceAnalyzer = boneAnalyzer;
                }
            }
            
            UpdatePreview();
        }
        
        private void UpdatePreview()
        {
            if (!isRealTimePreview) return;
            
            // リアルタイムプレビューを更新
            currentPreviewObject = clothingAdjuster.UpdateScalingPreview(
                avatarObject,
                clothingObject,
                boneMappings,
                globalScaleFactor
            );
            
            statusMessage = "リアルタイムプレビュー中...スケーリングを調整してください。";
        }
        
        private void EndPreviewMode()
        {
            isRealTimePreview = false;
            
            if (currentPreviewObject != null)
            {
                Object.DestroyImmediate(currentPreviewObject);
                currentPreviewObject = null;
            }
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
            
            // プレビュー処理の実行
            clothingAdjuster.PreviewAdjustment(
                avatarObject,
                clothingObject,
                boneMappings,
                globalScaleFactor,
                enablePenetrationCheck,
                penetrationPushOutDistance,
                penetrationThreshold,
                useAdvancedSampling,
                preferBodyMeshes,
                preserveMeshShape,
                preserveStrength
            );
            
            statusMessage = "プレビューを表示しています。30秒後に自動的に削除されます。";
            
            // シーンビューにフォーカス
            SceneView.lastActiveSceneView.FrameSelected();
        }
        
        private void OnEditorUpdate()
        {
            // エディタの更新時に必要な処理（微調整のリアルタイム反映など）
            if (showFineAdjustmentPanel && clothingObject != null && avatarObject != null)
            {
                // 必要に応じてここでリアルタイム更新を行うこともできる
            }
        }
    }
}
