using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using MelonLoader;
using ModsApp.UI.Input.FieldFactories;
using S1API.Internal.Abstraction;
using S1API.UI;

namespace ModsApp.UI.Input.Handlers;

public class VectorHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;

    public VectorHandler(UITheme theme, MelonLogger.Instance logger)
    {
        _theme = theme;
        _logger = logger;
    }

    public bool CanHandle(Type valueType) => IsFloatStruct(valueType);

    private bool IsFloatStruct(Type type)
    {
        if (!type.IsValueType) return false;
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.IsLiteral) continue;
            if (!typeof(float).IsAssignableFrom(field.FieldType))
                return false;
        }

        return true;
    }

    public void CreateInput(MelonPreferences_Entry entry, GameObject parent, string entryKey,
        object currentValue, Action<string, object> onValueChanged)
    {
        if (currentValue == null) return;

        Type structType = currentValue.GetType();
        var fields = structType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var originalValues = new List<string>();

        var container = new GameObject($"{entryKey}_VectorContainer");
        container.transform.SetParent(parent.transform, false);

        var gridLayout = container.AddComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 6; // 3 pairs
        gridLayout.spacing = new Vector2(2, 2);
        gridLayout.cellSize = new Vector2(60, 25);
        gridLayout.childAlignment = TextAnchor.MiddleLeft;

        var contentSize = container.AddComponent<ContentSizeFitter>();
        contentSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            string fieldName = field.Name.StartsWith("m_") ? field.Name.Substring(2) : field.Name;
            string fieldValue = field.GetValue(currentValue)?.ToString() ?? "0";
            originalValues.Add(fieldValue);

            var label = UIFactory.Text($"Label_{fieldName}", fieldName, container.transform, fontSize: 14,
                anchor: TextAnchor.MiddleRight);

            var input = InputFieldFactory.CreateInputField(container, $"Input_{fieldName}", fieldValue,
                InputField.ContentType.DecimalNumber, minWidth: 60);
            input.textComponent.color = _theme.TextPrimary;
            input.image.color = _theme.BgPrimary;

            int fieldIndex = i;
            EventHelper.AddListener<string>((val) =>
            {
                if (val == originalValues[fieldIndex]) return;

                if (float.TryParse(val,
                        System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands,
                        System.Globalization.CultureInfo.InvariantCulture, out float f))
                {
                    field.SetValue(currentValue, f);
                    onValueChanged(entryKey, currentValue);
                    originalValues[fieldIndex] = val;
                    _logger.Msg($"Modified preference {entryKey}: {currentValue}");
                }
                else
                {
                    input.text = originalValues[fieldIndex]; // revert
                }
            }, input.onEndEdit);
        }
    }
}