using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedievalRPG.Data
{
    public enum RoadsideEventType
    {
        AncientRuneChest = 0,
        SacredForestShrine = 1,
        WanderingScholar = 2
    }

    public enum RoadsideChoiceAction
    {
        QuizForReward = 0,
        SafeRestOrSmallReward = 1,
        PayGoldForPotion = 2,
        Leave = 3
    }

    [Serializable]
    public class RoadsideChoiceOption
    {
        public string label;
        public string subtitle;
        public RoadsideChoiceAction actionType;
        public int goldCost;
    }

    [Serializable]
    public class RoadsideEventOffer
    {
        public RoadsideEventType eventType;
        public string title;
        public string description;
        public List<RoadsideChoiceOption> options = new List<RoadsideChoiceOption>();
    }

    /// <summary>
    /// Dữ liệu và quy tắc sinh Sự Kiện Dọc Đường giữa các đợt quái (Rương cổ khóa mã, Đền thờ rừng thiêng, Học giả lưu động).
    /// </summary>
    public static class RoadsideEventCatalog
    {
        public static bool ShouldTriggerEventBeforeWave(int nextWaveIndex, bool isEndless, int totalWaves)
        {
            if (nextWaveIndex <= 0) return false;
            if (isEndless)
            {
                return nextWaveIndex % 3 == 0;
            }
            // Trong màn chiến dịch 6 đợt, xuất hiện giữa Đợt 3 -> Đợt 4 (và thêm 1 lần trước Boss nếu map dài hơn 6 đợt)
            if (nextWaveIndex == 3) return true;
            if (totalWaves > 6 && nextWaveIndex == totalWaves - 2) return true;
            return false;
        }

        public static RoadsideEventType PickEventTypeForWave(int nextWaveIndex, bool isEndless, int rollSeed)
        {
            if (isEndless)
            {
                int slot = Mathf.Abs((nextWaveIndex / 3) + rollSeed) % 3;
                return (RoadsideEventType)slot;
            }

            int idx = Mathf.Abs(nextWaveIndex + rollSeed) % 3;
            return (RoadsideEventType)idx;
        }

        public static RoadsideEventOffer CreateOffer(RoadsideEventType type)
        {
            switch (type)
            {
                case RoadsideEventType.AncientRuneChest:
                    return new RoadsideEventOffer
                    {
                        eventType = type,
                        title = "RƯƠNG CỔ KHÓA TRI THỨC",
                        description = "Bên vệ đường có một chiếc rương cổ khắc cổ ngữ Tiếng Anh đang phát sáng yếu ớt.",
                        options = new List<RoadsideChoiceOption>
                        {
                            new RoadsideChoiceOption
                            {
                                label = "Giải mã cổ ngữ (Quiz B1/B2)",
                                subtitle = "Đúng: Nhận ngay 1 Trang bị Hiếm/Sử Thi + 45 Vàng!",
                                actionType = RoadsideChoiceAction.QuizForReward
                            },
                            new RoadsideChoiceOption
                            {
                                label = "Cạy khóa ngoài (An toàn)",
                                subtitle = "Nhặt nhanh +15 Vàng rơi vãi rồi tiếp tục hành quân.",
                                actionType = RoadsideChoiceAction.SafeRestOrSmallReward
                            },
                            new RoadsideChoiceOption
                            {
                                label = "Bỏ qua & Đi tiếp",
                                subtitle = "Giữ nguyên đội hình tiến tới đợt quái kế tiếp.",
                                actionType = RoadsideChoiceAction.Leave
                            }
                        }
                    };

                case RoadsideEventType.SacredForestShrine:
                    return new RoadsideEventOffer
                    {
                        eventType = type,
                        title = "ĐỀN THỜ RỪNG THIÊNG",
                        description = "Một tượng đài cổ tỏa ra làn gió ấm áp giúp các anh hùng hồi phục giữa đường hành quân.",
                        options = new List<RoadsideChoiceOption>
                        {
                            new RoadsideChoiceOption
                            {
                                label = "Đọc lời cầu nguyện (Quiz B1/B2)",
                                subtitle = "Đúng: Hồi 35% HP & 30 MP, hồi sinh đồng đội gục ngã + 20 Nộ!",
                                actionType = RoadsideChoiceAction.QuizForReward
                            },
                            new RoadsideChoiceOption
                            {
                                label = "Nghỉ chân bên suối (Không Quiz)",
                                subtitle = "Hồi nhẹ 15% HP & 12 MP cho toàn đội hình đang sống.",
                                actionType = RoadsideChoiceAction.SafeRestOrSmallReward
                            },
                            new RoadsideChoiceOption
                            {
                                label = "Tiếp tục hành quân",
                                subtitle = "Không dừng lại, giữ nguyên nhịp chiến đấu.",
                                actionType = RoadsideChoiceAction.Leave
                            }
                        }
                    };

                default:
                    return new RoadsideEventOffer
                    {
                        eventType = RoadsideEventType.WanderingScholar,
                        title = "HỌC GIẢ LƯU ĐỘNG",
                        description = "Một vị học giả bí ẩn dừng xe bên đường, muốn trao đổi kiến thức và dược phẩm hiếm.",
                        options = new List<RoadsideChoiceOption>
                        {
                            new RoadsideChoiceOption
                            {
                                label = "Nhận thử thách dịch thuật (Quiz B1/B2)",
                                subtitle = "Đúng: Nhận +60 Vàng, +1 Thảo Dược Máu & +20 Nộ Tri Thức!",
                                actionType = RoadsideChoiceAction.QuizForReward
                            },
                            new RoadsideChoiceOption
                            {
                                label = "Mua nhanh Thảo Dược Máu (-25 Vàng)",
                                subtitle = "Giá ưu đãi dọc đường (tiết kiệm 15 Vàng so với Tiệm).",
                                actionType = RoadsideChoiceAction.PayGoldForPotion,
                                goldCost = 25
                            },
                            new RoadsideChoiceOption
                            {
                                label = "Chào tạm biệt & Đi tiếp",
                                subtitle = "Tiết kiệm Vàng và tiếp tục tiến lên.",
                                actionType = RoadsideChoiceAction.Leave
                            }
                        }
                    };
            }
        }
    }
}
