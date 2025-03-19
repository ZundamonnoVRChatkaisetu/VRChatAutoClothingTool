using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace VRChatAutoClothingTool
{
    /// <summary>
    /// 衣装の自動調整機能を提供するクラス
    /// </summary>
    public class ClothingAdjuster
    {
        /// <summary>
        /// 衣装を自動調整する
        /// </summary>
        public bool AdjustClothing(
            GameObject avatarObject,
            GameObject clothingObject,
            List<BoneMapping> boneMappings,
            float globalScaleFactor,
            bool enablePenetrationCheck,
            float penetrationPushOutDistance,
            float penetrationThreshold,
            bool useAdvancedSampling,
            bool preferBodyMeshes,
            bool preserveMeshShape = true,
            float preserveStrength = 0.5f)
        {
            if (avatarObject == null || clothingObject == null || boneMappings == null)
                return false;

            try
            {
                // Undo登録
                Undo.RegisterFullObjectHierarchyUndo(clothingObject, "Auto Adjust Clothing");
                
                // 元の親を保存
                Transform originalParent = clothingObject.transform.parent;
                
                // 衣装を一時的にアバターの子オブジェクトにする
                clothingObject.transform.parent = avatarObject.transform;
                
                // グローバルスケールの適用
                Vector3 scaleFactor = new Vector3(globalScaleFactor, globalScaleFactor, globalScaleFactor);
                
                // ルート位置の調整
                clothingObject.transform.localPosition = Vector3.zero;
                clothingObject.transform.localRotation = Quaternion.identity;
                clothingObject.transform.localScale = scaleFactor; // 直接設定
                
                // 各ボーンのマッピングに基づいて調整
                AdjustBones(boneMappings, scaleFactor);
                
                // メッシュアセットを作成
                AdjustMeshes(clothingObject);
                
                // 貫通チェックが有効な場合のみ処理を実行
                if (enablePenetrationCheck)
                {
                    // 衣装とアバターの貫通をチェックして調整
                    PenetrationDetection.AdjustClothingPenetration(
                        avatarObject,
                        clothingObject,
                        penetrationPushOutDistance,
                        penetrationThreshold,
                        useAdvancedSampling,
                        preferBodyMeshes,
                        preserveMeshShape,
                        preserveStrength
                    );
                }
                
                // 調整完了後に衣装を親から解除（元の親に戻すか、親がなければnull）
                clothingObject.transform.parent = originalParent;
                
                // 親が無い場合は位置をアバターに合わせる
                if (clothingObject.transform.parent == null)
                {
                    clothingObject.transform.position = avatarObject.transform.position;
                    clothingObject.transform.rotation = avatarObject.transform.rotation;
                }
                
                // すべてのレンダラーが表示されていることを確認
                EnsureRenderersVisible(clothingObject);
                
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"衣装調整中にエラーが発生: {e.Message}\n{e.StackTrace}");
                
                // エラー発生時は衣装を元に戻す
                if (clothingObject != null)
                {
                    clothingObject.transform.parent = null;
                    EnsureRenderersVisible(clothingObject);
                }
                
                return false;
            }
        }
        
        /// <summary>
        /// レンダラーが表示されていることを確認
        /// </summary>
        private void EnsureRenderersVisible(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                // レンダラーを有効化
                renderer.enabled = true;
                
                // マテリアルの透明度をチェック
                var materials = renderer.sharedMaterials;
                bool materialsChanged = false;
                
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null)
                    {
                        // 透明度の確認と修正
                        Color color = materials[i].color;
                        if (color.a < 0.9f) // 透明度が低い場合
                        {
                            // 新しいマテリアルを作成して不透明に
                            Material newMat = new Material(materials[i]);
                            newMat.color = new Color(color.r, color.g, color.b, 1.0f);
                            
                            // レンダリングモードを確認して透明設定を解除
                            if (newMat.HasProperty("_Mode"))
                            {
                                newMat.SetFloat("_Mode", 0); // Opaque
                                newMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                                newMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                                newMat.SetInt("_ZWrite", 1);
                                newMat.DisableKeyword("_ALPHATEST_ON");
                                newMat.DisableKeyword("_ALPHABLEND_ON");
                                newMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                                newMat.renderQueue = -1; // デフォルト
                            }
                            
                            materials[i] = newMat;
                            materialsChanged = true;
                        }
                    }
                }
                
                // マテリアルが変更された場合のみ適用
                if (materialsChanged)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }
        
        /// <summary>
        /// ボーンを調整
        /// </summary>
        private void AdjustBones(List<BoneMapping> boneMappings, Vector3 scaleFactor)
        {
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
        }
        
        /// <summary>
        /// メッシュを調整してアセットとして保存
        /// </summary>
        private void AdjustMeshes(GameObject clothingObject)
        {
            // 衣装のメッシュレンダラーを取得し、スキンメッシュのバインドポーズを再計算
            var skinRenderers = clothingObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in skinRenderers)
            {
                if (renderer.sharedMesh != null)
                {
                    // メッシュを複製して編集可能にする
                    Mesh meshCopy = Object.Instantiate(renderer.sharedMesh);
                    meshCopy.name = renderer.sharedMesh.name + "_Adjusted";
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
        }
        
        /// <summary>
        /// 衣装調整のプレビューを表示
        /// </summary>
        public void PreviewAdjustment(
            GameObject avatarObject,
            GameObject clothingObject,
            List<BoneMapping> boneMappings,
            float globalScaleFactor,
            bool enablePenetrationCheck,
            float penetrationPushOutDistance,
            float penetrationThreshold,
            bool useAdvancedSampling,
            bool preferBodyMeshes,
            bool preserveMeshShape = true,
            float preserveStrength = 0.5f)
        {
            if (avatarObject == null || clothingObject == null || boneMappings == null)
                return;

            // プレビュー用の一時的なオブジェクトを作成
            GameObject previewObject = Object.Instantiate(clothingObject);
            previewObject.name = clothingObject.name + " (Preview)";
            
            // プレビューオブジェクトを一時的にアバターの子オブジェクトにする
            previewObject.transform.parent = avatarObject.transform;
            
            // スケールの適用
            Vector3 scaleFactor = new Vector3(globalScaleFactor, globalScaleFactor, globalScaleFactor);
            
            // ルート位置の調整
            previewObject.transform.localPosition = Vector3.zero;
            previewObject.transform.localRotation = Quaternion.identity;
            previewObject.transform.localScale = scaleFactor; // 直接設定
            
            // プレビューオブジェクトのボーンを取得
            var previewTransforms = previewObject.GetComponentsInChildren<Transform>();
            
            // 各ボーンのマッピングに基づいて調整
            foreach (var mapping in boneMappings)
            {
                if (mapping.AvatarBone != null && mapping.ClothingBone != null)
                {
                    // プレビューオブジェクトの対応するボーンを検索
                    var previewBoneName = mapping.ClothingBone.name;
                    var previewBone = System.Array.Find(previewTransforms, t => t.name == previewBoneName);
                    
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
            
            // 貫通チェックが有効な場合のみ処理を実行
            if (enablePenetrationCheck)
            {
                // 貫通チェック
                PenetrationDetection.AdjustClothingPenetration(
                    avatarObject,
                    previewObject,
                    penetrationPushOutDistance,
                    penetrationThreshold,
                    useAdvancedSampling,
                    preferBodyMeshes,
                    preserveMeshShape,
                    preserveStrength
                );
            }
            
            // すべてのレンダラーが表示されていることを確認
            EnsureRenderersVisible(previewObject);
            
            // プレビューオブジェクトに半透明マテリアルを適用
            ApplyPreviewMaterials(previewObject);
            
            // 30秒後にプレビューを自動削除するコルーチン
            EditorApplication.delayCall += () =>
            {
                // 30秒後に実行
                EditorApplication.delayCall += () =>
                {
                    if (previewObject != null)
                    {
                        Object.DestroyImmediate(previewObject);
                    }
                };
            };
            
            // シーンビューにフォーカス
            SceneView.lastActiveSceneView.FrameSelected();
        }
        
        /// <summary>
        /// プレビュー用の半透明マテリアルを適用
        /// </summary>
        private void ApplyPreviewMaterials(GameObject previewObject)
        {
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
        }
    }
}
