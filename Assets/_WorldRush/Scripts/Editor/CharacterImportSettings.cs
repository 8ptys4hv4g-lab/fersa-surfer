using UnityEditor;

namespace WorldRush.EditorTools
{
    /// <summary>
    /// Figuren in Resources/Characters werden automatisch so importiert, dass das Spiel
    /// ihre Animationen ohne extra Animator-Setup abspielen kann (Legacy-Animationen, Laufen/Idle in Schleife).
    /// </summary>
    public sealed class CharacterImportSettings : AssetPostprocessor
    {
        private bool IsCharacter => assetPath.Replace('\\', '/').Contains("/Resources/Characters/");

        private void OnPreprocessModel()
        {
            if (!IsCharacter) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }

        private void OnPreprocessAnimation()
        {
            if (!IsCharacter) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                string n = clip.name.ToLowerInvariant();
                bool loop = n.Contains("run") || n.Contains("idle") || n.Contains("walk") || n.Contains("sprint");
                clip.loopTime = loop;
                clip.wrapMode = loop ? UnityEngine.WrapMode.Loop : UnityEngine.WrapMode.ClampForever;
            }
            importer.clipAnimations = clips;
        }
    }
}
