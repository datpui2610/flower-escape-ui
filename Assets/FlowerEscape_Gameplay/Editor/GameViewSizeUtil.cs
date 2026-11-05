using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlowerEscape.EditorTools
{
    /// <summary>
    /// Thêm 2 độ phân giải test vào Game view: 9:21 (1080x2520) và 3:4 (1536x2048).
    /// Unity không có API công khai cho việc này nên dùng reflection; nếu lỗi chỉ cảnh báo, không ảnh hưởng scene.
    /// </summary>
    internal static class GameViewSizeUtil
    {
        public static void EnsureFlowerEscapeSizes()
        {
            try
            {
                Add(1080, 2520, "Flower Escape 9:21");
                Add(1536, 2048, "Flower Escape 3:4");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FlowerEscape] Không tự thêm được độ phân giải Game view, hãy thêm tay bằng nút '+': " + e.Message);
            }
        }

        private static void Add(int width, int height, string label)
        {
            Assembly editorAssembly = typeof(Editor).Assembly;
            Type sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            Type singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object instance = singletonType.GetProperty("instance").GetValue(null, null);

            object groupType = sizesType.GetProperty("currentGroupType").GetValue(instance, null);
            object group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { groupType });

            var texts = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
            foreach (string text in texts)
            {
                if (text.StartsWith(label, StringComparison.Ordinal)) return; // đã có
            }

            Type sizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            Type sizeKind = editorAssembly.GetType("UnityEditor.GameViewSizeType");
            ConstructorInfo ctor = sizeType.GetConstructor(new[] { sizeKind, typeof(int), typeof(int), typeof(string) });
            object size = ctor.Invoke(new object[] { Enum.Parse(sizeKind, "FixedResolution"), width, height, label });

            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            MethodInfo save = sizesType.GetMethod("SaveToHDD");
            if (save != null) save.Invoke(instance, null);
        }
    }
}
