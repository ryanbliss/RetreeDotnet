// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using RetreeCore;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SpaceInvaders
{
    public class PlayerController : MonoBehaviour
    {
        public Player Player { get; private set; }
        private const float MoveSpeed = 6f;

        public void Initialize(Player player)
        {
            Player = player;
            player.RegisterOnNodeChanged(OnPlayerNodeChanged);
            UpdatePosition();
        }

        private void Update()
        {
            if (Player == null) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.leftArrowKey.isPressed)
                Player.MoveLeft(MoveSpeed * Time.deltaTime);
            if (keyboard.rightArrowKey.isPressed)
                Player.MoveRight(MoveSpeed * Time.deltaTime);
            if (keyboard.spaceKey.wasPressedThisFrame)
                Player.Shoot();
        }

        private void OnPlayerNodeChanged(NodeChangedArgs args)
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            transform.position = new Vector3(Player.xPos, Player.yPos, 0f);
        }

        private void OnDestroy()
        {
            if (Player != null)
                Player.UnregisterOnNodeChanged(OnPlayerNodeChanged);
        }
    }
}
