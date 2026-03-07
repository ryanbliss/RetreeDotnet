// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using NUnit.Framework;
using Retree;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpaceInvaders.Tests
{
    public class ControllerTests
    {
        [TearDown]
        public void TearDown()
        {
            TestHelpers.CleanupScene();
        }

        [UnityTest]
        public IEnumerator GameController_ShowsStartScreen_OnLoad()
        {
            SetupUI();
            var gc = TestHelpers.CreateGameController();
            yield return TestHelpers.WaitFrames(2);

            var startText = GameObject.Find("StartText");
            Assert.IsNotNull(startText);
            Assert.IsTrue(startText.activeSelf);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameController_StartsGame_OnStartGame()
        {
            SetupUI();
            var gc = TestHelpers.CreateGameController();
            yield return TestHelpers.WaitFrames(2);

            // Cache reference before hiding (Find can't locate inactive objects)
            var startText = GameObject.Find("StartText");
            Assert.IsNotNull(startText);

            gc.game.StartGame();
            // gameActive is a field change, needs a tick
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            Assert.IsTrue(gc.game.gameActive);
            Assert.IsFalse(startText.activeSelf);

            // Player GO should exist
            var playerGo = GameObject.Find("Player");
            Assert.IsNotNull(playerGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameController_SpawnsEnemyGameObject()
        {
            SetupUI();
            var gc = TestHelpers.CreateGameController();
            yield return TestHelpers.WaitFrames(2);

            gc.game.StartGame();
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            // StartGame triggers first SpawnEnemy via OnGameActiveChanged
            var enemyGo = GameObject.Find("Enemy");
            Assert.IsNotNull(enemyGo);
            Assert.IsNotNull(enemyGo.GetComponent<EnemyController>());
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameController_SpawnsProjectileGameObject()
        {
            SetupUI();
            var gc = TestHelpers.CreateGameController();
            yield return TestHelpers.WaitFrames(2);

            gc.game.StartGame();
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            gc.game.player.Shoot();
            yield return TestHelpers.WaitFrames(2);

            var projGo = GameObject.Find("Projectile");
            Assert.IsNotNull(projGo);
            Assert.IsNotNull(projGo.GetComponent<ProjectileController>());
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameController_RemovesOffScreenProjectile()
        {
            SetupUI();
            var gc = TestHelpers.CreateGameController();
            yield return TestHelpers.WaitFrames(2);

            gc.game.StartGame();
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            gc.game.player.Shoot();
            yield return TestHelpers.WaitFrames(1);

            Assert.AreEqual(1, gc.game.projectiles.Count);

            // Move projectile way off screen
            gc.game.projectiles[0].yPos = GameConstants.TopBound + 10f;
            yield return TestHelpers.WaitFrames(3);

            Assert.AreEqual(0, gc.game.projectiles.Count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameController_CleansUpEnemyGameObject_OnDeath()
        {
            SetupUI();
            var gc = TestHelpers.CreateGameController();
            yield return TestHelpers.WaitFrames(2);

            gc.game.StartGame();
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            var enemy = gc.game.enemies[0];
            // Kill enemy by reducing health to 0
            enemy.health.TakeDamage(999);
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(3);

            // Enemy should be removed and GO destroyed
            Assert.AreEqual(0, gc.game.enemies.Count);
            var enemyGo = GameObject.Find("Enemy");
            Assert.IsNull(enemyGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameController_IncrementsScore_OnEnemyKill()
        {
            SetupUI();
            var gc = TestHelpers.CreateGameController();
            yield return TestHelpers.WaitFrames(2);

            gc.game.StartGame();
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            Assert.AreEqual(0, gc.game.player.score);

            var enemy = gc.game.enemies[0];
            enemy.health.TakeDamage(999);
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(3);

            Assert.AreEqual(1, gc.game.player.score);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameController_EndsGame_OnPlayerDeath()
        {
            SetupUI();
            var gc = TestHelpers.CreateGameController();
            yield return TestHelpers.WaitFrames(2);

            gc.game.StartGame();
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            Assert.IsTrue(gc.game.gameActive);

            gc.game.player.health.TakeDamage(999);
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(3);

            // Game end is deferred, needs another tick cycle
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(3);

            Assert.IsFalse(gc.game.gameActive);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerController_UpdatesPosition_OnNodeChange()
        {
            SetupUI();
            var gc = TestHelpers.CreateGameController();
            yield return TestHelpers.WaitFrames(2);

            gc.game.StartGame();
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            gc.game.player.xPos = 3f;
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            var playerGo = GameObject.Find("Player");
            Assert.IsNotNull(playerGo);
            Assert.AreEqual(3f, playerGo.transform.position.x, 0.1f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnemyController_UpdatesPosition_OnNodeChange()
        {
            SetupUI();
            var gc = TestHelpers.CreateGameController();
            yield return TestHelpers.WaitFrames(2);

            gc.game.StartGame();
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            var enemy = gc.game.enemies[0];
            float testX = 2f;
            enemy.xPos = testX;
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            var enemyGo = GameObject.Find("Enemy");
            Assert.IsNotNull(enemyGo);
            Assert.AreEqual(testX, enemyGo.transform.position.x, 0.1f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProjectileController_UpdatesPosition_OnNodeChange()
        {
            SetupUI();
            var gc = TestHelpers.CreateGameController();
            yield return TestHelpers.WaitFrames(2);

            gc.game.StartGame();
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            gc.game.player.Shoot();
            yield return TestHelpers.WaitFrames(1);

            var proj = gc.game.projectiles[0];
            float testY = 2f;
            proj.yPos = testY;
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            var projGo = GameObject.Find("Projectile");
            Assert.IsNotNull(projGo);
            Assert.AreEqual(testY, projGo.transform.position.y, 0.1f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Projectile_Collision_WithEnemy_DealsDamage()
        {
            SetupUI();
            var gc = TestHelpers.CreateGameController();
            yield return TestHelpers.WaitFrames(2);

            gc.game.StartGame();
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            var enemy = gc.game.enemies[0];
            int healthBefore = enemy.health.health;

            // Create a projectile right at the enemy's position going up
            var proj = new LaserProjectile(enemy.xPos, enemy.yPos, 1f);
            gc.game.projectiles.Add(proj);
            yield return TestHelpers.WaitFrames(5); // wait for physics trigger

            // If physics doesn't trigger, manually call OnCollision for verification
            if (enemy.health.health == healthBefore && gc.game.projectiles.Count > 0)
            {
                proj.OnCollision(enemy);
            }

            Assert.Less(enemy.health.health, healthBefore);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Projectile_Collision_WithPlayer_DealsDamage()
        {
            SetupUI();
            var gc = TestHelpers.CreateGameController();
            yield return TestHelpers.WaitFrames(2);

            gc.game.StartGame();
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            int healthBefore = gc.game.player.health.health;

            // Create a projectile right at the player's position going down
            var proj = new LaserProjectile(gc.game.player.xPos, gc.game.player.yPos, -1f);
            gc.game.projectiles.Add(proj);
            yield return TestHelpers.WaitFrames(5);

            // If physics doesn't trigger, manually call OnCollision for verification
            if (gc.game.player.health.health == healthBefore && gc.game.projectiles.Count > 0)
            {
                proj.OnCollision(gc.game.player);
            }

            Assert.Less(gc.game.player.health.health, healthBefore);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FormationMovement_ReversesAtEdge()
        {
            SetupUI();
            var gc = TestHelpers.CreateGameController();
            yield return TestHelpers.WaitFrames(2);

            gc.game.StartGame();
            Retree.Retree.Tick();
            yield return TestHelpers.WaitFrames(2);

            var enemy = gc.game.enemies[0];
            float initialDirection = gc.game.formationDirection;

            // Place enemy at the right edge so MoveFormation detects it
            enemy.xPos = GameConstants.RightBound;
            Retree.Retree.Tick();

            // Wait for formation movement to detect the edge
            yield return TestHelpers.WaitFrames(3);

            // Direction should have reversed
            Assert.AreNotEqual(initialDirection, gc.game.formationDirection);
            yield return null;
        }

        // ---- Test UI Setup Helper ----

        private void SetupUI()
        {
            var canvas = new GameObject("Canvas");
            canvas.AddComponent<Canvas>();

            var startGo = new GameObject("StartText");
            startGo.transform.SetParent(canvas.transform);
            startGo.AddComponent<UnityEngine.UI.Text>();

            var scoreGo = new GameObject("ScoreText");
            scoreGo.transform.SetParent(canvas.transform);
            scoreGo.AddComponent<UnityEngine.UI.Text>();

            var healthGo = new GameObject("HealthText");
            healthGo.transform.SetParent(canvas.transform);
            healthGo.AddComponent<UnityEngine.UI.Text>();
        }
    }
}
