using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

namespace VRChatAutoClothingTool
{
    /// <summary>
    /// メッシュのボーンウェイト処理に関するユーティリティクラス
    /// </summary>
    public static class BoneWeightUtility
    {
        /// <summary>
        /// ボーンマッピングに基づいてボーンウェイトを再分配
        /// </summary>
        /// <param name="mesh">対象のメッシュ</param>
        /// <param name="bones">ボーン配列</param>
        /// <param name="boneIndexMapping">ボーンインデックスのマッピング</param>
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
        
        /// <summary>
        /// バインドポーズを再計算
        /// </summary>
        /// <param name="mesh">対象のメッシュ</param>
        /// <param name="rootBone">ルートボーン</param>
        /// <param name="bones">ボーン配列</param>
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
        
        /// <summary>
        /// メッシュを新しいボーンセットに合わせて最適化
        /// </summary>
        /// <param name="sourceMesh">元のメッシュ</param>
        /// <param name="originalBones">元のボーン配列</param>
        /// <param name="newBones">新しいボーン配列</param>
        /// <returns>最適化されたメッシュ</returns>
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
            Transform rootBone = MeshUtility.FindRootBone(newBones);
            if (rootBone != null)
            {
                RecalculateBindPoses(optimizedMesh, rootBone, newBones);
            }
            
            return optimizedMesh;
        }
        
        /// <summary>
        /// メッシュのボーンウェイトを可視化するための色を生成
        /// </summary>
        /// <param name="mesh">対象のメッシュ</param>
        /// <param name="boneIndex">表示するボーンのインデックス</param>
        /// <returns>各頂点の色配列</returns>
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
        
        /// <summary>
        /// スキンメッシュレンダラーのボーンを比較してマッピングを作成
        /// </summary>
        /// <param name="sourceRenderer">元のスキンメッシュレンダラー</param>
        /// <param name="targetRenderer">対象のスキンメッシュレンダラー</param>
        /// <returns>ボーンのマッピング</returns>
        public static Dictionary<Transform, Transform> CreateBoneMapping(SkinnedMeshRenderer sourceRenderer, SkinnedMeshRenderer targetRenderer)
        {
            Dictionary<Transform, Transform> boneMapping = new Dictionary<Transform, Transform>();
            
            if (sourceRenderer == null || targetRenderer == null) return boneMapping;
            
            Transform[] sourceBones = sourceRenderer.bones;
            Transform[] targetBones = targetRenderer.bones;
            
            // 名前ベースの対応付け
            foreach (var sourceBone in sourceBones)
            {
                if (sourceBone == null) continue;
                
                // 同じ名前のボーンを探す
                foreach (var targetBone in targetBones)
                {
                    if (targetBone == null) continue;
                    
                    if (sourceBone.name == targetBone.name)
                    {
                        boneMapping[sourceBone] = targetBone;
                        break;
                    }
                }
                
                // 見つからなかった場合、部分一致で探す
                if (!boneMapping.ContainsKey(sourceBone))
                {
                    foreach (var targetBone in targetBones)
                    {
                        if (targetBone == null) continue;
                        
                        if (sourceBone.name.Contains(targetBone.name) || targetBone.name.Contains(sourceBone.name))
                        {
                            boneMapping[sourceBone] = targetBone;
                            break;
                        }
                    }
                }
                
                // それでも見つからない場合、位置ベースで最も近いボーンを探す
                if (!boneMapping.ContainsKey(sourceBone))
                {
                    Transform closestBone = null;
                    float minDistance = float.MaxValue;
                    
                    foreach (var targetBone in targetBones)
                    {
                        if (targetBone == null) continue;
                        
                        float distance = Vector3.Distance(sourceBone.position, targetBone.position);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestBone = targetBone;
                        }
                    }
                    
                    if (closestBone != null)
                    {
                        boneMapping[sourceBone] = closestBone;
                    }
                }
            }
            
            return boneMapping;
        }
    }
}
