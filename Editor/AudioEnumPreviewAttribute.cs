#if UNITY_EDITOR
using System;


namespace Tiko.AudioSystem.EditorTools
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AudioEnumPreviewAttribute : Attribute { }
}
#endif