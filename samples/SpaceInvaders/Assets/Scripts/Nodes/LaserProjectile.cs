// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using RetreeCore;

namespace SpaceInvaders
{
    public class LaserProjectile : RetreeNode
    {
        [RetreeIgnore]
        public int damage = 10;

        [RetreeIgnore]
        public readonly float xPos;

        public float yPos;

        [RetreeIgnore]
        public readonly float yDirection;

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

        public void OnCollision(Ship hit)
        {
            hit.health.TakeDamage(damage);
            var parent = Retree.Parent(this);
            if (parent is RetreeList<LaserProjectile> list)
            {
                list.Remove(this);
            }
        }
    }
}
