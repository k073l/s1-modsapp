using System;
using System.Collections;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace ModsApp.Helpers;

public static class ReflectionHelper
{
    // tmp reflection data
    internal static bool TMPAvailable;
    internal static Type TMPInputFieldType;
    internal static Type TMPTextType;
    internal static Type TMPTextBaseType;

    // TMP_InputField members
    internal static MemberInfo MIfText;
    internal static MemberInfo MIfTextComponent;
    internal static MemberInfo MIfPlaceholder;
    internal static MemberInfo MIfTextViewport;
    internal static MemberInfo MIfLineType;
    internal static MemberInfo MIfContentType;
    internal static MemberInfo MIfCaretColor;
    internal static MemberInfo MIfCustomCaret;
    internal static MemberInfo MIfCaretBlinkRate;
    internal static MemberInfo MIfOnValueChanged;
    internal static MemberInfo MIfOnEndEdit;
    internal static MemberInfo MIfRichText;
    internal static MemberInfo MIfStringPosition;
    internal static MemberInfo MIfOnFocusSelectAll;
    internal static MethodInfo MiSetTextWithoutNotify;
    internal static MethodInfo MiActivateInputField;
    internal static MethodInfo MiCreateFontAsset;

    // TMP_Text members
    internal static MemberInfo MTText;
    internal static MemberInfo MTColor;
    internal static MemberInfo MTFontSize;
    internal static MemberInfo MTRichText;
    internal static MemberInfo MTWordWrap;
    internal static MemberInfo MTOverflow;
    internal static MemberInfo MTAutoSize;
    internal static MemberInfo MTAlignment;
    
    // S1 reflection data
    internal static bool S1TypesAvailable;
    internal static Type S1SettingsType;
    internal static Type S1DisplaySettingsType;
    internal static Type S1PhoneType;
    
    // S1 Settings members
    internal static MemberInfo MSettingsInstance;
    internal static MemberInfo MSettingsDisplaySettings;
    internal static MemberInfo MDisplaySettingsUIScale;

    // S1 Phone Instance
    internal static MemberInfo MPhoneInstance;


    public static void TryInitTMP()
    {
        try
        {
            var asm = TryLoadAssembly("Il2CppTMPro")
                      ?? TryLoadAssembly("Unity.TextMeshPro");

            if (asm == null)
            {
                Melon<ModsApp>.Logger.Warning("[JsonConfigUI] TMP assembly not found");
                return;
            }

            var ns = MelonUtils.IsGameIl2Cpp() ? "Il2CppTMPro" : "TMPro";

            TMPInputFieldType ??= asm.GetType($"{ns}.TMP_InputField");
            TMPTextType ??= asm.GetType($"{ns}.TextMeshProUGUI");
            TMPTextBaseType ??= asm.GetType($"{ns}.TMP_Text") ?? TMPTextType;
            var tmpFontAssetType = asm.GetType($"{ns}.TMP_FontAsset");

            if (TMPInputFieldType == null || TMPTextType == null) return;

            MIfText ??= GetMember(TMPInputFieldType, "text");
            MIfTextComponent ??= GetMember(TMPInputFieldType, "textComponent");
            MIfPlaceholder ??= GetMember(TMPInputFieldType, "placeholder");
            MIfTextViewport ??= GetMember(TMPInputFieldType, "textViewport");
            MIfLineType ??= GetMember(TMPInputFieldType, "lineType");
            MIfContentType ??= GetMember(TMPInputFieldType, "contentType");
            MIfCaretColor ??= GetMember(TMPInputFieldType, "caretColor");
            MIfCustomCaret ??= GetMember(TMPInputFieldType, "customCaretColor");
            MIfCaretBlinkRate ??= GetMember(TMPInputFieldType, "caretBlinkRate");
            MIfOnValueChanged ??= GetMember(TMPInputFieldType, "onValueChanged");
            MIfOnEndEdit ??= GetMember(TMPInputFieldType, "onEndEdit");
            MIfRichText ??= GetMember(TMPInputFieldType, "richText");
            MIfStringPosition ??= GetMember(TMPInputFieldType, "stringPosition");
            MIfOnFocusSelectAll ??= GetMember(TMPInputFieldType, "onFocusSelectAll");

            MiSetTextWithoutNotify ??= TMPInputFieldType.GetMethod("SetTextWithoutNotify",
                BindingFlags.Public | BindingFlags.Instance);
            MiActivateInputField ??= TMPInputFieldType.GetMethod("ActivateInputField",
                BindingFlags.Public | BindingFlags.Instance);
            MiCreateFontAsset ??= tmpFontAssetType.GetMethod("CreateFontAsset",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Font) },
                null);

            MTText ??= GetMember(TMPTextBaseType, "text");
            MTColor ??= GetMember(TMPTextBaseType, "color");
            MTFontSize ??= GetMember(TMPTextBaseType, "fontSize");
            MTRichText ??= GetMember(TMPTextBaseType, "richText");
            MTWordWrap ??= GetMember(TMPTextBaseType, "enableWordWrapping");
            MTOverflow ??= GetMember(TMPTextBaseType, "overflowMode");
            MTAutoSize ??= GetMember(TMPTextBaseType, "enableAutoSizing");
            MTAlignment ??= GetMember(TMPTextBaseType, "alignment");

