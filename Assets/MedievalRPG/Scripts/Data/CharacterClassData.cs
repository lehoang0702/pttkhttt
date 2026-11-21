using System.Collections.Generic;
using UnityEngine;

namespace MedievalRPG.Data
{
    [CreateAssetMenu(fileName = "NewCharacterClass", menuName = "Medieval RPG/Character Class Data")]
    public class CharacterClassData : ScriptableObject
    {
        [Header("Class Identity")]
        public CharacterClassType classType;
        public string className = "Knight";
        public int baseLevel = 1;
        public bool isBoss = false;
        [TextArea(2, 4)]
        public string loreDescription = "A stalwart protector clad in iron plate armor.";

        [Header("Base Combat Stats")]
        public int maxHealth = 150;
        public int maxMana = 40;
        public int physicalAttack = 22;
        public int magicAttack = 5;
        public int physicalDefense = 18;
        public int magicDefense = 8;
        [Tooltip("Higher speed acts earlier in the turn sequence")]
        public int speed = 10;
        [Range(0f, 1f)]
        public float critRate = 0.05f;
        public float critMultiplier = 1.5f;

        [Header("Sprites & Visuals")]
        public Sprite idleSprite;
        public Sprite idleSpriteAlt;
        public Sprite actionSprite;
        public Sprite hitSprite;

        [Header("Available Skills")]
        public List<SkillData> skills = new List<SkillData>();
    }
}
