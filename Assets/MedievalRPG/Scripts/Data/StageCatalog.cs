using System.Collections.Generic;
using UnityEngine;

namespace MedievalRPG.Data
{
    public readonly struct StageDefinition
    {
        public readonly CampaignStageId id;
        public readonly string title;
        public readonly string shortName;
        public readonly string subtitle;
        public readonly int recommendedLevel;
        public readonly string quizFocusLabel;
        public readonly QuizDifficultyFilter suggestedQuizFilter;
        public readonly EnvironmentThemeType theme;
        public readonly bool isEndless;
        public readonly int waveCount;
        public readonly float rewardMultiplier;
        public readonly int firstClearBonusGold;
        public readonly string unlockHint;

        public StageDefinition(
            CampaignStageId id, string title, string shortName, string subtitle,
            int recommendedLevel, string quizFocusLabel, QuizDifficultyFilter suggestedQuizFilter,
            EnvironmentThemeType theme, bool isEndless, int waveCount,
            float rewardMultiplier, int firstClearBonusGold, string unlockHint)
        {
            this.id = id;
            this.title = title;
            this.shortName = shortName;
            this.subtitle = subtitle;
            this.recommendedLevel = recommendedLevel;
            this.quizFocusLabel = quizFocusLabel;
            this.suggestedQuizFilter = suggestedQuizFilter;
            this.theme = theme;
            this.isEndless = isEndless;
            this.waveCount = waveCount;
            this.rewardMultiplier = rewardMultiplier;
            this.firstClearBonusGold = firstClearBonusGold;
            this.unlockHint = unlockHint;
        }
    }

    /// <summary>
    /// Danh mục 3 Chương chiến dịch + Chế độ Tháp Vô Tận (Endless Mode)
    /// và hàm sinh đợt quái tương ứng theo độ khó, chủ đề môi trường và phần thưởng.
    /// </summary>
    public static class StageCatalog
    {
        public static readonly StageDefinition[] Stages =
        {
            new StageDefinition(
                CampaignStageId.ForestChapter1,
                "Chương 1: Rừng Sương Mù",
                "Chương 1: Rừng",
                "Goblin trinh sát, đàn phục kích và Thủ Lĩnh Bóng Đêm.",
                1, "B1", QuizDifficultyFilter.OnlyB1,
                EnvironmentThemeType.MistyForest,
                false, 6, 1.0f, 100, "Mở khóa ngay từ đầu"),
            new StageDefinition(
                CampaignStageId.CryptChapter2,
                "Chương 2: Hầm Mộ Xương Khô",
                "Chương 2: Hầm Mộ",
                "Quái tầm xa Xương Cung, Pháp Sư Hắc Ám & chúa tể hầm ngục.",
                3, "B1–B2", QuizDifficultyFilter.AllB1B2,
                EnvironmentThemeType.AncientCrypt,
                false, 6, 1.35f, 180, "Hoàn thành Chương 1 để mở khóa"),
            new StageDefinition(
                CampaignStageId.CitadelChapter3,
                "Chương 3: Pháo Đài Hắc Ám",
                "Chương 3: Pháo Đài",
                "Tinh anh kháng phép/giáp dày, tỉ lệ rớt Sử Thi & Huyền Thoại cao.",
                5, "B2", QuizDifficultyFilter.OnlyB2,
                EnvironmentThemeType.DarkCitadel,
                false, 6, 1.75f, 300, "Hoàn thành Chương 2 để mở khóa"),
            new StageDefinition(
                CampaignStageId.EndlessTower,
                "Tháp Vô Tận (Endless)",
                "Tháp Vô Tận",
                "Đợt quái vô hạn leo thang sức mạnh. Cứ mỗi 5 đợt gặp 1 Boss!",
                4, "B1–B2", QuizDifficultyFilter.AllB1B2,
                EnvironmentThemeType.DarkCitadel,
                true, 999, 1.50f, 250, "Hoàn thành Chương 2 để mở khóa")
        };

        public static StageDefinition GetStage(CampaignStageId id)
        {
            for (int i = 0; i < Stages.Length; i++)
            {
                if (Stages[i].id == id) return Stages[i];
            }
            return Stages[0];
        }

