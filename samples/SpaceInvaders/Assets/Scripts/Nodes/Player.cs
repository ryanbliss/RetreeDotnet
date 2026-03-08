// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SpaceInvaders
{
    public class Player : Ship
    {
        public int score = 0;

        public Player() : base(100, 0f, GameConstants.PlayerY) { }

        public void MoveLeft(float speed)
        {
            xPos = Math.Max(GameConstants.LeftBound, xPos - speed);
        }

        public void MoveRight(float speed)
        {
            xPos = Math.Min(GameConstants.RightBound, xPos + speed);
        }

        public void Reset()
        {
            health.Reset();
            xPos = 0f;
            yPos = GameConstants.PlayerY;
            score = 0;
        }
    }
}
