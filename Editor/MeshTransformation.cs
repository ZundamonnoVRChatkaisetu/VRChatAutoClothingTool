using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace VRChatAutoClothingTool
{
    /// <summary>
    /// メッシュの変形や調整機能を提供するユーティリティクラス
    /// </summary>
    public static class MeshTransformation
    {
        /// <summary>
        /// 衣装サイズの微調整
        /// </summary>
        /// <param name="clothingObject">衣装のGameObject</param>
        /// <param name="sizeAdjustment">サイズ調整係数</param>
        /// <param name="useSpringBasedScaling">スプリングベースのスケーリングを使用するか</param>
        public static void AdjustClothingSize(GameObject clothingObject, float sizeAdjustment, bool useSpringBasedScaling = false)
        {
            if (clothingObject == null) return;
            
            // ルートオブジェクトのスケールを直接設定（乗算ではなく代入）
            Transform rootTransform = clothingObject.transform;
            Vector3 baseScale = Vector3.one;
            
            if (useSpringBasedScaling)
            {
                // スプリングベースのスケーリング
                // 実際のサイズよりも少し大きく設定し、時間経過と共に自動的に収縮するように
                float springFactor = Mathf.Lerp(1.0f, 1.1f, Mathf.Abs(sizeAdjustment - 1.0f));
                rootTransform.localScale = baseScale * sizeAdjustment * springFactor;
                
                // EditorApplication.delayCall を使用して数フレーム後に収縮
                EditorApplication.delayCall += () =>
                {
                    if (rootTransform != null)
                    {
                        rootTransform.localScale = baseScale * sizeAdjustment;
                    }
                };
            }
            else
            {
                // 通常のスケーリング
                rootTransform.localScale = baseScale * sizeAdjustment;
            }
            
            // スキンウェイトの再調整が必要な場合（大きなスケール変更時）
            if (Mathf.Abs(sizeAdjustment - 1.0f) > 0.3f)
            {
                AdjustSkinnedMeshesForLargeScale(clothingObject, sizeAdjustment);
            }
        }
        
        /// <summary>
        /// 大きなスケール変更時にスキンメッシュを調整
        /// </summary>
        private static void AdjustSkinnedMeshesForLargeScale(GameObject clothingObject, float sizeAdjustment)
        {
            var renderers = clothingObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null) continue;
                
                // メッシュのコピーを作成
                Mesh adjustedMesh = Object.Instantiate(renderer.sharedMesh);
                adjustedMesh.name = renderer.sharedMesh.name + "_ScaleAdjusted";
                
                // バインドポーズ行列のスケールを調整
                Matrix4x4[] bindposes = adjustedMesh.bindposes;
                for (int i = 0; i < bindposes.Length; i++)
                {
                    // スケール変更に合わせてバインドポーズを調整
                    bindposes[i] = AdjustBindPoseScale(bindposes[i], sizeAdjustment);
                }
                adjustedMesh.bindposes = bindposes;
                
                // 調整したメッシュを設定
                renderer.sharedMesh = adjustedMesh;
                
                // アセットとして保存
                string assetPath = $"Assets/AdjustedMeshes/{renderer.gameObject.name}_ScaleAdjusted.asset";
                string directory = System.IO.Path.GetDirectoryName(assetPath);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                AssetDatabase.CreateAsset(adjustedMesh, assetPath);
            }
            AssetDatabase.SaveAssets();
        }
        
        /// <summary>
        /// バインドポーズ行列のスケールを調整
        /// </summary>
        private static Matrix4x4 AdjustBindPoseScale(Matrix4x4 bindPose, float scaleFactor)
        {
            // 行列からTRS分解
            Vector3 pos;
            Quaternion rot;
            Vector3 scale;
            
            DecomposeMatrix(bindPose, out pos, out rot, out scale);
            
            // スケール値を調整
            scale *= 1f / scaleFactor;
            
            // 新しい行列を作成
            return Matrix4x4.TRS(pos, rot, scale);
        }
        
        /// <summary>
        /// Matrix4x4をTranslation, Rotation, Scaleに分解
        /// </summary>
        private static void DecomposeMatrix(Matrix4x4 matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            position = matrix.GetColumn(3);
            
            // スケール計算
            scale.x = matrix.GetColumn(0).magnitude;
            scale.y = matrix.GetColumn(1).magnitude;
            scale.z = matrix.GetColumn(2).magnitude;
            
            // 回転計算（スケールを取り除いた行列から）
            Matrix4x4 rotationMatrix = matrix;
            rotationMatrix.SetColumn(0, rotationMatrix.GetColumn(0) / scale.x);
            rotationMatrix.SetColumn(1, rotationMatrix.GetColumn(1) / scale.y);
            rotationMatrix.SetColumn(2, rotationMatrix.GetColumn(2) / scale.z);
            rotationMatrix.SetColumn(3, new Vector4(0, 0, 0, 1));
            
            rotation = Quaternion.LookRotation(
                rotationMatrix.GetColumn(2),
                rotationMatrix.GetColumn(1)
            );
        }
        
        /// <summary>
        /// 衣装の位置を微調整
        /// </summary>
        /// <param name="clothingObject">衣装のGameObject</param>
        /// <param name="positionOffset">位置オフセット</param>
        public static void AdjustClothingPosition(GameObject clothingObject, Vector3 positionOffset)
        {
            if (clothingObject == null) return;
            
            // ルートオブジェクトの位置を調整
            Transform rootTransform = clothingObject.transform;
            rootTransform.position += positionOffset;
        }
        
        /// <summary>
        /// 衣装の回転を微調整
        /// </summary>
        /// <param name="clothingObject">衣装のGameObject</param>
        /// <param name="rotationOffset">回転オフセット（オイラー角）</param>
        public static void AdjustClothingRotation(GameObject clothingObject, Vector3 rotationOffset)
        {
            if (clothingObject == null) return;
            
            // ルートオブジェクトの回転を調整
            Transform rootTransform = clothingObject.transform;
            rootTransform.rotation *= Quaternion.Euler(rotationOffset);
        }
        
        /// <summary>
        /// 衣装のプロポーションを調整
        /// </summary>
        /// <param name="clothingObject">衣装のGameObject</param>
        /// <param name="proportionOffset">プロポーションオフセット</param>
        public static void AdjustClothingProportion(GameObject clothingObject, Vector3 proportionOffset)
        {
            if (clothingObject == null) return;
            
            // 現在のスケールを取得
            Transform rootTransform = clothingObject.transform;
            Vector3 currentScale = rootTransform.localScale;
            
            // 各軸ごとにスケールを調整
            Vector3 newScale = new Vector3(
                currentScale.x * (1f + proportionOffset.x),
                currentScale.y * (1f + proportionOffset.y),
                currentScale.z * (1f + proportionOffset.z)
            );
            
            // 新しいスケールを適用
            rootTransform.localScale = newScale;
        }
        
        /// <summary>
        /// メッシュを指定軸でミラーリング
        /// </summary>
        /// <param name="sourceMesh">元のメッシュ</param>
        /// <param name="mirrorAxis">ミラーリングする軸</param>
        /// <returns>ミラーリングされたメッシュ</returns>
        public static Mesh MirrorMesh(Mesh sourceMesh, Vector3 mirrorAxis)
        {
            if (sourceMesh == null) return null;
            
            // メッシュを複製
            Mesh mirroredMesh = Object.Instantiate(sourceMesh);
            mirroredMesh.name = sourceMesh.name + "_Mirrored";
            
            // 頂点位置をミラーリング
            Vector3[] vertices = mirroredMesh.vertices;
            Vector3[] normals = mirroredMesh.normals;
            
            for (int i = 0; i < vertices.Length; i++)
            {
                // 指定した軸でミラーリング
                if (mirrorAxis.x != 0)
                    vertices[i].x *= -1;
                if (mirrorAxis.y != 0)
                    vertices[i].y *= -1;
                if (mirrorAxis.z != 0)
                    vertices[i].z *= -1;
                
                // 法線もミラーリング
                if (i < normals.Length)
                {
                    if (mirrorAxis.x != 0)
                        normals[i].x *= -1;
                    if (mirrorAxis.y != 0)
                        normals[i].y *= -1;
                    if (mirrorAxis.z != 0)
                        normals[i].z *= -1;
                }
            }
            
            mirroredMesh.vertices = vertices;
            mirroredMesh.normals = normals;
            
            // UVをミラーリング（オプション）
            MirrorUVs(mirroredMesh, mirrorAxis);
            
            // トライアングルの向きを反転
            int[] triangles = mirroredMesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                // 三角形の頂点順序を入れ替えて面の向きを反転
                int temp = triangles[i];
                triangles[i] = triangles[i + 1];
                triangles[i + 1] = temp;
            }
            
            mirroredMesh.triangles = triangles;
            
            // バウンディングボックスなどを再計算
            mirroredMesh.RecalculateBounds();
            
            return mirroredMesh;
        }
        
        /// <summary>
        /// UVをミラーリング
        /// </summary>
        private static void MirrorUVs(Mesh mesh, Vector3 mirrorAxis)
        {
            if (mesh == null) return;
            
            // UVを取得
            Vector2[] uvs = mesh.uv;
            if (uvs == null || uvs.Length == 0) return;
            
            // ミラーリングするUV方向を決定
            bool mirrorU = mirrorAxis.x != 0;
            bool mirrorV = mirrorAxis.z != 0; // モデルのZ軸はUVのV軸に対応することが多い
            
            for (int i = 0; i < uvs.Length; i++)
            {
                // U座標をミラーリング
                if (mirrorU)
                {
                    uvs[i].x = 1f - uvs[i].x;
                }
                
                // V座標をミラーリング
                if (mirrorV)
                {
                    uvs[i].y = 1f - uvs[i].y;
                }
            }
            
            mesh.uv = uvs;
        }
        
        /// <summary>
        /// メッシュを統合
        /// </summary>
        /// <param name="renderers">結合するスキンメッシュレンダラー配列</param>
        /// <param name="rootBone">ルートボーン</param>
        /// <returns>統合されたメッシュ</returns>
        public static Mesh CombineMeshes(SkinnedMeshRenderer[] renderers, Transform rootBone)
        {
            if (renderers == null || renderers.Length == 0 || rootBone == null) return null;
            
            // すべてのメッシュとトランスフォームを含む結合情報を用意
            var combineInstances = new List<CombineInstance>();
            var allBones = new List<Transform>();
            var allMaterials = new List<Material>();
            
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null) continue;
                
                // ボーンの追加
                foreach (var bone in renderer.bones)
                {
                    if (!allBones.Contains(bone))
                    {
                        allBones.Add(bone);
                    }
                }
                
                // マテリアルの追加
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material != null && !allMaterials.Contains(material))
                    {
                        allMaterials.Add(material);
                    }
                }
                
                // 複数のサブメッシュがある場合、それぞれを結合情報に追加
                for (int i = 0; i < renderer.sharedMesh.subMeshCount; i++)
                {
                    combineInstances.Add(new CombineInstance
                    {
                        mesh = renderer.sharedMesh,
                        subMeshIndex = i,
                        transform = renderer.transform.localToWorldMatrix
                    });
                }
            }
            
            if (combineInstances.Count == 0) return null;
            
            // メッシュの結合
            Mesh combinedMesh = new Mesh();
            combinedMesh.name = "CombinedMesh";
            combinedMesh.CombineMeshes(combineInstances.ToArray(), false, true);
            
            // 新しいバインドポーズの作成
            Matrix4x4[] bindposes = new Matrix4x4[allBones.Count];
            for (int i = 0; i < allBones.Count; i++)
            {
                bindposes[i] = allBones[i].worldToLocalMatrix * rootBone.localToWorldMatrix;
            }
            
            combinedMesh.bindposes = bindposes;
            
            return combinedMesh;
        }
    }
}
