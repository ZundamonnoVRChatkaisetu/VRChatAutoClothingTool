using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace VRChatAutoClothingTool
{
    /// <summary>
    /// 貫通検出における情報を保持するクラス
    /// </summary>
    public class PenetrationInfo
    {
        /// <summary>
        /// 貫通の深さ（小さいほど大きな貫通）
        /// </summary>
        public float Depth { get; set; }
        
        /// <summary>
        /// 押し出す方向（正規化済み）
        /// </summary>
        public Vector3 Direction { get; set; }
        
        public PenetrationInfo(float depth, Vector3 direction)
        {
            Depth = depth;
            Direction = direction;
        }
    }

    /// <summary>
    /// アバターと衣装の貫通検出および調整機能を提供するクラス
    /// </summary>
    public static class PenetrationDetection
    {
        /// <summary>
        /// アバターと衣装の貫通をチェックして調整する
        /// </summary>
        /// <param name="avatarObject">アバターのGameObject</param>
        /// <param name="clothingObject">衣装のGameObject</param>
        /// <param name="pushOutDistance">貫通した頂点を押し出す距離</param>
        /// <param name="penetrationThreshold">貫通とみなす距離の閾値</param>
        /// <param name="advancedSampling">高精度サンプリングを使用するか</param>
        /// <param name="preferBodyMeshes">ボディメッシュを優先するか</param>
        /// <param name="preserveShape">衣装の形状を維持するか</param>
        /// <param name="preserveStrength">形状維持の強度 (0-1)</param>
        public static void AdjustClothingPenetration(
            GameObject avatarObject, 
            GameObject clothingObject, 
            float pushOutDistance = 0.01f,
            float penetrationThreshold = 0.015f,
            bool advancedSampling = true, 
            bool preferBodyMeshes = true,
            bool preserveShape = true,
            float preserveStrength = 0.5f)
        {
            if (avatarObject == null || clothingObject == null) return;
            
            // パラメータの検証と制限
            pushOutDistance = Mathf.Clamp(pushOutDistance, 0.0005f, 0.05f);
            penetrationThreshold = Mathf.Clamp(penetrationThreshold, 0.001f, 0.05f);
            preserveStrength = Mathf.Clamp01(preserveStrength);
            
            Debug.Log($"貫通チェック開始: 閾値 = {penetrationThreshold}, 押し出し距離 = {pushOutDistance}, " +
                      $"高度なサンプリング = {advancedSampling}, ボディメッシュ優先 = {preferBodyMeshes}, " +
                      $"形状維持 = {preserveShape}, 形状維持強度 = {preserveStrength}");
            
            // アバターのスキンメッシュレンダラーを取得
            var avatarRenderers = avatarObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            if (avatarRenderers.Length == 0) return;
            
            // 優先処理するメッシュとその他のメッシュに分類
            List<SkinnedMeshRenderer> priorityRenderers = new List<SkinnedMeshRenderer>();
            List<SkinnedMeshRenderer> otherRenderers = new List<SkinnedMeshRenderer>();
            
            if (preferBodyMeshes)
            {
                // ボディメッシュを優先するパターン（より高度に）
                string[] bodyPatterns = new string[] 
                { 
                    "body", "torso", "chest", "skin", "face", "head", 
                    "leg", "arm", "hand", "feet", "foot", "character" 
                };
                
                foreach (var renderer in avatarRenderers)
                {
                    string rendererNameLower = renderer.name.ToLower();
                    
                    // ボディ関連のメッシュを優先
                    if (bodyPatterns.Any(pattern => rendererNameLower.Contains(pattern)))
                    {
                        priorityRenderers.Add(renderer);
                        Debug.Log($"優先: {renderer.name}（ボディメッシュとして検出）");
                    }
                    else
                    {
                        otherRenderers.Add(renderer);
                    }
                }
            }
            else
            {
                // すべてのメッシュを平等に扱う
                priorityRenderers.AddRange(avatarRenderers);
            }
            
            // 優先メッシュがない場合の対応
            if (priorityRenderers.Count == 0)
            {
                Debug.Log("ボディメッシュが検出されなかったため、すべてのメッシュを平等に処理します。");
                priorityRenderers.AddRange(otherRenderers);
                otherRenderers.Clear();
            }
            
            // 衣装のスキンメッシュレンダラーを取得
            var clothingRenderers = clothingObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (clothingRenderers.Length == 0) return;
            
            // 解像度設定（高度なサンプリングモード）
            int meshTriangleStep = advancedSampling ? 3 : 6; // 3=すべての三角形、6=半分の三角形
            float boundingBoxExpansion = advancedSampling ? 0.05f : 0.1f; // バウンディングボックスの拡張量
            
            // 衝突判定用のアバターメッシュを生成（優先順位の高いもの）
            List<Mesh> priorityMeshes = MeshUtility.BakeMeshes(priorityRenderers);
            List<Matrix4x4> priorityMatrices = priorityRenderers.Select(r => r.transform.localToWorldMatrix).ToList();
            
            // 衝突判定用のアバターメッシュを生成（その他）
            List<Mesh> otherMeshes = MeshUtility.BakeMeshes(otherRenderers);
            List<Matrix4x4> otherMatrices = otherRenderers.Select(r => r.transform.localToWorldMatrix).ToList();
            
            // 貫通チェック結果をログに出力するためのカウンター
            int totalVertices = 0;
            int adjustedVertices = 0;
            
            // 各衣装メッシュに対して処理
            foreach (var clothingRenderer in clothingRenderers)
            {
                // レンダラーが無効なら有効化する
                if (!clothingRenderer.enabled)
                {
                    clothingRenderer.enabled = true;
                }
                
                if (clothingRenderer.sharedMesh == null) continue;
                
                // 衣装の現在のメッシュを取得（内容をコピー）
                Mesh clothingMesh = clothingRenderer.sharedMesh;
                Mesh adjustedMesh = Object.Instantiate(clothingMesh);
                
                string meshName = clothingRenderer.name;
                Debug.Log($"メッシュ '{meshName}' を処理中...");
                
                Vector3[] clothingVertices = adjustedMesh.vertices;
                Vector3[] originalVertices = (Vector3[])clothingVertices.Clone(); // 元の頂点位置を保存
                totalVertices += clothingVertices.Length;
                
                // 頂点の隣接関係を構築（形状維持のために必要）
                Dictionary<int, List<int>> vertexAdjacency = preserveShape ?
                    BuildVertexAdjacency(adjustedMesh) : null;
                
                // 衣装のローカル→ワールド変換行列
                Matrix4x4 clothingLocalToWorld = clothingRenderer.localToWorldMatrix;
                Matrix4x4 clothingWorldToLocal = clothingLocalToWorld.inverse;
                
                bool meshModified = false;
                // 各頂点が処理済みかどうかを記録する配列
                bool[] vertexChecked = new bool[clothingVertices.Length];
                bool[] vertexDeformed = new bool[clothingVertices.Length]; // 形状維持処理用
                
                // 優先メッシュでの貫通チェック
                Debug.Log($"優先メッシュ ({priorityMeshes.Count} 個) との貫通チェック");
                ProcessPenetration(
                    clothingVertices, 
                    clothingLocalToWorld, 
                    clothingWorldToLocal,
                    priorityMeshes, 
                    priorityMatrices, 
                    penetrationThreshold, 
                    pushOutDistance,
                    meshTriangleStep,
                    boundingBoxExpansion,
                    ref adjustedVertices,
                    ref meshModified,
                    vertexChecked,
                    vertexDeformed
                );
                
                // その他のメッシュでの貫通チェック（優先メッシュで処理されなかった頂点のみ）
                if (otherMeshes.Count > 0)
                {
                    Debug.Log($"その他のメッシュ ({otherMeshes.Count} 個) との貫通チェック");
                    ProcessPenetration(
                        clothingVertices, 
                        clothingLocalToWorld, 
                        clothingWorldToLocal,
                        otherMeshes, 
                        otherMatrices, 
                        penetrationThreshold, 
                        pushOutDistance,
                        meshTriangleStep * 2, // その他のメッシュは少し粗いサンプリング
                        boundingBoxExpansion,
                        ref adjustedVertices,
                        ref meshModified,
                        vertexChecked,
                        vertexDeformed
                    );
                }
                
                // 形状を維持したままスムージングする
                if (preserveShape && meshModified)
                {
                    Debug.Log("メッシュの形状を維持しながらスムージングを適用中...");
                    SmoothMeshDeformation(
                        clothingVertices,
                        originalVertices,
                        vertexAdjacency,
                        vertexDeformed,
                        preserveStrength);
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
                    
                    Debug.Log($"メッシュ '{meshName}' の貫通を調整しました。調整された頂点数: {adjustedVertices}");
                }
                else
                {
                    Debug.Log($"メッシュ '{meshName}' に貫通は検出されませんでした。");
                    // 不要なメッシュを破棄
                    Object.DestroyImmediate(adjustedMesh);
                }
            }
            
            // 処理結果をログに出力
            Debug.Log($"貫通チェック完了: 合計 {totalVertices} 頂点中 {adjustedVertices} 頂点を調整しました。");
            
            // 一時メッシュを破棄
            foreach (var mesh in priorityMeshes)
            {
                Object.DestroyImmediate(mesh);
            }
            foreach (var mesh in otherMeshes)
            {
                Object.DestroyImmediate(mesh);
            }
        }
        
        /// <summary>
        /// メッシュの頂点の隣接関係を構築
        /// </summary>
        private static Dictionary<int, List<int>> BuildVertexAdjacency(Mesh mesh)
        {
            Dictionary<int, List<int>> adjacency = new Dictionary<int, List<int>>();
            int[] triangles = mesh.triangles;
            
            // すべての頂点について空のリストを初期化
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                adjacency[i] = new List<int>();
            }
            
            // 三角形の各辺に基づいて隣接関係を構築
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];
                
                // 重複を避けつつ隣接頂点を追加
                if (!adjacency[a].Contains(b)) adjacency[a].Add(b);
                if (!adjacency[a].Contains(c)) adjacency[a].Add(c);
                if (!adjacency[b].Contains(a)) adjacency[b].Add(a);
                if (!adjacency[b].Contains(c)) adjacency[b].Add(c);
                if (!adjacency[c].Contains(a)) adjacency[c].Add(a);
                if (!adjacency[c].Contains(b)) adjacency[c].Add(b);
            }
            
            return adjacency;
        }
        
        /// <summary>
        /// メッシュの変形をスムージングして形状を維持
        /// </summary>
        private static void SmoothMeshDeformation(
            Vector3[] currentVertices,
            Vector3[] originalVertices,
            Dictionary<int, List<int>> adjacency,
            bool[] vertexDeformed,
            float preserveStrength)
        {
            // ラプラシアン平滑化のパラメータ
            int iterations = 3;  // 平滑化の反復回数
            float lambda = preserveStrength;  // 形状保持の強度 (0-1)
            
            // 平滑化用の一時的な配列
            Vector3[] smoothedVertices = new Vector3[currentVertices.Length];
            
            // 現在の頂点位置をコピー
            System.Array.Copy(currentVertices, smoothedVertices, currentVertices.Length);
            
            // 平滑化処理を繰り返す
            for (int iter = 0; iter < iterations; iter++)
            {
                // 各頂点を処理
                for (int i = 0; i < currentVertices.Length; i++)
                {
                    // 変形された頂点とその隣接頂点のみを処理
                    if (!vertexDeformed[i] && !HasDeformedNeighbor(i, vertexDeformed, adjacency))
                        continue;
                    
                    List<int> neighbors = adjacency[i];
                    
                    // 隣接頂点が存在する場合
                    if (neighbors.Count > 0)
                    {
                        Vector3 centroid = Vector3.zero;
                        
                        // 隣接頂点の重心を計算
                        foreach (var neighbor in neighbors)
                        {
                            centroid += currentVertices[neighbor];
                        }
                        centroid /= neighbors.Count;
                        
                        // 形状を維持するラプラシアン平滑化
                        Vector3 laplacian = centroid - currentVertices[i];
                        Vector3 newPosition = currentVertices[i] + laplacian * 0.5f;
                        
                        // 元の形状に引き戻す力を適用
                        newPosition = Vector3.Lerp(newPosition, originalVertices[i], lambda);
                        
                        // 変形された頂点や強く変形された頂点の隣接頂点は異なる重みで処理
                        if (vertexDeformed[i])
                        {
                            // 貫通修正された頂点は元の形状に引き戻す力を弱める
                            newPosition = Vector3.Lerp(currentVertices[i], newPosition, 0.7f);
                        }
                        else if (HasStronglyDeformedNeighbor(i, vertexDeformed, adjacency, currentVertices, originalVertices))
                        {
                            // 大きく変形された頂点に隣接する頂点は、中間的な影響を受ける
                            newPosition = Vector3.Lerp(currentVertices[i], newPosition, 0.4f);
                        }
                        
                        smoothedVertices[i] = newPosition;
                    }
                }
                
                // 平滑化された頂点位置を現在の位置に更新
                System.Array.Copy(smoothedVertices, currentVertices, currentVertices.Length);
            }
        }
        
        /// <summary>
        /// 頂点が変形された隣接頂点を持つかどうかをチェック
        /// </summary>
        private static bool HasDeformedNeighbor(int vertexIndex, bool[] vertexDeformed, Dictionary<int, List<int>> adjacency)
        {
            if (!adjacency.TryGetValue(vertexIndex, out List<int> neighbors))
                return false;
            
            foreach (var neighbor in neighbors)
            {
                if (vertexDeformed[neighbor])
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 頂点が大きく変形された隣接頂点を持つかどうかをチェック
        /// </summary>
        private static bool HasStronglyDeformedNeighbor(
            int vertexIndex, 
            bool[] vertexDeformed, 
            Dictionary<int, List<int>> adjacency, 
            Vector3[] currentVertices, 
            Vector3[] originalVertices)
        {
            if (!adjacency.TryGetValue(vertexIndex, out List<int> neighbors))
                return false;
            
            foreach (var neighbor in neighbors)
            {
                if (vertexDeformed[neighbor])
                {
                    // 変形の大きさをチェック
                    float deformationMagnitude = Vector3.Distance(currentVertices[neighbor], originalVertices[neighbor]);
                    if (deformationMagnitude > 0.01f) // 閾値は調整可能
                        return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 貫通処理のヘルパーメソッド
        /// </summary>
        private static void ProcessPenetration(
            Vector3[] clothingVertices,
            Matrix4x4 clothingLocalToWorld,
            Matrix4x4 clothingWorldToLocal,
            List<Mesh> avatarMeshes,
            List<Matrix4x4> avatarMatrices,
            float penetrationThreshold,
            float pushOutDistance,
            int triangleStep,
            float boundingBoxExpansion,
            ref int adjustedVertices,
            ref bool meshModified,
            bool[] vertexChecked,
            bool[] vertexDeformed)
        {
            if (avatarMeshes.Count == 0) return;
            
            // 複数の貫通を検出した場合の解決戦略
            // 各頂点ごとに最適な調整方向を決定するためのデータ
            Dictionary<int, List<PenetrationInfo>> vertexPenetrations = new Dictionary<int, List<PenetrationInfo>>();
            
            // 各頂点に対して貫通をチェック
            for (int i = 0; i < clothingVertices.Length; i++)
            {
                // 既にチェック済みの頂点はスキップ
                if (vertexChecked[i]) continue;
                
                // 衣装の頂点をワールド座標に変換
                Vector3 worldVertex = clothingLocalToWorld.MultiplyPoint3x4(clothingVertices[i]);
                
                bool penetrationDetected = false;
                
                // 各アバターメッシュとの貫通をチェック
                for (int meshIndex = 0; meshIndex < avatarMeshes.Count; meshIndex++)
                {
                    // アバターメッシュと変換行列を取得
                    Mesh avatarMesh = avatarMeshes[meshIndex];
                    Matrix4x4 avatarMatrix = avatarMatrices[meshIndex];
                    Matrix4x4 avatarWorldToLocal = avatarMatrix.inverse;
                    
                    // アバターのローカル座標に変換
                    Vector3 avatarLocalVertex = avatarWorldToLocal.MultiplyPoint3x4(worldVertex);
                    
                    // アバターのバウンディングボックスをチェック
                    Bounds avatarBounds = avatarMesh.bounds;
                    avatarBounds.Expand(boundingBoxExpansion); // 境界を少し広げて余裕を持たせる
                    
                    if (!avatarBounds.Contains(avatarLocalVertex))
                    {
                        continue; // この頂点はアバターの範囲外
                    }
                    
                    // アバターの三角形との貫通チェック
                    Vector3[] avatarVertices = avatarMesh.vertices;
                    int[] avatarTriangles = avatarMesh.triangles;
                    
                    for (int t = 0; t < avatarTriangles.Length; t += triangleStep)
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
                            
                            // ワールド座標に変換した法線方向
                            Vector3 worldNormal = avatarMatrix.MultiplyVector(triangleNormal).normalized;
                            
                            // この頂点の貫通情報リストに追加
                            if (!vertexPenetrations.TryGetValue(i, out List<PenetrationInfo> penetrations))
                            {
                                penetrations = new List<PenetrationInfo>();
                                vertexPenetrations[i] = penetrations;
                            }
                            
                            // 貫通情報を追加
                            penetrations.Add(new PenetrationInfo(distance, worldNormal));
                            
                            // 大きな貫通が見つかったらループを抜ける（最適化）
                            if (distance < 0.005f) break;
                        }
                    }
                    
                    // 大きな貫通が見つかったらメッシュループを抜ける（最適化）
                    if (penetrationDetected && vertexPenetrations.TryGetValue(i, out var list) && 
                        list.Any(p => p.Depth < 0.005f)) 
                        break;
                }
                
                // この頂点はチェック済みとマーク
                vertexChecked[i] = true;
            }
            
            // 検出された貫通に基づいて頂点を調整
            foreach (var entry in vertexPenetrations)
            {
                int vertexIndex = entry.Key;
                List<PenetrationInfo> penetrations = entry.Value;
                
                if (penetrations.Count == 0) continue;
                
                // 最適な調整方向を決定
                Vector3 adjustmentDirection;
                float penetrationDepth;
                
                if (penetrations.Count == 1)
                {
                    // 単一の貫通
                    adjustmentDirection = penetrations[0].Direction;
                    penetrationDepth = penetrations[0].Depth;
                }
                else
                {
                    // 複数の貫通 - 最も浅い方向を優先
                    var bestPenetration = penetrations.OrderBy(p => p.Depth).First();
                    adjustmentDirection = bestPenetration.Direction;
                    penetrationDepth = bestPenetration.Depth;
                    
                    // より良い戦略：貫通方向の加重平均
                    // この場合、深い貫通に大きな重みを付ける
                    /*
                    Vector3 weightedDirection = Vector3.zero;
                    float totalWeight = 0f;
                    
                    foreach (var penetration in penetrations)
                    {
                        // 深い貫通ほど大きな重みを持つ (inverse)
                        float weight = 1f / Mathf.Max(0.001f, penetration.Depth);
                        weightedDirection += penetration.Direction * weight;
                        totalWeight += weight;
                    }
                    
                    if (totalWeight > 0f)
                    {
                        adjustmentDirection = (weightedDirection / totalWeight).normalized;
                    }
                    */
                }
                
                // 衣装の頂点をワールド座標に変換
                Vector3 worldVertex = clothingLocalToWorld.MultiplyPoint3x4(clothingVertices[vertexIndex]);
                
                // 法線方向に頂点を押し出す（貫通深度 + 余裕分）
                Vector3 adjustedWorldVertex = worldVertex + adjustmentDirection * (penetrationDepth + pushOutDistance);
                
                // 衣装のローカル座標に戻す
                clothingVertices[vertexIndex] = clothingWorldToLocal.MultiplyPoint3x4(adjustedWorldVertex);
                
                // この頂点が変形されたとマーク
                vertexDeformed[vertexIndex] = true;
                
                meshModified = true;
                adjustedVertices++;
            }
        }
        
        /// <summary>
        /// 三角形上の最近点を計算
        /// </summary>
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
    }
}
