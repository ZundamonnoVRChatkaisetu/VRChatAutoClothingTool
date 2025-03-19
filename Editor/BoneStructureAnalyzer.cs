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
            "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes",
            // 新しく追加：手足の詳細なボーン
            "Foot_L", "Foot_R", "Hand_L", "Hand_R", "Toe_L", "Toe_R",
            // Breast/乳房ボーンも追加
            "Breast_L", "Breast_R",
            // 特殊ボーンも追加
            "WingRoot", "TailRoot", "W_ArmCoverTag_L", "W_ArmCoverTag_R",
            "XC_WristTwist_L", "XC_WristTwist_R", "XC_ArmTwist_L", "XC_ArmTwist_R"
        };
        
        // 追加のボーン名パターン (ドット形式とアンダースコア形式両方) - 拡張版
        private readonly List<string> additionalBonePatterns = new List<string>
        {
            // UpperLegの様々なバリエーション
            "UpperLeg.L", "UpperLeg_L", "Upper.Leg.L", "Upper.Leg_L", 
            "Upper_Leg.L", "Upper_Leg_L", "Upper_leg.L", "Upper_leg_L",
            "UpperLeg.R", "UpperLeg_R", "Upper.Leg.R", "Upper.Leg_R", 
            "Upper_Leg.R", "Upper_Leg_R", "Upper_leg.R", "Upper_leg_R",
            "UpperLeg_L_001", "UpperLeg_R_001", "Upper_Leg_L_001", "Upper_Leg_R_001",
            
            // Shoulderの様々なバリエーション
            "Shoulder.L", "Shoulder_L", "shoulder.L", "shoulder_L",
            "Shoulder.R", "Shoulder_R", "shoulder.R", "shoulder_R",
            "Shoulder_L_001", "Shoulder_R_001",
            
            // 追加のLowerLegバリエーション
            "LowerLeg.L", "LowerLeg_L", "Lower.Leg.L", "Lower.Leg_L", 
            "Lower_Leg.L", "Lower_Leg_L", "Lower_leg.L", "Lower_leg_L",
            "LowerLeg.R", "LowerLeg_R", "Lower.Leg.R", "Lower.Leg_R", 
            "Lower_Leg.R", "Lower_Leg_R", "Lower_leg.R", "Lower_leg_R",
            "LowerLeg_L_001", "LowerLeg_R_001", "Lower_Leg_L_001", "Lower_Leg_R_001",
            
            // 追加のUpperArmバリエーション
            "UpperArm.L", "UpperArm_L", "Upper.Arm.L", "Upper.Arm_L", 
            "Upper_Arm.L", "Upper_Arm_L", "Upper_arm.L", "Upper_arm_L",
            "UpperArm.R", "UpperArm_R", "Upper.Arm.R", "Upper.Arm_R", 
            "Upper_Arm.R", "Upper_Arm_R", "Upper_arm.R", "Upper_arm_R",
            "UpperArm_L_001", "UpperArm_R_001", "Upper_Arm_L_001", "Upper_Arm_R_001",
            
            // 追加のLowerArmバリエーション
            "LowerArm.L", "LowerArm_L", "Lower.Arm.L", "Lower.Arm_L", 
            "Lower_Arm.L", "Lower_Arm_L", "Lower_arm.L", "Lower_arm_L",
            "LowerArm.R", "LowerArm_R", "Lower.Arm.R", "Lower.Arm_R", 
            "Lower_Arm.R", "Lower_Arm_R", "Lower_arm.R", "Lower_arm_R",
            "LowerArm_L_001", "LowerArm_R_001", "Lower_Arm_L_001", "Lower_Arm_R_001",
            
            // 追加の脚バリエーション (leg.L/Rなど)
            "leg.L", "leg_L", "Leg.L", "Leg_L", "LEG.L", "LEG_L",
            "leg.R", "leg_R", "Leg.R", "Leg_R", "LEG.R", "LEG_R",
            "leg_L_001", "leg_R_001", "Leg_L_001", "Leg_R_001",
            
            // 追加の腕バリエーション (arm.L/Rなど)
            "arm.L", "arm_L", "Arm.L", "Arm_L", "ARM.L", "ARM_L",
            "arm.R", "arm_R", "Arm.R", "Arm_R", "ARM.R", "ARM_R",
            "arm_L_001", "arm_R_001", "Arm_L_001", "Arm_R_001",
            
            // 足首バリエーション
            "ankle.L", "ankle_L", "Ankle.L", "Ankle_L",
            "ankle.R", "ankle_R", "Ankle.R", "Ankle_R",
            "ankle_L_001", "ankle_R_001", "Ankle_L_001", "Ankle_R_001",
            
            // 手首バリエーション
            "wrist.L", "wrist_L", "Wrist.L", "Wrist_L",
            "wrist.R", "wrist_R", "Wrist.R", "Wrist_R",
            "wrist_L_001", "wrist_R_001", "Wrist_L_001", "Wrist_R_001",
            
            // 肩バリエーション
            "clavicle.L", "clavicle_L", "Clavicle.L", "Clavicle_L",
            "clavicle.R", "clavicle_R", "Clavicle.R", "Clavicle_R",
            "clavicle_L_001", "clavicle_R_001", "Clavicle_L_001", "Clavicle_R_001",

            // 追加:装飾品、アクセサリ、紐などのマッピング対応
            "Accessory", "Ornament", "Himo", "Ribbon", "String", "Rope", "Belt", "Strap", "Attachment",
            "Accessory_L", "Accessory_R", "Ornament_L", "Ornament_R", "Himo_L", "Himo_R",
            "Ribbon_L", "Ribbon_R", "String_L", "String_R", "Rope_L", "Rope_R", "Belt_L", "Belt_R",
            "ShirtRibbon", "Tie", "Scarf", "Chain", "Necklace", "Collar", "Button", "Bow", "Brooch",
            "Emblem", "Badge", "Pin", "Tassel", "Fringe", "Trim", "Lace", "Fur", "Feather", "Attachment",
            
            // 新しく追加：手と足のバリエーション
            "Hand.L", "Hand.R", "Hand_L", "Hand_R", "hand.L", "hand.R", "hand_L", "hand_R",
            "Foot.L", "Foot.R", "Foot_L", "Foot_R", "foot.L", "foot.R", "foot_L", "foot_R",
            "Toe.L", "Toe.R", "Toe_L", "Toe_R", "toe.L", "toe.R", "toe_L", "toe_R",
            "Toes.L", "Toes.R", "Toes_L", "Toes_R", "toes.L", "toes.R", "toes_L", "toes_R",
            
            // 新しく追加：特殊ボーン
            "WingRoot", "TailRoot", "Wing", "Tail", "W_ArmCoverTag_L", "W_ArmCoverTag_R",
            "XC_WristTwist_L", "XC_WristTwist_R", "XC_ArmTwist_L", "XC_ArmTwist_R",
            
            // 新しく追加：指ボーン
            "Thumb_L", "Thumb_R", "Index_L", "Index_R", "Middle_L", "Middle_R", 
            "Ring_L", "Ring_R", "Little_L", "Little_R",
            "Finger1_L", "Finger1_R", "Finger2_L", "Finger2_R", "Finger3_L", "Finger3_R",
            "Finger4_L", "Finger4_R", "Finger5_L", "Finger5_R"
        };
        
        // 指のボーン名
        private readonly List<string> fingerNames = new List<string> { "Thumb", "Index", "Middle", "Ring", "Little" };
        private readonly List<string> jointNames = new List<string> { "Proximal", "Intermediate", "Distal" };
        
        // 各ハンドサイド
        private readonly string[] handSides = new string[] { "Left", "Right" };

        // 装飾ボーンのキーワード
        private readonly string[] decorationKeywords = new string[] 
        { 
            "himo", "accessory", "ornament", "ribbon", "string", "rope", 
            "belt", "strap", "attachment", "decoration", "button",
            "brooch", "pendant", "badge", "pin", "bangle", "shirtrrbbon",
            "tie", "scarf", "chain", "necklace", "collar", "bow",
            "emblem", "badge", "pin", "tassel", "fringe", "trim", "lace", 
            "fur", "feather", "decoration", "ornament", "accessory"
        };

        // 重要なボーン名（常にマッピングに含める）
        private readonly List<string> criticalBoneNames = new List<string>
        {
            "Hand_L", "Hand_R", "Foot_L", "Foot_R", "Toe_L", "Toe_R",
            "hand_l", "hand_r", "foot_l", "foot_r", "toe_l", "toe_r",
            "Hand.L", "Hand.R", "Foot.L", "Foot.R", "Toe.L", "Toe.R",
            "hand.l", "hand.r", "foot.l", "foot.r", "toe.l", "toe.r"
        };
        
        /// <summary>
        /// 未マッピングボーンとその親ボーンの相対位置・回転・スケールを保存
        /// </summary>
        public Dictionary<string, UnmappedBoneInfo> UnmappedBoneInfos { get; private set; } = new Dictionary<string, UnmappedBoneInfo>();
        
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
            
            // 衣装のボーンを検索 - まずは標準のボーン検索を行う
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
            
            // 再帰的にボーンを追加（親子関係を考慮）
            AddRecursiveBonesFromAvatar(avatarObject.transform, boneMappings);
            
            // 重要なボーン（Hand_L/R、Foot_L/R、Toe_L/R等）が含まれているか確認し、なければ追加
            EnsureCriticalBonesAreMapped(avatarTransforms, clothingTransforms, boneMappings);
            
            // 親子関係によるボーンの検索と追加（特に手足のボーン）
            FindAndMapHierarchicalBones(avatarTransforms, clothingTransforms, boneMappings);
            
            // マッピングされなかった衣装のボーンを処理
            ProcessUnmappedBones(clothingObject, clothingTransforms, boneMappings);
            
            return boneMappings;
        }

        /// <summary>
        /// 親子関係を使用して重要なボーンを検索し、マッピングに追加する
        /// </summary>
        private void FindAndMapHierarchicalBones(Transform[] avatarTransforms, Transform[] clothingTransforms, List<BoneMapping> boneMappings)
        {
            // 親子関係で特定の重要なボーンを探す（Hand/Foot/Toeなど）
            string[] parentChildPairs = new string[]
            {
                "LowerArm_L,Hand_L", "LowerArm_R,Hand_R",
                "Lower_Arm_L,Hand_L", "Lower_Arm_R,Hand_R",
                "LowerLeg_L,Foot_L", "LowerLeg_R,Foot_R",
                "Lower_Leg_L,Foot_L", "Lower_Leg_R,Foot_R",
                "Foot_L,Toe_L", "Foot_R,Toe_R"
            };
            
            // すでにマッピングされているボーン名のセット
            HashSet<string> mappedBoneNames = new HashSet<string>(
                boneMappings.Where(m => m.ClothingBone != null).Select(m => m.BoneName.ToLower())
            );
            
            // アバター側のボーン階層マップを作成
            Dictionary<string, List<Transform>> avatarChildrenMap = new Dictionary<string, List<Transform>>();
            foreach (var transform in avatarTransforms)
            {
                if (transform.parent != null)
                {
                    string parentName = transform.parent.name.ToLower();
                    if (!avatarChildrenMap.ContainsKey(parentName))
                    {
                        avatarChildrenMap[parentName] = new List<Transform>();
                    }
                    avatarChildrenMap[parentName].Add(transform);
                }
            }
            
            // 衣装側のボーン階層マップを作成
            Dictionary<string, List<Transform>> clothingChildrenMap = new Dictionary<string, List<Transform>>();
            foreach (var transform in clothingTransforms)
            {
                if (transform.parent != null)
                {
                    string parentName = transform.parent.name.ToLower();
                    if (!clothingChildrenMap.ContainsKey(parentName))
                    {
                        clothingChildrenMap[parentName] = new List<Transform>();
                    }
                    clothingChildrenMap[parentName].Add(transform);
                }
            }
            
            // 各親子ペアで検索
            foreach (string pair in parentChildPairs)
            {
                string[] parts = pair.Split(',');
                string parentName = parts[0].ToLower();
                string childName = parts[1].ToLower();
                
                // すでにマッピングされている場合はスキップ
                if (mappedBoneNames.Contains(childName))
                {
                    continue;
                }
                
                // アバター側で親ボーンがマッピングされているか確認
                Transform avatarParentBone = null;
                foreach (var mapping in boneMappings)
                {
                    if (mapping.AvatarBone != null && 
                        NormalizeBoneName(mapping.AvatarBone.name).ToLower() == NormalizeBoneName(parentName).ToLower())
                    {
                        avatarParentBone = mapping.AvatarBone;
                        break;
                    }
                }
                
                if (avatarParentBone == null) continue;
                
                // アバター側で子ボーンを見つける
                Transform avatarChildBone = null;
                
                // 直接の子ボーンから探す
                foreach (Transform child in avatarParentBone)
                {
                    string normalizedChildName = NormalizeBoneName(child.name).ToLower();
                    if (normalizedChildName == NormalizeBoneName(childName).ToLower() || 
                        normalizedChildName.Contains(NormalizeBoneName(childName).ToLower()))
                    {
                        avatarChildBone = child;
                        break;
                    }
                }
                
                // 子ボーンのリストから探す
                if (avatarChildBone == null && avatarChildrenMap.ContainsKey(avatarParentBone.name.ToLower()))
                {
                    foreach (var child in avatarChildrenMap[avatarParentBone.name.ToLower()])
                    {
                        string normalizedChildName = NormalizeBoneName(child.name).ToLower();
                        if (normalizedChildName == NormalizeBoneName(childName).ToLower() || 
                            normalizedChildName.Contains(NormalizeBoneName(childName).ToLower()))
                        {
                            avatarChildBone = child;
                            break;
                        }
                    }
                }
                
                // アバター側のボーンが見つからなかった場合
                if (avatarChildBone == null) continue;
                
                // 衣装側で親ボーンに対応するマッピングを見つける
                Transform clothingParentBone = null;
                foreach (var mapping in boneMappings)
                {
                    if (mapping.AvatarBone == avatarParentBone && mapping.ClothingBone != null)
                    {
                        clothingParentBone = mapping.ClothingBone;
                        break;
                    }
                }
                
                if (clothingParentBone == null) continue;
                
                // 衣装側で子ボーンを見つける
                Transform clothingChildBone = null;
                
                // 直接の子ボーンから探す
                foreach (Transform child in clothingParentBone)
                {
                    string normalizedChildName = NormalizeBoneName(child.name).ToLower();
                    if (normalizedChildName == NormalizeBoneName(childName).ToLower() || 
                        normalizedChildName.Contains(NormalizeBoneName(childName).ToLower()))
                    {
                        clothingChildBone = child;
                        break;
                    }
                }
                
                // 子ボーンのリストから探す
                if (clothingChildBone == null && clothingChildrenMap.ContainsKey(clothingParentBone.name.ToLower()))
                {
                    foreach (var child in clothingChildrenMap[clothingParentBone.name.ToLower()])
                    {
                        string normalizedChildName = NormalizeBoneName(child.name).ToLower();
                        if (normalizedChildName == NormalizeBoneName(childName).ToLower() || 
                            normalizedChildName.Contains(NormalizeBoneName(childName).ToLower()))
                        {
                            clothingChildBone = child;
                            break;
                        }
                    }
                }
                
                // 両方見つかった場合、マッピングに追加
                if (avatarChildBone != null && clothingChildBone != null)
                {
                    boneMappings.Add(new BoneMapping
                    {
                        BoneName = childName,
                        AvatarBone = avatarChildBone,
                        ClothingBone = clothingChildBone
                    });
                    
                    // マッピング済みのセットに追加
                    mappedBoneNames.Add(childName);
                }
            }
        }

        /// <summary>
        /// 重要なボーン（Hand、Foot、Toe等）が必ずマッピングに含まれるようにする
        /// </summary>
        private void EnsureCriticalBonesAreMapped(Transform[] avatarTransforms, Transform[] clothingTransforms, List<BoneMapping> boneMappings)
        {
            // すでにマッピングされているボーン名を抽出
            HashSet<string> mappedBoneNames = new HashSet<string>(boneMappings.Select(b => b.BoneName.ToLower()));
            
            foreach (var criticalBoneName in criticalBoneNames)
            {
                string lowerCriticalName = criticalBoneName.ToLower();
                
                // すでにマッピングに含まれているかチェック
                if (mappedBoneNames.Contains(lowerCriticalName))
                    continue;
                
                // アバター側に重要なボーンがあるか検索
                Transform avatarBone = null;
                foreach (var transform in avatarTransforms)
                {
                    if (transform.name.ToLower() == lowerCriticalName ||
                        NormalizeBoneName(transform.name).ToLower() == NormalizeBoneName(lowerCriticalName).ToLower())
                    {
                        avatarBone = transform;
                        break;
                    }
                }
                
                // 該当するボーンがアバターにない場合はスキップ
                if (avatarBone == null)
                    continue;
                
                // 衣装側に対応するボーンを検索
                Transform clothingBone = null;
                foreach (var transform in clothingTransforms)
                {
                    if (transform.name.ToLower() == lowerCriticalName ||
                        NormalizeBoneName(transform.name).ToLower() == NormalizeBoneName(lowerCriticalName).ToLower())
                    {
                        clothingBone = transform;
                        break;
                    }
                }
                
                // パターンマッチング（より広範囲な検索）
                if (clothingBone == null)
                {
                    // 左側のボーンか右側のボーンかを判断
                    bool isLeftSide = lowerCriticalName.Contains("_l") || lowerCriticalName.EndsWith("l") || lowerCriticalName.Contains(".l");
                    bool isRightSide = lowerCriticalName.Contains("_r") || lowerCriticalName.EndsWith("r") || lowerCriticalName.Contains(".r");
                    
                    // Hand/Foot/Toeのどのタイプかを判断
                    bool isHand = lowerCriticalName.Contains("hand");
                    bool isFoot = lowerCriticalName.Contains("foot");
                    bool isToe = lowerCriticalName.Contains("toe");
                    
                    // より広い検索条件で衣装側のボーンを探す
                    foreach (var transform in clothingTransforms)
                    {
                        string lowerName = transform.name.ToLower();
                        
                        bool matchesSide = (isLeftSide && (lowerName.Contains("_l") || lowerName.EndsWith("l") || lowerName.Contains(".l") || lowerName.Contains("left"))) ||
                                         (isRightSide && (lowerName.Contains("_r") || lowerName.EndsWith("r") || lowerName.Contains(".r") || lowerName.Contains("right")));
                        
                        bool matchesType = (isHand && (lowerName.Contains("hand") || lowerName.Contains("wrist") || lowerName.Contains("palm"))) ||
                                         (isFoot && (lowerName.Contains("foot") || lowerName.Contains("ankle") || lowerName.Contains("heel"))) ||
                                         (isToe && (lowerName.Contains("toe") || lowerName.Contains("toes")));
                        
                        if (matchesSide && matchesType)
                        {
                            clothingBone = transform;
                            break;
                        }
                    }
                }
                
                // 位置ベースのマッチングも試す（位置が近いボーンを探す）
                if (clothingBone == null)
                {
                    float bestDistance = float.MaxValue;
                    Transform bestMatch = null;
                    
                    foreach (var transform in clothingTransforms)
                    {
                        // 明らかに関係ないものは除外（Root、Mesh、Renderer等）
                        if (transform.name.Contains("Root") || 
                            transform.name.Contains("Mesh") || 
                            transform.name.Contains("Renderer"))
                            continue;
                            
                        float distance = Vector3.Distance(avatarBone.position, transform.position);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            bestMatch = transform;
                        }
                    }
                    
                    // 距離が十分近い場合のみ使用（しきい値は調整可能）
                    if (bestDistance < 0.05f)
                    {
                        clothingBone = bestMatch;
                    }
                }
                
                // マッピングに追加
                boneMappings.Add(new BoneMapping
                {
                    BoneName = criticalBoneName,
                    AvatarBone = avatarBone,
                    ClothingBone = clothingBone
                });
                
                // マッピング名のセットに追加
                mappedBoneNames.Add(lowerCriticalName);
            }
        }

        /// <summary>
        /// 再帰的にアバターのボーンを検索して追加
        /// </summary>
        private void AddRecursiveBonesFromAvatar(Transform currentBone, List<BoneMapping> boneMappings)
        {
            // このボーンが既にマッピングリストに含まれているか確認
            bool alreadyMapped = boneMappings.Any(m => m.AvatarBone == currentBone);
            
            // まだマッピングされていない重要なボーンの場合、追加
            if (!alreadyMapped && IsImportantBone(currentBone.name))
            {
                boneMappings.Add(new BoneMapping
                {
                    BoneName = currentBone.name,
                    AvatarBone = currentBone,
                    ClothingBone = null // マッチングは後で行う
                });
            }
            
            // 子ボーンに対して再帰的に適用
            foreach (Transform child in currentBone)
            {
                AddRecursiveBonesFromAvatar(child, boneMappings);
            }
        }
        
        /// <summary>
        /// 重要なボーンかどうかを判定
        /// </summary>
        private bool IsImportantBone(string boneName)
        {
            string lowerName = boneName.ToLower();
            
            // 手足の重要部位
            if (lowerName.Contains("hand") || lowerName.Contains("foot") || 
                lowerName.Contains("toe") || lowerName.Contains("finger"))
                return true;
                
            // 重要な特定ボーン名が含まれているか確認
            foreach (var criticalName in criticalBoneNames)
            {
                if (lowerName == criticalName.ToLower() || 
                    NormalizeBoneName(lowerName) == NormalizeBoneName(criticalName.ToLower()))
                    return true;
            }
                
            // 除外するボーン（よくある一般的なメッシュ名など）
            if (lowerName.Contains("mesh") || lowerName.Contains("renderer") || 
                lowerName.Contains("collider") || lowerName.Contains("camera"))
                return false;
                
            return false;
        }

        /// <summary>
        /// マッピングされなかった衣装のボーンを処理
        /// </summary>
        private void ProcessUnmappedBones(
            GameObject clothingObject, 
            Transform[] clothingTransforms, 
            List<BoneMapping> boneMappings)
        {
            // 既にマッピングされたボーンのリスト
            var mappedBonesSet = new HashSet<Transform>(
                boneMappings
                .Where(m => m.ClothingBone != null)
                .Select(m => m.ClothingBone)
            );
            
            // マッピングされていないボーンを検出
            var unmappedBones = new List<Transform>();
            foreach (var transform in clothingTransforms)
            {
                // ルートオブジェクト自体または直接の子はスキップ
                if (transform == clothingObject.transform || 
                    transform.parent == clothingObject.transform)
                {
                    continue;
                }
                
                // すでにマッピングされているボーンはスキップ
                if (mappedBonesSet.Contains(transform))
                {
                    continue;
                }
                
                // 明らかにボーンではないもの（MeshRenderer等を持つもの）はスキップ
                if (transform.GetComponent<MeshRenderer>() != null || 
                    transform.GetComponent<SkinnedMeshRenderer>() != null)
                {
                    continue;
                }
                
                // 未マッピングボーンとして追加
                unmappedBones.Add(transform);
            }
            
            // 各未マッピングボーンの親ボーンとの相対位置を記録
            UnmappedBoneInfos.Clear();
            foreach (var unmappedBone in unmappedBones)
            {
                // 親を遡って最初にマッピングされているボーンを見つける
                Transform currentParent = unmappedBone.parent;
                Transform mappedParent = null;
                
                while (currentParent != null)
                {
                    if (mappedBonesSet.Contains(currentParent))
                    {
                        mappedParent = currentParent;
                        break;
                    }
                    
                    // 親を遡る
                    if (currentParent.parent != null)
                    {
                        currentParent = currentParent.parent;
                    }
                    else
                    {
                        break;
                    }
                }
                
                // マッピングされた親ボーンが見つかった場合、相対位置を記録
                if (mappedParent != null)
                {
                    // 親ボーンからの相対位置を記録
                    Vector3 localPosition = mappedParent.InverseTransformPoint(unmappedBone.position);
                    Quaternion localRotation = Quaternion.Inverse(mappedParent.rotation) * unmappedBone.rotation;
                    
                    // 相対情報を保存
                    UnmappedBoneInfos[unmappedBone.name] = new UnmappedBoneInfo
                    {
                        BoneTransform = unmappedBone,
                        ParentBoneTransform = mappedParent,
                        RelativePosition = localPosition,
                        RelativeRotation = localRotation,
                        LocalScale = unmappedBone.localScale
                    };
                }
            }
            
            // ボーンマッピングにもUnmappedとして追加（UI表示のため）
            foreach (var unmappedInfo in UnmappedBoneInfos)
            {
                // マッピングされていることを示すために、アバターボーンに親のボーンを使用
                Transform parentBone = null;
                
                // 親のボーンに対応するアバターのボーンを探す
                foreach (var mapping in boneMappings)
                {
                    if (mapping.ClothingBone == unmappedInfo.Value.ParentBoneTransform)
                    {
                        parentBone = mapping.AvatarBone;
                        break;
                    }
                }
                
                // マッピングを追加
                if (parentBone != null)
                {
                    boneMappings.Add(new BoneMapping
                    {
                        BoneName = unmappedInfo.Key + " (Unmapped)", // Unmappedであることを明示
                        AvatarBone = parentBone, // 親のボーンをアバターボーンとして表示
                        ClothingBone = unmappedInfo.Value.BoneTransform,
                        IsUnmapped = true // Unmappedフラグを設定
                    });
                }
            }
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
            return boneName.Replace('.', '_').Replace(' ', '_');
        }
        
        /// <summary>
        /// アバターのボーン名に対応する衣装のボーンを探す（改良版）
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
            
            // 1. セマンティック解析: 同じ部位と左右指定のボーンを優先的に検索
            string bonePart = ExtractBonePart(normalizedAvatarBoneName);
            string boneSide = ExtractBoneSide(normalizedAvatarBoneName);
            
            if (!string.IsNullOrEmpty(bonePart) && !string.IsNullOrEmpty(boneSide))
            {
                // まず同じ部位+側を持つボーンを検索
                var semanticMatches = new Dictionary<string, float>();
                foreach (var clothingBone in clothingBones)
                {
                    string normalizedClothingName = NormalizeBoneName(clothingBone.Key).ToLower();
                    
                    if (normalizedClothingName.Contains(bonePart) && ExtractBoneSide(normalizedClothingName) == boneSide)
                    {
                        // スコアの計算: 文字列の長さの差が小さいほど類似度が高い
                        float lengthDifference = Mathf.Abs(normalizedClothingName.Length - normalizedAvatarBoneName.Length);
                        float similarity = 1.0f / (1.0f + lengthDifference);
                        
                        // upperかlowerを含む場合は、同じupperまたはlowerを含む場合にスコアを上げる
                        if ((normalizedAvatarBoneName.Contains("upper") && normalizedClothingName.Contains("upper")) ||
                            (normalizedAvatarBoneName.Contains("lower") && normalizedClothingName.Contains("lower")))
                        {
                            similarity += 0.5f;
                        }
                        
                        semanticMatches.Add(clothingBone.Key, similarity);
                    }
                }
                
                // 最も類似度が高いボーンを返す
                if (semanticMatches.Count > 0)
                {
                    var bestMatch = semanticMatches.OrderByDescending(x => x.Value).First();
                    return clothingBones[bestMatch.Key];
                }
            }
            
            // 2. スマートなパターンマッチング
            Dictionary<string, float> smartMatches = new Dictionary<string, float>();
            foreach (var clothingBone in clothingBones)
            {
                string normalizedClothingName = NormalizeBoneName(clothingBone.Key).ToLower();
                
                // スマートマッチングのスコアを取得
                float matchScore = GetSmartMatchScore(normalizedAvatarBoneName, normalizedClothingName);
                if (matchScore > 0)
                {
                    smartMatches.Add(clothingBone.Key, matchScore);
                }
            }
            
            // 最も高いスコアのボーンを選択
            if (smartMatches.Count > 0)
            {
                var bestMatch = smartMatches.OrderByDescending(x => x.Value).First();
                return clothingBones[bestMatch.Key];
            }
            
            // 3. Leg/Arm等の基本部位名でフォールバックマッチング
            foreach (var clothingBone in clothingBones)
            {
                string normalizedClothingName = NormalizeBoneName(clothingBone.Key).ToLower();
                
                if (DoesShareCommonPartName(normalizedAvatarBoneName, normalizedClothingName))
                {
                    return clothingBone.Value;
                }
            }
            
            // 4. 特殊ケース：手足の部位
            if (normalizedAvatarBoneName.Contains("foot") || normalizedAvatarBoneName.Contains("toe"))
            {
                // 足のボーンに対してより積極的なマッチング
                foreach (var clothingBone in clothingBones)
                {
                    string normalizedClothingName = NormalizeBoneName(clothingBone.Key).ToLower();
                    
                    if (normalizedClothingName.Contains("foot") || normalizedClothingName.Contains("toe") ||
                        normalizedClothingName.Contains("ankle"))
                    {
                        // 左右の一致も確認
                        if (ExtractBoneSide(normalizedAvatarBoneName) == ExtractBoneSide(normalizedClothingName))
                        {
                            return clothingBone.Value;
                        }
                    }
                }
            }
            else if (normalizedAvatarBoneName.Contains("hand") || normalizedAvatarBoneName.Contains("finger"))
            {
                // 手のボーンに対してより積極的なマッチング
                foreach (var clothingBone in clothingBones)
                {
                    string normalizedClothingName = NormalizeBoneName(clothingBone.Key).ToLower();
                    
                    if (normalizedClothingName.Contains("hand") || normalizedClothingName.Contains("finger") ||
                        normalizedClothingName.Contains("wrist"))
                    {
                        // 左右の一致も確認
                        if (ExtractBoneSide(normalizedAvatarBoneName) == ExtractBoneSide(normalizedClothingName))
                        {
                            return clothingBone.Value;
                        }
                    }
                }
            }
            
            // 5. 特に重要なボーン名に対する特別な処理
            if (IsCriticalBoneName(avatarBoneName))
            {
                // Hand, Foot, Toe などの重要なボーンは広範囲に検索
                bool isLeft = normalizedAvatarBoneName.Contains("_l") || normalizedAvatarBoneName.EndsWith("l") || 
                              normalizedAvatarBoneName.Contains("left") || normalizedAvatarBoneName.Contains(".l");
                bool isRight = normalizedAvatarBoneName.Contains("_r") || normalizedAvatarBoneName.EndsWith("r") || 
                               normalizedAvatarBoneName.Contains("right") || normalizedAvatarBoneName.Contains(".r");
                
                bool isHand = normalizedAvatarBoneName.Contains("hand");
                bool isFoot = normalizedAvatarBoneName.Contains("foot");
                bool isToe = normalizedAvatarBoneName.Contains("toe");
                
                foreach (var clothingBone in clothingBones)
                {
                    string normalizedClothingName = NormalizeBoneName(clothingBone.Key).ToLower();
                    
                    // 左右の一致
                    bool sideMatch = (isLeft && (normalizedClothingName.Contains("_l") || 
                                              normalizedClothingName.EndsWith("l") || 
                                              normalizedClothingName.Contains(".l") ||
                                              normalizedClothingName.Contains("left"))) ||
                                   (isRight && (normalizedClothingName.Contains("_r") || 
                                              normalizedClothingName.EndsWith("r") || 
                                              normalizedClothingName.Contains(".r") ||
                                              normalizedClothingName.Contains("right")));
                    
                    // 部位の一致
                    bool typeMatch = (isHand && (normalizedClothingName.Contains("hand") || 
                                              normalizedClothingName.Contains("palm") || 
                                              normalizedClothingName.Contains("wrist"))) ||
                                   (isFoot && (normalizedClothingName.Contains("foot") || 
                                              normalizedClothingName.Contains("ankle"))) ||
                                   (isToe && (normalizedClothingName.Contains("toe") || 
                                              normalizedClothingName.Contains("toes")));
                    
                    if (sideMatch && typeMatch)
                    {
                        return clothingBone.Value;
                    }
                }
            }
            
            // 位置ベースのマッチングを追加（空間的な位置関係で最も近いボーンを見つける）
            return null;
        }
        
        /// <summary>
        /// 重要なボーン名かどうかをチェック
        /// </summary>
        private bool IsCriticalBoneName(string boneName)
        {
            string lowerName = boneName.ToLower();
            return criticalBoneNames.Any(name => lowerName == name.ToLower() || 
                                               lowerName.Contains(name.ToLower()));
        }
        
        /// <summary>
        /// ボーン名からボーンの部位（leg, arm等）を抽出
        /// </summary>
        private string ExtractBonePart(string normalizedBoneName)
        {
            string[] commonParts = new string[] 
            { 
                "leg", "arm", "hand", "foot", "shoulder", "head", 
                "spine", "chest", "hip", "neck",
                "ankle", "wrist", "elbow", "knee", "clavicle", "toe" 
            };
            
            foreach (var part in commonParts)
            {
                if (normalizedBoneName.Contains(part))
                {
                    return part;
                }
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// ボーン名から左右の情報を抽出
        /// </summary>
        private string ExtractBoneSide(string normalizedBoneName)
        {
            if (normalizedBoneName.Contains("left") || normalizedBoneName.Contains("_l") || 
                normalizedBoneName.EndsWith("l") || normalizedBoneName.Contains(".l"))
            {
                return "l";
            }
            else if (normalizedBoneName.Contains("right") || normalizedBoneName.Contains("_r") || 
                    normalizedBoneName.EndsWith("r") || normalizedBoneName.Contains(".r"))
            {
                return "r";
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// スマートなマッチングスコアを計算
        /// </summary>
        private float GetSmartMatchScore(string name1, string name2)
        {
            float score = 0;
            
            // 完全一致
            if (name1 == name2) return 10.0f;
            
            // 区切り文字を無視した一致
            string clean1 = name1.Replace("_", "").Replace(".", "");
            string clean2 = name2.Replace("_", "").Replace(".", "");
            if (clean1 == clean2) return 9.5f;
            
            // 部分一致チェック
            if (name1.Contains(name2) || name2.Contains(name1)) 
            {
                score += 5.0f;
                
                // 長さの差が小さいほどスコアを上げる
                float lengthDifference = Mathf.Abs(name1.Length - name2.Length);
                score += 3.0f / (1.0f + lengthDifference);
            }
            
            // Upper/Lowerのパターンチェック
            string[] prefixes = new string[] { "upper", "lower" };
            foreach (var prefix in prefixes)
            {
                if (name1.Contains(prefix) && name2.Contains(prefix))
                {
                    score += 2.0f;
                }
            }
            
            // 左右の一致チェック
            string side1 = ExtractBoneSide(name1);
            string side2 = ExtractBoneSide(name2);
            if (!string.IsNullOrEmpty(side1) && side1 == side2)
            {
                score += 2.0f;
            }
            
            // 部位の一致チェック
            string part1 = ExtractBonePart(name1);
            string part2 = ExtractBonePart(name2);
            if (!string.IsNullOrEmpty(part1) && part1 == part2)
            {
                score += 2.0f;
            }
            
            // 拡張: Upper_Leg.R と UpperLeg.R の特殊ケース対応
            string[] specialParts = new string[] { "leg", "arm", "foot", "hand", "toe" };
            foreach (var part in specialParts)
            {
                string pattern1 = "upper_" + part;
                string pattern2 = "upper" + part;
                
                if ((name1.Contains(pattern1) && name2.Contains(pattern2)) ||
                    (name1.Contains(pattern2) && name2.Contains(pattern1)))
                {
                    // UpperLeg_R と Upper_Leg_R のようなパターンを優先的にマッチング
                    score += 3.0f;
                }
            }
            
            // 追加: 手足特殊ケース
            if ((name1.Contains("foot") && name2.Contains("foot")) || 
                (name1.Contains("toe") && name2.Contains("toe")))
            {
                score += 2.5f;
            }
            if ((name1.Contains("hand") && name2.Contains("hand")) || 
                (name1.Contains("finger") && name2.Contains("finger")))
            {
                score += 2.5f;
            }
            
            return score;
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
                    bool name1IsLeft = name1.Contains("left") || name1.Contains(".l") || name1.EndsWith("l") || name1.Contains("_l");
                    bool name1IsRight = name1.Contains("right") || name1.Contains(".r") || name1.EndsWith("r") || name1.Contains("_r");
                    bool name2IsLeft = name2.Contains("left") || name2.Contains(".l") || name2.EndsWith("l") || name2.Contains("_l");
                    bool name2IsRight = name2.Contains("right") || name2.Contains(".r") || name2.EndsWith("r") || name2.Contains("_r");
                    
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
                "upper", "lower", "leg", "arm", "shoulder", "ankle", "wrist", "knee", "elbow", "hand", "foot", "toe"
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
                                    bool name1IsLeft = name1.Contains("left") || name1.Contains(".l") || name1.EndsWith("l") || name1.Contains("_l");
                                    bool name1IsRight = name1.Contains("right") || name1.Contains(".r") || name1.EndsWith("r") || name1.Contains("_r");
                                    bool name2IsLeft = name2.Contains("left") || name2.Contains(".l") || name2.EndsWith("l") || name2.Contains("_l");
                                    bool name2IsRight = name2.Contains("right") || name2.Contains(".r") || name2.EndsWith("r") || name2.Contains("_r");
                                    
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
                { "hand", new List<string> { "wrist", "palm", "finger", "thumb", "index", "middle", "ring", "little" } },
                { "foot", new List<string> { "ankle", "toe", "heel", "toes" } }
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
        
        /// <summary>
        /// 装飾品や非標準ボーンかどうかを判定
        /// </summary>
        public bool IsDecorationBone(string boneName)
        {
            string lowercaseName = boneName.ToLower();
            
            foreach (var keyword in decorationKeywords)
            {
                if (lowercaseName.Contains(keyword))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// 未マッピングボーンの情報
    /// </summary>
    public class UnmappedBoneInfo
    {
        // ボーンのTransform
        public Transform BoneTransform;
        
        // 親ボーンのTransform
        public Transform ParentBoneTransform;
        
        // 親ボーンからの相対位置
        public Vector3 RelativePosition;
        
        // 親ボーンからの相対回転
        public Quaternion RelativeRotation;
        
        // ローカルスケール
        public Vector3 LocalScale;
    }
}
