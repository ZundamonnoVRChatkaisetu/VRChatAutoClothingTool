using UnityEngine;

namespace VRChatAutoClothingTool
{
    /// <summary>
    /// ボーンマッピング情報を保持するクラス
    /// </summary>
    [System.Serializable]
    public class BoneMapping
    {
        /// <summary>
        /// ボーン名
        /// </summary>
        public string BoneName;
        
        /// <summary>
        /// アバターのボーン
        /// </summary>
        public Transform AvatarBone;
        
        /// <summary>
        /// 衣装のボーン
        /// </summary>
        public Transform ClothingBone;
        
        /// <summary>
        /// マッピングされていないボーンかどうか
        /// </summary>
        public bool IsUnmapped = false;
        
        /// <summary>
        /// ボーン分析器の参照
        /// </summary>
        public BoneStructureAnalyzer SourceAnalyzer;
    }
}
