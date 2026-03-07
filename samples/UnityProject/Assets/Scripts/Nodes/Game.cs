// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using RetreeCore;

namespace SpaceInvaders
{
    public class Game : RetreeNode
    {
        public RetreeList<Enemy> enemies = new RetreeList<Enemy>();
        public Player player = new Player();
        public RetreeList<LaserProjectile> projectiles = new RetreeList<LaserProjectile>();
        public bool gameActive = false;

        [RetreeIgnore]
        public float formationDirection = 1f;

        public Enemy SpawnEnemy()
        {
            var enemy = new Enemy();
            enemies.Add(enemy);
            return enemy;
        }

        public void SpawnProjectile(Ship shooter)
        {
            float direction = shooter.yPos < 0 ? 1f : -1f;
            var proj = new LaserProjectile(
                shooter.xPos,
                shooter.yPos,
                direction
            );
            projectiles.Add(proj);
        }

        public void StartGame()
        {
            gameActive = true;
        }

        public void End()
        {
            gameActive = false;
            player.Reset();
            enemies.Clear();
            projectiles.Clear();
            formationDirection = 1f;
        }
    }
}
