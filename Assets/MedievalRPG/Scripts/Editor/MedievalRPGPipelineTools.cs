using Unity.Pipeline.Commands;
using UnityEditor;
using UnityEngine;

namespace MedievalRPG.EditorTools
{
    /// <summary>
    /// Lệnh Unity Pipeline CLI phục vụ sinh âm thanh, chuẩn hóa dữ liệu RPG và dựng lại toàn bộ Scene/UI.
    /// </summary>
    public static class MedievalRPGPipelineTools
    {
        [CliCommand("medieval_setup_all", "Generate audio clips, update RPG assets, and rebuild MainMenu + Battle UI scenes")]
        public static string SetupAll()
        {
            MedievalAudioGenerator.GenerateAllAudioAndBank();
            MedievalRPGAssetSetup.EnsureCombatDataSetup();
            MedievalUIBuilder.BuildAllUI();
            return "OK: Generated audio bank, configured skills/waves, and rebuilt MainMenu + MedievalBattleDemo UI.";
        }

        [CliCommand("medieval_generate_audio", "Generate procedural medieval WAV audio clips and Resources/MedievalAudioBank.asset")]
        public static string GenerateAudio()
        {
            var bank = MedievalAudioGenerator.GenerateAllAudioAndBank();
            return bank != null ? $"OK: AudioBank ready at {AssetDatabase.GetAssetPath(bank)}" : "FAIL: AudioBank null";
        }
    }
}
