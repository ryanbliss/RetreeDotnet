// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using RetreeCore;
using RetreeCore.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceInvaders
{
    public class GameController : RetreeUpdater
    {
        public Game game;

        // UI controller (lives on the Canvas)
        private GameUIController _ui;

        // Node-to-GameObject tracking
        private Dictionary<RetreeBase, GameObject> _nodeToGameObject =
            new Dictionary<RetreeBase, GameObject>();

        // Enemy shoot timers
        private Dictionary<Enemy, float> _enemyShootTimers =
            new Dictionary<Enemy, float>();

        // Deferred actions to avoid re-entrancy in tree listener
        private List<Enemy> _deadEnemies = new List<Enemy>();
        private bool _shouldEndGame = false;

        // Spawning (ramps up with score)
        private float _enemySpawnTimer = 0f;
        private const float BaseSpawnInterval = 6f;
        private const float MinSpawnInterval = 1f;
        private const float SpawnAccelPerKill = 0.5f;
        private const int BaseMaxEnemies = 8;
        private const int MaxEnemiesCap = 20;

        private float CurrentSpawnInterval =>
            Mathf.Max(MinSpawnInterval, BaseSpawnInterval - game.player.score * SpawnAccelPerKill);

        private int CurrentMaxEnemies =>
            Mathf.Min(MaxEnemiesCap, BaseMaxEnemies + game.player.score);

        // Movement constants
        private const float FormationSpeed = 2f;
        private const float ProjectileSpeed = 8f;

        protected override void Start()
        {
            base.Start();
            game = new Game();
            game.RegisterOnTreeChanged(OnGameTreeChanged);

            _ui = FindFirstObjectByType<GameUIController>();
            _ui?.ShowStartScreen();
        }

        protected override void Update()
        {
            base.Update(); // calls Retree.Tick()

            ProcessDeferredActions();

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame && !game.gameActive)
            {
                game.StartGame();
            }

            if (!game.gameActive) return;

            MoveFormation();
            MoveProjectiles();
            HandleEnemyShooting();
            HandleEnemySpawning();
            RemoveOffScreenProjectiles();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (game != null)
            {
                game.UnregisterOnTreeChanged(OnGameTreeChanged);
                Retree.ClearListeners(game, recursive: true);
            }
        }

        // ---- Tree Listener ----

        private void OnGameTreeChanged(TreeChangedArgs args)
        {
            foreach (var change in args.Changes)
            {
                // RetreeList mutations (enemy/projectile add/remove)
                if (change.FieldName == "Items")
                {
                    HandleItemChange(change);
                }
                else if (change.FieldName == "gameActive")
                {
                    OnGameActiveChanged();
                }
                else if (change.FieldName == "health" && args.SourceNode is Health h)
                {
                    HandleHealthChange(h);
                }
                else if (change.FieldName == "score")
                {
                    _ui?.UpdateScore(game.player.score);
                }
            }
        }

        private void HandleItemChange(FieldChange change)
        {
            // Item added
            if (change.NewValue != null && change.OldValue == null)
            {
                if (change.NewValue is Enemy enemy)
                    CreateEnemyGameObject(enemy);
                else if (change.NewValue is LaserProjectile proj)
                    CreateProjectileGameObject(proj);
            }
            // Item removed
            else if (change.OldValue != null && change.NewValue == null)
            {
                if (change.OldValue is Enemy)
                {
                    if (game.gameActive)
                        game.player.score++;
                }
                DestroyTrackedGameObject(change.OldValue as RetreeBase);
            }
        }

        private void HandleHealthChange(Health h)
        {
            var parent = Retree.Parent(h);
            if (parent is Player && !h.IsAlive)
            {
                _shouldEndGame = true;
            }
            else if (parent is Enemy enemy && !h.IsAlive)
            {
                if (!_deadEnemies.Contains(enemy))
                    _deadEnemies.Add(enemy);
            }
            _ui?.UpdateHealth(game.player.health.health);
        }

        private void ProcessDeferredActions()
        {
            if (_shouldEndGame)
            {
                _shouldEndGame = false;
                EndGame();
                return;
            }

            for (int i = 0; i < _deadEnemies.Count; i++)
            {
                var enemy = _deadEnemies[i];
                if (game.enemies.Contains(enemy))
                    game.enemies.Remove(enemy);
            }
            _deadEnemies.Clear();
        }

        // ---- Game State ----

        private void OnGameActiveChanged()
        {
            if (game.gameActive)
            {
                _ui?.HideStartScreen();
                _ui?.UpdateScore(game.player.score);
                _ui?.UpdateHealth(game.player.health.health);
                CreatePlayerGameObject();
                game.SpawnEnemy();
                _enemySpawnTimer = 0f;
            }
            else
            {
                _ui?.ShowStartScreen();
            }
        }

        private void EndGame()
        {
            // Destroy all tracked GameObjects before game.End() clears lists
            var toDestroy = new List<RetreeBase>(_nodeToGameObject.Keys);
            foreach (var node in toDestroy)
                DestroyTrackedGameObject(node);

            _nodeToGameObject.Clear();
            _enemyShootTimers.Clear();

            game.End();
        }

        // ---- Formation Movement ----

        private void MoveFormation()
        {
            if (game.enemies.Count == 0) return;

            // Find the leading edge of the formation
            float leadingEdge = game.enemies[0].xPos;
            for (int i = 1; i < game.enemies.Count; i++)
            {
                if (game.formationDirection > 0)
                    leadingEdge = Mathf.Max(leadingEdge, game.enemies[i].xPos);
                else
                    leadingEdge = Mathf.Min(leadingEdge, game.enemies[i].xPos);
            }

            float delta = game.formationDirection * FormationSpeed * Time.deltaTime;
            float newLeadingEdge = leadingEdge + delta;

            // Check if movement would push the leading enemy past the bound
            bool hitEdge = newLeadingEdge >= GameConstants.RightBound || newLeadingEdge <= GameConstants.LeftBound;

            if (hitEdge)
            {
                game.formationDirection *= -1f;
                for (int i = 0; i < game.enemies.Count; i++)
                    game.enemies[i].yPos -= 0.5f;
            }
            else
            {
                for (int i = 0; i < game.enemies.Count; i++)
                    game.enemies[i].xPos += delta;
            }
        }

        // ---- Projectile Movement ----

        private void MoveProjectiles()
        {
            for (int i = 0; i < game.projectiles.Count; i++)
                game.projectiles[i].Move(ProjectileSpeed * Time.deltaTime);
        }

        private void RemoveOffScreenProjectiles()
        {
            for (int i = game.projectiles.Count - 1; i >= 0; i--)
            {
                var proj = game.projectiles[i];
                if (proj.yPos > GameConstants.TopBound + 1f || proj.yPos < GameConstants.BottomBound - 1f)
                    game.projectiles.Remove(proj);
            }
        }

        // ---- Enemy Shooting ----

        private void HandleEnemyShooting()
        {
            // Clean up stale timers
            var stale = new List<Enemy>();
            foreach (var kvp in _enemyShootTimers)
            {
                if (!game.enemies.Contains(kvp.Key))
                    stale.Add(kvp.Key);
            }
            foreach (var e in stale)
                _enemyShootTimers.Remove(e);

            // Tick timers
            var keys = new List<Enemy>(_enemyShootTimers.Keys);
            foreach (var enemy in keys)
            {
                _enemyShootTimers[enemy] -= Time.deltaTime;
                if (_enemyShootTimers[enemy] <= 0f)
                {
                    enemy.Shoot();
                    _enemyShootTimers[enemy] = Random.Range(2f, 4f);
                }
            }
        }

        // ---- Enemy Spawning ----

        private void HandleEnemySpawning()
        {
            _enemySpawnTimer += Time.deltaTime;
            if (_enemySpawnTimer >= CurrentSpawnInterval && game.enemies.Count < CurrentMaxEnemies)
            {
                game.SpawnEnemy();
                _enemySpawnTimer = 0f;
            }
        }

        // ---- GameObject Factory ----

        private GameObject CreateQuadGameObject(string name, Color color, float scaleX, float scaleY)
        {
            var go = new GameObject(name);
            go.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            // Add sprite renderer with a white pixel texture for coloring
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePixelSprite();
            sr.color = color;

            // Add 2D physics
            var collider2D = go.AddComponent<BoxCollider2D>();
            collider2D.isTrigger = true;
            var rb2D = go.AddComponent<Rigidbody2D>();
            rb2D.bodyType = RigidbodyType2D.Kinematic;

            return go;
        }

        private static Sprite _pixelSprite;
        private static Sprite CreatePixelSprite()
        {
            if (_pixelSprite != null) return _pixelSprite;
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _pixelSprite;
        }

        private void CreatePlayerGameObject()
        {
            var go = CreateQuadGameObject("Player", Color.blue, 1f, 1f);
            var controller = go.AddComponent<PlayerController>();
            controller.Initialize(game.player);
            _nodeToGameObject[game.player] = go;
        }

        private void CreateEnemyGameObject(Enemy enemy)
        {
            var go = CreateQuadGameObject("Enemy", Color.red, 1f, 1f);
            var controller = go.AddComponent<EnemyController>();
            controller.Initialize(enemy);
            _nodeToGameObject[enemy] = go;
            _enemyShootTimers[enemy] = Random.Range(2f, 4f);
        }

        private void CreateProjectileGameObject(LaserProjectile proj)
        {
            var go = CreateQuadGameObject("Projectile", Color.white, 0.2f, 0.6f);
            var controller = go.AddComponent<ProjectileController>();
            controller.Initialize(proj);
            _nodeToGameObject[proj] = go;
        }

        private void DestroyTrackedGameObject(RetreeBase node)
        {
            if (node != null && _nodeToGameObject.TryGetValue(node, out var go))
            {
                _nodeToGameObject.Remove(node);
                if (node is Enemy enemy)
                    _enemyShootTimers.Remove(enemy);
                if (go != null)
                    Destroy(go);
            }
        }

    }
}
