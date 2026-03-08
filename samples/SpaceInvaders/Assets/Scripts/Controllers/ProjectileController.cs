// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using RetreeCore;
using UnityEngine;

namespace SpaceInvaders
{
    public class ProjectileController : MonoBehaviour
    {
        public LaserProjectile Projectile { get; private set; }

        public void Initialize(LaserProjectile projectile)
        {
            Projectile = projectile;
            projectile.RegisterOnNodeChanged(OnProjectileNodeChanged);
            UpdatePosition();
        }

        private void OnProjectileNodeChanged(NodeChangedArgs args)
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            transform.position = new Vector3(Projectile.xPos, Projectile.yPos, 0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Projectile == null) return;

            // Upward projectiles hit enemies only
            if (Projectile.yDirection > 0)
            {
                var enemyController = other.GetComponent<EnemyController>();
                if (enemyController != null && enemyController.Enemy != null)
                {
                    Projectile.OnCollision(enemyController.Enemy);
                }
            }
            // Downward projectiles hit players only
            else
            {
                var playerController = other.GetComponent<PlayerController>();
                if (playerController != null && playerController.Player != null)
                {
                    Projectile.OnCollision(playerController.Player);
                }
            }
        }

        private void OnDestroy()
        {
            if (Projectile != null)
                Projectile.UnregisterOnNodeChanged(OnProjectileNodeChanged);
        }
    }
}
