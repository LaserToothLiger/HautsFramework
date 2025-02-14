using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using VFEPirates;
using HautsFramework;

namespace HF_VFE_Pirates
{
    [StaticConstructorOnStartup]
    public class HF_VFE_Pirates
	{
		private static readonly Type patchType = typeof(HF_VFE_Pirates);
		static HF_VFE_Pirates()
		{
			Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsfconncompatibility.main");
			harmony.Patch(AccessTools.Property(typeof(Ability_PowerJump), nameof(Ability_PowerJump.Range)).GetGetMethod(),
						   postfix: new HarmonyMethod(patchType, nameof(Hauts_VFEP_PowerJumpRangePostfix)));
		}
		public static void Hauts_VFEP_PowerJumpRangePostfix(ref float __result, Ability_PowerJump __instance)
		{
			__result *= __instance.pawn.GetStatValue(HautsDefOf.Hauts_JumpRangeFactor);
		}
	}
}
