using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace VRChatAutoClothingTool
{
    /// <summary>
    /// 衣装の微調整機能を提供するクラス
    /// </summary>
    public class FineAdjustmentHandler
    {
        /// <summary>
        /// 調整可能な部位の一覧
        /// </summary>
        private string[] bodyParts = new string[] 
        {
            "全体",
            "胸部 (Chest)",
            "腰部 (Hips)",
            "腕 (Arms)",
            "脚 (Legs)",
            "頭 (Head)",
            "背中 (Back)",
            "その他"
        };

        /// <summary>
        /// 部位ごとのボーン名
        /// </summary>
        private Dictionary<string, string[]> bodyPartBonesMap = new Dictionary<string, string[]>()
        {
            // 胸部に関連するボーン
            { "胸部 (Chest)", new string[] 
                { 
                    "Chest", "UpperChest", "Breast", "Spine1", "Spine2", "Bust" 
                }
            },
            // 腰部に関連するボーン
            { "腰部 (Hips)", new string[] 
                { 
                    "Hips", "Pelvis", "Spine", "Waist", "LowerBack"
                }
            },
            // 腕に関連するボーン
            { "腕 (Arms)", new string[] 
                { 
                    "Shoulder", "Arm", "Elbow", "Hand", "Wrist", "Finger", "Thumb", "Index", "Middle", "Ring", "Little"
                }
            },
            // 脚に関連するボーン
            { "脚 (Legs)", new string[] 
                { 
                    "Leg", "Thigh", "Knee", "Shin", "Ankle", "Foot", "Toe", "UpperLeg", "LowerLeg", "Heel"
                }
            },
            // 頭に関連するボーン
            { "頭 (Head)", new string[] 
                { 
                    "Head", "Neck", "Face", "Eye", "Jaw", "Ear", "Nose", "Mouth", "Throat", "Tongue", "Teeth", "Hair"
                }
            },
            // 背中に関連するボーン
            { "背中 (Back)", new string[] 
                { 
                    "Back", "Spine", "Shoulder", "Scapula", "Clavicle"
                }
            }
        };

        /// <summary>
        /// 現在選択されている部位
        /// </summary>
        private int selectedPartIndex = 0;

        /// <summary>
        /// 部位ごとのサイズ調整値
        /// </summary>
        private Dictionary<string, float> partSizeAdjustments = new Dictionary<string, float>();

        /// <summary>
        /// 部位ごとの位置調整値
        /// </summary>
        private Dictionary<string, Vector3> partPositionAdjustments = new Dictionary<string, Vector3>();

        /// <summary>
        /// 初期化
        /// </summary>
        public FineAdjustmentHandler()
        {
            // 部位ごとの調整値を初期化
            foreach (string part in bodyParts)
            {
                partSizeAdjustments[part] = 1.0f;
                partPositionAdjustments[part] = Vector3.zero;
            }
        }

        /// <summary>
        /// 微調整パネルを描画する
        /// </summary>
        public void DrawFineAdjustmentPanel(
            GameObject clothingObject,
            GameObject avatarObject,
            ref float sizeAdjustment,
            ref Vector3 positionAdjustment,
            ref Vector3 rotationAdjustment,
            ref float lastSizeAdjustment,
            ref bool fineAdjustmentStarted,
            float globalScaleFactor,
            out string statusMessage)
        {
            statusMessage = "";
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("微調整パネル", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("以下のスライダーを使って衣装の位置・回転・サイズを微調整できます。調整はリアルタイムで反映されます。", MessageType.Info);
            
            // 部位選択ドロップダウン
            EditorGUILayout.Space(5);
            GUILayout.Label("調整部位", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            selectedPartIndex = EditorGUILayout.Popup("部位選択", selectedPartIndex, bodyParts);
            string selectedPart = bodyParts[selectedPartIndex];
            
            // サイズ調整
            EditorGUILayout.Space(5);
            GUILayout.Label("サイズ調整", EditorStyles.boldLabel);
            
            // 部位ごとのサイズ調整
            float newPartSizeAdjustment = 0;
            if (selectedPart == "全体")
            {
                newPartSizeAdjustment = EditorGUILayout.Slider("サイズ倍率", sizeAdjustment, 0.5f, 2.0f);
            }
            else
            {
                newPartSizeAdjustment = EditorGUILayout.Slider("サイズ倍率", partSizeAdjustments[selectedPart], 0.5f, 2.0f);
            }
            
            // 位置調整
            EditorGUILayout.Space(5);
            GUILayout.Label("位置調整", EditorStyles.boldLabel);
            Vector3 newPositionAdjustment;
            
            if (selectedPart == "全体")
            {
                newPositionAdjustment = new Vector3(
                    EditorGUILayout.Slider("X位置", positionAdjustment.x, -0.5f, 0.5f),
                    EditorGUILayout.Slider("Y位置", positionAdjustment.y, -0.5f, 0.5f),
                    EditorGUILayout.Slider("Z位置", positionAdjustment.z, -0.5f, 0.5f)
                );
            }
            else
            {
                newPositionAdjustment = new Vector3(
                    EditorGUILayout.Slider("X位置", partPositionAdjustments[selectedPart].x, -0.5f, 0.5f),
                    EditorGUILayout.Slider("Y位置", partPositionAdjustments[selectedPart].y, -0.5f, 0.5f),
                    EditorGUILayout.Slider("Z位置", partPositionAdjustments[selectedPart].z, -0.5f, 0.5f)
                );
            }
            
            // 回転調整（全体のみ）
            EditorGUILayout.Space(5);
            GUILayout.Label("回転調整（全体）", EditorStyles.boldLabel);
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
                bool sizeChanged = false;
                bool positionChanged = false;
                bool rotationChanged = rotationAdjustment != newRotationAdjustment;

                // 選択された部位に基づいて調整
                if (selectedPart == "全体")
                {
                    sizeChanged = sizeAdjustment != newPartSizeAdjustment;
                    positionChanged = positionAdjustment != newPositionAdjustment;
                    
                    // 全体の調整値を更新
                    sizeAdjustment = newPartSizeAdjustment;
                    positionAdjustment = newPositionAdjustment;
                }
                else
                {
                    sizeChanged = partSizeAdjustments[selectedPart] != newPartSizeAdjustment;
                    positionChanged = partPositionAdjustments[selectedPart] != newPositionAdjustment;
                    
                    // 部位ごとの調整値を更新
                    partSizeAdjustments[selectedPart] = newPartSizeAdjustment;
                    partPositionAdjustments[selectedPart] = newPositionAdjustment;
                }

                // 回転は常に全体に適用
                rotationAdjustment = newRotationAdjustment;
                
                // 調整を適用
                if (selectedPart == "全体")
                {
                    UpdateGlobalAdjustment(
                        clothingObject,
                        avatarObject,
                        sizeAdjustment,
                        positionAdjustment,
                        rotationAdjustment,
                        ref lastSizeAdjustment,
                        sizeChanged,
                        positionChanged,
                        rotationChanged
                    );
                }
                else
                {
                    UpdatePartAdjustment(
                        clothingObject,
                        selectedPart,
                        partSizeAdjustments[selectedPart],
                        partPositionAdjustments[selectedPart],
                        rotationAdjustment,
                        sizeChanged,
                        positionChanged,
                        rotationChanged
                    );
                }
            }
            
            EditorGUILayout.Space(10);
            if (GUILayout.Button("調整をリセット", GUILayout.Height(25)))
            {
                ResetFineAdjustment(
                    clothingObject,
                    avatarObject,
                    ref sizeAdjustment,
                    ref positionAdjustment,
                    ref rotationAdjustment,
                    ref fineAdjustmentStarted,
                    globalScaleFactor
                );
                statusMessage = "微調整をリセットしました。";
            }
            
            if (GUILayout.Button("調整を確定", GUILayout.Height(25)))
            {
                FinalizeFineAdjustment(
                    clothingObject,
                    sizeAdjustment,
                    positionAdjustment,
                    rotationAdjustment,
                    ref fineAdjustmentStarted
                );
                statusMessage = "微調整が確定されました。";
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 全体的な微調整の変更をリアルタイムで適用
        /// </summary>
        private void UpdateGlobalAdjustment(
            GameObject clothingObject,
            GameObject avatarObject,
            float sizeAdjustment,
            Vector3 positionAdjustment,
            Vector3 rotationAdjustment,
            ref float lastSizeAdjustment,
            bool sizeChanged,
            bool positionChanged,
            bool rotationChanged)
        {
            if (clothingObject == null || avatarObject == null) return;
            
            // サイズ調整
            if (sizeChanged)
            {
                // サイズを調整する新しいユーティリティを使用
                MeshTransformation.AdjustClothingSize(clothingObject, sizeAdjustment);
                
                // 現在のサイズ調整値を保存
                lastSizeAdjustment = sizeAdjustment;
            }
            
            // 位置調整
            if (positionChanged)
            {
                // 位置を調整する新しいユーティリティを使用
                Vector3 newPosition = avatarObject.transform.position + positionAdjustment;
                clothingObject.transform.position = newPosition;
            }
            
            // 回転調整
            if (rotationChanged)
            {
                // 回転を調整する新しいユーティリティを使用
                Quaternion newRotation = avatarObject.transform.rotation * Quaternion.Euler(rotationAdjustment);
                clothingObject.transform.rotation = newRotation;
            }
            
            // シーンビューの更新
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// 部位ごとの微調整を適用
        /// </summary>
        private void UpdatePartAdjustment(
            GameObject clothingObject,
            string partName,
            float partSizeAdjustment,
            Vector3 partPositionAdjustment,
            Vector3 rotationAdjustment,
            bool sizeChanged,
            bool positionChanged,
            bool rotationChanged)
        {
            if (clothingObject == null) return;
            
            // 関連するボーンを検索
            List<Transform> partBones = FindPartBones(clothingObject, partName);
            
            if (partBones.Count == 0)
            {
                Debug.LogWarning($"部位 '{partName}' に対応するボーンが見つかりませんでした。");
                return;
            }
            
            // 該当部位のボーンに対してサイズ調整を適用
            if (sizeChanged)
            {
                foreach (Transform bone in partBones)
                {
                    // ボーンのスケールを調整
                    Undo.RecordObject(bone, "Part Size Adjustment");
                    bone.localScale = Vector3.one * partSizeAdjustment;
                }
            }
            
            // 該当部位のボーンに対して位置調整を適用
            if (positionChanged)
            {
                // 親ボーンを特定
                Transform parentBone = FindParentBone(partBones);
                
                if (parentBone != null)
                {
                    // 親ボーンの位置を調整
                    Undo.RecordObject(parentBone, "Part Position Adjustment");
                    parentBone.position += new Vector3(
                        partPositionAdjustment.x,
                        partPositionAdjustment.y,
                        partPositionAdjustment.z
                    );
                }
                else
                {
                    // 親ボーンが特定できない場合は、各ボーンを個別に調整
                    foreach (Transform bone in partBones)
                    {
                        Undo.RecordObject(bone, "Part Position Adjustment");
                        bone.position += new Vector3(
                            partPositionAdjustment.x / partBones.Count,
                            partPositionAdjustment.y / partBones.Count,
                            partPositionAdjustment.z / partBones.Count
                        );
                    }
                }
            }
            
            // 部分的な回転調整はより複雑なため、ここでは全体的な回転のみを適用（必要に応じて実装）
            // 回転は全体に対してのみ適用するため、ここでは何もしない
            
            // シーンビューの更新
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// 部位に関連するボーンを検索
        /// </summary>
        private List<Transform> FindPartBones(GameObject clothingObject, string partName)
        {
            List<Transform> partBones = new List<Transform>();
            
            // 部位名に対応するボーン名の配列を取得
            if (!bodyPartBonesMap.TryGetValue(partName, out string[] boneNames))
            {
                return partBones; // 空のリストを返す
            }
            
            // 衣装オブジェクトのすべてのボーンを取得
            Transform[] allBones = clothingObject.GetComponentsInChildren<Transform>();
            
            // ボーン名に基づいてフィルタリング
            foreach (Transform bone in allBones)
            {
                string boneName = bone.name.ToLower();
                
                // いずれかのボーン名パターンが含まれるか確認
                if (boneNames.Any(pattern => boneName.Contains(pattern.ToLower())))
                {
                    partBones.Add(bone);
                }
            }
            
            return partBones;
        }
        
        /// <summary>
        /// 部位の親ボーンを特定
        /// </summary>
        private Transform FindParentBone(List<Transform> partBones)
        {
            if (partBones.Count == 0) return null;
            
            // もっとも階層が浅いボーンを親として選択
            Transform parentBone = partBones.OrderBy(b => GetHierarchyDepth(b)).FirstOrDefault();
            
            return parentBone;
        }
        
        /// <summary>
        /// ボーンの階層の深さを取得
        /// </summary>
        private int GetHierarchyDepth(Transform bone)
        {
            int depth = 0;
            Transform current = bone;
            
            while (current.parent != null)
            {
                depth++;
                current = current.parent;
            }
            
            return depth;
        }
        
        /// <summary>
        /// 微調整をリセット
        /// </summary>
        private void ResetFineAdjustment(
            GameObject clothingObject,
            GameObject avatarObject,
            ref float sizeAdjustment,
            ref Vector3 positionAdjustment,
            ref Vector3 rotationAdjustment,
            ref bool fineAdjustmentStarted,
            float globalScaleFactor)
        {
            sizeAdjustment = 1.0f;
            positionAdjustment = Vector3.zero;
            rotationAdjustment = Vector3.zero;
            
            // 部位ごとの調整値もリセット
            foreach (string part in bodyParts)
            {
                partSizeAdjustments[part] = 1.0f;
                partPositionAdjustments[part] = Vector3.zero;
            }
            
            if (clothingObject != null && avatarObject != null)
            {
                // 元の位置・回転・スケールに戻す
                Undo.RecordObject(clothingObject.transform, "Reset Fine Adjustment");
                
                clothingObject.transform.position = avatarObject.transform.position;
                clothingObject.transform.rotation = avatarObject.transform.rotation;
                
                // スケールは直接設定
                Vector3 baseScale = Vector3.one * globalScaleFactor;
                clothingObject.transform.localScale = baseScale;
                
                // 各ボーンのスケールをリセット
                ResetAllBonesScale(clothingObject);
                
                // 微調整開始フラグをリセット
                fineAdjustmentStarted = false;
                
                // シーンビューを更新
                SceneView.RepaintAll();
            }
        }
        
        /// <summary>
        /// すべてのボーンのスケールをリセット
        /// </summary>
        private void ResetAllBonesScale(GameObject clothingObject)
        {
            Transform[] allBones = clothingObject.GetComponentsInChildren<Transform>();
            
            foreach (Transform bone in allBones)
            {
                if (bone == clothingObject.transform) continue; // ルートは除外
                
                Undo.RecordObject(bone, "Reset Bone Scale");
                bone.localScale = Vector3.one;
            }
        }
        
        /// <summary>
        /// 微調整を確定
        /// </summary>
        private void FinalizeFineAdjustment(
            GameObject clothingObject,
            float sizeAdjustment,
            Vector3 positionAdjustment,
            Vector3 rotationAdjustment,
            ref bool fineAdjustmentStarted)
        {
            if (clothingObject == null) return;
            
            // 調整結果を確定（アンドゥポイントを設定）
            Undo.RecordObject(clothingObject.transform, "Finalize Fine Adjustment");
            
            // 各ボーンの調整も確定
            Transform[] allBones = clothingObject.GetComponentsInChildren<Transform>();
            foreach (Transform bone in allBones)
            {
                if (bone == clothingObject.transform) continue; // ルートは除外
                Undo.RecordObject(bone, "Finalize Bone Adjustment");
            }
            
            // 微調整開始フラグをリセット
            fineAdjustmentStarted = false;
            
            // 現在の調整を確定（アセットとして保存するなど）
            string adjustmentSummary = "現在の微調整設定が確定されました。\n\n";
            adjustmentSummary += $"全体サイズ: {sizeAdjustment}\n";
            adjustmentSummary += $"全体位置: {positionAdjustment.ToString("F2")}\n";
            adjustmentSummary += $"全体回転: {rotationAdjustment.ToString("F1")}\n\n";
            
            // 部位ごとの調整値を表示
            adjustmentSummary += "部位ごとの調整:\n";
            foreach (string part in bodyParts)
            {
                if (part == "全体") continue;
                if (partSizeAdjustments[part] != 1.0f || partPositionAdjustments[part] != Vector3.zero)
                {
                    adjustmentSummary += $"{part} - サイズ: {partSizeAdjustments[part]}, 位置: {partPositionAdjustments[part].ToString("F2")}\n";
                }
            }
            
            EditorUtility.DisplayDialog("微調整確定", adjustmentSummary, "OK");
        }
        
        /// <summary>
        /// 現在の微調整設定を取得
        /// </summary>
        public void GetCurrentAdjustment(
            out float size,
            out Vector3 position,
            out Vector3 rotation)
        {
            size = 1.0f;
            position = Vector3.zero;
            rotation = Vector3.zero;
        }
        
        /// <summary>
        /// プリセットから微調整を適用
        /// </summary>
        public void ApplyPreset(
            GameObject clothingObject,
            GameObject avatarObject,
            float presetSize,
            Vector3 presetPosition,
            Vector3 presetRotation,
            ref float sizeAdjustment,
            ref Vector3 positionAdjustment,
            ref Vector3 rotationAdjustment,
            ref float lastSizeAdjustment)
        {
            if (clothingObject == null || avatarObject == null) return;
            
            // Undo登録
            Undo.RecordObject(clothingObject.transform, "Apply Adjustment Preset");
            
            // プリセット値を適用
            sizeAdjustment = presetSize;
            positionAdjustment = presetPosition;
            rotationAdjustment = presetRotation;
            
            // サイズ調整
            MeshTransformation.AdjustClothingSize(clothingObject, sizeAdjustment);
            lastSizeAdjustment = sizeAdjustment;
            
            // 位置調整
            clothingObject.transform.position = avatarObject.transform.position + positionAdjustment;
            
            // 回転調整
            clothingObject.transform.rotation = avatarObject.transform.rotation * Quaternion.Euler(rotationAdjustment);
            
            // シーンビューの更新
            SceneView.RepaintAll();
        }
    }
}
