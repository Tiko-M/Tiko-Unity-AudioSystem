// ============================================================================
// File: Runtime/Libraries/BoundEnumLibrary.cs
// Namespace: Tiko.AudioSystem
// Purpose: ScriptableObject library that binds to ANY enum chosen in the Inspector
//          (enum must live in the consuming project). Works with existing EnumLibraryBase editor.
// ============================================================================
using System;
using UnityEngine;

namespace Tiko.AudioSystem
{
    [CreateAssetMenu(menuName = "Tiko/AudioSystem/Libraries/Bound Enum Library", fileName = "BoundEnumLibrary")]
    public sealed class BoundEnumLibrary : EnumLibraryBase
    {
        [SerializeField]
        [Tooltip("Assembly-qualified name of the enum to bind (selected via the custom editor).")]
        private string selectedEnumTypeName;

        /// <summary>
        /// Editor helper to set the bound enum type.
        /// </summary>
#if UNITY_EDITOR
        public void EditorBindEnumType(Type type)
        {
            selectedEnumTypeName = type != null ? type.AssemblyQualifiedName : null;
            SortByEnumOrder();
        }
#endif
        public override Type ResolveEnumType()
        {
            // Prefer the explicitly selected enum type; fallback to base (generic subclasses)
            if (!string.IsNullOrEmpty(selectedEnumTypeName))
            {
                var t = Type.GetType(selectedEnumTypeName);
                if (t != null) return t;
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = asm.GetType(selectedEnumTypeName) ?? asm.GetType(selectedEnumTypeName.Split(',')[0]);
                    if (t != null) return t;
                }
            }
            return base.ResolveEnumType();
        }
    }
}
