using System.Reflection;
using MelonLoader;
using ModsApp.UI.Input.FieldFactories;
using S1API.Internal.Abstraction;
using S1API.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ModsApp.UI.Input.Handlers;

public class VectorHandler : IPreferenceInputHandler
{
    private readonly UITheme _theme;
    private readonly MelonLogger.Instance _logger;

    private GameObject _parent;
    private string _entryKey;
    private MelonPreferences_Entry _entry;
    private Action<string, object> _onValueChanged;
    private GameObject _container;

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

        _parent = parent;
        _entryKey = entryKey;
        _onValueChanged = onValueChanged;
        _entry = entry;

        var structType = currentValue.GetType();
        var fields = structType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var originalValues = new List<string>();

        _container = new GameObject($"{entryKey}_VectorContainer");
        _container.transform.SetParent(parent.transform, false);

        var gridLayout = _container.AddComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 6;
        gridLayout.spacing = new Vector2(2, 2);
        gridLayout.cellSize = new Vector2(60, 25);
        gridLayout.childAlignment = TextAnchor.MiddleLeft;

        var contentSize = _container.AddComponent<ContentSizeFitter>();
        contentSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            string fieldName = field.Name.StartsWith("m_") ? field.Name.Substring(2) : field.Name;
            string fieldValue = field.GetValue(currentValue)?.ToString() ?? "0";
            originalValues.Add(fieldValue);

            var label = UIFactory.Text($"Label_{fieldName}", fieldName, _container.transform, fontSize: _theme.SizeStandard,
                anchor: TextAnchor.MiddleRight);

            var input = InputFieldFactory.CreateInputField(_container, $"Input_{fieldName}", fieldValue,
                InputField.ContentType.DecimalNumber, minWidth: 60);

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
                    input.text = originalValues[fieldIndex];
                }
            }, input.onEndEdit);
        }
    }

    public void Recreate(object currentValue)
    {
        if (_container != null)
            UnityEngine.Object.DestroyImmediate(_container);

        CreateInput(_entry, _parent, _entryKey, currentValue, _onValueChanged);
    }
}