        public static List<BattleWave> BuildWavesForStage(
            CampaignStageId stageId,
            GameObject goblinPrefab,
            GameObject skeletonPrefab,
            GameObject skeletonArcherPrefab,
            GameObject shadowMagePrefab,
            GameObject bossPrefab)
        {
            GameObject rangedSkel = skeletonArcherPrefab != null ? skeletonArcherPrefab : skeletonPrefab;
            GameObject mageEnemy = shadowMagePrefab != null ? shadowMagePrefab : goblinPrefab;
            GameObject boss = bossPrefab != null ? bossPrefab : skeletonPrefab;

            if (stageId == CampaignStageId.CryptChapter2)
            {
                return new List<BattleWave>
                {
                    MakeWave("Đợt 1: Lính Gác Mộ Cổ", "Xương chiến binh & Xương cung thủ", false, 1.05f, 0.05f, 0.04f, skeletonPrefab, rangedSkel),
                    MakeWave("Đợt 2: Đội Tuần Tra Đêm", "Đàn 3 quái tầm xa và cận chiến", false, 1.12f, 0.08f, 0.06f, goblinPrefab, rangedSkel, rangedSkel),
                    MakeWave("Đợt 3: Nghi Lễ Bóng Tối", "Pháp sư hắc ám kháng phép", false, 1.18f, 0.10f, 0.08f, skeletonPrefab, mageEnemy),
                    MakeWave("Đợt 4: Đàn Hài Cốt", "Tiểu đội hầm mộ hỗn hợp 3 hướng", false, 1.25f, 0.13f, 0.12f, rangedSkel, mageEnemy, skeletonPrefab),
                    MakeWave("Đợt 5: Vệ Binh Tinh Anh", "Tinh Anh canh lăng mộ", false, 1.38f, 0.16f, 0.18f, mageEnemy, skeletonPrefab, rangedSkel),
                    MakeWave("Đợt 6: Chúa Tể Hầm Mộ", "BOSS CHƯƠNG 2 • Chắc chắn rớt 2 đồ cao cấp", true, 1.25f, 0f, 0.22f, boss, mageEnemy)
                };
            }

            if (stageId == CampaignStageId.CitadelChapter3)
            {
                return new List<BattleWave>
                {
                    MakeWave("Đợt 1: Tiền Đồn Pháo Đài", "Hộ vệ pháo đài & xạ thủ xương", false, 1.24f, 0.10f, 0.10f, skeletonPrefab, rangedSkel),
                    MakeWave("Đợt 2: Tháp Pháp Sư Đen", "Bộ ba hắc ám gây thiêu đốt liên tục", false, 1.32f, 0.12f, 0.14f, rangedSkel, mageEnemy, mageEnemy),
                    MakeWave("Đợt 3: Chiến Tuyến Thiết Giáp", "Tinh anh giáp cứng cản đường", false, 1.40f, 0.15f, 0.16f, skeletonPrefab, skeletonPrefab, rangedSkel),
                    MakeWave("Đợt 4: Cận Vệ Hoàng Thành", "Tinh Anh hỗn hợp sát thương cực lớn", false, 1.48f, 0.18f, 0.20f, mageEnemy, skeletonPrefab, mageEnemy),
                    MakeWave("Đợt 5: Hội Đồng Hắc Ám", "Cận kề ngai vàng • Tỉ lệ Sử Thi cực cao", false, 1.56f, 0.22f, 0.25f, skeletonPrefab, mageEnemy, rangedSkel),
                    MakeWave("Đợt 6: Ma Vương Hắc Ám", "BOSS CUỐI CHƯƠNG 3 • Sức mạnh hủy diệt", true, 1.45f, 0f, 0.30f, boss, rangedSkel)
                };
            }

            if (stageId == CampaignStageId.EndlessTower)
            {
                List<BattleWave> waves = new List<BattleWave>(6);
                for (int i = 0; i < 6; i++)
                {
                    waves.Add(CreateEndlessWave(i, goblinPrefab, skeletonPrefab, skeletonArcherPrefab, shadowMagePrefab, bossPrefab));
                }
                return waves;
            }

            // Mặc định Chương 1: Rừng Sương Mù
            return new List<BattleWave>
            {
                MakeWave("Đợt 1: Trinh Sát Rừng Sâu", "Quái yếu đơn lẻ • Khởi động", false, 0.80f, 0f, 0f, goblinPrefab),
                MakeWave("Đợt 2: Đàn Goblin Phục Kích", "Thành đàn 3 Goblin • Ưu tiên chiêu diện rộng", false, 0.90f, 0.02f, 0.03f, goblinPrefab, goblinPrefab, goblinPrefab),
                MakeWave("Đợt 3: Vệ Binh Xương Khô", "Quái mạnh hơn • Giáp cứng", false, 1.00f, 0.05f, 0.06f, goblinPrefab, rangedSkel),
                MakeWave("Đợt 4: Tiểu Đội Hầm Ngục", "Đàn quái hạng nặng (3 con)", false, 1.08f, 0.08f, 0.10f, skeletonPrefab, mageEnemy, rangedSkel),
                MakeWave("Đợt 5: Cận Vệ Tinh Anh", "Tinh Anh áp sát Boss • Rớt đồ Sử Thi cao", false, 1.25f, 0.12f, 0.16f, skeletonPrefab, mageEnemy),
                MakeWave("Đợt 6: Thủ Lĩnh Bóng Đêm", "BOSS CUỐI MAP • Chắc chắn rớt 2 trang bị cao cấp", true, 1.05f, 0f, 0.15f, boss)
            };
        }

