using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedievalRPG.Data
{
    [Serializable]
    public class BattleWave
    {
        public string waveTitle = "Đợt Quái Vật";
        public bool isBossWave = false;
        public List<GameObject> enemyPrefabs = new List<GameObject>();
    }

    [CreateAssetMenu(fileName = "NewEncounter", menuName = "Medieval RPG/Encounter Data")]
    public class EncounterData : ScriptableObject
    {
        public string encounterTitle = "Rừng Sương Mù";
        public List<BattleWave> waves = new List<BattleWave>();
    }
}
