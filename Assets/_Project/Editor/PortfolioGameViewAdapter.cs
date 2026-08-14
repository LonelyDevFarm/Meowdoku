using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Meowdoku.Editor
{
    public static class PortfolioGameViewAdapter
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static bool SetResolution(int width, int height)
        {
            if (width <= 0 || height <= 0)
                return false;

            try
            {
                Assembly editorAssembly = typeof(EditorWindow).Assembly;
                Type gameViewType = editorAssembly.GetType("UnityEditor.GameView");
                Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
                Type groupType = editorAssembly.GetType(
                    "UnityEditor.GameViewSizeGroupType");
                Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
                Type sizeKindType = editorAssembly.GetType(
                    "UnityEditor.GameViewSizeType");
                if (gameViewType == null || sizesType == null ||
                    groupType == null || sizeType == null ||
                    sizeKindType == null)
                    return false;

                UnityEngine.Object[] gameViews =
                    Resources.FindObjectsOfTypeAll(gameViewType);
                EditorWindow gameView = gameViews != null &&
                    gameViews.Length > 0
                        ? gameViews[0] as EditorWindow
                        : EditorWindow.GetWindow(
                            gameViewType,
                            false,
                            "Game",
                            false);
                if (gameView == null) return false;

                Type singletonType = editorAssembly
                    .GetType("UnityEditor.ScriptableSingleton`1")
                    ?.MakeGenericType(sizesType);
                PropertyInfo instanceProperty = singletonType?.GetProperty(
                    "instance",
                    BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic);
                object sizes = instanceProperty?.GetValue(null, null);
                if (sizes == null)
                    return false;

                MethodInfo getGroup = sizesType.GetMethod(
                    "GetGroup", InstanceFlags, null, new[] { groupType }, null);
                if (getGroup == null)
                    return false;

                object standalone = Enum.Parse(groupType, "Standalone");
                object group = getGroup.Invoke(sizes, new[] { standalone });
                if (group == null)
                    return false;

                int selectedIndex = FindSizeIndex(group, sizeType, width, height);
                if (selectedIndex < 0)
                {
                    object fixedResolution = Enum.Parse(
                        sizeKindType, "FixedResolution");
                    ConstructorInfo constructor = sizeType.GetConstructor(
                        InstanceFlags,
                        null,
                        new[]
                        {
                            sizeKindType,
                            typeof(int),
                            typeof(int),
                            typeof(string)
                        },
                        null);
                    MethodInfo addCustomSize = group.GetType().GetMethod(
                        "AddCustomSize", InstanceFlags, null,
                        new[] { sizeType }, null);
                    if (constructor == null || addCustomSize == null)
                        return false;

                    string label = $"Meowdoku {width}x{height}";
                    object newSize = constructor.Invoke(new[]
                    {
                        fixedResolution,
                        (object)width,
                        height,
                        label
                    });
                    addCustomSize.Invoke(group, new[] { newSize });
                    selectedIndex = FindSizeIndex(
                        group, sizeType, width, height);
                    if (selectedIndex < 0)
                        return false;
                }

                PropertyInfo selectedSizeIndex = gameViewType.GetProperty(
                    "selectedSizeIndex", InstanceFlags);
                if (selectedSizeIndex == null || !selectedSizeIndex.CanWrite)
                    return false;

                selectedSizeIndex.SetValue(
                    gameView, selectedIndex, null);
                gameView.Repaint();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static int FindSizeIndex(
            object group,
            Type sizeType,
            int width,
            int height)
        {
            Type actualGroupType = group.GetType();
            MethodInfo getTotalCount = actualGroupType.GetMethod(
                "GetTotalCount", InstanceFlags, null, Type.EmptyTypes, null);
            MethodInfo getGameViewSize = actualGroupType.GetMethod(
                "GetGameViewSize", InstanceFlags, null,
                new[] { typeof(int) }, null);
            if (getTotalCount == null || getGameViewSize == null)
                return -1;

            int count = Convert.ToInt32(getTotalCount.Invoke(group, null));
            for (int index = 0; index < count; index++)
            {
                object size = getGameViewSize.Invoke(group, new object[] { index });
                if (size != null &&
                    ReadDimension(size, sizeType, "width", "m_Width") == width &&
                    ReadDimension(size, sizeType, "height", "m_Height") == height)
                    return index;
            }

            return -1;
        }

        private static int ReadDimension(
            object size,
            Type sizeType,
            string propertyName,
            string fieldName)
        {
            PropertyInfo property = sizeType.GetProperty(
                propertyName, InstanceFlags);
            if (property != null)
                return Convert.ToInt32(property.GetValue(size, null));

            FieldInfo field = sizeType.GetField(fieldName, InstanceFlags);
            return field != null
                ? Convert.ToInt32(field.GetValue(size))
                : -1;
        }
    }
}
