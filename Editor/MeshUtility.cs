using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;

namespace VRChatAutoClothingTool
{
    public static class MeshUtility
    {
        // スキンメッシュの調整処理
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
            
            // ボーンウェイトの調整（必要に応じて）
            
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
        
        // メッシュのスケール調整
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
        
        // 頂点ウェイトを再分配
        public static void RedistributeWeights(Mesh mesh, Transform[] bones, Dictionary<int, int> boneIndexMapping)
        {
            if (mesh == null || bones == null || boneIndexMapping == null) return;
            
            // ボーンウェイト情報を取得
            NativeArray<byte> bonesPerVertex = mesh.GetBonesPerVertex();
            NativeArray<BoneWeight1> weights = mesh.GetAllBoneWeights();
            
            List<BoneWeight1> newWeights = new List<BoneWeight1>();
            int currentWeightIndex = 0;
            
            // 各頂点のボーンウェイトを処理
            for (int vertIndex = 0; vertIndex < bonesPerVertex.Length; vertIndex++)
            {
                int boneCount = bonesPerVertex[vertIndex];
                float weightSum = 0;
                
                // この頂点に影響するすべてのボーンのウェイトを取得
                List<BoneWeight1> vertexWeights = new List<BoneWeight1>();
                
                for (int j = 0; j < boneCount; j++)
                {
                    BoneWeight1 weight = weights[currentWeightIndex + j];
                    
                    // ボーンインデックスをマッピングで変換
                    if (boneIndexMapping.TryGetValue(weight.boneIndex, out int newIndex))
                    {
                        weight.boneIndex = newIndex;
                    }
                    
                    vertexWeights.Add(weight);
                    weightSum += weight.weight;
                }
                
                // ウェイトを正規化（合計が1になるようにする）
                if (weightSum > 0 && vertexWeights.Count > 0)
                {
                    for (int j = 0; j < vertexWeights.Count; j++)
                    {
                        BoneWeight1 normalizedWeight = vertexWeights[j];
                        normalizedWeight.weight /= weightSum;
                        newWeights.Add(normalizedWeight);
                    }
                }
                
                currentWeightIndex += boneCount;
            }
            
            // 新しいボーンウェイトをメッシュに設定
            var newBonesPerVertex = bonesPerVertex;
            var nativeWeights = new NativeArray<BoneWeight1>(newWeights.ToArray(), Allocator.Temp);
            mesh.SetBoneWeights(newBonesPerVertex, nativeWeights);
            nativeWeights.Dispose();
            
            // NativeArrayのリソースを解放
            if (weights.IsCreated)
            {
                weights.Dispose();
            }
            if (bonesPerVertex.IsCreated)
            {
                bonesPerVertex.Dispose();
            }
        }
        
        // バインドポーズを再計算
        public static void RecalculateBindPoses(Mesh mesh, Transform rootBone, Transform[] bones)
        {
            if (mesh == null || rootBone == null || bones == null) return;
            
            Matrix4x4[] bindPoses = new Matrix4x4[bones.Length];
            
            // 各ボーンに対してバインドポーズ行列を計算
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] != null)
                {
                    // ルートボーンを基準にしたワールド空間からローカル空間への変換行列
                    bindPoses[i] = bones[i].worldToLocalMatrix * rootBone.localToWorldMatrix;
                }
                else
                {
                    bindPoses[i] = Matrix4x4.identity;
                }
            }
            
            mesh.bindposes = bindPoses;
        }
        
        // メッシュを新しいボーンセットに合わせて最適化
        public static Mesh OptimizeMesh(Mesh sourceMesh, Transform[] originalBones, Transform[] newBones)
        {
            if (sourceMesh == null || originalBones == null || newBones == null) return null;
            
            // メッシュを複製
            Mesh optimizedMesh = Object.Instantiate(sourceMesh);
            optimizedMesh.name = sourceMesh.name + "_Optimized";
            
            // ボーンマッピングを構築
            var boneMapping = new Dictionary<int, int>();
            for (int i = 0; i < originalBones.Length; i++)
            {
                Transform originalBone = originalBones[i];
                
                // 名前でマッチングを試みる
                for (int j = 0; j < newBones.Length; j++)
                {
                    if (newBones[j] != null && originalBone != null && 
                        newBones[j].name == originalBone.name)
                    {
                        boneMapping[i] = j;
                        break;
                    }
                }
            }
            
            // ボーンのインデックスが変わった場合、ボーンウェイトの再分配
            if (boneMapping.Count > 0)
            {
                RedistributeWeights(optimizedMesh, newBones, boneMapping);
            }
            
            // バインドポーズの再計算
            // 新しいボーンの親を探す
            Transform rootBone = FindRootBone(newBones);
            if (rootBone != null)
            {
                RecalculateBindPoses(optimizedMesh, rootBone, newBones);
            }
            
            return optimizedMesh;
        }
        
        // ボーンのルートを見つける
        private static Transform FindRootBone(Transform[] bones)
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
        
        // マテリアルのテクスチャスケーリングを調整
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
        
        // メッシュのミラーリング
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
            
            // UVのミラーリング（オプション）
            
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
        
        // メッシュを統合
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
            
            // ボーンウェイトの再マッピング（詳細な実装は省略）
            
            return combinedMesh;
        }
        
        // メッシュのボーンウェイトを可視化するための色を生成
        public static Color[] GenerateBoneWeightColors(Mesh mesh, int boneIndex)
        {
            if (mesh == null) return null;
            
            Vector3[] vertices = mesh.vertices;
            Color[] colors = new Color[vertices.Length];
            
            // ボーンウェイト情報を取得
            NativeArray<byte> bonesPerVertex = mesh.GetBonesPerVertex();
            NativeArray<BoneWeight1> weights = mesh.GetAllBoneWeights();
            int weightIndex = 0;
            
            for (int i = 0; i < vertices.Length; i++)
            {
                float weight = 0f;
                int boneCount = bonesPerVertex[i];
                
                // このボーンに対応するウェイトを探す
                for (int j = 0; j < boneCount; j++)
                {
                    BoneWeight1 boneWeight = weights[weightIndex + j];
                    
                    if (boneWeight.boneIndex == boneIndex)
                    {
                        weight = boneWeight.weight;
                        break;
                    }
                }
                
                // ウェイトに基づいて色を設定（赤→青のグラデーション）
                colors[i] = new Color(weight, 0f, 1f - weight, 1f);
                
                weightIndex += boneCount;
            }
            
            // NativeArrayのリソース解放
            if (weights.IsCreated)
            {
                weights.Dispose();
            }
            if (bonesPerVertex.IsCreated)
            {
                bonesPerVertex.Dispose();
            }
            
            return colors;
        }
    }
}
