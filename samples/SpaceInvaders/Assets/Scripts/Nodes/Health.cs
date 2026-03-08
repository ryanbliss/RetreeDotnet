// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using RetreeCore;

namespace SpaceInvaders
{
    public class Health : RetreeNode
    {
        public int health;

        [RetreeIgnore]
        public int startHealth;

        public Health(int startHealth)
        {
            this.health = startHealth;
            this.startHealth = startHealth;
        }

        public void TakeDamage(int amount)
        {
            health = Math.Max(0, health - amount);
        }

        public void Reset()
        {
            health = startHealth;
        }

        public bool IsAlive => health > 0;
    }
}
