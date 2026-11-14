using System.Collections.Generic;
using UnityEngine;

namespace WorldRush.Runtime
{
    /// <summary>
    /// Lädt echte 3D-Stadtmodelle aus Resources/Environment/{Buildings,Props,Cars}
    /// und setzt sie passend skaliert in die Szene. Fehlen sie, bleibt die einfache Neon-Stadt.
    /// </summary>
    public sealed class EnvironmentKit
    {
        public readonly List<GameObject> Buildings = Load("Environment/Buildings");
        public readonly List<GameObject> Props = Load("Environment/Props");
        public readonly List<GameObject> Cars = Load("Environment/Cars");

        public bool HasAny => Buildings.Count + Props.Count + Cars.Count > 0;

        private static List<GameObject> Load(string path)
        {
            var list = new List<GameObject>();
            foreach (var go in Resources.LoadAll<GameObject>(path))
                if (go != null && go.GetComponentInChildren<Renderer>(true) != null) list.Add(go);
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list;
        }

        /// <summary>
        /// Erzeugt ein Modell, steht auf dem Boden bei <paramref name="position"/>.
        /// size &gt; 0: auf diese Grundfläche (byHeight=false) bzw. Höhe (byHeight=true) skalieren.
        /// maxHeight begrenzt zu große Modelle.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, float yRotation, float size, bool byHeight, float maxHeight)
        {
            var go = Object.Instantiate(prefab, parent);
            go.name = prefab.name;
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.updateWhenOffscreen = true;

            if (CharacterView.TryMeshBounds(go, out var b))
            {
                float current = byHeight ? b.size.y : Mathf.Max(b.size.x, b.size.z);
                float factor = size > 0f && current > 0.001f ? size / current : 1f;
                if (maxHeight > 0f && b.size.y * factor > maxHeight) factor = maxHeight / Mathf.Max(0.001f, b.size.y);
                go.transform.localScale *= factor;
                CharacterView.TryMeshBounds(go, out b);
                go.transform.position = position + new Vector3(-b.center.x, -b.min.y, -b.center.z);
            }
            else
            {
                go.transform.position = position;
            }
            return go;
        }

        public static bool NameHas(GameObject go, params string[] keys)
        {
            string n = go.name.ToLowerInvariant();
            foreach (var k in keys) if (n.Contains(k)) return true;
            return false;
        }
    }
}
