using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace ModsApp.Helpers;

public static class ReflectionHelper
{
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
        if (member == null || instance == null) return null;
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
            MelonLoader.MelonLogger.Warning($"[ReflectionHelper] SetValue {member.Name} failed: {ex.Message}");
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
                .GetMethod("AddListener", BindingFlags.Public | BindingFlags.Instance);
            if (addListener == null) return false;

            var paramType = addListener.GetParameters()[0].ParameterType;
            var del = Delegate.CreateDelegate(paramType, callback.Target,
                callback.Method, throwOnBindFailure: false);

            if (del == null) return false;

            addListener.Invoke(unityEvent, new object[] { del });
            return true;
        }
        catch
        {
            return false;
        }
    }


    public static System.Reflection.Assembly TryLoadAssembly(string name)
    {
        try
        {
            return System.Reflection.Assembly.Load(name);
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