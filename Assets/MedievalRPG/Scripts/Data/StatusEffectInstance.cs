using System;
using UnityEngine;

namespace MedievalRPG.Data
{
    [Serializable]
    public class StatusEffectInstance
    {
        public StatusEffectType type;
        public int remainingTurns;
        public int potency;

        public StatusEffectInstance(StatusEffectType type, int remainingTurns, int potency = 0)
        {
            this.type = type;
            this.remainingTurns = Mathf.Max(1, remainingTurns);
            this.potency = potency;
        }

        public string GetShortBadgeLabel()
        {
            switch (type)
            {
                case StatusEffectType.Stun:      return $"CHOÁNG ({remainingTurns})";
                case StatusEffectType.Burn:      return $"CHÁY {potency} ({remainingTurns})";
                case StatusEffectType.Taunt:     return $"KHIÊU KHÍCH ({remainingTurns})";
                case StatusEffectType.DefenseUp: return $"+{potency} GIÁP ({remainingTurns})";
                case StatusEffectType.AttackUp:  return $"+{potency}% CÔNG ({remainingTurns})";
                default:                         return string.Empty;
            }
        }
    }
}
