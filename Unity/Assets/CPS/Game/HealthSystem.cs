using System;

namespace CPS.KitchenCatch
{
    public sealed class HealthSystem
    {
        public int CurrentHp { get; private set; }

        public void Reset(int initialHp)
        {
            CurrentHp = Math.Max(0, initialHp);
        }

        public void Damage(int amount)
        {
            CurrentHp = Math.Max(0, CurrentHp - Math.Max(0, amount));
        }
    }
}
