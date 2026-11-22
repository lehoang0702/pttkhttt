using UnityEngine;
using UnityEngine.Audio;

namespace MedievalRPG.Audio
{
    [CreateAssetMenu(fileName = "MedievalAudioBank", menuName = "Medieval RPG/Audio Bank")]
    public class AudioBank : ScriptableObject
    {
        [Header("Optional Audio Mixer Routing")]
        public AudioMixer audioMixer;
        public AudioMixerGroup musicGroup;
        public AudioMixerGroup sfxGroup;
        public AudioMixerGroup ambientGroup;

        [Header("Music & Ambience")]
        public AudioClip bgmMainMenu;
        public AudioClip bgmBattle;
        public AudioClip bgmBoss;
        public AudioClip ambientForest;

        [Header("UI & Quiz SFX")]
        public AudioClip uiClick;
        public AudioClip uiHover;
        public AudioClip quizOpen;
        public AudioClip quizCorrect;
        public AudioClip quizWrong;

        [Header("Combat SFX")]
        public AudioClip swordSlash;
        public AudioClip bowShoot;
        public AudioClip magicCast;
        public AudioClip fireballHit;
        public AudioClip healChime;
        public AudioClip shieldDefend;
        public AudioClip critHit;
        public AudioClip unitHit;
        public AudioClip unitDeath;

        [Header("Outcome Stings")]
        public AudioClip victoryFanfare;
        public AudioClip defeatSting;

        [Header("Loot & Equipment SFX")]
        public AudioClip lootDrop;
        public AudioClip lootLegendary;
        public AudioClip lootPickup;

        [Header("Economy & Progression SFX")]
        public AudioClip coinReward;
        public AudioClip forgeUpgrade;
        public AudioClip levelUp;
    }
}
