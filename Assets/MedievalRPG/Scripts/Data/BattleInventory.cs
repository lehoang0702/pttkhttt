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
            Reset(healthPotions, manaElixirs, phoenixFeathers);
        }

        public void Reset(int healthPotions = 3, int manaElixirs = 2, int phoenixFeathers = 1)
        {
            _counts[ConsumableItemType.HealthPotion] = Mathf.Max(0, healthPotions);
            _counts[ConsumableItemType.ManaElixir] = Mathf.Max(0, manaElixirs);
            _counts[ConsumableItemType.PhoenixFeather] = Mathf.Max(0, phoenixFeathers);
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

        public static string GetDisplayName(ConsumableItemType item)
        {
            switch (item)
            {
                case ConsumableItemType.HealthPotion:   return "Thảo Dược Máu";
                case ConsumableItemType.ManaElixir:     return "Bình Mana";
                case ConsumableItemType.PhoenixFeather: return "Lông Phượng Hoàng";
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
                default:                                return string.Empty;
            }
        }
    }
}
