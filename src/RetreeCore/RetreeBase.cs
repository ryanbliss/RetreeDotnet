// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace RetreeCore
{
    public abstract class RetreeBase
    {
        internal RetreeBase _parent;

        private event Action<NodeChangedArgs> _onNodeChanged;
        private int _nodeListenerCount;

        private event Action<TreeChangedArgs> _onTreeChanged;
        private int _treeListenerCount;

        internal bool HasNodeListeners => _nodeListenerCount > 0;
        internal bool HasTreeListeners => _treeListenerCount > 0;
        internal bool HasAnyListeners => _nodeListenerCount > 0 || _treeListenerCount > 0;

        public void OnNodeChanged(Action<NodeChangedArgs> listener)
        {
            _onNodeChanged += listener;
            _nodeListenerCount++;
            if (_nodeListenerCount == 1)
                OnFirstNodeListenerAdded();
        }

        public void OffNodeChanged(Action<NodeChangedArgs> listener)
        {
            _onNodeChanged -= listener;
            _nodeListenerCount--;
            if (_nodeListenerCount == 0)
                OnLastNodeListenerRemoved();
        }

        public void OnTreeChanged(Action<TreeChangedArgs> listener)
        {
            _onTreeChanged += listener;
            _treeListenerCount++;
            if (_treeListenerCount == 1)
                OnFirstTreeListenerAdded();
        }

        public void OffTreeChanged(Action<TreeChangedArgs> listener)
        {
            _onTreeChanged -= listener;
            _treeListenerCount--;
            if (_treeListenerCount == 0)
                OnLastTreeListenerRemoved();
        }

        internal void EmitNodeChanged(NodeChangedArgs args)
        {
            _onNodeChanged?.Invoke(args);
        }

        internal void EmitTreeChanged(TreeChangedArgs args)
        {
            _onTreeChanged?.Invoke(args);
        }

        internal void ClearAllListeners()
        {
            if (_nodeListenerCount > 0)
            {
                _onNodeChanged = null;
                _nodeListenerCount = 0;
                OnLastNodeListenerRemoved();
            }
            if (_treeListenerCount > 0)
            {
                _onTreeChanged = null;
                _treeListenerCount = 0;
                OnLastTreeListenerRemoved();
            }
        }

        /// <summary>
        /// Propagates a tree change upward through the parent chain.
        /// Fires OnTreeChanged on each ancestor that has tree listeners.
        /// </summary>
        internal void PropagateAsTreeChange(RetreeBase sourceNode, IReadOnlyList<FieldChange> changes)
        {
            if (Retree.IsSilent) return;

            if (HasTreeListeners)
            {
                if (Retree.InTransaction)
                    Retree.QueueTreeChange(this, sourceNode, changes);
                else
                    EmitTreeChanged(new TreeChangedArgs(this, sourceNode, changes));
            }

            _parent?.PropagateAsTreeChange(sourceNode, changes);
        }

        protected virtual void OnFirstNodeListenerAdded() { }
        protected virtual void OnLastNodeListenerRemoved() { }
        protected virtual void OnFirstTreeListenerAdded() { }
        protected virtual void OnLastTreeListenerRemoved() { }
    }
}
