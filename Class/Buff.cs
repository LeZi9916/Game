﻿using RoguelikeGame.Prefabs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RoguelikeGame.Interfaces;
using static RoguelikeGame.Game;

namespace RoguelikeGame.Class
{
    internal class Buff : ISerializable<Buff>
    {
        /// <summary>
        /// 表示该Buff的影响
        /// </summary>
        public required BuffEffect Effect;
        /// <summary>
        /// 表示该Buff的生效轮数
        /// </summary>
        public required int Rounds;
        /// <summary>
        /// 表示该Buff叠加时的计算方式
        /// </summary>
        public required Overlay OverlayType;
        /// <summary>
        /// 表示该Buff的值，可为整数亦可为百分比；当Effect属于DamageEffect或HPRecovery时，此值表示百分比，且必定为正数
        /// </summary>
        public required float Value;
        public Buff()
        {
        
        }
    
        public static string Serialize(Buff? buff)
        {
            string serializeStr = "";
            if(buff is null)
                serializeStr = "null"; 
            else
            {
                serializeStr = $"{GetBase64Str((int)buff.Effect)};";            
                serializeStr += $"{GetBase64Str(buff.Rounds)};";
                serializeStr += $"{GetBase64Str((int)buff.OverlayType)};";
                serializeStr += $"{GetBase64Str(buff.Value)}";
            }
            return GetBase64Str(serializeStr);
        }
        public static Buff? Deserialize(string serializeStr)
        {
            if (Base64ToStr(serializeStr) == "null")
                return null;
            var deserializeArray = Base64ToStr(serializeStr).Split(";",StringSplitOptions.RemoveEmptyEntries);
            BuffEffect effect = (BuffEffect)int.Parse(deserializeArray[0]);
            int rounds = int.Parse(deserializeArray[1]);
            Overlay overlayType = (Overlay)int.Parse(deserializeArray[2]);
            float value = float.Parse(deserializeArray[3]);
            return new()
            {
                Effect = effect,
                Rounds = rounds,
                OverlayType = overlayType,
                Value = value
            };
        }
        public static string SerializeArray(IEnumerable<Buff?> buffs)
        {
            string serializeStr = "";            
            foreach(var buff in buffs)
                serializeStr += $"{Buff.Serialize(buff)};";
            return GetBase64Str(serializeStr);
        }
        public static Buff?[] DeserializeArray(string serializeStr)
        {
            var deserializeArray = Base64ToStr(serializeStr).Split(";",StringSplitOptions.RemoveEmptyEntries);
            List<Buff?> buffs = new();
            foreach(var buffStr in deserializeArray)
                buffs.Add(Buff.Deserialize(buffStr));
            return buffs.ToArray();
        }
    }
    internal class BuffCollection : IEnumerable<Buff>
    {
        static BuffEffect[] NegativeEffect = new BuffEffect[]
        {
            BuffEffect.DamageDown,
            BuffEffect.ArmorDown,
            BuffEffect.DodgeDown,
            BuffEffect.Dizziness,
            BuffEffect.Freeze,
            BuffEffect.Firing,
        };
        static BuffEffect[] DamageEffect = new BuffEffect[]
        {
            BuffEffect.Firing,
            BuffEffect.Freeze
        };
        List<Buff> Buffs = new();
        public int Count
        {
            get { return Buffs.Count; }
        }
        public Buff this[int index]
        {
            get { return Buffs[index]; }
            set { Buffs[index] = value; }
        }
        public Buff[] this[BuffEffect effect]
        {
            get
            {
                if (!Contains(effect))
                    return new Buff[] { };
                return (from buff in Buffs
                        where buff.Effect == effect
                        select buff).ToArray();
            }
        }
        public Buff[] this[BuffEffect[] effects]
        {
            get
            {
                List<Buff> Buffs = new();
                foreach (var effect in effects)
                    Buffs.AddRange(this[effect]);
                return Buffs.ToArray();
            }
        }
        public bool Contains(BuffEffect effect)
        {
            foreach (var buff in Buffs)
                if (buff.Effect.Equals(effect))
                    return true;
            return false;
        }
        public IEnumerator<Buff> GetEnumerator() => ((IEnumerable<Buff>)Buffs).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<Buff>)Buffs).GetEnumerator();
        public void Add(Buff newBuff)
        {
            if (newBuff.Effect is BuffEffect.ClearNegativeBuff)
                Buffs.Clear();
            else
            {
                if (Contains(newBuff.Effect))
                {
                    var oldBuffs = from buff in this[newBuff.Effect]
                                  where buff.OverlayType == newBuff.OverlayType
                                  select buff;
                    if(oldBuffs.Count() != 0)
                    {
                        var oldBuff = oldBuffs.ToArray()[0];
                        var index = Buffs.IndexOf(oldBuff);
                        if(oldBuff.OverlayType is Overlay.Add)
                        {
                            oldBuff.Value += newBuff.Value;
                            this[index] = oldBuff;
                        }
                        else
                        {
                            oldBuff.Value *= newBuff.Value;
                            this[index] = oldBuff;
                        }
                    }
                    else
                        Buffs.Add(newBuff);
                    //var effect = newBuff.Effect;
                    //var oldBuffs = this[effect];
                    //foreach (var oldBuff in oldBuffs)
                    //{
                    //    var index = Buffs.IndexOf(oldBuff);
                    //    oldBuff.Rounds = Math.Max(oldBuff.Rounds, newBuff.Rounds);
                    //    if (newBuff.OverlayType == oldBuff.OverlayType)
                    //    {
                    //        if (newBuff.OverlayType.Equals(Overlay.Add))
                    //            oldBuff.Value += newBuff.Value;
                    //        else
                    //            oldBuff.Value *= newBuff.Value;
                    //    }
                    //    else
                    //        continue;
                    //    this[index] = oldBuff;
                    //}
                }
                else
                    Buffs.Add(newBuff);
            }
        }
        public void Remove(Buff oldBuff)
        {
            Buffs.Remove(oldBuff);
        }
        public void Remove(BuffEffect effect)
        {
            var buffs = this[effect];
            foreach (var buff in buffs)
                Buffs.Remove(buff);
        }
        public void Clear()
        {
            var negativeBuffs = this[NegativeEffect];
            foreach (var buff in negativeBuffs)
                Remove(buff);
        }
        public void ClearAll() => Buffs.Clear();
        public void NextRound(Prefab target)
        {
            ForEach(buff =>
            {
                target.Skills.CoolDownRatio = target.CoolDownRatio;

                if (DamageEffect.Contains(buff.Effect))
                    target.Health -= (long)(target.MaxHealth * buff.Value);
                else if (buff.Effect is BuffEffect.CoolDownBoost)
                    target.Skills.CoolDownRatio *= buff.Value;
                else if (buff.Effect is BuffEffect.HPRecovery)
                    target.Health += (long)(target.MaxHealth * buff.Value);
                if (--buff.Rounds <= 0)
                    Buffs.Remove(buff);
            });
        }
        public void AddRange(IEnumerable<Buff> target)
        {
            foreach (var buff in target)
                Add(buff);
        }
        public void ForEach(Action<Buff> action)
        {
            foreach (var buff in Buffs)
                action(buff);
        }
    }
}
