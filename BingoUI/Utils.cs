using System;
using System.Reflection;

namespace Celeste.Mod.BingoUI
{
    public static class BingoUtils {
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            if (instance == null)
                return null;
            return field.GetValue(instance);
        }

        internal static object GetInstanceProp(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Static;
            PropertyInfo prop = type.GetProperty(fieldName, bindFlags);
            MethodInfo getter = prop.GetGetMethod(nonPublic: true);
            if (getter == null || prop == null || instance == null)
                return null;

            return getter.Invoke(instance,null);
        }

        internal static MethodInfo GetInstanceMethod(Type type, string methodName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Static;
            MethodInfo method = type.GetMethod(methodName, bindFlags);
            return method;
        }

    }
}
