// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using Retree;

namespace SpaceInvaders
{
    public class Ship : RetreeNode, IHasYPos, IHasHealth
    {
        public Health health;
        public float xPos;
        public float yPos;

        // Explicit interface implementations delegate to the fields
        float IHasYPos.yPos => this.yPos;
        Health IHasHealth.health => this.health;

        public Ship(int startHealth, float xPos, float yPos)
        {
            this.health = new Health(startHealth);
            this.xPos = xPos;
            this.yPos = yPos;
        }

        public void Shoot()
        {
            // Walk up the parent chain to find the Game node
            var current = Retree.Retree.Parent(this);
            while (current != null)
            {
                if (current is Game game)
                {
                    game.SpawnProjectile(this);
                    return;
                }
                current = Retree.Retree.Parent(current);
            }
        }
    }
}
