using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using HautsFramework;
using VPEPuppeteer;
using RimWorld;
using Verse;
using System.Reflection;
using RimWorld.Planet;

namespace Hauts_VPEP
{
    [StaticConstructorOnStartup]
    public class HautsF_VPEP
    {
        private static readonly Type patchType = typeof(HautsF_VPEP);
        static HautsF_VPEP()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframeworkvpepcompatibility.main");
            harmony.Patch(AccessTools.Method(typeof(Ability_Puppet), nameof(Ability_Puppet.Cast)),
                            prefix: new HarmonyMethod(patchType, nameof(HautsVPEP_CastPrefix)));
            harmony.Patch(AccessTools.Method(typeof(Ability_Puppet), nameof(Ability_Puppet.Cast)),
                            postfix: new HarmonyMethod(patchType, nameof(HautsVPEP_CastPostfix)));
        }
        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
        public static void HautsVPEP_CastPrefix(out List<Trait> __state, GlobalTargetInfo[] targets)
        {
            Pawn pawn = targets[0].Thing as Pawn;
            if (pawn != null && pawn.story != null)
            {
                __state = new List<Trait>();
                foreach (Trait t in pawn.story.traits.allTraits)
                {
                    if (HautsUtility.IsExciseTraitExempt(t.def))
                    {
                        __state.Add(t);
                    }
                }
            }
            else
            {
                __state = null;
            }
        }
        public static void HautsVPEP_CastPostfix(List<Trait> __state, Ability_Puppet __instance, GlobalTargetInfo[] targets)
        {
            Pawn pawn = targets[0].Thing as Pawn;
            if (pawn != null && pawn.story != null)
            {
                foreach (Trait t in pawn.story.traits.allTraits.ToList<Trait>())
                {
                    if (HautsUtility.IsExciseTraitExempt(t.def))
                    {
                        pawn.story.traits.RemoveTrait(t, false);
                    }
                }
                if (__state != null && __state.Count > 0)
                {
                    foreach (Trait t in __state)
                    {
                        pawn.story.traits.GainTrait(t, true);
                    }
                }
            }
        }
    }
}
