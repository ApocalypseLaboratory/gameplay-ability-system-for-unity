﻿using GAS.Runtime.Component;
using GAS.Runtime.Effects;
using GAS.Runtime.Effects.Modifier;

namespace GAS.Runtime.Ability
{
    public abstract class AbilitySpec
    {
        private object[] _abilityArguments;

        public AbilitySpec(AbstractAbility ability, AbilitySystemComponent owner)
        {
            Ability = ability;
            Owner = owner;
        }

        public AbstractAbility Ability { get; }

        public AbilitySystemComponent Owner { get; protected set; }

        public float Level { get; }

        public bool IsActive { get; private set; }

        public int ActiveCount { get; private set; }

        public virtual bool CanActivate()
        {
            return !IsActive
                   && CheckGameplayTagsValidTpActivate()
                   && CheckCost()
                   && CheckCooldown().TimeRemaining <= 0;
        }

        private bool CheckGameplayTagsValidTpActivate()
        {
            var hasAllTags = Owner.HasAllTags(Ability.Tag.ActivationRequiredTags);
            var notHasAnyTags = !Owner.HasAnyTags(Ability.Tag.ActivationBlockedTags);
            var notBlockedByOtherAbility = true;

            foreach (var kv in Owner.AbilityContainer.AbilitySpecs())
            {
                var abilitySpec = kv.Value;
                if (abilitySpec.IsActive)
                    if (Ability.Tag.AssetTag.HasAnyTags(abilitySpec.Ability.Tag.BlockAbilitiesWithTags))
                    {
                        notBlockedByOtherAbility = false;
                        break;
                    }
            }

            return hasAllTags && notHasAnyTags && notBlockedByOtherAbility;
        }

        protected virtual bool CheckCost()
        {
            if (Ability.Cost.NULL) return true;
            var costSpec = Ability.Cost.CreateSpec(Owner, Owner, Level);
            if (costSpec == null) return false;

            if (Ability.Cost.DurationPolicy != EffectsDurationPolicy.Instant) return true;

            foreach (var modifier in Ability.Cost.Modifiers)
            {
                if (modifier.Operation != GEOperation.Add) continue;

                var costValue = modifier.MMC.CalculateMagnitude(costSpec, modifier.ModiferMagnitude);
                var attributeCurrentValue =
                    Owner.GetAttributeCurrentValue(modifier.AttributeSetName, modifier.AttributeShortName);

                if (attributeCurrentValue + costValue < 0) return false;
            }

            return true;
        }

        protected virtual CooldownTimer CheckCooldown()
        {
            return Ability.Cooldown.NULL
                ? new CooldownTimer { TimeRemaining = 0, Duration = Ability.Cooldown.Duration }
                : Owner.CheckCooldownFromTags(Ability.Cooldown.TagContainer.GrantedTags);
        }

        /// <summary>
        /// Some skills include wind-up and follow-through, where the wind-up may be interrupted, causing the skill not to be successfully released.
        /// Therefore, the actual timing and logic of skill release (triggering costs and initiating cooldown) should be determined by developers within the AbilitySpec,
        /// rather than being systematically standardized.
        /// </summary>
        public void DoCost()
        {
            if (!Ability.Cost.NULL) Owner.ApplyGameplayEffectToSelf(Ability.Cost);

            if (!Ability.Cooldown.NULL) Owner.ApplyGameplayEffectToSelf(Ability.Cooldown);
        }

        public virtual bool TryActivateAbility(params object[] args)
        {
            if (!CanActivate()) return false;

            _abilityArguments = args;
            IsActive = true;
            ActiveCount++;

            Owner.GameplayTagAggregator.ApplyGameplayAbilityDynamicTag(this);
            ActivateAbility(_abilityArguments);
            return true;
        }

        public virtual void TryEndAbility()
        {
            if (!IsActive) return;
            IsActive = false;

            Owner.GameplayTagAggregator.RestoreGameplayAbilityDynamicTags(this);
            EndAbility();
        }

        public virtual void TryCancelAbility()
        {
            if (!IsActive) return;
            IsActive = false;
            CancelAbility();
        }

        public void Tick()
        {
            // TODO
            if (!IsActive) return;
            AbilityTick();
        }

        protected virtual void AbilityTick()
        {
        }

        public abstract void ActivateAbility(params object[] args);

        public abstract void CancelAbility();

        public abstract void EndAbility();
    }
}