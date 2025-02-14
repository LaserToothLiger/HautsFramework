using HarmonyLib;
using HautsFramework;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using VFECore;
using VFEInsectoids;

namespace HautsFramework_VFEInsectoids2
{
    [StaticConstructorOnStartup]
    public class HautsFramework_VFEInsectoids2
    {
        private static readonly Type patchType = typeof(HautsFramework_VFEInsectoids2);
        static HautsFramework_VFEInsectoids2()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.insectoids2");
            harmony.Patch(AccessTools.Method(typeof(HautsUtility), nameof(HautsUtility.IsLeapVerb)),
                           postfix: new HarmonyMethod(patchType, nameof(HVFEI2IsLeapVerbPostfix)));
            harmony.Patch(AccessTools.Property(typeof(Verb_CastAbilityJumpUnrestricted), nameof(Verb_CastAbilityJumpUnrestricted.EffectiveRange)).GetGetMethod(),
                           postfix: new HarmonyMethod(patchType, nameof(HVFEI2_VCAJU_EffectiveRangePostfix)));
        }
        public static void HVFEI2IsLeapVerbPostfix(ref bool __result, Verb verb)
        {
            if (verb is Verb_CastAbilityJumpUnrestricted)
            {
                __result = true;
            }
        }
        public static void HVFEI2_VCAJU_EffectiveRangePostfix(ref float __result, Verb_CastAbilityJumpUnrestricted __instance)
        {
            if (__instance.CasterPawn != null)
            {
                __result *= __instance.CasterPawn.GetStatValue(HautsDefOf.Hauts_JumpRangeFactor);
            }
        }
    }
    public class CompProperties_AbilityAcidSpewScalable : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityAcidSpewScalable()
        {
            this.compClass = typeof(CompAbilityAcidSpewScalable);
        }
        public float range;
        public float lineWidthEnd;
        public ThingDef filthDef;
        public int damAmount = -1;
        public EffecterDef effecterDef;
        public bool canHitFilledCells;
    }
    public class CompAbilityAcidSpewScalable : CompAbilityEffect
    {
        private new CompProperties_AbilityAcidSpewScalable Props
        {
            get
            {
                return (CompProperties_AbilityAcidSpewScalable)this.props;
            }
        }
        private Pawn Pawn
        {
            get
            {
                return this.parent.pawn;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            IntVec3 cell = target.Cell;
            Map mapHeld = this.parent.pawn.MapHeld;
            float num = 0f;
            DamageDef vfei2_AcidSpit = VFEI_DefOf.VFEI2_AcidSpit;
            Thing pawn = this.Pawn;
            ThingDef filthDef = this.Props.filthDef;
            GenExplosion.DoExplosion(cell, mapHeld, num, vfei2_AcidSpit, pawn, this.Props.damAmount, -1f, null, null, null, null, filthDef, 1f, 1, null, false, null, 0f, 1, 0f, false, null, null, null, false, 0.6f, 0f, false, null, 1f, null, this.AffectedCells(target));
            base.Apply(target, dest);
        }
        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            if (this.Props.effecterDef != null)
            {
                yield return new PreCastAction
                {
                    action = delegate (LocalTargetInfo a, LocalTargetInfo b)
                    {
                        this.parent.AddEffecterToMaintain(this.Props.effecterDef.Spawn(this.parent.pawn.Position, a.Cell, this.parent.pawn.Map, 1f), this.Pawn.Position, a.Cell, 17, this.Pawn.MapHeld);
                    },
                    ticksAwayFromCast = 17
                };
            }
            yield break;
        }
        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(this.AffectedCells(target));
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (this.Pawn.Faction != null)
            {
                foreach (IntVec3 intVec in this.AffectedCells(target))
                {
                    List<Thing> thingList = intVec.GetThingList(this.Pawn.Map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        if (thingList[i].Faction == this.Pawn.Faction)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return true;
        }
        private float Range
        {
            get
            {
                return this.Props.range * ((this.parent.def.HasModExtension<Hauts_SpewAbility>() && ModsConfig.BiotechActive) ? this.parent.pawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) : 1f);
            }
        }
        private List<IntVec3> AffectedCells(LocalTargetInfo target)
        {
            this.tmpCells.Clear();
            Vector3 vector = this.Pawn.Position.ToVector3Shifted().Yto0();
            IntVec3 intVec = target.Cell.ClampInsideMap(this.Pawn.Map);
            if (this.Pawn.Position == intVec)
            {
                return this.tmpCells;
            }
            float lengthHorizontal = (intVec - this.Pawn.Position).LengthHorizontal;
            float num = (float)(intVec.x - this.Pawn.Position.x) / lengthHorizontal;
            float num2 = (float)(intVec.z - this.Pawn.Position.z) / lengthHorizontal;
            intVec.x = Mathf.RoundToInt((float)this.Pawn.Position.x + num * this.Range);
            intVec.z = Mathf.RoundToInt((float)this.Pawn.Position.z + num2 * this.Range);
            float num3 = Vector3.SignedAngle(intVec.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up);
            float num4 = this.Props.lineWidthEnd * ((this.parent.def.HasModExtension<Hauts_SpewAbility>() && ModsConfig.BiotechActive) ? (this.parent.pawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) / (this.parent.pawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) > 1.5f ? 1.5f : 1f)) : 1f) / 2f;
            float num5 = Mathf.Sqrt(Mathf.Pow((intVec - this.Pawn.Position).LengthHorizontal, 2f) + Mathf.Pow(num4, 2f));
            float num6 = 57.29578f * Mathf.Asin(num4 / num5);
            int num7 = GenRadial.NumCellsInRadius(this.Range);
            for (int i = 0; i < num7; i++)
            {
                IntVec3 intVec2 = this.Pawn.Position + GenRadial.RadialPattern[i];
                if (this.CanUseCell(intVec2) && Mathf.Abs(Mathf.DeltaAngle(Vector3.SignedAngle(intVec2.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up), num3)) <= num6)
                {
                    this.tmpCells.Add(intVec2);
                }
            }
            List<IntVec3> list = GenSight.BresenhamCellsBetween(this.Pawn.Position, intVec);
            for (int j = 0; j < list.Count; j++)
            {
                IntVec3 intVec3 = list[j];
                if (!this.tmpCells.Contains(intVec3) && this.CanUseCell(intVec3))
                {
                    this.tmpCells.Add(intVec3);
                }
            }
            return this.tmpCells;
        }
        [CompilerGenerated]
        private bool CanUseCell(IntVec3 c)
        {
            ShootLine shootLine;
            return c.InBounds(this.Pawn.Map) && !(c == this.Pawn.Position) && (this.Props.canHitFilledCells || !c.Filled(this.Pawn.Map)) && c.InHorDistOf(this.Pawn.Position, this.Range) && this.parent.verb.TryFindShootLineFromTo(this.parent.pawn.Position, c, out shootLine, false);
        }
        private readonly List<IntVec3> tmpCells = new List<IntVec3>();
    }
    public class CompProperties_AbilityFuelSpewScalable : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityFuelSpewScalable()
        {
            this.compClass = typeof(CompAbilityFuelSpewScalable);
        }
        public float range;
        public float lineWidthEnd;
        public ThingDef filthDef;
        public int damAmount = -1;
        public EffecterDef effecterDef;
        public bool canHitFilledCells;
    }
    public class CompAbilityFuelSpewScalable : CompAbilityEffect
    {
        private new CompProperties_AbilityFuelSpewScalable Props
        {
            get
            {
                return (CompProperties_AbilityFuelSpewScalable)this.props;
            }
        }
        private Pawn Pawn
        {
            get
            {
                return this.parent.pawn;
            }
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            IntVec3 cell = target.Cell;
            Map mapHeld = this.parent.pawn.MapHeld;
            float num = 0f;
            DamageDef blunt = DamageDefOf.Blunt;
            Thing pawn = this.Pawn;
            ThingDef filthDef = this.Props.filthDef;
            GenExplosion.DoExplosion(cell, mapHeld, num, blunt, pawn, this.Props.damAmount, -1f, null, null, null, null, filthDef, 0.75f, 1, null, false, null, 0f, 1, 0f, false, null, null, null, false, 0.6f, 0f, false, null, 1f, null, this.AffectedCells(target));
            base.Apply(target, dest);
        }
        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            if (this.Props.effecterDef != null)
            {
                yield return new PreCastAction
                {
                    action = delegate (LocalTargetInfo a, LocalTargetInfo b)
                    {
                        this.parent.AddEffecterToMaintain(this.Props.effecterDef.Spawn(this.parent.pawn.Position, a.Cell, this.parent.pawn.Map, 1f), this.Pawn.Position, a.Cell, 17, this.Pawn.MapHeld);
                    },
                    ticksAwayFromCast = 17
                };
            }
            yield break;
        }
        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(this.AffectedCells(target));
        }
        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (this.Pawn.Faction != null)
            {
                foreach (IntVec3 intVec in this.AffectedCells(target))
                {
                    List<Thing> thingList = intVec.GetThingList(this.Pawn.Map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        if (thingList[i].Faction == this.Pawn.Faction)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return true;
        }
        private float Range
        {
            get
            {
                return this.Props.range * ((this.parent.def.HasModExtension<Hauts_SpewAbility>() && ModsConfig.BiotechActive) ? this.parent.pawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) : 1f);
            }
        }
        private List<IntVec3> AffectedCells(LocalTargetInfo target)
        {
            this.tmpCells.Clear();
            Vector3 vector = this.Pawn.Position.ToVector3Shifted().Yto0();
            IntVec3 intVec = target.Cell.ClampInsideMap(this.Pawn.Map);
            if (this.Pawn.Position == intVec)
            {
                return this.tmpCells;
            }
            float lengthHorizontal = (intVec - this.Pawn.Position).LengthHorizontal;
            float num = (float)(intVec.x - this.Pawn.Position.x) / lengthHorizontal;
            float num2 = (float)(intVec.z - this.Pawn.Position.z) / lengthHorizontal;
            intVec.x = Mathf.RoundToInt((float)this.Pawn.Position.x + num * this.Range);
            intVec.z = Mathf.RoundToInt((float)this.Pawn.Position.z + num2 * this.Range);
            float num3 = Vector3.SignedAngle(intVec.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up);
            float num4 = this.Props.lineWidthEnd * ((this.parent.def.HasModExtension<Hauts_SpewAbility>() && ModsConfig.BiotechActive) ? (this.parent.pawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) / (this.parent.pawn.GetStatValue(HautsDefOf.Hauts_SpewRangeFactor) > 1.5f ? 1.5f : 1f)) : 1f) / 2f;
            float num5 = Mathf.Sqrt(Mathf.Pow((intVec - this.Pawn.Position).LengthHorizontal, 2f) + Mathf.Pow(num4, 2f));
            float num6 = 57.29578f * Mathf.Asin(num4 / num5);
            int num7 = GenRadial.NumCellsInRadius(this.Range);
            for (int i = 0; i < num7; i++)
            {
                IntVec3 intVec2 = this.Pawn.Position + GenRadial.RadialPattern[i];
                if (this.CanUseCell(intVec2) && Mathf.Abs(Mathf.DeltaAngle(Vector3.SignedAngle(intVec2.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up), num3)) <= num6)
                {
                    this.tmpCells.Add(intVec2);
                }
            }
            List<IntVec3> list = GenSight.BresenhamCellsBetween(this.Pawn.Position, intVec);
            for (int j = 0; j < list.Count; j++)
            {
                IntVec3 intVec3 = list[j];
                if (!this.tmpCells.Contains(intVec3) && this.CanUseCell(intVec3))
                {
                    this.tmpCells.Add(intVec3);
                }
            }
            return this.tmpCells;
        }
        [CompilerGenerated]
        private bool CanUseCell(IntVec3 c)
        {
            ShootLine shootLine;
            return c.InBounds(this.Pawn.Map) && !(c == this.Pawn.Position) && (this.Props.canHitFilledCells || !c.Filled(this.Pawn.Map)) && c.InHorDistOf(this.Pawn.Position, this.Range) && this.parent.verb.TryFindShootLineFromTo(this.parent.pawn.Position, c, out shootLine, false);
        }
        private readonly List<IntVec3> tmpCells = new List<IntVec3>();
    }
}
