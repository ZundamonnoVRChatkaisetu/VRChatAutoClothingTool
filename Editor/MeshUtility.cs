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
        
        // アバターと衣装の貫通をチェックして調整する（修正版）
        public static void AdjustClothingPenetration(GameObject avatarObject, GameObject clothingObject, float pushOutDistance = 0.01f)
        {
            if (avatarObject == null || clothingObject == null) return;
            
            // 貫通チェックの閾値とプッシュアウト距離を制限
            pushOutDistance = Mathf.Min(pushOutDistance, 0.003f);
            float penetrationThreshold = 0.01f; // 貫通と見なす距離の閾値（小さいほど厳密になる）
            
            Debug.Log($"貫通チェック開始: 閾値 = {penetrationThreshold}, 押し出し距離 = {pushOutDistance}");
            
            // アバターのスキンメッシュを取得
            var avatarRenderers = avatarObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (avatarRenderers.Length == 0) return;
            
            // 衣装のスキンメッシュを取得
            var clothingRenderers = clothingObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (clothingRenderers.Length == 0) return;
            
            // 衝突判定用のアバターメッシュを生成
            List<Mesh> avatarMeshes = new List<Mesh>();
            List<Matrix4x4> avatarMatrices = new List<Matrix4x4>();
            
            foreach (var renderer in avatarRenderers)
            {
                Mesh bakedMesh = new Mesh();
                renderer.BakeMesh(bakedMesh);
                avatarMeshes.Add(bakedMesh);
                avatarMatrices.Add(renderer.transform.localToWorldMatrix);
            }
            
            // 貫通チェック結果をログに出力するためのカウンター
            int totalVertices = 0;
            int adjustedVertices = 0;
            
            // 各衣装メッシュに対して処理
            foreach (var clothingRenderer in clothingRenderers)
            {
                if (clothingRenderer.sharedMesh == null) continue;
                
                // 衣装の現在のメッシュを取得
                Mesh clothingMesh = clothingRenderer.sharedMesh;
                
                // 編集可能なメッシュへのコピーを作成
                Mesh adjustedMesh = Object.Instantiate(clothingMesh);
                Vector3[] clothingVertices = adjustedMesh.vertices;
                totalVertices += clothingVertices.Length;
                
                // 衣装のローカル→ワールド変換行列
                Matrix4x4 clothingLocalToWorld = clothingRenderer.localToWorldMatrix;
                Matrix4x4 clothingWorldToLocal = clothingLocalToWorld.inverse;
                
                bool meshModified = false;
                
                // 各頂点に対して処理（処理対象を減らすため、一部の頂点のみ処理）
                for (int i = 0; i < clothingVertices.Length; i += 2) // 隣り合う頂点は近いので、間引いて処理
                {
                    // 衣装の頂点をワールド座標に変換
                    Vector3 worldVertex = clothingLocalToWorld.MultiplyPoint3x4(clothingVertices[i]);
                    
                    // 各アバターメッシュとの貫通チェック
                    bool penetrationDetected = false;
                    Vector3 bestAdjustmentDirection = Vector3.zero;
                    float minPenetrationDepth = float.MaxValue;
                    
                    for (int meshIndex = 0; meshIndex < avatarMeshes.Count; meshIndex++)
                    {
                        Mesh avatarMesh = avatarMeshes[meshIndex];
                        Matrix4x4 avatarMatrix = avatarMatrices[meshIndex];
                        Matrix4x4 avatarWorldToLocal = avatarMatrix.inverse;
                        
                        // アバターのローカル座標に変換
                        Vector3 avatarLocalVertex = avatarWorldToLocal.MultiplyPoint3x4(worldVertex);
                        
                        // アバターのバウンディングボックスをチェック（境界を少し広げる）
                        Bounds avatarBounds = avatarMesh.bounds;
                        avatarBounds.Expand(0.05f); // 境界を少し広げて余裕を持たせる
                        
                        if (!avatarBounds.Contains(avatarLocalVertex))
                        {
                            continue; // この頂点はアバターの範囲外
                        }
                        
                        // アバターの三角形をサンプリング（すべての三角形ではなく一部をチェック）
                        Vector3[] avatarVertices = avatarMesh.vertices;
                        int[] avatarTriangles = avatarMesh.triangles;
                        
                        for (int t = 0; t < avatarTriangles.Length; t += 9) // より少ない三角形をチェック
                        {
                            if (t + 2 >= avatarTriangles.Length) continue;
                            
                            Vector3 a = avatarVertices[avatarTriangles[t]];
                            Vector3 b = avatarVertices[avatarTriangles[t + 1]];
                            Vector3 c = avatarVertices[avatarTriangles[t + 2]];
                            
                            // 三角形の法線を計算
                            Vector3 triangleNormal = Vector3.Cross(b - a, c - a).normalized;
                            
                            // 三角形上の最近点を計算
                            Vector3 closestPoint = ClosestPointOnTriangle(avatarLocalVertex, a, b, c);
                            
                            // 距離を計算
                            float distance = Vector3.Distance(avatarLocalVertex, closestPoint);
                            
                            // 点と三角形の法線方向の内外判定
                            float dotProduct = Vector3.Dot(avatarLocalVertex - closestPoint, triangleNormal);
                            
                            // 貫通を検出（距離が小さく、点が三角形の裏側にある場合）
                            if (dotProduct < 0 && distance < penetrationThreshold)
                            {
                                penetrationDetected = true;
                                
                                // より浅い貫通の場合、その方向に調整
                                if (distance < minPenetrationDepth)
                                {
                                    minPenetrationDepth = distance;
                                    // ワールド座標に変換した法線方向
                                    bestAdjustmentDirection = avatarMatrix.MultiplyVector(triangleNormal).normalized;
                                }
                            }
                        }
                    }
                    
                    // 貫通が検出された場合、頂点を調整
                    if (penetrationDetected)
                    {
                        // 法線方向に頂点を押し出す（最小貫通深度 + 余裕分）
                        Vector3 adjustedWorldVertex = worldVertex + bestAdjustmentDirection * (minPenetrationDepth + pushOutDistance);
                        
                        // 衣装のローカル座標に戻す
                        clothingVertices[i] = clothingWorldToLocal.MultiplyPoint3x4(adjustedWorldVertex);
                        
                        meshModified = true;
                        adjustedVertices++;
                    }
                }
                
                // メッシュが変更された場合のみ更新
                if (meshModified)
                {
                    // 法線再計算のためのバックアップ
                    int[] triangles = adjustedMesh.triangles;
                    
                    adjustedMesh.vertices = clothingVertices;
                    adjustedMesh.triangles = triangles; // 三角形情報を再設定
                    adjustedMesh.RecalculateBounds();
                    adjustedMesh.RecalculateNormals();
                    
                    // アセットとして保存
                    string assetPath = $"Assets/AdjustedMeshes/{clothingRenderer.gameObject.name}_NoPenetration.asset";
                    string directory = System.IO.Path.GetDirectoryName(assetPath);
                    if (!System.IO.Directory.Exists(directory))
                    {
                        System.IO.Directory.CreateDirectory(directory);
                    }
                    
                    AssetDatabase.CreateAsset(adjustedMesh, assetPath);
                    AssetDatabase.SaveAssets();
                    
                    // 新しいメッシュを適用
                    clothingRenderer.sharedMesh = adjustedMesh;
                }
            }
            
            // 処理結果をログに出力
            Debug.Log($"貫通チェック完了: 合計 {totalVertices} 頂点中 {adjustedVertices} 頂点を調整しました。");
            
            // 一時メッシュを破棄
            foreach (var mesh in avatarMeshes)
            {
                Object.DestroyImmediate(mesh);
            }
        }
        
        // 三角形上の最近点を計算
        private static Vector3 ClosestPointOnTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            // 点から三角形への最近点を計算
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = point - a;
            
            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            
            // 点がaの外側にある場合
            if (d1 <= 0 && d2 <= 0)
                return a;
            
            // 点がbの外側にある場合
            Vector3 bp = point - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3)
                return b;
            
            // 点がabエッジの外側にある場合
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0)
            {
                float v = d1 / (d1 - d3);
                return a + v * ab;
            }
            
            // 点がcの外側にある場合
            Vector3 cp = point - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6)
                return c;
            
            // 点がacエッジの外側にある場合
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0)
            {
                float w = d2 / (d2 - d6);
                return a + w * ac;
            }
            
            // 点がbcエッジの外側にある場合
            float va = d3 * d6 - d5 * d4;
            if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + w * (c - b);
            }
            
            // 三角形内部の点
            float denom = 1.0f / (va + vb + vc);
            float v2 = vb * denom;
            float w2 = vc * denom;
            
            return a + ab * v2 + ac * w2;
        }
        
        // 衣装のサイズを微調整
        public static void AdjustClothingSize(GameObject clothingObject, float sizeAdjustment)
        {
            if (clothingObject == null) return;
            
            // ルートオブジェクトのスケールを調整
            Transform rootTransform = clothingObject.transform;
            Vector3 originalScale = rootTransform.localScale;
            rootTransform.localScale = originalScale * sizeAdjustment;
        }
        
        // 衣装の位置を微調整
        public static void AdjustClothingPosition(GameObject clothingObject, Vector3 positionOffset)
        {
            if (clothingObject == null) return;
            
            // ルートオブジェクトの位置を調整
            Transform rootTransform = clothingObject.transform;
            rootTransform.position += positionOffset;
        }
        
        // 衣装の回転を微調整
        public static void AdjustClothingRotation(GameObject clothingObject, Vector3 rotationOffset)
        {
            if (clothingObject == null) return;
            
            // ルートオブジェクトの回転を調整
            Transform rootTransform = clothingObject.transform;
            rootTransform.rotation *= Quaternion.Euler(rotationOffset);
        }
    }
}
