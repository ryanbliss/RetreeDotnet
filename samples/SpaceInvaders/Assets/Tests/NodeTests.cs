// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using NUnit.Framework;
using RetreeCore;
using UnityEngine.TestTools;

namespace SpaceInvaders.Tests
{
    public class NodeTests
    {
        [TearDown]
        public void TearDown()
        {
            TestHelpers.CleanupScene();
        }

        [UnityTest]
        public IEnumerator Health_TakeDamage_ReducesHealth()
        {
            var health = new Health(100);
            health.TakeDamage(30);
            Assert.AreEqual(70, health.health);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Health_TakeDamage_ClampsToZero()
        {
            var health = new Health(50);
            health.TakeDamage(999);
            Assert.AreEqual(0, health.health);
            Assert.IsFalse(health.IsAlive);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Health_Reset_RestoresStartHealth()
        {
            var health = new Health(100);
            health.TakeDamage(60);
            Assert.AreEqual(40, health.health);
            health.Reset();
            Assert.AreEqual(100, health.health);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Player_MoveLeft_ClampsAtBound()
        {
            var player = new Player();
            player.MoveLeft(100f);
            Assert.AreEqual(GameConstants.LeftBound, player.xPos);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Player_MoveRight_ClampsAtBound()
        {
            var player = new Player();
            player.MoveRight(100f);
            Assert.AreEqual(GameConstants.RightBound, player.xPos);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Player_Reset_RestoresDefaults()
        {
            var player = new Player();
            player.xPos = 5f;
            player.score = 10;
            player.health.TakeDamage(50);

            player.Reset();

            Assert.AreEqual(0f, player.xPos);
            Assert.AreEqual(0, player.score);
            Assert.AreEqual(100, player.health.health);
            Assert.AreEqual(GameConstants.PlayerY, player.yPos);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LaserProjectile_Move_UpdatesYPos()
        {
            var proj = new LaserProjectile(0f, 0f, 1f); // direction up
            float startY = proj.yPos;
            proj.Move(1f);
            Assert.AreEqual(startY + 1f, proj.yPos, 0.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LaserProjectile_OnCollision_DamagesAndRemovesSelf()
        {
            var game = new Game();
            // Activate tree listening so parent tracking works
            game.RegisterOnTreeChanged(_ => { });
            Retree.Tick();

            var proj = new LaserProjectile(0f, 0f, 1f);
            game.projectiles.Add(proj);
            Assert.AreEqual(1, game.projectiles.Count);

            var enemy = game.SpawnEnemy();
            int healthBefore = enemy.health.health;

            proj.OnCollision(enemy);

            Assert.AreEqual(healthBefore - 10, enemy.health.health);
            Assert.AreEqual(0, game.projectiles.Count);

            Retree.ClearListeners(game, recursive: true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ship_Shoot_CreatesProjectile()
        {
            var game = new Game();
            game.RegisterOnTreeChanged(_ => { });
            Retree.Tick(); // takes snapshot, sets parent for player

            game.player.Shoot();

            Assert.AreEqual(1, game.projectiles.Count);
            Assert.AreEqual(1f, game.projectiles[0].yDirection); // player fires upward

            Retree.ClearListeners(game, recursive: true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Enemy_Shoot_CreatesDownwardProjectile()
        {
            var game = new Game();
            game.RegisterOnTreeChanged(_ => { });
            Retree.Tick();

            var enemy = game.SpawnEnemy();
            Retree.Tick(); // snapshot sees new enemy field

            enemy.Shoot();

            Assert.AreEqual(1, game.projectiles.Count);
            Assert.AreEqual(-1f, game.projectiles[0].yDirection); // enemy fires downward

            Retree.ClearListeners(game, recursive: true);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Game_SpawnEnemy_AddsToList()
        {
            var game = new Game();
            Assert.AreEqual(0, game.enemies.Count);

            game.SpawnEnemy();
            Assert.AreEqual(1, game.enemies.Count);

            game.SpawnEnemy();
            Assert.AreEqual(2, game.enemies.Count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Game_End_ResetsState()
        {
            var game = new Game();
            game.StartGame();
            game.SpawnEnemy();
            game.SpawnEnemy();
            game.player.score = 5;
            game.player.health.TakeDamage(30);

            game.End();

            Assert.IsFalse(game.gameActive);
            Assert.AreEqual(0, game.enemies.Count);
            Assert.AreEqual(0, game.projectiles.Count);
            Assert.AreEqual(0, game.player.score);
            Assert.AreEqual(100, game.player.health.health);
            Assert.AreEqual(0f, game.player.xPos);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Game_TreeChanged_FiresOnHealthChange()
        {
            var game = new Game();
            bool treeChangedFired = false;

            game.RegisterOnTreeChanged(args =>
            {
                foreach (var change in args.Changes)
                {
                    if (change.FieldName == "health")
                        treeChangedFired = true;
                }
            });
            Retree.Tick(); // initial snapshot

            game.player.health.TakeDamage(10);
            Retree.Tick(); // detect health change

            Assert.IsTrue(treeChangedFired);

            Retree.ClearListeners(game, recursive: true);
            yield return null;
        }
    }
}
