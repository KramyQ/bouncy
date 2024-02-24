using UnityEngine;

namespace MystroBot
{
    public interface IsDamageable
    {
        void dealDamage(float damage);

        public ulong getOwnerIdIfOwned();
    }
}