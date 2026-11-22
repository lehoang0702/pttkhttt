using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedievalRPG.Data
{
    [Serializable]
    public class BattleInventory
    {
        private readonly Dictionary<ConsumableItemType, int> _counts = new Dictionary<ConsumableItemType, int>();

        public event Action OnInventoryChanged;

        public BattleInventory(int healthPotions = 3, int manaElixirs = 2, int phoenixFeathers = 1)
        {
            Reset(healthPotions, manaElixirs, phoenixFeathers, 0, 0);
        }

        public void Reset(int healthPotions = 3, int manaElixirs = 2, int phoenixFeathers = 1, int warElixirs = 0, int ironTonics = 0)
        {
            _counts[ConsumableItemType.HealthPotion] = Mathf.Max(0, healthPotions);
            _counts[ConsumableItemType.ManaElixir] = Mathf.Max(0, manaElixirs);
            _counts[ConsumableItemType.PhoenixFeather] = Mathf.Max(0, phoenixFeathers);
            _counts[ConsumableItemType.WarElixir] = Mathf.Max(0, warElixirs);
            _counts[ConsumableItemType.IronTonic] = Mathf.Max(0, ironTonics);
            OnInventoryChanged?.Invoke();
        }

        public int GetCount(ConsumableItemType item)
        {
            return _counts.TryGetValue(item, out int count) ? count : 0;
        }

        public int TotalItems
        {
            get
            {
                int total = 0;
                foreach (var kv in _counts) total += kv.Value;
                return total;
            }
        }

        public bool TryConsume(ConsumableItemType item)
        {
            int count = GetCount(item);
            if (count <= 0) return false;
            _counts[item] = count - 1;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void AddItem(ConsumableItemType item, int amount = 1)
        {
            if (amount <= 0) return;
            _counts[item] = GetCount(item) + amount;
            OnInventoryChanged?.Invoke();
        }

        public static string GetDisplayName(ConsumableItemType item)
        {
            switch (item)
            {
                case ConsumableItemType.HealthPotion:   return "Thảo Dược Máu";
                case ConsumableItemType.ManaElixir:     return "Bình Mana";
                case ConsumableItemType.PhoenixFeather: return "Lông Phượng Hoàng";
                case ConsumableItemType.WarElixir:      return "Thuốc Cuồng Chiến";
                case ConsumableItemType.IronTonic:      return "Thuốc Thiết Giáp";
                default:                                return item.ToString();
            }
        }

        public static string GetEffectDescription(ConsumableItemType item)
        {
            switch (item)
            {
                case ConsumableItemType.HealthPotion:   return "Hồi ngay +55 HP cho 1 đồng đội còn sống.";
                case ConsumableItemType.ManaElixir:     return "Hồi ngay +30 MP cho 1 đồng đội còn sống.";
                case ConsumableItemType.PhoenixFeather: return "Hồi sinh 1 đồng đội gục ngã với 50% HP tối đa.";
                case ConsumableItemType.WarElixir:      return "Tăng +30% Sát Thương (ATK/MAG) cho cả đội trong 3 lượt.";
                case ConsumableItemType.IronTonic:      return "Hồi +25 HP & tăng +12 Giáp cho cả đội trong 3 lượt.";
                default:                                return string.Empty;
            }
        }

        public static string GetShortEffectDescription(ConsumableItemType item)
        {
            switch (item)
            {
                case ConsumableItemType.HealthPotion:   return "+55 HP (1 đồng đội)";
                case ConsumableItemType.ManaElixir:     return "+30 MP (1 đồng đội)";
                case ConsumableItemType.PhoenixFeather: return "Hồi sinh 50% HP";
                case ConsumableItemType.WarElixir:      return "+30% Dame 3 lượt";
                case ConsumableItemType.IronTonic:      return "+25 HP & +12 Giáp 3 lượt";
                default:                                return string.Empty;
            }
        }
    }
}
