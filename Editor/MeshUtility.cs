using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace VRChatAutoClothingTool
{
    /// <summary>
    /// 基本的なメッシュ処理機能を提供するユーティリティクラス
    /// </summary>
    public static class MeshUtility
    {
        /// <summary>
        /// スキンメッシュの調整処理
        /// </summary>
        public static SkinnedMeshRenderer AdjustSkinnedMesh(SkinnedMeshRenderer renderer, Dictionary<Transform, Transform> boneMapping, Vector3 scaleFactor)
        {
            if (renderer == null || renderer.sharedMesh == null) return renderer;
            
            // 元のメッシュを複製
            Mesh originalMesh = renderer.sharedMesh;
            Mesh adjustedMesh = Object.Instantiate(originalMesh);
            
            // メッシュ名を設定
            adjustedMesh.name = originalMesh.name + "_Adjusted";
            
            // ボーンの再マッピング
            Transform[] originalBones = renderer.bones;
            Transform[] newBones = new Transform[originalBones.Length];
            
            for (int i = 0; i < originalBones.Length; i++)
            {
                if (boneMapping.TryGetValue(originalBones[i], out Transform mappedBone))
                {
                    newBones[i] = mappedBone;
                }
                else
                {
                    newBones[i] = originalBones[i];
                }
            }
            
            // バインドポーズの更新
            Matrix4x4[] bindposes = adjustedMesh.bindposes;
            
            for (int i = 0; i < bindposes.Length && i < newBones.Length; i++)
            {
                if (newBones[i] != originalBones[i] && newBones[i] != null)
                {
                    // 新しいボーンの逆変換行列を計算
                    bindposes[i] = newBones[i].worldToLocalMatrix;
                }
            }
            
            adjustedMesh.bindposes = bindposes;
            
            // 新しいレンダラーを設定
            renderer.bones = newBones;
            renderer.sharedMesh = adjustedMesh;
            
            // メッシュをアセットとして保存
            string assetPath = $"Assets/AdjustedMeshes/{renderer.gameObject.name}_Adjusted.asset";
            
            // アセットフォルダの作成
            string directory = System.IO.Path.GetDirectoryName(assetPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            AssetDatabase.CreateAsset(adjustedMesh, assetPath);
            AssetDatabase.SaveAssets();
            
            return renderer;
        }
        
        /// <summary>
        /// メッシュのスケール調整
        /// </summary>
        public static void ScaleMesh(Mesh mesh, Vector3 scale)
        {
            if (mesh == null) return;
            
            // 頂点位置をスケーリング
            Vector3[] vertices = mesh.vertices;
            
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = Vector3.Scale(vertices[i], scale);
            }
            
            mesh.vertices = vertices;
            
            // バウンディングボックスを再計算
            mesh.RecalculateBounds();
            
            // 法線と接線が必要に応じて再計算
            if (scale.x * scale.y * scale.z < 0) // 負のスケールがある場合
            {
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
            }
        }
        
        /// <summary>
        /// マテリアルのテクスチャスケーリングを調整
        /// </summary>
        public static void AdjustMaterialTextureScale(Material material, Vector3 scaleFactor)
        {
            if (material == null) return;
            
            // テクスチャスケールを持つ可能性のあるプロパティ名
            string[] texturePropertyNames = new string[]
            {
                "_MainTex", "_BumpMap", "_MetallicGlossMap", "_EmissionMap", "_OcclusionMap", 
                "_DetailAlbedoMap", "_DetailNormalMap", "_DetailMask"
            };
            
            // 各テクスチャプロパティに対して
            foreach (var propertyName in texturePropertyNames)
            {
                if (material.HasProperty(propertyName))
                {
                    // 現在のスケールとオフセットを取得
                    Vector2 scale = material.GetTextureScale(propertyName);
                    
                    // スケールを調整（X, Y方向のみ）
                    Vector2 newScale = new Vector2(
                        scale.x / scaleFactor.x,
                        scale.y / scaleFactor.y
                    );
                    
                    // 調整したスケールを適用
                    material.SetTextureScale(propertyName, newScale);
                }
            }
        }
        
        /// <summary>
        /// メッシュのミラーリング
        /// </summary>
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
        /// メッシュを統合
        /// </summary>
        public static Mesh CombineMeshes(SkinnedMeshRenderer[] renderers, Transform rootBone)
        {
            if (renderers == null || renderers.Length == 0 || rootBone == null) return null;
            
            // すべてのメッシュとトランスフォームを含む結合情報を用意
            var combineInstances = new List<CombineInstance>();
            var allBones = new List<Transform>();
            
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
                
                // 結合情報の追加
                combineInstances.Add(new CombineInstance
                {
                    mesh = renderer.sharedMesh,
                    transform = renderer.transform.localToWorldMatrix
                });
            }
            
            if (combineInstances.Count == 0) return null;
            
            // メッシュの結合
            Mesh combinedMesh = new Mesh();
            combinedMesh.name = "CombinedMesh";
            combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);
            
            // 新しいバインドポーズの作成
            Matrix4x4[] bindposes = new Matrix4x4[allBones.Count];
            for (int i = 0; i < allBones.Count; i++)
            {
                bindposes[i] = allBones[i].worldToLocalMatrix * rootBone.localToWorldMatrix;
            }
            
            combinedMesh.bindposes = bindposes;
            
            return combinedMesh;
        }
        
        /// <summary>
        /// ボーンのルートを見つける
        /// </summary>
        public static Transform FindRootBone(Transform[] bones)
        {
            if (bones == null || bones.Length == 0) return null;
            
            foreach (var bone in bones)
            {
                if (bone != null)
                {
                    // Hipsまたは最上位のボーンを探す
                    if (bone.name.Contains("Hips") || bone.name.Contains("Root") || 
                        (bone.parent != null && !bones.Contains(bone.parent)))
                    {
                        return bone;
                    }
                }
            }
            
            // 見つからない場合は最初の非nullボーンを返す
            return bones.FirstOrDefault(b => b != null);
        }
        
        /// <summary>
        /// スキンメッシュレンダラーの一覧からMeshをベイクして取得
        /// </summary>
        public static List<Mesh> BakeMeshes(List<SkinnedMeshRenderer> renderers)
        {
            List<Mesh> bakedMeshes = new List<Mesh>();
            
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.sharedMesh != null)
                {
                    Mesh bakedMesh = new Mesh();
                    renderer.BakeMesh(bakedMesh);
                    bakedMeshes.Add(bakedMesh);
                    
                    // メッシュ名を設定（デバッグ用）
                    bakedMesh.name = $"Baked_{renderer.name}";
                }
            }
            
            return bakedMeshes;
        }
        
        /// <summary>
        /// 衣装のサイズを微調整
        /// </summary>
        public static void AdjustClothingSize(GameObject clothingObject, float sizeAdjustment)
        {
            if (clothingObject == null) return;
            
            // ルートオブジェクトのスケールを直接設定（乗算ではなく代入）
            Transform rootTransform = clothingObject.transform;
            Vector3 baseScale = Vector3.one;
            rootTransform.localScale = baseScale * sizeAdjustment;
        }
        
        /// <summary>
        /// 衣装の位置を微調整
        /// </summary>
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
        public static void AdjustClothingRotation(GameObject clothingObject, Vector3 rotationOffset)
        {
            if (clothingObject == null) return;
            
            // ルートオブジェクトの回転を調整
            Transform rootTransform = clothingObject.transform;
            rootTransform.rotation *= Quaternion.Euler(rotationOffset);
        }
    }
}
