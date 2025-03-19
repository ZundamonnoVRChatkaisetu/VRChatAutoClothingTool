using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace VRChatAutoClothingTool
{
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
        public static void AdjustClothingPenetration(
            GameObject avatarObject, 
            GameObject clothingObject, 
            float pushOutDistance = 0.01f,
            float penetrationThreshold = 0.015f,
            bool advancedSampling = true, 
            bool preferBodyMeshes = true)
        {
            if (avatarObject == null || clothingObject == null) return;
            
            // パラメータの検証と制限
            pushOutDistance = Mathf.Clamp(pushOutDistance, 0.0005f, 0.05f);
            penetrationThreshold = Mathf.Clamp(penetrationThreshold, 0.001f, 0.05f);
            
            Debug.Log($"貫通チェック開始: 閾値 = {penetrationThreshold}, 押し出し距離 = {pushOutDistance}, " +
                      $"高度なサンプリング = {advancedSampling}, ボディメッシュ優先 = {preferBodyMeshes}");
            
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
                if (clothingRenderer.sharedMesh == null) continue;
                
                // 衣装の現在のメッシュを取得（内容をコピー）
                Mesh clothingMesh = clothingRenderer.sharedMesh;
                Mesh adjustedMesh = Object.Instantiate(clothingMesh);
                
                string meshName = clothingRenderer.name;
                Debug.Log($"メッシュ '{meshName}' を処理中...");
                
                Vector3[] clothingVertices = adjustedMesh.vertices;
                totalVertices += clothingVertices.Length;
                
                // 衣装のローカル→ワールド変換行列
                Matrix4x4 clothingLocalToWorld = clothingRenderer.localToWorldMatrix;
                Matrix4x4 clothingWorldToLocal = clothingLocalToWorld.inverse;
                
                bool meshModified = false;
                // 各頂点が処理済みかどうかを記録する配列
                bool[] vertexChecked = new bool[clothingVertices.Length];
                
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
                    vertexChecked
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
                        vertexChecked
                    );
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
            bool[] vertexChecked)
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