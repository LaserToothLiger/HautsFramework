using HarmonyLib;
using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VanillaPsycastsExpanded;
using Verse;

namespace HautsFrameworkVPE
{
    [StaticConstructorOnStartup]
    public class HautsFrameworkVPE
    {
        private static readonly Type patchType = typeof(HautsFrameworkVPE);
        static HautsFrameworkVPE()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframeworkvpe.main");
            harmony.Patch(AccessTools.Method(typeof(AbilityExtension_Psycast), nameof(AbilityExtension_Psycast.GetPsyfocusUsedByPawn)),
                            postfix: new HarmonyMethod(patchType, nameof(HautsVPEGetPsyfocusUsedByPawnPostfix)));
            harmony.Patch(AccessTools.Method(typeof(AbilityExtension_Psycast), nameof(AbilityExtension_Psycast.Cast), new[] { typeof(GlobalTargetInfo[]), typeof(VFECore.Abilities.Ability) }),
                            postfix: new HarmonyMethod(patchType, nameof(HautsVPECastPostfix)));
            harmony.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.Ingested)),
                            postfix: new HarmonyMethod(patchType, nameof(HautsVPEIngestedPostfix)));
            harmony.Patch(AccessTools.Method(typeof(MeditationUtility), nameof(MeditationUtility.CheckMeditationScheduleTeachOpportunity)),
                            postfix: new HarmonyMethod(patchType, nameof(HautsVPECheckMeditationScheduleTeachOpportunityPostfix)));
            harmony.Patch(AccessTools.Method(typeof(RecordsUtility), nameof(RecordsUtility.Notify_PawnKilled)),
                            postfix: new HarmonyMethod(patchType, nameof(HautsVPENotify_PawnKilledPostfix)));
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.IsVPEPsycast)),
                            postfix: new HarmonyMethod(patchType, nameof(HautsIsVPEPSycastPostfix)));
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.GetVPEPsycastLevel)),
                            postfix: new HarmonyMethod(patchType, nameof(HautsGetPsycastLevel_Postfix)));
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.GetVPEEntropyCost)),
                            postfix: new HarmonyMethod(patchType, nameof(HautsGetEntropyCost_Postfix)));
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.GetVPEPsyfocusCost)),
                            postfix: new HarmonyMethod(patchType, nameof(HautsGetPsyfocusCost_Postfix)));
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.VPEUnlockAbility)),
                            postfix: new HarmonyMethod(patchType, nameof(HautsVPEUnlockAbilityPostfix)));
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.VPESetSkillPointsAndExperienceTo)),
                            postfix: new HarmonyMethod(patchType, nameof(HautsVPESetSkillPointsAndExperiencePostfix)));
        }
        public static void HautsVPEGetPsyfocusUsedByPawnPostfix(ref float __result, AbilityExtension_Psycast __instance, Pawn pawn)
        {
            if (__instance.level <= 1)
            {
                __result += pawn.GetStatValue(HautsDefOf.Hauts_TierOnePsycastCostOffset);
                if (__result < 0)
                {
                    __result = 0f;
                }
            }
        }
        public static void HautsVPECastPostfix(AbilityExtension_Psycast __instance, VFECore.Abilities.Ability ability)
        {
            Pawn pawn = ability.pawn;
            if (pawn != null && pawn.psychicEntropy != null)
            {
                if (StatExtension.GetStatValue(pawn, VPE_DefOf.VPE_PsyfocusCostFactor, true) < 1f)
                {
                    foreach (Hediff h in pawn.health.hediffSet.hediffs)
                    {
                        if (h is HediffWithComps hwc)
                        {
                            HediffComp_PsyfocusSpentTracker pst = hwc.TryGetComp<HediffComp_PsyfocusSpentTracker>();
                            if (pst != null)
                            {
                                pst.UpdatePsyfocusExpenditure(-__instance.psyfocusCost * (1 - StatExtension.GetStatValue(pawn, VPE_DefOf.VPE_PsyfocusCostFactor, true)));
                                if (__instance.level <= 1)
                                {
                                    pst.UpdatePsyfocusExpenditure(Math.Min(-__instance.psyfocusCost * StatExtension.GetStatValue(pawn, VPE_DefOf.VPE_PsyfocusCostFactor, true), pawn.GetStatValue(HautsDefOf.Hauts_TierOnePsycastCostOffset)));
                                }
                            }
                        }
                    }
                }
                float psyfocus = Math.Min(__instance.GetPsyfocusUsedByPawn(pawn), HautsUtility.TotalPsyfocusRefund(pawn, __instance.GetPsyfocusUsedByPawn(pawn), ability.def.abilityClass == typeof(Ability_WordOf), HautsUtility.IsSkipAbility(ability.def)));
                pawn.psychicEntropy.OffsetPsyfocusDirectly(psyfocus);
                Hediff_PsycastAbilities psylink = pawn.Psycasts();
                if (psyfocus > 0f && psylink != null)
                {
                    psylink.GainExperience(-psyfocus * 100f * PsycastsMod.Settings.XPPerPercent, true);
                }
            }
        }
        public static void HautsVPEIngestedPostfix(float __result, Pawn ingester)
        {
            Pawn_PsychicEntropyTracker psychicEntropy = ingester.psychicEntropy;
            if (psychicEntropy != null && ingester.GetStatValue(HautsDefOf.Hauts_PsyfocusFromFood) != 0f)
            {
                float psyfocus = __result * ingester.GetStatValue(HautsDefOf.Hauts_PsyfocusFromFood);
                psychicEntropy.OffsetPsyfocusDirectly(psyfocus);
                Hediff_PsycastAbilities psylink = ingester.Psycasts();
                if (psyfocus > 0f && psylink != null)
                {
                    psylink.GainExperience(-psyfocus * 70f * PsycastsMod.Settings.XPPerPercent, true);
                }
            }
        }
        public static void HautsVPECheckMeditationScheduleTeachOpportunityPostfix(Pawn pawn)
        {
            float psyfocus = (pawn.GetStatValue(HautsDefOf.Hauts_PsyfocusRegenRate) + Pawn_PsychicEntropyTracker.FallRatePerPsyfocusBand[pawn.psychicEntropy.PsyfocusBand]) / 400f;
            pawn.psychicEntropy.OffsetPsyfocusDirectly(psyfocus);
            Hediff_PsycastAbilities psylink = pawn.Psycasts();
            if (psyfocus > 0f && psylink != null)
            {
                psylink.GainExperience(-psyfocus * 100f * PsycastsMod.Settings.XPPerPercent, true);
            }
        }
        public static void HautsVPENotify_PawnKilledPostfix(Pawn killed, Pawn killer)
        {
            if (killer.psychicEntropy != null)
            {
                Pawn_PsychicEntropyTracker psychicEntropy = killer.psychicEntropy;
                float psyfocus = killer.GetStatValue(HautsDefOf.Hauts_PsyfocusGainOnKill) * killed.GetStatValue(StatDefOf.PsychicSensitivity);
                if (killed.RaceProps != null)
                {
                    if (killed.RaceProps.intelligence == Intelligence.Animal)
                    {
                        psyfocus *= 0.5f;
                    }
                    else if (killed.RaceProps.intelligence == Intelligence.ToolUser)
                    {
                        psyfocus *= 0.75f;
                    }
                }
                psychicEntropy.OffsetPsyfocusDirectly(psyfocus);
                Hediff_PsycastAbilities psylink = killer.Psycasts();
                if (psylink != null)
                {
                    psylink.GainExperience(-psyfocus * 100f * PsycastsMod.Settings.XPPerPercent, true);
                }
            }
        }
        public static void HautsIsVPEPSycastPostfix(ref bool __result, VFECore.Abilities.Ability ability)
        {
            if (ability.pawn != null)
            {
                AbilityExtension_Psycast isPsycast = ability.def.GetModExtension<AbilityExtension_Psycast>();
                if (isPsycast != null)
                {
                    __result = true;
                }
            }
        }
        public static void HautsGetPsycastLevel_Postfix(ref int __result, VFECore.Abilities.Ability ability)
        {
            AbilityExtension_Psycast isPsycast = ability.def.GetModExtension<AbilityExtension_Psycast>();
            if (isPsycast != null)
            {
                __result = isPsycast.level;
            }
        }
        public static void HautsGetEntropyCost_Postfix(ref float __result, VFECore.Abilities.Ability ability)
        {
            AbilityExtension_Psycast isPsycast = ability.def.GetModExtension<AbilityExtension_Psycast>();
            if (isPsycast != null)
            {
                __result = isPsycast.GetEntropyUsedByPawn(ability.pawn);
            }
        }
        public static void HautsGetPsyfocusCost_Postfix(ref float __result, VFECore.Abilities.Ability ability)
        {
            AbilityExtension_Psycast isPsycast = ability.def.GetModExtension<AbilityExtension_Psycast>();
            if (isPsycast != null)
            {
                __result = isPsycast.GetPsyfocusUsedByPawn(ability.pawn);
                if (StatExtension.GetStatValue(ability.pawn, VPE_DefOf.VPE_PsyfocusCostFactor, true) < 1f)
                {
                    __result += isPsycast.psyfocusCost * (1 - StatExtension.GetStatValue(ability.pawn, VPE_DefOf.VPE_PsyfocusCostFactor, true));
                }
                if (isPsycast.level <= 1)
                {
                    __result -= Math.Min(isPsycast.psyfocusCost * (1 - StatExtension.GetStatValue(ability.pawn, VPE_DefOf.VPE_PsyfocusCostFactor, true)), -ability.pawn.GetStatValue(HautsDefOf.Hauts_TierOnePsycastCostOffset));
                }
            }
        }
        public static void HautsVPEUnlockAbilityPostfix(Pawn pawn, VFECore.Abilities.AbilityDef abilityDef)
        {
            Hediff_PsycastAbilities psylink = pawn.Psycasts();
            if (psylink != null)
            {
                AbilityExtension_Psycast modExtension = abilityDef.GetModExtension<AbilityExtension_Psycast>();
                if (modExtension != null && modExtension.path != null && !psylink.unlockedPaths.Contains(modExtension.path))
                {
                    psylink.UnlockPath(modExtension.path);
                }
            }
        }
        public static void HautsVPESetSkillPointsAndExperiencePostfix(Pawn setFor, Pawn copyFrom)
        {
            Hediff_PsycastAbilities psylinkFor = setFor.Psycasts();
            Hediff_PsycastAbilities psylinkFrom = copyFrom.Psycasts();
            psylinkFor.points = psylinkFrom.points;
            psylinkFor.experience = psylinkFrom.experience;
        }
    }
}
