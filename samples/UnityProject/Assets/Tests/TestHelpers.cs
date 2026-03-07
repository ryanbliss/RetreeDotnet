// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace SpaceInvaders.Tests
{
    public static class TestHelpers
    {
        public static GameController CreateGameController()
        {
            var go = new GameObject("GameController");
            var controller = go.AddComponent<GameController>();
            return controller;
        }

        public static IEnumerator WaitFrames(int count = 2)
        {
            for (int i = 0; i < count; i++)
                yield return null;
        }

        public static IEnumerator WaitUntil(Func<bool> condition, float timeout = 5f)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            Assert.IsTrue(condition(), $"WaitUntil timed out after {timeout}s");
        }

        public static void CleanupScene()
        {
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go != null)
                    UnityEngine.Object.Destroy(go);
            }
            Retree.Retree.StopTicks();
        }
    }
}
