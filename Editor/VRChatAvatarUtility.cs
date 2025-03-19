using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace VRChatAutoClothingTool
{
    public static class VRChatAvatarUtility
    {
        // VRChatアバターで一般的に使用される可能性のあるコンポーネント名
        private static readonly string[] vrcComponentTypes = new string[]
        {
            "VRCAvatarDescriptor",
            "VRCPhysBone",
            "VRCPhysBoneCollider",
            "DynamicBone",
            "DynamicBoneCollider",
            "VRCSpatialAudioSource",
            "VRCAnimatorLayerControl",
            "VRCAnimatorTemporaryPoseSpace",
            "VRCAnimatorTrackingControl",
            "VRCPlayableLayerControl",
            "VRCContactReceiver",
            "VRCContactSender"
        };
        
        // Modular Avatarコンポーネント
        private static readonly string[] modularAvatarComponentTypes = new string[]
        {
            "ModularAvatarMenuInstaller",
            "ModularAvatarParameters",
            "ModularAvatarMergeAnimator",
            "ModularAvatarBlendshapeSync"
        };
        
        // アバタータイプを検出する
        public static AvatarType DetectAvatarType(GameObject avatarObject)
        {
            if (avatarObject == null) return AvatarType.Unknown;
            
            var components = avatarObject.GetComponentsInChildren<Component>();
            bool hasVRCComponents = components.Any(c => IsVRCComponent(c));
            bool hasHumanoidAnimator = avatarObject.GetComponent<Animator>()?.isHuman ?? false;
            
            if (hasVRCComponents && hasHumanoidAnimator)
            {
                return AvatarType.VRChatHumanoid;
            }
            else if (hasVRCComponents)
            {
                return AvatarType.VRChatGeneric;
            }
            else if (hasHumanoidAnimator)
            {
                return AvatarType.HumanoidNonVRChat;
            }
            
            return AvatarType.Unknown;
        }
        
        // VRChatコンポーネントかどうかを判定
        public static bool IsVRCComponent(Component component)
        {
            if (component == null) return false;
            
            string typeName = component.GetType().Name;
            return vrcComponentTypes.Contains(typeName) || 
                   modularAvatarComponentTypes.Contains(typeName) ||
                   typeName.StartsWith("VRC") || 
                   typeName.StartsWith("ModularAvatar");
        }
        
        // アバターの衣装用ボーンを特定する
        public static List<Transform> GetClothingBones(GameObject avatarObject)
        {
            var clothingBones = new List<Transform>();
            
            if (avatarObject == null) return clothingBones;
            
            // アバターのコンポーネントからPhysBoneや衣装関連の可能性があるボーンを取得
            var allComponents = avatarObject.GetComponentsInChildren<Component>();
            
            foreach (var component in allComponents)
            {
                if (component == null) continue;
                
                string typeName = component.GetType().Name;
                
                // PhysBoneやDynamicBoneの場合
                if (typeName == "VRCPhysBone" || typeName == "DynamicBone")
                {
                    // リフレクションでrootTransformやm_Rootなどのプロパティにアクセスする方法もある
                    // 簡易的な実装では、コンポーネントがアタッチされているオブジェクトを使用
                    var bone = component.transform;
                    
                    if (IsPotentialClothingBone(bone))
                    {
                        clothingBones.Add(bone);
                        
                        // 子ボーンも追加
                        foreach (Transform child in bone)
                        {
                            clothingBones.Add(child);
                        }
                    }
                }
            }
            
            return clothingBones;
        }
        
        // 衣装関連のボーンである可能性が高いかどうかを判定
        private static bool IsPotentialClothingBone(Transform bone)
        {
            if (bone == null) return false;
            
            string name = bone.name.ToLower();
            string[] clothingKeywords = new string[]
            {
                "cloth", "dress", "skirt", "pant", "shirt", "jacket", "coat", "sleeve",
                "collar", "hat", "accessory", "hair", "tail", "ear", "outfit", "costume",
                "uniform", "armor", "wing", "cape", "robe", "sock", "shoe", "boot", "glove"
            };
            
            return clothingKeywords.Any(keyword => name.Contains(keyword));
        }
        
        // アバターと衣装のスケール差を計算
        public static Vector3 CalculateScaleDifference(GameObject avatarObject, GameObject clothingObject)
        {
            if (avatarObject == null || clothingObject == null) return Vector3.one;
            
            // アバターと衣装のHipsボーンを取得
            Transform avatarHips = FindHipsRecursive(avatarObject.transform);
            Transform clothingHips = FindHipsRecursive(clothingObject.transform);
            
            if (avatarHips == null || clothingHips == null)
            {
                // Hipsが見つからない場合はルートオブジェクトのスケールを使用
                return new Vector3(
                    avatarObject.transform.localScale.x / clothingObject.transform.localScale.x,
                    avatarObject.transform.localScale.y / clothingObject.transform.localScale.y,
                    avatarObject.transform.localScale.z / clothingObject.transform.localScale.z
                );
            }
            
            // ボーンの長さを比較してスケール差を計算
            Vector3 scaleDifference = CalculateBoneScaleDifference(avatarHips, clothingHips);
            
            return scaleDifference;
        }
        
        // Hipsボーンを再帰的に検索
        private static Transform FindHipsRecursive(Transform current)
        {
            if (current == null) return null;
            
            if (current.name.Contains("Hips") || current.name.Contains("Hip") || 
                current.name.Contains("Pelvis") || current.name.EndsWith("Hips"))
            {
                return current;
            }
            
            foreach (Transform child in current)
            {
                Transform result = FindHipsRecursive(child);
                if (result != null) return result;
            }
            
            return null;
        }
        
        // ボーンのスケール差を計算
        private static Vector3 CalculateBoneScaleDifference(Transform avatarBone, Transform clothingBone)
        {
            if (avatarBone == null || clothingBone == null)
            {
                return Vector3.one;
            }
            
            // 子ボーンの位置を使用してボーンの長さを計算
            float avatarBoneLength = CalculateAverageBoneLength(avatarBone);
            float clothingBoneLength = CalculateAverageBoneLength(clothingBone);
            
            if (avatarBoneLength <= 0 || clothingBoneLength <= 0)
            {
                return Vector3.one;
            }
            
            // スケール比率を計算
            float scaleRatio = avatarBoneLength / clothingBoneLength;
            
            return new Vector3(scaleRatio, scaleRatio, scaleRatio);
        }
        
        // ボーンの平均的な長さを計算
        private static float CalculateAverageBoneLength(Transform bone)
        {
            if (bone == null || bone.childCount == 0)
            {
                return 0f;
            }
            
            float totalLength = 0f;
            int validChildCount = 0;
            
            foreach (Transform child in bone)
            {
                float length = Vector3.Distance(bone.position, child.position);
                if (length > 0.001f) // 微小距離は無視
                {
                    totalLength += length;
                    validChildCount++;
                }
            }
            
            return validChildCount > 0 ? totalLength / validChildCount : 0f;
        }
        
        // 衣装の初期配置を最適化
        public static void OptimizeClothingPlacement(GameObject avatarObject, GameObject clothingObject)
        {
            if (avatarObject == null || clothingObject == null) return;
            
            // アバターと衣装のHipsボーンを検索
            Transform avatarHips = FindHipsRecursive(avatarObject.transform);
            Transform clothingHips = FindHipsRecursive(clothingObject.transform);
            
            if (avatarHips != null && clothingHips != null)
            {
                // 衣装のHipsをアバターのHipsに合わせる
                clothingObject.transform.position = avatarObject.transform.position;
                clothingHips.position = avatarHips.position;
                clothingHips.rotation = avatarHips.rotation;
            }
            else
            {
                // Hipsが見つからない場合はルート位置を合わせる
                clothingObject.transform.position = avatarObject.transform.position;
                clothingObject.transform.rotation = avatarObject.transform.rotation;
            }
        }
        
        // VRChatアバター用の衣装を検出
        public static bool IsVRChatClothing(GameObject clothingObject)
        {
            if (clothingObject == null) return false;
            
            // 衣装に特有のコンポーネントやボーン構造があるか確認
            var components = clothingObject.GetComponentsInChildren<Component>();
            bool hasVRCComponents = components.Any(c => IsVRCComponent(c));
            
            // スキンメッシュレンダラーがあるか確認
            bool hasSkinnedMesh = clothingObject.GetComponentInChildren<SkinnedMeshRenderer>() != null;
            
            // ボーン構造を確認
            bool hasBoneStructure = HasClothingBoneStructure(clothingObject);
            
            return hasSkinnedMesh && (hasVRCComponents || hasBoneStructure);
        }
        
        // 衣装のボーン構造を持っているかどうかを確認
        private static bool HasClothingBoneStructure(GameObject clothingObject)
        {
            if (clothingObject == null) return false;
            
            // 衣装によく含まれるボーン名
            string[] commonClothingBoneNames = new string[]
            {
                "Hips", "Spine", "Chest", "Neck", "Shoulder", "Arm", "Elbow", "Wrist", "Hand",
                "Leg", "Knee", "Ankle", "Foot", "Skirt", "Dress", "Cloth"
            };
            
            var allTransforms = clothingObject.GetComponentsInChildren<Transform>();
            int matchCount = 0;
            
            foreach (var transform in allTransforms)
            {
                string name = transform.name;
                if (commonClothingBoneNames.Any(boneName => name.Contains(boneName)))
                {
                    matchCount++;
                }
            }
            
            // 一定数以上のボーン名が一致した場合、衣装のボーン構造と判断
            return matchCount >= 3;
        }
        
        // ダイナミックボーンやPhysBoneの設定を調整
        public static void AdjustDynamicBones(GameObject clothingObject, float scaleFactor)
        {
            if (clothingObject == null) return;
            
            var allComponents = clothingObject.GetComponentsInChildren<Component>();
            
            foreach (var component in allComponents)
            {
                if (component == null) continue;
                
                string typeName = component.GetType().Name;
                
                // PhysBoneの場合
                if (typeName == "VRCPhysBone")
                {
                    // リフレクションを使用してプロパティを調整する
                    // この部分はVRChatSDKの詳細によって実装が変わる可能性があります
                    /*
                    var physBone = component as dynamic;
                    physBone.radius *= scaleFactor;
                    physBone.springForce /= scaleFactor;
                    physBone.stiffness /= scaleFactor;
                    */
                    
                    Debug.Log($"PhysBone detected on {component.name}. Scale factor: {scaleFactor}");
                }
                // DynamicBoneの場合
                else if (typeName == "DynamicBone")
                {
                    /*
                    var dynamicBone = component as dynamic;
                    dynamicBone.m_Radius *= scaleFactor;
                    dynamicBone.m_Spring /= scaleFactor;
                    dynamicBone.m_Stiffness /= scaleFactor;
                    */
                    
                    Debug.Log($"DynamicBone detected on {component.name}. Scale factor: {scaleFactor}");
                }
            }
        }
        
        // スキンウェイトをボーンマッピングに基づいて調整
        public static void AdjustSkinningWeights(SkinnedMeshRenderer renderer, Dictionary<Transform, Transform> boneMapping)
        {
            if (renderer == null || renderer.sharedMesh == null || boneMapping == null) return;
            
            Mesh mesh = Object.Instantiate(renderer.sharedMesh);
            
            // メッシュのボーン情報を取得
            Transform[] meshBones = renderer.bones;
            
            // 新しいボーン配列を作成
            var newBones = new Transform[meshBones.Length];
            
            // ボーンを置き換え
            for (int i = 0; i < meshBones.Length; i++)
            {
                if (boneMapping.TryGetValue(meshBones[i], out Transform mappedBone))
                {
                    newBones[i] = mappedBone;
                }
                else
                {
                    newBones[i] = meshBones[i];
                }
            }
            
            // 新しいボーン配列を適用
            renderer.bones = newBones;
            
            // バインドポーズを調整する必要がある場合は、ここで実装
            
            // メッシュをアセットとして保存
            string assetPath = $"Assets/AdjustedMeshes/{renderer.gameObject.name}_Adjusted.asset";
            
            // アセットフォルダの作成
            string directory = System.IO.Path.GetDirectoryName(assetPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            AssetDatabase.CreateAsset(mesh, assetPath);
            AssetDatabase.SaveAssets();
            
            // 調整したメッシュを適用
            renderer.sharedMesh = mesh;
        }
    }
    
    // アバタータイプの列挙型
    public enum AvatarType
    {
        Unknown,
        VRChatHumanoid,
        VRChatGeneric,
        HumanoidNonVRChat
    }
}
