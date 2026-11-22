using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedievalRPG.Data
{
    [Serializable]
    public class BattleWave
    {
        public string waveTitle = "Đợt Quái Vật";
        public string waveSubtitle = "";
        public bool isBossWave = false;
        [Range(0.5f, 2.5f)]
        public float enemyStatMultiplier = 1f;
        [Range(0f, 0.5f)]
        public float dropChanceBonus = 0f;
        [Range(0f, 0.5f)]
        public float rarityLuckBonus = 0f;
        public List<GameObject> enemyPrefabs = new List<GameObject>();
    }

    [CreateAssetMenu(fileName = "NewEncounter", menuName = "Medieval RPG/Encounter Data")]
    public class EncounterData : ScriptableObject
    {
        public string encounterTitle = "Rừng Sương Mù";
        public List<BattleWave> waves = new List<BattleWave>();
    }
}
