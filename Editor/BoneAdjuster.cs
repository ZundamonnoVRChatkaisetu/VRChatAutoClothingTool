using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace VRChatAutoClothingTool
{
    public class BoneAdjuster
    {
        // ボーン名とボーン位置の情報を格納する構造体
        public struct BoneInfo
        {
            public string Name;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public Transform Transform;
        }
        
        // アバターボーンマップ
        private Dictionary<string, BoneInfo> avatarBones = new Dictionary<string, BoneInfo>();
        
        // 衣装ボーンマップ
        private Dictionary<string, BoneInfo> clothingBones = new Dictionary<string, BoneInfo>();
        
        // ボーンのヒエラルキーを記録
        private Dictionary<string, List<string>> boneHierarchy = new Dictionary<string, List<string>>();
        
        // ボーン調整のためのスケーリング設定
        private Vector3 globalScaleFactor = Vector3.one;
        
        // 基本的なヒューマノイドボーン名のリスト
        private static readonly string[] humanoidBoneNames = new string[]
        {
            "Hips", "Spine", "Chest", "UpperChest", "Neck", "Head",
            "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
            "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
            "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes",
            "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes"
        };
        
        // 指のボーン名
        private static readonly string[] fingerBoneNames = new string[]
        {
            "Thumb", "Index", "Middle", "Ring", "Little"
        };
        
        // 指の関節名
        private static readonly string[] fingerJointNames = new string[]
        {
            "Proximal", "Intermediate", "Distal"
        };
        
        // コンストラクタ
        public BoneAdjuster(GameObject avatarObject, GameObject clothingObject, Vector3 scaleFactor)
        {
            globalScaleFactor = scaleFactor;
            
            // アバターのボーン情報を収集
            CollectBoneInfo(avatarObject, avatarBones, true);
            
            // 衣装のボーン情報を収集
            CollectBoneInfo(clothingObject, clothingBones, false);
        }
        
        // ボーン情報を収集するメソッド
        private void CollectBoneInfo(GameObject targetObject, Dictionary<string, BoneInfo> boneMap, bool isAvatar)
        {
            if (targetObject == null) return;
            
            // すべてのTransformコンポーネントを取得
            var transforms = targetObject.GetComponentsInChildren<Transform>();
            
            // ボーン名リストを生成
            var boneNames = new List<string>(humanoidBoneNames);
            
            // 指のボーン名を追加
            foreach (var hand in new[] { "Left", "Right" })
            {
                foreach (var finger in fingerBoneNames)
                {
                    foreach (var joint in fingerJointNames)
                    {
                        boneNames.Add($"{hand}{finger}{joint}");
                    }
                }
            }
            
            // オブジェクトのTransformを検索しボーン情報を収集
            foreach (var transform in transforms)
            {
                // 標準ボーン名と一致するか、または含まれているかチェック
                var boneName = transform.name;
                bool isHumanoidBone = boneNames.Contains(boneName) || 
                                     boneNames.Any(name => boneName.Contains(name));
                
                if (isHumanoidBone || ShouldIncludeBone(boneName))
                {
                    // ボーン情報を作成
                    BoneInfo boneInfo = new BoneInfo
                    {
                        Name = boneName,
                        Position = transform.position,
                        Rotation = transform.rotation,
                        Scale = transform.localScale,
                        Transform = transform
                    };
                    
                    // ボーンマップに追加
                    boneMap[boneName] = boneInfo;
                    
                    // ヒエラルキー情報を記録（アバターのみ）
                    if (isAvatar && transform.parent != null)
                    {
                        string parentName = transform.parent.name;
                        if (!boneHierarchy.ContainsKey(parentName))
                        {
                            boneHierarchy[parentName] = new List<string>();
                        }
                        boneHierarchy[parentName].Add(boneName);
                    }
                }
            }
        }
        
        // 追加で含めるべきボーンかどうかを判定
        private bool ShouldIncludeBone(string boneName)
        {
            // VRChatやUnityのアバターに特有の追加ボーン
            string[] additionalBones = new string[]
            {
                "Eye", "Jaw", "Breast", "Shoulder", "Arm", "Leg", "Foot", "Toe", "Finger",
                "Hair", "Skirt", "Tail", "Wing", "Ear", "Tongue", "Teeth", "Accessory"
            };
            
            return additionalBones.Any(bone => boneName.Contains(bone));
        }
        
        // 最も近いボーンを見つけるメソッド
        public Transform FindClosestBone(string avatarBoneName)
        {
            if (!avatarBones.ContainsKey(avatarBoneName))
                return null;
            
            var avatarBone = avatarBones[avatarBoneName];
            Transform closestBone = null;
            float minDistance = float.MaxValue;
            
            foreach (var clothingBone in clothingBones.Values)
            {
                // 名前が一致する場合は優先
                if (clothingBone.Name == avatarBoneName)
                {
                    return clothingBone.Transform;
                }
                
                // 部分一致する場合も考慮
                if (clothingBone.Name.Contains(avatarBoneName) || avatarBoneName.Contains(clothingBone.Name))
                {
                    float distance = Vector3.Distance(avatarBone.Position, clothingBone.Position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestBone = clothingBone.Transform;
                    }
                }
            }
            
            // それでも見つからない場合は距離のみで探す
            if (closestBone == null)
            {
                foreach (var clothingBone in clothingBones.Values)
                {
                    float distance = Vector3.Distance(avatarBone.Position, clothingBone.Position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestBone = clothingBone.Transform;
                    }
                }
            }
            
            return closestBone;
        }
        
        // 衣装のボーンをアバターのボーンに合わせて調整するメソッド
        public void AdjustClothingBones(Transform clothingRoot, List<BoneMapping> boneMappings)
        {
            if (clothingRoot == null) return;
            
            // 衣装のルートトランスフォームをアバターのHipsに合わせる
            foreach (var mapping in boneMappings)
            {
                if (mapping.BoneName == "Hips" && mapping.AvatarBone != null && mapping.ClothingBone != null)
                {
                    // ルート位置の調整
                    clothingRoot.position = mapping.AvatarBone.position;
                    clothingRoot.rotation = mapping.AvatarBone.rotation;
                    
                    // グローバルスケールファクターを適用
                    clothingRoot.localScale = Vector3.Scale(clothingRoot.localScale, globalScaleFactor);
                    break;
                }
            }
            
            // 各ボーンのマッピングに基づいて調整
            foreach (var mapping in boneMappings)
            {
                if (mapping.AvatarBone != null && mapping.ClothingBone != null)
                {
                    AdjustBone(mapping.AvatarBone, mapping.ClothingBone);
                }
            }
            
            // スキンメッシュレンダラーの調整
            AdjustSkinnedMeshRenderers(clothingRoot.gameObject);
        }
        
        // 個別のボーンを調整するメソッド
        private void AdjustBone(Transform avatarBone, Transform clothingBone)
        {
            if (avatarBone == null || clothingBone == null) return;
            
            // ボーンの位置と回転を合わせる
            clothingBone.position = avatarBone.position;
            clothingBone.rotation = avatarBone.rotation;
            
            // 特定のボーン（主要なボディボーン）にはスケーリングも適用
            if (IsMainBodyBone(clothingBone.name))
            {
                clothingBone.localScale = Vector3.Scale(clothingBone.localScale, globalScaleFactor);
            }
        }
        
        // 主要なボディボーンかどうかを判定
        private bool IsMainBodyBone(string boneName)
        {
            string[] mainBodyBones = new string[]
            {
                "Hips", "Spine", "Chest", "UpperChest", "Waist", "Body"
            };
            
            return mainBodyBones.Any(bone => boneName.Contains(bone));
        }
        
        // スキンメッシュレンダラーを調整するメソッド
        private void AdjustSkinnedMeshRenderers(GameObject clothingObject)
        {
            if (clothingObject == null) return;
            
            // スキンメッシュレンダラーを取得
            var renderers = clothingObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null) continue;
                
                // メッシュを複製して編集可能にする
                Mesh meshCopy = Object.Instantiate(renderer.sharedMesh);
                
                // ボーンウェイトの調整（必要に応じて）
                
                // バインドポーズの更新が必要な場合
                Matrix4x4[] bindPoses = meshCopy.bindposes;
                bool bindPosesUpdated = false;
                
                if (bindPosesUpdated)
                {
                    meshCopy.bindposes = bindPoses;
                }
                
                // 調整したメッシュを適用
                renderer.sharedMesh = meshCopy;
            }
        }
        
        // ボーンの自動マッピングを生成
        public List<BoneMapping> GenerateBoneMappings()
        {
            var mappings = new List<BoneMapping>();
            
            // 標準ヒューマノイドボーンのマッピング
            foreach (var boneName in humanoidBoneNames)
            {
                if (avatarBones.ContainsKey(boneName))
                {
                    var avatarBone = avatarBones[boneName];
                    Transform clothingBone = FindClosestBone(boneName);
                    
                    mappings.Add(new BoneMapping
                    {
                        BoneName = boneName,
                        AvatarBone = avatarBone.Transform,
                        ClothingBone = clothingBone
                    });
                }
            }
            
            // 指のボーンのマッピング
            foreach (var hand in new[] { "Left", "Right" })
            {
                foreach (var finger in fingerBoneNames)
                {
                    foreach (var joint in fingerJointNames)
                    {
                        string boneName = $"{hand}{finger}{joint}";
                        if (avatarBones.ContainsKey(boneName))
                        {
                            var avatarBone = avatarBones[boneName];
                            Transform clothingBone = FindClosestBone(boneName);
                            
                            mappings.Add(new BoneMapping
                            {
                                BoneName = boneName,
                                AvatarBone = avatarBone.Transform,
                                ClothingBone = clothingBone
                            });
                        }
                    }
                }
            }
            
            return mappings;
        }
        
        // 新しいアバターに対する衣装の調整を最適化
        public void OptimizeForAvatar(GameObject clothingObject)
        {
            if (clothingObject == null) return;
            
            // スキンメッシュレンダラーを取得
            var renderers = clothingObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null) continue;
                
                // メッシュを複製
                Mesh optimizedMesh = Object.Instantiate(renderer.sharedMesh);
                string assetPath = $"Assets/OptimizedMeshes/{clothingObject.name}_{renderer.name}_Optimized.asset";
                
                // アセットフォルダの作成
                string directory = System.IO.Path.GetDirectoryName(assetPath);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                
                // メッシュの最適化処理をここに追加
                // （バインドポーズの更新やウェイトの再割り当てなど）
                
                // メッシュをアセットとして保存
                AssetDatabase.CreateAsset(optimizedMesh, assetPath);
                AssetDatabase.SaveAssets();
                
                // 最適化したメッシュを適用
                renderer.sharedMesh = optimizedMesh;
            }
        }
    }
}
