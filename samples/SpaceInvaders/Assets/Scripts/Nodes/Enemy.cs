// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SpaceInvaders
{
    public class Enemy : Ship
    {
        private static readonly Random _random = new Random();

        public Enemy() : base(20, RandomXInBounds(), GameConstants.EnemySpawnY) { }

        private static float RandomXInBounds()
        {
            float min = GameConstants.LeftBound + 1f;
            float max = GameConstants.RightBound - 1f;
            return (float)(_random.NextDouble() * (max - min) + min);
        }
    }
}
