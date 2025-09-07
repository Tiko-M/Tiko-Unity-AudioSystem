#if UNITY_EDITOR
using System;


namespace Tiko.AudioSystem.Editor
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AudioEnumPreviewAttribute : Attribute { }
}
#endif