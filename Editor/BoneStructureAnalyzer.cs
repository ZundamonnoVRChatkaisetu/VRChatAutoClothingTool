using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VRChatAutoClothingTool
{
    /// <summary>
    /// アバターと衣装のボーン構造を分析するためのクラス
    /// </summary>
    public class BoneStructureAnalyzer
    {
        // 基本的なボーン名のリスト（VRChatの標準的なボーン名）
        private readonly List<string> commonBoneNames = new List<string>
        {
            "Hips", "Spine", "Chest", "UpperChest", "Neck", "Head",
            "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
            "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
            "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes",
            "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes"
        };
        
        // 追加のボーン名パターン
        private readonly List<string> additionalBonePatterns = new List<string>
        {
            "UpperLeg.L", "UpperLeg_L",
            "UpperLeg.R", "UpperLeg_R",
            "Shoulder.L", "Shoulder_L",
            "Shoulder.R", "Shoulder_R"
        };
        
        // 指のボーン名
        private readonly List<string> fingerNames = new List<string> { "Thumb", "Index", "Middle", "Ring", "Little" };
        private readonly List<string> jointNames = new List<string> { "Proximal", "Intermediate", "Distal" };
        
        // 各ハンドサイド
        private readonly string[] handSides = new string[] { "Left", "Right" };
        
        /// <summary>
        /// アバターと衣装のボーン構造を分析し、マッピングリストを生成する
        /// </summary>
        /// <param name="avatarObject">アバターのGameObject</param>
        /// <param name="clothingObject">衣装のGameObject</param>
        /// <returns>ボーンマッピングのリスト</returns>
        public List<BoneMapping> AnalyzeBones(GameObject avatarObject, GameObject clothingObject)
        {
            if (avatarObject == null || clothingObject == null)
                return new List<BoneMapping>();
            
            // 結果格納用のリスト
            List<BoneMapping> boneMappings = new List<BoneMapping>();
            
            // 完全なボーン名リストを生成
            List<string> fullBoneNamesList = GenerateFullBoneNamesList();
            
            // アバターと衣装のTransformを取得
            var avatarTransforms = avatarObject.GetComponentsInChildren<Transform>();
            var clothingTransforms = clothingObject.GetComponentsInChildren<Transform>();
            
            // アバターのボーンを検索
            var avatarBones = FindBonesInTransforms(avatarTransforms, fullBoneNamesList);
            
            // 衣装のボーンを検索
            var clothingBones = FindBonesInTransforms(clothingTransforms, fullBoneNamesList);
            
            // マッピングリストを作成
            foreach (var avatarBone in avatarBones)
            {
                var boneName = avatarBone.Key;
                var avatarTransform = avatarBone.Value;
                
                // 同じ名前の衣装ボーンを探す
                Transform clothingTransform = FindMatchingClothingBone(boneName, clothingBones);
                
                // マッピングを追加
                boneMappings.Add(new BoneMapping
                {
                    BoneName = boneName,
                    AvatarBone = avatarTransform,
                    ClothingBone = clothingTransform
                });
            }
            
            return boneMappings;
        }
        
        /// <summary>
        /// 完全なボーン名リストを生成する
        /// </summary>
        private List<string> GenerateFullBoneNamesList()
        {
            List<string> fullBoneNamesList = new List<string>(commonBoneNames);
            
            // 追加のボーン名パターンを追加
            fullBoneNamesList.AddRange(additionalBonePatterns);
            
            // 指のボーン名を追加
            foreach (var hand in handSides)
            {
                foreach (var finger in fingerNames)
                {
                    foreach (var joint in jointNames)
                    {
                        fullBoneNamesList.Add($"{hand}{finger}{joint}");
                    }
                }
            }
            
            return fullBoneNamesList;
        }
        
        /// <summary>
        /// Transformの配列から特定のボーン名を持つものを検索
        /// </summary>
        private Dictionary<string, Transform> FindBonesInTransforms(Transform[] transforms, List<string> boneNames)
        {
            Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
            
            foreach (var boneTransform in transforms)
            {
                var boneName = boneTransform.name;
                
                // 正確な名前マッチング
                if (boneNames.Contains(boneName))
                {
                    bones[boneName] = boneTransform;
                    continue;
                }
                
                // パターンマッチング - ドット/アンダースコア表記に対応
                foreach (var pattern in boneNames)
                {
                    // "Shoulder.L" と "Shoulder_L" を同等に扱う
                    string normalizedPattern = NormalizeBoneName(pattern);
                    string normalizedBoneName = NormalizeBoneName(boneName);
                    
                    if (normalizedBoneName.Contains(normalizedPattern) || 
                        normalizedPattern.Contains(normalizedBoneName))
                    {
                        bones[boneName] = boneTransform;
                        break;
                    }
                }
            }
            
            return bones;
        }
        
        /// <summary>
        /// ボーン名を正規化（ドットとアンダースコアの違いを無視）
        /// </summary>
        private string NormalizeBoneName(string boneName)
        {
            return boneName.Replace('.', '_').Replace('_', '.');
        }
        
        /// <summary>
        /// アバターのボーン名に対応する衣装のボーンを探す
        /// </summary>
        private Transform FindMatchingClothingBone(string avatarBoneName, Dictionary<string, Transform> clothingBones)
        {
            // 完全一致を試す
            if (clothingBones.TryGetValue(avatarBoneName, out Transform clothingTransform))
            {
                return clothingTransform;
            }
            
            // 部分一致または類似パターンを探す
            string normalizedBoneName = NormalizeBoneName(avatarBoneName);
            
            foreach (var clothingBone in clothingBones)
            {
                string normalizedClothingName = NormalizeBoneName(clothingBone.Key);
                
                // ドット/アンダースコアの違いを無視して一致するかチェック
                if (normalizedClothingName.Contains(normalizedBoneName) || 
                    normalizedBoneName.Contains(normalizedClothingName))
                {
                    return clothingBone.Value;
                }
            }
            
            // 位置ベースのマッチングを試みる（将来実装）
            
            return null;
        }
        
        /// <summary>
        /// 二つのボーン名が同等かどうかを判定
        /// </summary>
        public bool AreBonesEquivalent(string boneName1, string boneName2)
        {
            // 完全一致
            if (boneName1 == boneName2) return true;
            
            // 正規化して比較
            string normalized1 = NormalizeBoneName(boneName1);
            string normalized2 = NormalizeBoneName(boneName2);
            
            if (normalized1 == normalized2) return true;
            
            // 部分一致
            if (normalized1.Contains(normalized2) || normalized2.Contains(normalized1)) return true;
            
            // 特別なケース（"Left"と"Right"の対称性など）の処理
            foreach (var side in handSides)
            {
                if (normalized1.Contains(side) && normalized2.Contains(side)) return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// ボーンの位置による近似度を計算
        /// </summary>
        public float CalculateBoneSimilarity(Transform bone1, Transform bone2)
        {
            if (bone1 == null || bone2 == null) return 0f;
            
            // 位置の類似度（距離が小さいほど類似度が高い）
            float positionSimilarity = 1f / (1f + Vector3.Distance(bone1.position, bone2.position));
            
            // 回転の類似度（角度が小さいほど類似度が高い）
            float rotationSimilarity = 1f - (Quaternion.Angle(bone1.rotation, bone2.rotation) / 180f);
            
            // 総合スコア（位置の類似度を重視）
            return positionSimilarity * 0.7f + rotationSimilarity * 0.3f;
        }
    }
}
