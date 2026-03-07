// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using Retree;

namespace SpaceInvaders
{
    public class LaserProjectile : RetreeNode, IHasYPos
    {
        [RetreeIgnore]
        public int damage = 10;

        [RetreeIgnore]
        public readonly float xPos;

        public float yPos;

        [RetreeIgnore]
        public readonly float yDirection;

        // Explicit interface implementation delegates to the field
        float IHasYPos.yPos => this.yPos;

        public LaserProjectile(float xPos, float startYPos, float yDirection)
        {
            this.xPos = xPos;
            this.yPos = startYPos;
            this.yDirection = yDirection;
        }

        public void Move(float speed)
        {
            yPos += yDirection * speed;
        }

        public void OnCollision(IHasHealth hit)
        {
            hit.health.TakeDamage(damage);
            var parent = Retree.Retree.Parent(this);
            if (parent is RetreeList<LaserProjectile> list)
            {
                list.Remove(this);
            }
        }
    }
}
