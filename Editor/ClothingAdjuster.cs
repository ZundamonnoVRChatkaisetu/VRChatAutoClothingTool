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
        /// リアルタイムプレビューモード
        /// </summary>
        private bool isRealTimePreview = false;
        
        /// <summary>
        /// プレビューオブジェクト
        /// </summary>
        private GameObject currentPreviewObject = null;
        
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
                
                // BoneStructureAnalyzerのインスタンスを取得
                var boneAnalyzer = boneMappings.Count > 0 && boneMappings[0] is BoneMapping ? 
                    (boneMappings[0] as BoneMapping).SourceAnalyzer : 
                    new BoneStructureAnalyzer();
                
                // 各ボーンのマッピングに基づいて調整
                AdjustBones(boneMappings, scaleFactor);
                
                // 未マッピングボーンの処理 (相対位置を維持)
                AdjustUnmappedBones(boneAnalyzer);
                
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
        /// 未マッピングボーンを処理（相対位置を維持）
        /// </summary>
        private void AdjustUnmappedBones(BoneStructureAnalyzer boneAnalyzer)
        {
            foreach (var unmappedInfo in boneAnalyzer.UnmappedBoneInfos)
            {
                var info = unmappedInfo.Value;
                
                if (info.BoneTransform != null && info.ParentBoneTransform != null)
                {
                    // 親ボーンからの相対位置を維持
                    Vector3 newPosition = info.ParentBoneTransform.TransformPoint(info.RelativePosition);
                    info.BoneTransform.position = newPosition;
                    
                    // 親ボーンからの相対回転を維持
                    Quaternion newRotation = info.ParentBoneTransform.rotation * info.RelativeRotation;
                    info.BoneTransform.rotation = newRotation;
                    
                    // ローカルスケールを維持
                    info.BoneTransform.localScale = info.LocalScale;
                }
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
                
                // 空のマテリアル配列をチェック
                if (materials == null || materials.Length == 0)
                {
                    Debug.LogWarning($"オブジェクト '{obj.name}' のレンダラー '{renderer.name}' にマテリアルがありません。デフォルトマテリアルを適用します。");
                    Material defaultMat = new Material(Shader.Find("Standard"));
                    defaultMat.color = Color.white;
                    renderer.sharedMaterial = defaultMat;
                    continue;
                }
                
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null)
                    {
                        // nullマテリアルでないかチェック
                        try
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
                            
                            // シェーダーの透明設定を確認
                            if (materials[i].shader != null && 
                               (materials[i].shader.name.Contains("Transparent") || 
                                materials[i].shader.name.Contains("Fade") || 
                                materials[i].shader.name.Contains("Cutout")))
                            {
                                // 透明シェーダーを使用している場合は、Standard不透明シェーダーに変更
                                Material newMat = new Material(Shader.Find("Standard"));
                                newMat.color = materials[i].color;
                                // アルファ値を1に設定
                                Color newColor = newMat.color;
                                newColor.a = 1.0f;
                                newMat.color = newColor;
                                
                                materials[i] = newMat;
                                materialsChanged = true;
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"マテリアル処理中にエラー: {e.Message}");
                            // エラーが発生した場合はデフォルトマテリアルを使用
                            materials[i] = new Material(Shader.Find("Standard"));
                            materials[i].color = Color.white;
                            materialsChanged = true;
                        }
                    }
                    else
                    {
                        // nullマテリアルを検出した場合はデフォルトマテリアルを使用
                        materials[i] = new Material(Shader.Find("Standard"));
                        materials[i].color = Color.white;
                        materialsChanged = true;
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
            // 標準ボーンのマッピングを先に処理
            var standardBoneMappings = boneMappings.FindAll(m => !m.IsUnmapped);
            foreach (var mapping in standardBoneMappings)
            {
                AdjustBone(mapping, scaleFactor);
            }
        }

        /// <summary>
        /// 個別のボーンを調整
        /// </summary>
        private void AdjustBone(BoneMapping mapping, Vector3 scaleFactor)
        {
            if (mapping.AvatarBone != null && mapping.ClothingBone != null)
            {
                // ボーンの位置と回転を合わせる
                mapping.ClothingBone.position = mapping.AvatarBone.position;
                mapping.ClothingBone.rotation = mapping.AvatarBone.rotation;
                
                // スケールの調整（オプション）
                if (mapping.BoneName.Contains("Hips") || mapping.BoneName.Contains("Spine") || mapping.BoneName.Contains("Chest"))
                {
                    // トランスフォームツリーの異なるパスで同じボーンが参照される可能性があるため、
                    // ローカルスケールを慎重に調整
                    mapping.ClothingBone.localScale = new Vector3(
                        mapping.ClothingBone.localScale.x * scaleFactor.x,
                        mapping.ClothingBone.localScale.y * scaleFactor.y,
                        mapping.ClothingBone.localScale.z * scaleFactor.z
                    );
                }
            }
        }
        
        /// <summary>
        /// 装飾品や非標準ボーンかどうかを判定
        /// </summary>
        private bool IsCustomBone(string boneName)
        {
            BoneStructureAnalyzer analyzer = new BoneStructureAnalyzer();
            return analyzer.IsDecorationBone(boneName);
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
                    
                    // アセットフォルダの作成
                    string assetPath = $"Assets/AdjustedMeshes/{clothingObject.name}_{renderer.name}_Adjusted.asset";
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
                    
                    // ボーン階層を維持するための処理を追加
                    UpdateBoneBindings(renderer);
                }
            }
        }
        
        /// <summary>
        /// スキンメッシュレンダラーのボーンバインディングを更新
        /// </summary>
        private void UpdateBoneBindings(SkinnedMeshRenderer renderer)
        {
            if (renderer.sharedMesh == null || renderer.bones == null || renderer.bones.Length == 0)
                return;
            
            // バインドポーズを取得
            Matrix4x4[] bindPoses = renderer.sharedMesh.bindposes;
            
            // 各ボーンの行列を更新
            for (int i = 0; i < renderer.bones.Length && i < bindPoses.Length; i++)
            {
                Transform bone = renderer.bones[i];
                
                // ボーンが存在する場合のみ処理
                if (bone != null)
                {
                    // アバターの世界空間からボーンのローカル空間への変換行列を計算
                    bindPoses[i] = bone.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                }
            }
            
            // 更新したバインドポーズを適用
            renderer.sharedMesh.bindposes = bindPoses;
        }
        
        /// <summary>
        /// スケーリングのリアルタイムプレビューを開始/更新する
        /// </summary>
        public GameObject UpdateScalingPreview(
            GameObject avatarObject,
            GameObject clothingObject,
            List<BoneMapping> boneMappings,
            float globalScaleFactor)
        {
            if (avatarObject == null || clothingObject == null || boneMappings == null)
                return null;
                
            // すでにプレビューオブジェクトが存在する場合は削除
            if (currentPreviewObject != null)
            {
                Object.DestroyImmediate(currentPreviewObject);
                currentPreviewObject = null;
            }
            
            // プレビュー用の一時的なオブジェクトを作成
            currentPreviewObject = Object.Instantiate(clothingObject);
            currentPreviewObject.name = clothingObject.name + " (Preview)";
            
            // プレビューオブジェクトを一時的にアバターの子オブジェクトにする
            currentPreviewObject.transform.parent = avatarObject.transform;
            
            // スケールの適用
            Vector3 scaleFactor = new Vector3(globalScaleFactor, globalScaleFactor, globalScaleFactor);
            
            // ルート位置の調整
            currentPreviewObject.transform.localPosition = Vector3.zero;
            currentPreviewObject.transform.localRotation = Quaternion.identity;
            currentPreviewObject.transform.localScale = scaleFactor; // 直接設定
            
            // BoneStructureAnalyzerのインスタンスを取得
            var boneAnalyzer = boneMappings.Count > 0 && boneMappings[0] is BoneMapping ? 
                (boneMappings[0] as BoneMapping).SourceAnalyzer : 
                new BoneStructureAnalyzer();
            
            // プレビューオブジェクトのボーンを取得
            var previewTransforms = currentPreviewObject.GetComponentsInChildren<Transform>();
            
            // 各ボーンのマッピングに基づいて調整
            // 実際のボーンではなくプレビューのボーンに適用
            foreach (var mapping in boneMappings)
            {
                if (mapping.AvatarBone != null && mapping.ClothingBone != null && !mapping.IsUnmapped)
                {
                    // プレビューオブジェクトの対応するボーンを検索
                    string boneName = mapping.ClothingBone.name;
                    Transform previewBone = System.Array.Find(previewTransforms, t => t.name == boneName);
                    
                    if (previewBone != null)
                    {
                        // ボーンの位置と回転を合わせる
                        previewBone.position = mapping.AvatarBone.position;
                        previewBone.rotation = mapping.AvatarBone.rotation;
                        
                        // スケールの調整（オプション）
                        if (mapping.BoneName.Contains("Hips") || mapping.BoneName.Contains("Spine") || mapping.BoneName.Contains("Chest"))
                        {
                            previewBone.localScale = new Vector3(
                                previewBone.localScale.x * scaleFactor.x,
                                previewBone.localScale.y * scaleFactor.y,
                                previewBone.localScale.z * scaleFactor.z
                            );
                        }
                    }
                }
            }
            
            // 未マッピングボーンの処理 - プレビュー用のバージョン
            foreach (var unmappedInfo in boneAnalyzer.UnmappedBoneInfos)
            {
                var info = unmappedInfo.Value;
                string boneName = info.BoneTransform.name;
                
                // プレビューの対応するボーンを見つける
                Transform previewBone = System.Array.Find(previewTransforms, t => t.name == boneName);
                
                if (previewBone != null)
                {
                    // 対応する親ボーンをプレビューオブジェクトから見つける
                    string parentName = info.ParentBoneTransform.name;
                    Transform previewParent = System.Array.Find(previewTransforms, t => t.name == parentName);
                    
                    if (previewParent != null)
                    {
                        // 親ボーンからの相対位置を維持
                        Vector3 newPosition = previewParent.TransformPoint(info.RelativePosition);
                        previewBone.position = newPosition;
                        
                        // 親ボーンからの相対回転を維持
                        Quaternion newRotation = previewParent.rotation * info.RelativeRotation;
                        previewBone.rotation = newRotation;
                        
                        // ローカルスケールを維持
                        previewBone.localScale = info.LocalScale;
                    }
                }
            }
            
            // プレビューオブジェクトに半透明マテリアルを適用
            ApplyPreviewMaterials(currentPreviewObject);
            
            return currentPreviewObject;
        }
        
        /// <summary>
        /// プレビューモードを終了する
        /// </summary>
        public void EndPreview()
        {
            if (currentPreviewObject != null)
            {
                Object.DestroyImmediate(currentPreviewObject);
                currentPreviewObject = null;
            }
            
            isRealTimePreview = false;
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
            if (currentPreviewObject != null)
            {
                Object.DestroyImmediate(currentPreviewObject);
            }
            
            currentPreviewObject = Object.Instantiate(clothingObject);
            currentPreviewObject.name = clothingObject.name + " (Preview)";
            
            // プレビューオブジェクトを一時的にアバターの子オブジェクトにする
            currentPreviewObject.transform.parent = avatarObject.transform;
            
            // スケールの適用
            Vector3 scaleFactor = new Vector3(globalScaleFactor, globalScaleFactor, globalScaleFactor);
            
            // ルート位置の調整
            currentPreviewObject.transform.localPosition = Vector3.zero;
            currentPreviewObject.transform.localRotation = Quaternion.identity;
            currentPreviewObject.transform.localScale = scaleFactor; // 直接設定
            
            // BoneStructureAnalyzerのインスタンスを取得
            var boneAnalyzer = boneMappings.Count > 0 && boneMappings[0] is BoneMapping ? 
                (boneMappings[0] as BoneMapping).SourceAnalyzer : 
                new BoneStructureAnalyzer();
            
            // プレビューオブジェクトのボーンを取得
            var previewTransforms = currentPreviewObject.GetComponentsInChildren<Transform>();
            
            // 各ボーンのマッピングに基づいて調整
            // 標準ボーンを先に処理
            foreach (var mapping in boneMappings)
            {
                if (mapping.AvatarBone != null && mapping.ClothingBone != null && !mapping.IsUnmapped)
                {
                    // プレビューオブジェクトの対応するボーンを検索
                    string boneName = mapping.ClothingBone.name;
                    Transform previewBone = System.Array.Find(previewTransforms, t => t.name == boneName);
                    
                    if (previewBone != null)
                    {
                        // ボーンの位置と回転を合わせる
                        previewBone.position = mapping.AvatarBone.position;
                        previewBone.rotation = mapping.AvatarBone.rotation;
                        
                        // スケールの調整（オプション）
                        if (mapping.BoneName.Contains("Hips") || mapping.BoneName.Contains("Spine") || mapping.BoneName.Contains("Chest"))
                        {
                            previewBone.localScale = new Vector3(
                                previewBone.localScale.x * scaleFactor.x,
                                previewBone.localScale.y * scaleFactor.y,
                                previewBone.localScale.z * scaleFactor.z
                            );
                        }
                    }
                }
            }
            
            // 未マッピングボーンの処理 - プレビュー用のバージョン
            foreach (var unmappedInfo in boneAnalyzer.UnmappedBoneInfos)
            {
                var info = unmappedInfo.Value;
                string boneName = info.BoneTransform.name;
                
                // プレビューの対応するボーンを見つける
                Transform previewBone = System.Array.Find(previewTransforms, t => t.name == boneName);
                
                if (previewBone != null)
                {
                    // 対応する親ボーンをプレビューオブジェクトから見つける
                    string parentName = info.ParentBoneTransform.name;
                    Transform previewParent = System.Array.Find(previewTransforms, t => t.name == parentName);
                    
                    if (previewParent != null)
                    {
                        // 親ボーンからの相対位置を維持
                        Vector3 newPosition = previewParent.TransformPoint(info.RelativePosition);
                        previewBone.position = newPosition;
                        
                        // 親ボーンからの相対回転を維持
                        Quaternion newRotation = previewParent.rotation * info.RelativeRotation;
                        previewBone.rotation = newRotation;
                        
                        // ローカルスケールを維持
                        previewBone.localScale = info.LocalScale;
                    }
                }
            }
            
            // 貫通チェックが有効な場合のみ処理を実行
            if (enablePenetrationCheck)
            {
                // 貫通チェック
                PenetrationDetection.AdjustClothingPenetration(
                    avatarObject,
                    currentPreviewObject,
                    penetrationPushOutDistance,
                    penetrationThreshold,
                    useAdvancedSampling,
                    preferBodyMeshes,
                    preserveMeshShape,
                    preserveStrength
                );
            }
            
            // すべてのレンダラーが表示されていることを確認
            EnsureRenderersVisible(currentPreviewObject);
            
            // プレビューオブジェクトに半透明マテリアルを適用
            ApplyPreviewMaterials(currentPreviewObject);
            
            // 30秒後にプレビューを自動削除するコルーチン
            EditorApplication.delayCall += () =>
            {
                // 30秒後に実行
                EditorApplication.delayCall += () =>
                {
                    if (currentPreviewObject != null && !isRealTimePreview)
                    {
                        Object.DestroyImmediate(currentPreviewObject);
                        currentPreviewObject = null;
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
                
                // 空のマテリアル配列をチェック
                if (materials == null || materials.Length == 0)
                {
                    Material defaultMat = new Material(Shader.Find("Standard"));
                    defaultMat.color = new Color(1f, 1f, 1f, 0.5f);
                    defaultMat.SetFloat("_Mode", 3); // Transparent
                    defaultMat.renderQueue = 3000;
                    defaultMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    defaultMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    defaultMat.SetInt("_ZWrite", 0);
                    defaultMat.DisableKeyword("_ALPHATEST_ON");
                    defaultMat.EnableKeyword("_ALPHABLEND_ON");
                    defaultMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    
                    renderer.sharedMaterial = defaultMat;
                    continue;
                }
                
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null)
                    {
                        try
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
                        catch
                        {
                            // エラーが発生した場合は新しいマテリアルを作成
                            Material defaultMat = new Material(Shader.Find("Standard"));
                            defaultMat.color = new Color(1f, 1f, 1f, 0.5f);
                            defaultMat.SetFloat("_Mode", 3); // Transparent
                            defaultMat.renderQueue = 3000;
                            defaultMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                            defaultMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            defaultMat.SetInt("_ZWrite", 0);
                            defaultMat.DisableKeyword("_ALPHATEST_ON");
                            defaultMat.EnableKeyword("_ALPHABLEND_ON");
                            defaultMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                            
                            materials[i] = defaultMat;
                        }
                    }
                    else
                    {
                        // nullマテリアルの場合は新しいマテリアルを作成
                        Material defaultMat = new Material(Shader.Find("Standard"));
                        defaultMat.color = new Color(1f, 1f, 1f, 0.5f);
                        defaultMat.SetFloat("_Mode", 3); // Transparent
                        defaultMat.renderQueue = 3000;
                        defaultMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        defaultMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        defaultMat.SetInt("_ZWrite", 0);
                        defaultMat.DisableKeyword("_ALPHATEST_ON");
                        defaultMat.EnableKeyword("_ALPHABLEND_ON");
                        defaultMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        
                        materials[i] = defaultMat;
                    }
                }
                renderer.sharedMaterials = materials;
            }
        }
    }
}
