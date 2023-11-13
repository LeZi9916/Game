using System;
using RoguelikeGame.Class;
using RoguelikeGame.Interfaces;

namespace RoguelikeGame.Prefabs
{
    internal class Player : Prefab
    {
        public required long Experience;//目前经验值
        public required long ExpMaxLimit;//下一级

        public ItemCollection Items = new();
        public Player()
        {
            Type = PrefabType.Player;
        }
        public override bool Upgrade()
        {
            if (Experience >= ExpMaxLimit)
            {
                Level++;
                Experience -= ExpMaxLimit;
                ExpMaxLimit += 5 * (Level - 1);
                MaxHealth *= (long)Math.Pow(1.055, Level - 1);
                Damage *= (long)Math.Pow(1.06, Level - 1);
                if (Experience >= ExpMaxLimit)
                    Upgrade();
                return true;
            }
            return false;
        }
        public bool AddExp(long exp)
        {
            Experience += exp;
            return Upgrade();
        }
        public void DisposeItem(int index)//丢弃物品
        {
            Items.Remove(index);
        }
        /// <summary>
        /// 装备武器
        /// </summary>
        /// <param name="wear"></param>
        /// <returns></returns>
        public Item? Dress(Item wear)
        {
            switch (wear.Type)
            {
                case ItemType.Weapon:
                    if (Hand is not null )
                    {
                        var _Hand = Hand;
                        Hand = wear as Weapon;
                        if(!Items.Add(_Hand))
                            return _Hand;
                    }
                    else
                        Hand = wear as Weapon;
                    break;
                case ItemType.Armor:
                    if (Body is not null)
                    {
                        var _Body = Body;
                        Body = wear as Armor;
                        if (!Items.Add(_Body))
                            return _Body;
                    }
                    else
                        Body = wear as Armor;
                    break;
            }
            return null;
        }
        /// <summary>
        /// 卸下装备
        /// </summary>
        /// <param name="wearType"></param>
        /// <returns></returns>
        public Item? UnDress(ItemType wearType)
        {
            switch(wearType) 
            {
                case ItemType.Weapon:
                    if (!Items.Add(Hand))
                        return Hand;
                    Hand = null;
                    break;
                case ItemType.Armor:
                    if (!Items.Add(Body))
                        return Body;
                    Body = null;
                    break;
            }
            return null;
        }
    }
}