using AthenaFramework;
using HarmonyLib;
using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HautsFramework_Athena
{
    [StaticConstructorOnStartup]
    public static class HautsFramework_Athena
    {
        private static readonly Type patchType = typeof(HautsFramework_Athena);
        static HautsFramework_Athena()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.athenaframework");
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.AthenaAbilityCooldownPatch)),
                          postfix: new HarmonyMethod(patchType, nameof(Hauts_AthenaAbilityCooldownPostfix)));
        }
        public static void Hauts_AthenaAbilityCooldownPostfix(ref bool __result, Ability ability, HediffCompProperties hcp)
        {
            if (hcp is HediffCompProperties_GiveSingularAbility gsa && gsa.abilityDefs.Contains(ability.def))
            {
                __result = true;
            }
        }
    }
}
