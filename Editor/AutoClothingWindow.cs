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
        private bool useAdvancedSampling = true; // 高精度サンプリングの使用フラグ
        private bool preferBodyMeshes = true; // ボディメッシュを優先する
        private float penetrationThreshold = 0.015f; // 貫通とみなす距離の閾値
        
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
            
            // ボーン構造分析の実行
            boneMappings = boneAnalyzer.AnalyzeBones(avatarObject, clothingObject);
            
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
                preferBodyMeshes
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
                preferBodyMeshes
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
    
    // ボーンマッピング用のクラス
    [System.Serializable]
    public class BoneMapping
    {
        public string BoneName;
        public Transform AvatarBone;
        public Transform ClothingBone;
    }
}