            TMPAvailable = MIfText != null
                           && MIfTextComponent != null
                           && MTText != null
                           && ModsApp.UseNewJsonEditor.Value;
        }
        catch (Exception ex)
        {
            Melon<ModsApp>.Logger.Warning($"[JsonConfigUI] TMP init threw: {ex.Message}");
        }
    }

    public static void TryInitGameTypes()
    {
        try
        {
            var asm = TryLoadAssembly("Assembly-CSharp");
            
            if (asm == null)
            {
                Melon<ModsApp>.Logger.Warning("[Maximize] ScheduleOne assembly not found (how?)");
                return;
            }
            
            var ns = MelonUtils.IsGameIl2Cpp() ? "Il2CppScheduleOne" : "ScheduleOne";
            
            S1SettingsType ??= asm.GetType($"{ns}.DevUtilities.Settings");
            S1DisplaySettingsType ??= asm.GetType($"{ns}.DevUtilities.DisplaySettings");
            S1PhoneType ??= asm.GetType($"{ns}.UI.Phone.Phone");

            if (S1SettingsType == null || S1DisplaySettingsType == null || S1PhoneType == null) return;

            MSettingsInstance ??= GetMember(S1SettingsType, "Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            MSettingsDisplaySettings ??= GetMember(S1SettingsType, "DisplaySettings", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            MDisplaySettingsUIScale ??= GetMember(S1DisplaySettingsType, "UIScale", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            MPhoneInstance ??= GetMember(S1PhoneType, "Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            
            S1TypesAvailable = MSettingsInstance != null
                            && MSettingsDisplaySettings != null
                            && MDisplaySettingsUIScale != null
                            && MPhoneInstance != null;
        }
        catch (Exception ex)
        {
            Melon<ModsApp>.Logger.Warning($"[Maximize] GameTypes init threw: {ex.Message}");
        }
    }

    public static MemberInfo GetMember(Type type, string name,
        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
    {
        if (type == null || string.IsNullOrEmpty(name)) return null;

        MemberInfo result = type.GetProperty(name, flags);
        if (result != null) return result;

        result = type.GetField(name, flags);
        return result; // may still be null
    }

    // gets property or field
    public static object GetValue(MemberInfo member, object instance)
    {
        if (member == null) return null;
        try
        {
            return member switch
            {
                PropertyInfo pi => pi.GetValue(instance),
                FieldInfo fi => fi.GetValue(instance),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }


    public static bool SetValue(MemberInfo member, object instance, object value)
    {
        if (member == null || instance == null) return false;
        try
        {
            switch (member)
            {
                case PropertyInfo pi: pi.SetValue(instance, value); break;
                case FieldInfo fi: fi.SetValue(instance, value); break;
                default: return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Melon<ModsApp>.Logger.Warning($"[ReflectionHelper] SetValue {member.Name} failed: {ex.Message}");
            return false;
        }
    }

    public static object EnumValue(MemberInfo member, int intValue)
    {
        if (member == null) return null;
        var enumType = member switch
        {
            PropertyInfo pi => pi.PropertyType,
            FieldInfo fi => fi.FieldType,
            _ => null
        };
        return enumType == null ? null : Enum.ToObject(enumType, intValue);
    }

    public static bool AddStringListener(object unityEvent, Action<string> callback)
    {
        if (unityEvent == null || callback == null) return false;
        try
        {
            var addListener = unityEvent.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "AddListener" && m.GetParameters().Length == 1);

            if (addListener == null) return false;

            var delegateType = addListener.GetParameters()[0].ParameterType;

            object del;
            if (MelonUtils.IsGameIl2Cpp())
            {
                var interopAsm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Il2CppInterop.Runtime");
                var delegateSupport = interopAsm?.GetType("Il2CppInterop.Runtime.DelegateSupport");
                var convertMethod = delegateSupport?.GetMethod("ConvertDelegate",
                    BindingFlags.Public | BindingFlags.Static);
                var closedMethod = convertMethod?.MakeGenericMethod(delegateType);
                del = closedMethod?.Invoke(null, new object[] { callback });
            }
            else
            {
                del = Delegate.CreateDelegate(delegateType, callback.Target, callback.Method);
            }

            if (del == null)
            {
                Melon<ModsApp>.Logger.Error("[AddStringListener] Delegate is null");
                return false;
            }

            addListener.Invoke(unityEvent, new object[] { del });
            return true;
        }
        catch (Exception ex)
        {
            Melon<ModsApp>.Logger.Error($"[AddStringListener] Crashed: {ex}");
            return false;
        }
    }


    public static Assembly TryLoadAssembly(string name)
    {
        try
        {
            return Assembly.Load(name);
        }
        catch
        {
            return null;
        }
    }

    public static object AddComponent(GameObject go, Type type)
    {
        var methods = typeof(GameObject)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance);

        MethodInfo generic = null;

        foreach (var m in methods)
        {
            if (m.Name == "AddComponent" && m.IsGenericMethodDefinition)
            {
                generic = m;
                break;
            }
        }

        if (generic == null)
            throw new Exception("AddComponent<T> not found.");

        var concrete = generic.MakeGenericMethod(type);
        return concrete.Invoke(go, null);
    }

    public static IEnumerable FindObjectsOfType(Type type)
    {
        var methods = typeof(Resources)
            .GetMethods(BindingFlags.Public | BindingFlags.Static);

        MethodInfo generic = null;

        foreach (var m in methods)
        {
            if (m.Name == "FindObjectsOfTypeAll" && m.IsGenericMethodDefinition)
            {
                generic = m;
                break;
            }
        }

        if (generic == null)
            throw new Exception("FindObjectsOfTypeAll<T> not found.");

        var concrete = generic.MakeGenericMethod(type);
        return concrete.Invoke(null, null) as IEnumerable;
    }
}