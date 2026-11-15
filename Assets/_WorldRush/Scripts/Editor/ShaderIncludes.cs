using UnityEditor;
using UnityEngine;

namespace WorldRush.EditorTools
{
    /// <summary>
    /// Shader, die das Spiel erst zur Laufzeit per Shader.Find lädt (z. B. den Himmel), fehlen sonst im fertigen Build.
    /// Trägt sie in "Project Settings → Graphics → Always Included Shaders" ein.
    /// </summary>
    public static class ShaderIncludes
    {
        public static readonly string[] Needed = { "Skybox/Panoramic" };

        public static void EnsureAll()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (assets == null || assets.Length == 0) { Debug.LogWarning("[Fesara] GraphicsSettings nicht gefunden – Himmel-Shader bitte von Hand eintragen."); return; }
            var so = new SerializedObject(assets[0]);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            if (list == null) return;
            bool changed = false;
            foreach (var name in Needed)
            {
                var shader = Shader.Find(name);
                if (shader == null) { Debug.LogWarning("[Fesara] Shader nicht gefunden: " + name); continue; }
                bool present = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) { present = true; break; }
                if (present) continue;
                int at = list.arraySize;
                list.InsertArrayElementAtIndex(at);
                list.GetArrayElementAtIndex(at).objectReferenceValue = shader;
                changed = true;
            }
            if (changed)
            {
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                Debug.Log("[Fesara] Himmel-Shader für Builds eingetragen.");
            }
        }
    }
}
