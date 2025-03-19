using UnityEngine;
using UnityEditor;

namespace VRChatAutoClothingTool
{
    /// <summary>
    /// 衣装の微調整機能を提供するクラス
    /// </summary>
    public class FineAdjustmentHandler
    {
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
            
            // サイズ調整
            EditorGUILayout.Space(5);
            GUILayout.Label("サイズ調整", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
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
                
                // サイズ値を更新
                sizeAdjustment = newSizeAdjustment;
                positionAdjustment = newPositionAdjustment;
                rotationAdjustment = newRotationAdjustment;
                
                UpdateFineAdjustment(
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
        /// 微調整の変更をリアルタイムで適用
        /// </summary>
        private void UpdateFineAdjustment(
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
            
            if (clothingObject != null && avatarObject != null)
            {
                // 元の位置・回転・スケールに戻す
                Undo.RecordObject(clothingObject.transform, "Reset Fine Adjustment");
                
                clothingObject.transform.position = avatarObject.transform.position;
                clothingObject.transform.rotation = avatarObject.transform.rotation;
                
                // スケールは直接設定
                Vector3 baseScale = Vector3.one * globalScaleFactor;
                clothingObject.transform.localScale = baseScale;
                
                // 微調整開始フラグをリセット
                fineAdjustmentStarted = false;
                
                // シーンビューを更新
                SceneView.RepaintAll();
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
            
            // 微調整開始フラグをリセット
            fineAdjustmentStarted = false;
            
            // 現在の調整を確定（アセットとして保存するなど）
            EditorUtility.DisplayDialog("微調整確定", 
                "現在の微調整設定が確定されました。\n\nサイズ: " + sizeAdjustment + 
                "\n位置: " + positionAdjustment.ToString("F2") + 
                "\n回転: " + rotationAdjustment.ToString("F1"), "OK");
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
