// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using RetreeCore;

namespace SpaceInvaders
{
    public class Ship : RetreeNode
    {
        public Health health;
        public float xPos;
        public float yPos;

        public Ship(int startHealth, float xPos, float yPos)
        {
            this.health = new Health(startHealth);
            this.xPos = xPos;
            this.yPos = yPos;
        }

        public void Shoot()
        {
            // Walk up the parent chain to find the Game node
            var current = Retree.Parent(this);
            while (current != null)
            {
                if (current is Game game)
                {
                    game.SpawnProjectile(this);
                    return;
                }
                current = Retree.Parent(current);
            }
        }
    }
}
