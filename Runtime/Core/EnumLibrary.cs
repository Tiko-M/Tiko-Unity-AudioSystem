using System;
using System.Collections.Generic;


namespace Tiko.AudioSystem
{
    public class EnumLibrary<TEnum> : EnumLibraryBase where TEnum : struct, Enum
    {
        protected void Awake() => EnsureEnumType<TEnum>();
#if UNITY_EDITOR
        protected new void OnValidate()
        {
            base.OnValidate();
            EnsureEnumType<TEnum>();
        }
#endif
        public bool TryGet(TEnum key, out Entry entry) => TryGet(Convert.ToInt32(key), out entry);
        public IEnumerable<TEnum> AllKeys() => (TEnum[])Enum.GetValues(typeof(TEnum));
    }
}