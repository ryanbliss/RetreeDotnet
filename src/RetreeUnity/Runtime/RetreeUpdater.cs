// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using UnityEngine;

namespace RetreeCore.Unity
{
    public class RetreeUpdater : MonoBehaviour
    {
        protected virtual void Start() { }

        protected virtual void Update()
        {
            Retree.Tick();
        }

        protected virtual void OnDestroy()
        {
            Reset();
        }
    }
}
