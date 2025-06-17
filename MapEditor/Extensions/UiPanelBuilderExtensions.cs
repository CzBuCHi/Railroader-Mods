using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UI.Builder;
using UnityEngine;

namespace MapEditor.Extensions;

public static class UiPanelBuilderExtensions
{
    #region AddEnumDropdown

    private static readonly Dictionary<Type, List<string>> _EnumValues = new();

    private static List<string> GetEnumValues<T>() {
        var type = typeof(T);
        if (!_EnumValues.TryGetValue(type, out var values)) {
            values = new List<string>();
            foreach (var value in Enum.GetValues(type)) {
                values.Add(value.ToString());
            }

            _EnumValues.Add(type, values);
        }

        return values!;
    }

    public static RectTransform AddEnumDropdown<T>(this UIPanelBuilder builder, T current, Action<T> onSelected) where T : Enum {
        var values               = GetEnumValues<T>();
        var currentSelectedIndex = values.IndexOf(current.ToString());
        var rect                 = builder.AddDropdown(values, currentSelectedIndex, o => onSelected((T)Enum.Parse(typeof(T), values[o]!)))!;
        var tmpDropdown          = rect.GetComponent<TMP_Dropdown>()!;
        tmpDropdown.MultiSelect = false;
        return rect;
    }

    #endregion
}
