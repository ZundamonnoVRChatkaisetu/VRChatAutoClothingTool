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
        
        // 追加のボーン名パターン (ドット形式とアンダースコア形式両方) - 拡張版
        private readonly List<string> additionalBonePatterns = new List<string>
        {
            // UpperLegの様々なバリエーション
            "UpperLeg.L", "UpperLeg_L", "Upper.Leg.L", "Upper.Leg_L", 
            "Upper_Leg.L", "Upper_Leg_L", "Upper_leg.L", "Upper_leg_L",
            "UpperLeg.R", "UpperLeg_R", "Upper.Leg.R", "Upper.Leg_R", 
            "Upper_Leg.R", "Upper_Leg_R", "Upper_leg.R", "Upper_leg_R",
            
            // Shoulderの様々なバリエーション
            "Shoulder.L", "Shoulder_L", "shoulder.L", "shoulder_L",
            "Shoulder.R", "Shoulder_R", "shoulder.R", "shoulder_R",
            
            // 追加のLowerLegバリエーション
            "LowerLeg.L", "LowerLeg_L", "Lower.Leg.L", "Lower.Leg_L", 
            "Lower_Leg.L", "Lower_Leg_L", "Lower_leg.L", "Lower_leg_L",
            "LowerLeg.R", "LowerLeg_R", "Lower.Leg.R", "Lower.Leg_R", 
            "Lower_Leg.R", "Lower_Leg_R", "Lower_leg.R", "Lower_leg_R",
            
            // 追加のUpperArmバリエーション
            "UpperArm.L", "UpperArm_L", "Upper.Arm.L", "Upper.Arm_L", 
            "Upper_Arm.L", "Upper_Arm_L", "Upper_arm.L", "Upper_arm_L",
            "UpperArm.R", "UpperArm_R", "Upper.Arm.R", "Upper.Arm_R", 
            "Upper_Arm.R", "Upper_Arm_R", "Upper_arm.R", "Upper_arm_R",
            
            // 追加のLowerArmバリエーション
            "LowerArm.L", "LowerArm_L", "Lower.Arm.L", "Lower.Arm_L", 
            "Lower_Arm.L", "Lower_Arm_L", "Lower_arm.L", "Lower_arm_L",
            "LowerArm.R", "LowerArm_R", "Lower.Arm.R", "Lower.Arm_R", 
            "Lower_Arm.R", "Lower_Arm_R", "Lower_arm.R", "Lower_arm_R",
            
            // 追加の脚バリエーション (leg.L/Rなど)
            "leg.L", "leg_L", "Leg.L", "Leg_L", "LEG.L", "LEG_L",
            "leg.R", "leg_R", "Leg.R", "Leg_R", "LEG.R", "LEG_R",
            
            // 追加の腕バリエーション (arm.L/Rなど)
            "arm.L", "arm_L", "Arm.L", "Arm_L", "ARM.L", "ARM_L",
            "arm.R", "arm_R", "Arm.R", "Arm_R", "ARM.R", "ARM_R",
            
            // 足首バリエーション
            "ankle.L", "ankle_L", "Ankle.L", "Ankle_L",
            "ankle.R", "ankle_R", "Ankle.R", "Ankle_R",
            
            // 手首バリエーション
            "wrist.L", "wrist_L", "Wrist.L", "Wrist_L",
            "wrist.R", "wrist_R", "Wrist.R", "Wrist_R",
            
            // 肩バリエーション
            "clavicle.L", "clavicle_L", "Clavicle.L", "Clavicle_L",
            "clavicle.R", "clavicle_R", "Clavicle.R", "Clavicle_R"
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
            
            // 正規化されたボーン名のマッピング (大文字小文字、ドット、アンダースコアの違いを無視)
            Dictionary<string, string> normalizedNameMap = new Dictionary<string, string>();
            foreach (var boneName in boneNames)
            {
                string normalizedName = NormalizeBoneName(boneName).ToLower();
                if (!normalizedNameMap.ContainsKey(normalizedName))
                {
                    normalizedNameMap[normalizedName] = boneName;
                }
            }
            
            foreach (var boneTransform in transforms)
            {
                var boneName = boneTransform.name;
                
                // 正確な名前マッチング
                if (boneNames.Contains(boneName))
                {
                    bones[boneName] = boneTransform;
                    continue;
                }
                
                // 正規化した名前でマッチング
                string normalizedBoneName = NormalizeBoneName(boneName).ToLower();
                if (normalizedNameMap.ContainsKey(normalizedBoneName))
                {
                    // 元の標準名を使用
                    bones[normalizedNameMap[normalizedBoneName]] = boneTransform;
                    continue;
                }
                
                // パターンマッチング（部分一致）- 大文字小文字を区別せずに
                foreach (var pattern in boneNames)
                {
                    // "Shoulder.L" と "Shoulder_L" を同等に扱う
                    string normalizedPattern = NormalizeBoneName(pattern).ToLower();
                    
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
            // ドットとアンダースコアを共通文字に置換
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
            
            // 正規化された名前でマッチングを試みる
            string normalizedAvatarBoneName = NormalizeBoneName(avatarBoneName).ToLower();
            
            // 部分一致または類似パターンを探す
            foreach (var clothingBone in clothingBones)
            {
                string normalizedClothingName = NormalizeBoneName(clothingBone.Key).ToLower();
                
                // 正規化された名前でスマートマッチング
                if (IsSmartBoneMatch(normalizedAvatarBoneName, normalizedClothingName))
                {
                    return clothingBone.Value;
                }
            }
            
            // Leg/Arm等の基本部位名でフォールバックマッチング
            foreach (var clothingBone in clothingBones)
            {
                string normalizedClothingName = NormalizeBoneName(clothingBone.Key).ToLower();
                
                if (DoesShareCommonPartName(normalizedAvatarBoneName, normalizedClothingName))
                {
                    return clothingBone.Value;
                }
            }
            
            // 位置ベースのマッチングを追加（空間的な位置関係で最も近いボーンを見つける）
            return null;
        }
        
        /// <summary>
        /// 2つのボーン名が特定の部位（Leg/Arm等）を共有しているかを判定
        /// </summary>
        private bool DoesShareCommonPartName(string name1, string name2)
        {
            string[] commonParts = new string[] 
            { 
                "leg", "arm", "hand", "foot", "shoulder", "head", 
                "spine", "chest", "hip", "neck",
                "ankle", "wrist", "elbow", "knee", "clavicle", "toe" 
            };
            
            foreach (var part in commonParts)
            {
                if (name1.Contains(part) && name2.Contains(part))
                {
                    // 左右の一致も確認
                    bool name1IsLeft = name1.Contains("left") || name1.Contains(".l") || name1.EndsWith("l");
                    bool name1IsRight = name1.Contains("right") || name1.Contains(".r") || name1.EndsWith("r");
                    bool name2IsLeft = name2.Contains("left") || name2.Contains(".l") || name2.EndsWith("l");
                    bool name2IsRight = name2.Contains("right") || name2.Contains(".r") || name2.EndsWith("r");
                    
                    // 左右が一致、または左右の指定がない場合
                    if ((name1IsLeft && name2IsLeft) || 
                        (name1IsRight && name2IsRight) || 
                        (!name1IsLeft && !name1IsRight && !name2IsLeft && !name2IsRight))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// スマートなボーンマッチング（Upper_leg.L -> UpperLeg.L などを処理）
        /// </summary>
        private bool IsSmartBoneMatch(string name1, string name2)
        {
            // 完全一致
            if (name1 == name2) return true;
            
            // ドット/アンダースコアの違いを無視した一致
            if (name1.Replace('.', '_').Replace('_', '.') == 
                name2.Replace('.', '_').Replace('_', '.')) 
                return true;
            
            // 部分一致
            if (name1.Contains(name2) || name2.Contains(name1)) return true;
            
            // Upper_leg.L -> UpperLeg.L などのバリエーション
            if (TryMatchUpperLowerVariants(name1, name2)) return true;
            
            // Left/Right と .L/.R の対応
            if (MatchLeftRightVariants(name1, name2)) return true;
            
            // 特殊なケース: upper_leg.l と leg.l など、より一般的な部位名も考慮
            if (TryMatchGenericPartNames(name1, name2)) return true;
            
            return false;
        }
        
        /// <summary>
        /// Upper_leg.L -> UpperLeg.L などのバリエーションを処理
        /// </summary>
        private bool TryMatchUpperLowerVariants(string name1, string name2)
        {
            // Upper/Lowerのパターン
            string[] patterns = new string[] 
            { 
                "upper", "lower", "leg", "arm", "shoulder", "ankle", "wrist", "knee", "elbow" 
            };
            
            // 異なる区切り文字による同一ボーンのバリエーションをチェック
            foreach (var pattern in patterns)
            {
                if ((name1.Contains(pattern) && name2.Contains(pattern))
                    || (name1.Contains(pattern.ToUpper()) && name2.Contains(pattern))
                    || (name1.Contains(pattern) && name2.Contains(pattern.ToUpper())))
                {
                    string[] variants = new string[] 
                    {
                        pattern,              // leg
                        "." + pattern,        // .leg
                        "_" + pattern,        // _leg
                        pattern.ToUpper(),    // LEG
                        "." + pattern.ToUpper(), // .LEG
                        "_" + pattern.ToUpper()  // _LEG
                    };
                    
                    foreach (var variant1 in variants)
                    {
                        if (name1.Contains(variant1))
                        {
                            foreach (var variant2 in variants)
                            {
                                if (name2.Contains(variant2))
                                {
                                    // 左右の一致も確認
                                    bool name1IsLeft = name1.Contains("left") || name1.Contains(".l") || name1.EndsWith("l");
                                    bool name1IsRight = name1.Contains("right") || name1.Contains(".r") || name1.EndsWith("r");
                                    bool name2IsLeft = name2.Contains("left") || name2.Contains(".l") || name2.EndsWith("l");
                                    bool name2IsRight = name2.Contains("right") || name2.Contains(".r") || name2.EndsWith("r");
                                    
                                    if ((name1IsLeft && name2IsLeft) || (name1IsRight && name2IsRight))
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 一般的な部位名のマッチング (leg, arm等と、upper_leg, upper_armなどの対応)
        /// </summary>
        private bool TryMatchGenericPartNames(string name1, string name2)
        {
            // 基本部位名と詳細部位名のペア
            Dictionary<string, List<string>> partVariants = new Dictionary<string, List<string>>()
            {
                { "leg", new List<string> { "upperleg", "lowerleg", "thigh", "shin", "knee" } },
                { "arm", new List<string> { "upperarm", "lowerarm", "elbow" } },
                { "hand", new List<string> { "wrist", "palm", "finger" } },
                { "foot", new List<string> { "ankle", "toe", "heel" } }
            };
            
            // 左右の識別子
            string[] sides = new string[] { "l", "r", "left", "right", ".l", ".r", "_l", "_r" };
            
            // 名前を正規化（小文字化、スペース・点・アンダースコア除去）
            string cleanName1 = name1.ToLower().Replace(" ", "").Replace(".", "").Replace("_", "");
            string cleanName2 = name2.ToLower().Replace(" ", "").Replace(".", "").Replace("_", "");
            
            // 左右の識別
            string side1 = "";
            string side2 = "";
            
            foreach (var side in sides)
            {
                if (cleanName1.Contains(side) || cleanName1.EndsWith(side))
                {
                    side1 = side.Replace(".", "").Replace("_", "");
                }
                
                if (cleanName2.Contains(side) || cleanName2.EndsWith(side))
                {
                    side2 = side.Replace(".", "").Replace("_", "");
                }
            }
            
            // 左右が一致するか確認
            bool sidesMatch = (side1 == side2) || 
                             (side1 == "l" && side2 == "left") || 
                             (side1 == "left" && side2 == "l") ||
                             (side1 == "r" && side2 == "right") || 
                             (side1 == "right" && side2 == "r");
            
            if (!sidesMatch) return false;
            
            // 部位名のチェック
            foreach (var partEntry in partVariants)
            {
                string basePart = partEntry.Key;
                List<string> variants = partEntry.Value;
                
                // 一方が基本部位で、他方が詳細部位の場合
                bool name1HasBase = cleanName1.Contains(basePart);
                bool name2HasBase = cleanName2.Contains(basePart);
                
                bool name1HasVariant = variants.Any(v => cleanName1.Contains(v));
                bool name2HasVariant = variants.Any(v => cleanName2.Contains(v));
                
                if ((name1HasBase && name2HasVariant) || (name1HasVariant && name2HasBase))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Left/Right と .L/.R の対応を処理
        /// </summary>
        private bool MatchLeftRightVariants(string name1, string name2)
        {
            // 左右のバリエーション
            string[][] sideVariants = new string[][] 
            {
                new string[] { "left", ".l", "_l", "l." },
                new string[] { "right", ".r", "_r", "r." }
            };
            
            // 各バリエーションでチェック
            foreach (var sideGroup in sideVariants)
            {
                bool name1HasSide = false;
                bool name2HasSide = false;
                
                foreach (var side in sideGroup)
                {
                    name1HasSide |= name1.Contains(side);
                    name2HasSide |= name2.Contains(side);
                }
                
                if (name1HasSide && name2HasSide)
                {
                    // 基本部分の名前（leg, arm等）も一致するかチェック
                    string baseName1 = RemoveSideSuffix(name1);
                    string baseName2 = RemoveSideSuffix(name2);
                    
                    if (baseName1.Contains(baseName2) || baseName2.Contains(baseName1))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// ボーン名から左右の接尾辞を除去
        /// </summary>
        private string RemoveSideSuffix(string boneName)
        {
            // .L, .R, _L, _R等の接尾辞を削除
            string[] suffixes = new string[] { ".l", ".r", "_l", "_r", "l.", "r." };
            string result = boneName.ToLower();
            
            foreach (var suffix in suffixes)
            {
                if (result.EndsWith(suffix))
                {
                    return result.Substring(0, result.Length - suffix.Length);
                }
            }
            
            // Left, Right等の文字列を削除
            result = result.Replace("left", "").Replace("right", "");
            
            return result;
        }
        
        /// <summary>
        /// 二つのボーン名が同等かどうかを判定
        /// </summary>
        public bool AreBonesEquivalent(string boneName1, string boneName2)
        {
            // スマートマッチングで判定
            return IsSmartBoneMatch(boneName1.ToLower(), boneName2.ToLower());
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
