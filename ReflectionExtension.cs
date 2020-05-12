using System;
using Reflection = System.Reflection;

namespace ReflectionExtension {
    internal static class ReflectionExtension {
        private const Reflection.BindingFlags allMembers = Reflection.BindingFlags.Public
                                                           | Reflection.BindingFlags.NonPublic
                                                           | Reflection.BindingFlags.Instance
                                                           | Reflection.BindingFlags.Static;

        public static Type FindNestedType(this Type type, string name) {
            return type.GetNestedType(name, allMembers);
        }

        public static Reflection.FieldInfo FindField(this Type type, string name) {
            return type.GetField(name, allMembers);
        }

        public static Reflection.FieldInfo[] GetAllFields(this Type type) {
            return type.GetFields(allMembers);
        }

        public static object New(this Type type, params object[] parameters) {
            return type.GetConstructors()[0].Invoke(parameters);
        }

        public static T GetFieldValueOrDefault<T>(this object obj, string name) {
            var field = obj.GetType().GetField(name, allMembers);
            if (field != null && typeof(T).IsAssignableFrom(field.FieldType)) {
                return (T)field.GetValue(obj);
            }
            return default;
        }
        public static T GetFieldValue<T>(this object obj, string name) {
            return (T)obj.GetType().GetField(name, allMembers).GetValue(obj);
        }
        public static void SetFieldValue(this object obj, string name, object value) {
            var field = obj.GetType().GetField(name, allMembers);
            field.SetValue(obj, value);
        }

        public static object InvokeMethod(this object obj, string name, params object[] parameters) {
            return obj.GetType().GetMethod(name, allMembers).Invoke(obj, parameters);
        }
    }
}
