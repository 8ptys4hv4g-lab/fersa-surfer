// Nur für die Kompilierprüfung: minimale Nachbildung der benutzten URP/Core-APIs.
namespace UnityEngine.Rendering
{
    public class VolumeParameter<T> { public T value; public virtual void Override(T x) { value = x; } }
    public class FloatParameter : VolumeParameter<float> { }
    public class MinFloatParameter : FloatParameter { }
    public class ClampedFloatParameter : FloatParameter { }
    public abstract class VolumeComponent : ScriptableObject { public bool active = true; }
    public sealed class VolumeProfile : ScriptableObject { public T Add<T>(bool overrides = false) where T : VolumeComponent => CreateInstance<T>(); }
    public class Volume : MonoBehaviour { public bool isGlobal; public float priority; public VolumeProfile profile; }
}
namespace UnityEngine.Rendering.Universal
{
    using UnityEngine.Rendering;
    public enum TonemappingMode { None, Neutral, ACES }
    public sealed class TonemappingModeParameter : VolumeParameter<TonemappingMode> { }
    public enum DepthOfFieldMode { Off, Gaussian, Bokeh }
    public sealed class DepthOfFieldModeParameter : VolumeParameter<DepthOfFieldMode> { }
    public sealed class Bloom : VolumeComponent { public MinFloatParameter intensity = new(), threshold = new(); public ClampedFloatParameter scatter = new(); }
    public sealed class ColorAdjustments : VolumeComponent { public ClampedFloatParameter saturation = new(), contrast = new(); }
    public sealed class Vignette : VolumeComponent { public ClampedFloatParameter intensity = new(); }
    public sealed class Tonemapping : VolumeComponent { public TonemappingModeParameter mode = new(); }
    public sealed class DepthOfField : VolumeComponent { public DepthOfFieldModeParameter mode = new(); public MinFloatParameter gaussianStart = new(), gaussianEnd = new(); public ClampedFloatParameter gaussianMaxRadius = new(); }
    public class UniversalRenderPipelineAsset : RenderPipelineAsset
    {
        public int msaaSampleCount { get; set; } public bool supportsHDR { get; set; } public float shadowDistance { get; set; } public int shadowCascadeCount { get; set; }
        public override RenderPipeline CreatePipeline() => null;
    }
    public class UniversalAdditionalCameraData : MonoBehaviour { public bool renderPostProcessing { get; set; } }
    public static class CameraExtensions { public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera c) => null; }
}
