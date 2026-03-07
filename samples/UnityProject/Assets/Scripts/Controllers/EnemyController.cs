// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using Retree;
using UnityEngine;

namespace SpaceInvaders
{
    public class EnemyController : MonoBehaviour
    {
        public Enemy Enemy { get; private set; }

        public void Initialize(Enemy enemy)
        {
            Enemy = enemy;
            enemy.RegisterOnNodeChanged(OnEnemyNodeChanged);
            UpdatePosition();
        }

        private void OnEnemyNodeChanged(NodeChangedArgs args)
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            transform.position = new Vector3(Enemy.xPos, Enemy.yPos, 0f);
        }

        private void OnDestroy()
        {
            if (Enemy != null)
                Enemy.UnregisterOnNodeChanged(OnEnemyNodeChanged);
        }
    }
}
