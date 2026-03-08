// Copyright (c) Ryan Bliss and contributors. All rights reserved.
// Licensed under the MIT License.

namespace RetreeCore
{
    public readonly struct FieldChange
    {
        public string FieldName { get; }
        public object OldValue { get; }
        public object NewValue { get; }

        public FieldChange(string fieldName, object oldValue, object newValue)
        {
            FieldName = fieldName;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public override string ToString()
        {
            return $"FieldChange({FieldName}: {OldValue} -> {NewValue})";
        }
    }
}
