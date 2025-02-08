using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Hauts_Anomaly
{
    [StaticConstructorOnStartup]
    public class Hauts_Anomaly
    {
        private static readonly Type patchType = typeof(Hauts_Anomaly);
        static Hauts_Anomaly()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.anomaly");
            harmony.Patch(AccessTools.Method(typeof(Recipe_GhoulInfusion), nameof(Recipe_GhoulInfusion.ApplyOnPawn)),
                           prefix: new HarmonyMethod(patchType, nameof(HVBARecipe_GhoulInfusionPrefix)));
            harmony.Patch(AccessTools.Method(typeof(MetalhorrorUtility), nameof(MetalhorrorUtility.Infect)),
                           postfix: new HarmonyMethod(patchType, nameof(HVBAInfectPostfix)));
        }
        public static bool HVBARecipe_GhoulInfusionPrefix(Pawn pawn, List<Thing> ingredients)
        {
            foreach (Hediff h in pawn.health.hediffSet.hediffs)
            {
                if (h is HediffWithComps hwc)
                {
                    HediffComp_BioferriteDestabilizer bfd = hwc.TryGetComp<HediffComp_BioferriteDestabilizer>();
                    if (bfd != null && bfd.Props.ghoulImmunity)
                    {
                        if (PawnUtility.ShouldSendNotificationAbout(pawn))
                        {
                            Messages.Message("Hauts_BFDBlockedGhoulizing".Translate().CapitalizeFirst().Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true).Resolve(), pawn, MessageTypeDefOf.NegativeHealthEvent, true);
                        }
                        return false;
                    }
                }
            }
            return true;
        }
        public static void HVBAInfectPostfix(Pawn pawn, Pawn source)
        {
            if (source != null)
            {
                bool slayMetalhorror = false;
                float damageOnSlay = 0f;
                DamageDef damageType = DamageDefOf.Flame;
                foreach (Hediff h in pawn.health.hediffSet.hediffs)
                {
                    if (h is HediffWithComps hwc)
                    {
                        HediffComp_BioferriteDestabilizer bfd = hwc.TryGetComp<HediffComp_BioferriteDestabilizer>();
                        if (bfd != null && bfd.Props.metalhorrorImmunity)
                        {
                            slayMetalhorror = true;
                            if (bfd.Props.damageOnBlockingMetalhorror != null && bfd.Props.damageTypeVsMetalhorrors != null && bfd.Props.damageOnBlockingMetalhorror.max > 0f)
                            {
                                damageOnSlay = Math.Max(0f, bfd.Props.damageOnBlockingMetalhorror.RandomInRange);
                                damageType = bfd.Props.damageTypeVsMetalhorrors;
                            }
                            bfd.CheckDestroy();
                            break;
                        }
                    }
                }
                if (slayMetalhorror)
                {
                    List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                    for (int i = hediffs.Count - 1; i >= 0; i--)
                    {
                        if (hediffs[i].def == HediffDefOf.MetalhorrorImplant)
                        {
                            pawn.health.RemoveHediff(hediffs[i]);
                            pawn.TakeDamage(new DamageInfo(damageType, damageOnSlay, 200f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true, QualityCategory.Normal, true));
                            MetalhorrorUtility.TryEmerge(source, "Hauts_MetalhorrorReasonBFD".Translate(source.Named("INFECTED")), false);
                            continue;
                        }
                    }
                }
            }
        }
    }
    public class HediffCompProperties_BioferriteDestabilizer : HediffCompProperties
    {
        public HediffCompProperties_BioferriteDestabilizer()
        {
            this.compClass = typeof(HediffComp_BioferriteDestabilizer);
        }
        public bool destroyedBySevereInteractions;
        public float damageVsGhouls;
        public DamageDef damageTypeVsGhouls;
        public float postKillDamageToShamblers;
        public DamageDef damageTypeVsShamblers;
        public bool entityImmunity = true;
        public bool ghoulImmunity = true;
        public bool shamblerImmunity = true;
        public bool metalhorrorImmunity = true;
        public bool mutationImmunity = true;
        public FloatRange damageOnBlockingMetalhorror = new FloatRange(0);
        public DamageDef damageTypeVsMetalhorrors;
    }
    public class HediffComp_BioferriteDestabilizer : HediffComp
    {
        public HediffCompProperties_BioferriteDestabilizer Props
        {
            get
            {
                return (HediffCompProperties_BioferriteDestabilizer)this.props;
            }
        }
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            this.TickRareEffects();
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            if (this.Pawn.IsHashIntervalTick(250))
            {
                this.TickRareEffects();
            }
        }
        public void TickRareEffects()
        {
            if (this.Props.entityImmunity && this.Pawn.RaceProps.IsAnomalyEntity)
            {
                this.Pawn.TakeDamage(new DamageInfo(this.Props.damageTypeVsGhouls, this.Props.damageVsGhouls, 200f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true, QualityCategory.Normal, true));
                this.CheckDestroy();
            }
            if (this.Props.ghoulImmunity && this.Pawn.IsGhoul)
            {
                this.Pawn.TakeDamage(new DamageInfo(this.Props.damageTypeVsGhouls, this.Props.damageVsGhouls, 200f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true, QualityCategory.Normal, true));
                this.CheckDestroy();
            }
            if (this.Props.shamblerImmunity && this.Pawn.IsShambler)
            {
                this.Pawn.Kill(new DamageInfo(this.Props.damageTypeVsShamblers, 999f, 200f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true, QualityCategory.Normal, true));
                if (this.Pawn.Corpse != null)
                {
                    this.Pawn.Corpse.TakeDamage(new DamageInfo(this.Props.damageTypeVsShamblers, this.Props.postKillDamageToShamblers, 200f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true, QualityCategory.Normal, true));
                    this.Pawn.Corpse.TryAttachFire(0.2f, null);
                }
            }
            bool destroy = false;
            List<Hediff> hediffs = this.Pawn.health.hediffSet.hediffs;
            for (int i = hediffs.Count - 1; i >= 0; i--)
            {
                if (hediffs[i].def == HediffDefOf.MetalhorrorImplant && this.Props.metalhorrorImmunity)
                {
                    this.Pawn.health.RemoveHediff(hediffs[i]);
                    if (PawnUtility.ShouldSendNotificationAbout(this.Pawn))
                    {
                        if (this.Props.destroyedBySevereInteractions)
                        {
                            Messages.Message("Hauts_BFDDestroyedMetalhorror".Translate().CapitalizeFirst().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), this.Pawn, MessageTypeDefOf.PositiveEvent, true);
                        }
                        else
                        {
                            Messages.Message("Hauts_BFDDestroyedMetalhorror2".Translate().CapitalizeFirst().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), this.Pawn, MessageTypeDefOf.PositiveEvent, true);
                        }
                    }
                    if (this.Props.damageTypeVsMetalhorrors != null && this.Props.damageOnBlockingMetalhorror != null && this.Props.damageOnBlockingMetalhorror.max > 0f)
                    {

                    }
                    this.Pawn.TakeDamage(new DamageInfo(this.Props.damageTypeVsMetalhorrors, this.Props.damageOnBlockingMetalhorror.RandomInRange, 200f, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true, QualityCategory.Normal, true));
                    destroy = true;
                    continue;
                }
                if (hediffs[i] is HediffWithComps hwc && this.Props.mutationImmunity)
                {
                    HediffComp_FleshbeastEmerge fbe = hwc.TryGetComp<HediffComp_FleshbeastEmerge>();
                    if (fbe != null)
                    {
                        this.Pawn.health.RemoveHediff(hediffs[i]);
                        if (PawnUtility.ShouldSendNotificationAbout(this.Pawn))
                        {
                            Messages.Message("Hauts_BFDDestroyedMutation".Translate().CapitalizeFirst().Formatted(this.Pawn.Named("PAWN")).AdjustedFor(this.Pawn, "PAWN", true).Resolve(), this.Pawn, MessageTypeDefOf.NeutralEvent, true);
                        }
                        if (this.Pawn.SpawnedOrAnyParentSpawned)
                        {
                            GasUtility.AddGas(this.Pawn.PositionHeld, Find.CurrentMap, GasType.RotStink, 2f);
                        }
                    }
                }
            }
            if (destroy)
            {
                this.CheckDestroy();
            }
        }
        public void CheckDestroy()
        {
            if (this.Props.destroyedBySevereInteractions)
            {
                this.Pawn.health.RemoveHediff(this.parent);
            }
        }
    }
}
