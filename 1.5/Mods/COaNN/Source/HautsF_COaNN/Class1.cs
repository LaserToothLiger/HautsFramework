using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using HarmonyLib;
using CONN;
using HautsFramework;

namespace HautsF_COaNN
{
	[StaticConstructorOnStartup]
	public static class HautsF_COaNN
	{
		private static readonly Type patchType = typeof(HautsF_COaNN);
		static HautsF_COaNN()
		{
			Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsfconncompatibility.main");
			harmony.Patch(AccessTools.Method(typeof(CompUseEffect_AddRandomTrait), nameof(CompUseEffect_AddRandomTrait.DoEffect)),
							prefix: new HarmonyMethod(patchType, nameof(HautsAddRandomTrait_DoEffectPrefix)));
			harmony.Patch(AccessTools.Method(typeof(CompUseEffect_TraitReset), nameof(CompUseEffect_TraitReset.DoEffect)),
							prefix: new HarmonyMethod(patchType, nameof(HautsTraitReset_DoEffectPrefix)));
		}
		public static bool HautsAddRandomTrait_DoEffectPrefix(CompUseEffect_AddRandomTrait __instance, Pawn user)
		{
			if (user != null && user.story != null)
			{
				List<TraitDef> blacklistedTraits = new List<TraitDef>();
				List<TraitDef> list = new List<TraitDef>();
				foreach (Trait t in user.story.traits.allTraits)
				{
					blacklistedTraits.Add(t.def);
				}
				if (user.story.Childhood != null && user.story.Childhood.disallowedTraits != null)
				{
					foreach (BackstoryTrait bt in user.story.Childhood.disallowedTraits)
					{
						blacklistedTraits.Add(bt.def);
					}
				}
				if (user.story.Adulthood != null && user.story.Adulthood.disallowedTraits != null)
				{
					foreach (BackstoryTrait bt in user.story.Adulthood.disallowedTraits)
					{
						blacklistedTraits.Add(bt.def);
					}
				}
				List<TraitDef> allDefsListForReading = DefDatabase<TraitDef>.AllDefsListForReading;
				for (int i = 0; i < allDefsListForReading.Count; i++)
				{
					TraitDef iTrait = allDefsListForReading[i];
					if (!blacklistedTraits.Contains(iTrait) && iTrait.GetGenderSpecificCommonality(user.gender) > 0f && !user.story.traits.allTraits.Any((Trait t) => t.def.conflictingTraits.Contains(iTrait)) && !HautsUtility.IsExciseTraitExempt(iTrait) && !HautsCOaNNUtility.AddRandomTraitExempt(iTrait))
					{
						list.Add(iTrait);
					}
				}
				TraitDef traitDef = list.RandomElement<TraitDef>();
				user.story.traits.GainTrait(new Trait(traitDef, PawnGenerator.RandomTraitDegree(traitDef), false), false);
			}
			CompUseEffect_AddRandomTrait.RefreshPawnStat(user);
			__instance.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
			return false;
		}
		public static bool HautsTraitReset_DoEffectPrefix(CompUseEffect_TraitReset __instance, Pawn user)
		{
			if (user != null && user.story != null)
			{
				List<Trait> traitsToRemove = new List<Trait>();
				List<TraitDef> backstoryTraits = new List<TraitDef>();
				List<TraitDef> bonusEffectPrompts = new List<TraitDef>();
				if (user.story.Childhood != null && user.story.Childhood.forcedTraits != null)
				{
					foreach (BackstoryTrait bt in user.story.Childhood.forcedTraits)
					{
						backstoryTraits.Add(bt.def);
					}
				}
				if (user.story.Adulthood != null && user.story.Adulthood.forcedTraits != null)
				{
					foreach (BackstoryTrait bt in user.story.Adulthood.forcedTraits)
					{
						backstoryTraits.Add(bt.def);
					}
				}
				foreach (Trait t in user.story.traits.allTraits)
				{
					if (!backstoryTraits.Contains(t.def) && t.sourceGene == null && !HautsUtility.IsExciseTraitExempt(t.def,false) && !HautsCOaNNUtility.TraitResetExempt(t.def))
					{
						traitsToRemove.Add(t);
						if (HautsUtility.COaNN_TraitReset_ShouldDoBonusEffect(t.def))
                        {
							bonusEffectPrompts.Add(t.def);
                        }
					}
				}
				foreach (Trait t in traitsToRemove)
				{
					user.story.traits.RemoveTrait(t);
				}
				HautsUtility.COaNN_TraitReset_BonusEffects(user, bonusEffectPrompts);
			}
			CompUseEffect_AddRandomTrait.RefreshPawnStat(user);
			__instance.parent.SplitOff(1).Destroy(DestroyMode.Vanish);
			return false;
		}
	}
	public static class HautsCOaNNUtility
    {
		public static bool AddRandomTraitExempt (TraitDef t)
        {
			return false;
        }
		public static bool TraitResetExempt (TraitDef t)
        {
			return false;
        }
    }
}