        public static BattleWave CreateEndlessWave(
            int zeroBasedWaveIndex,
            GameObject goblinPrefab,
            GameObject skeletonPrefab,
            GameObject skeletonArcherPrefab,
            GameObject shadowMagePrefab,
            GameObject bossPrefab)
        {
            int num = zeroBasedWaveIndex + 1;
            bool isBoss = (num % 5 == 0);
            float scale = 0.92f + zeroBasedWaveIndex * 0.11f;
            float dropBonus = Mathf.Min(0.35f, zeroBasedWaveIndex * 0.025f);
            float luckBonus = Mathf.Min(0.40f, zeroBasedWaveIndex * 0.03f);

            GameObject ranged = skeletonArcherPrefab != null ? skeletonArcherPrefab : skeletonPrefab;
            GameObject mage = shadowMagePrefab != null ? shadowMagePrefab : goblinPrefab;
            GameObject skel = skeletonPrefab != null ? skeletonPrefab : goblinPrefab;
            GameObject boss = bossPrefab != null ? bossPrefab : skel;

            if (isBoss)
            {
                return MakeWave(
                    $"Tầng {num}: Thủ Lĩnh Tháp Vô Tận",
                    "BOSS TẦNG • Rớt 2 trang bị cao cấp!",
                    true, scale * 0.95f, dropBonus, luckBonus + 0.15f,
                    boss, num >= 10 ? mage : null);
            }

            int enemyCount = num == 1 ? 2 : 3;
            List<GameObject> pool = new List<GameObject>(3);
            for (int i = 0; i < enemyCount; i++)
            {
                int pick = (zeroBasedWaveIndex + i) % 4;
                pool.Add(pick switch
                {
                    0 => goblinPrefab,
                    1 => skel,
                    2 => ranged,
                    _ => mage
                });
            }

            return new BattleWave
            {
                waveTitle = $"Tầng {num}: Chiến Tuyến Vô Tận",
                waveSubtitle = $"Độ khó x{scale:0.00} • May mắn +{Mathf.RoundToInt(luckBonus * 100f)}%",
                isBossWave = false,
                enemyStatMultiplier = scale,
                dropChanceBonus = dropBonus,
                rarityLuckBonus = luckBonus,
                enemyPrefabs = pool
            };
        }

        private static BattleWave MakeWave(
            string title, string subtitle, bool isBoss, float mult, float dropBonus, float luckBonus,
            params GameObject[] prefabs)
        {
            List<GameObject> list = new List<GameObject>();
            if (prefabs != null)
            {
                for (int i = 0; i < prefabs.Length; i++)
                {
                    if (prefabs[i] != null) list.Add(prefabs[i]);
                }
            }
            return new BattleWave
            {
                waveTitle = title,
                waveSubtitle = subtitle,
                isBossWave = isBoss,
                enemyStatMultiplier = mult,
                dropChanceBonus = dropBonus,
                rarityLuckBonus = luckBonus,
                enemyPrefabs = list
            };
        }
    }
}
