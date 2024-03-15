﻿using System.Collections.Generic;
using GAS.Runtime.Component;
using UnityEngine;
using UnityEngine.UIElements;

namespace GAS.Runtime
{
    public abstract class TargetCatcherBase
    {
        public AbilitySystemComponent Owner;

        public TargetCatcherBase()
        {

        }

        public virtual void Init(AbilitySystemComponent owner)
        {
            Owner = owner;
        }

        public abstract List<AbilitySystemComponent> CatchTargets(AbilitySystemComponent mainTarget);

#if UNITY_EDITOR
        public virtual void OnEditorPreview(GameObject obj)
        {
        }
#endif
    }
}