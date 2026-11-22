namespace MedievalRPG.Data
{
    public readonly struct AchievementDefinition
    {
        public readonly AchievementId id;
        public readonly string title;
        public readonly string description;
        public readonly int goldReward;

        public AchievementDefinition(AchievementId id, string title, string description, int goldReward)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.goldReward = goldReward;
        }
    }

    public static class AchievementCatalog
    {
        public static readonly AchievementDefinition[] All =
        {
            new AchievementDefinition(AchievementId.FirstChapterClear, "Hộ Vệ Rừng Xanh", "Hoàn thành Chương 1: Rừng Sương Mù.", 80),
            new AchievementDefinition(AchievementId.StreakFiveB2, "Học Giả Song Ngữ", "Đạt chuỗi 5 câu trả lời đúng liên tiếp (bao gồm B2).", 100),
            new AchievementDefinition(AchievementId.FirstLegendary, "Thợ Săn Huyền Thoại", "Sở hữu hoặc rèn được trang bị cấp Huyền Thoại đầu tiên.", 120),
            new AchievementDefinition(AchievementId.ForgePlusThree, "Bậc Thầy Lò Rèn", "Cường hóa một trang bị lên cấp +3 trở lên.", 90),
            new AchievementDefinition(AchievementId.MistakeReviewer, "Không Bỏ Cuộc", "Ôn tập và sửa đúng ít nhất 3 câu từng làm sai trong Sổ Tay.", 90),
            new AchievementDefinition(AchievementId.ChapterThreeClear, "Chinh Phục Pháo Đài", "Đánh bại Ma Vương Hắc Ám ở Chương 3.", 220),
            new AchievementDefinition(AchievementId.EndlessWaveTen, "Chiến Binh Bất Tử", "Vượt qua Tầng 10 trong Tháp Vô Tận.", 250)
        };

        public static AchievementDefinition Get(AchievementId id)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].id == id) return All[i];
            }
            return All[0];
        }
    }
}
