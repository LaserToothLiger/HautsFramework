using HarmonyLib;
using HautsFramework;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Grammar;

namespace HautsF_Ideology
{
    [StaticConstructorOnStartup]
    public class HautsF_Ideology
    {
        private static readonly Type patchType = typeof(HautsF_Ideology);
        static HautsF_Ideology()
        {
            Harmony harmony = new Harmony(id: "rimworld.hautarche.hautsframework.ideology");
            harmony.Patch(AccessTools.Method(typeof(CompAbilityEffect_GiveHediff), nameof(CompAbilityEffect_GiveHediff.Apply), new[] { typeof(LocalTargetInfo), typeof(LocalTargetInfo) }),
                          postfix: new HarmonyMethod(patchType, nameof(HautsGiveHediffPostfix)));
        }
        public static void HautsGiveHediffPostfix(CompAbilityEffect_GiveHediff __instance, LocalTargetInfo target)
        {
            if (__instance.parent.sourcePrecept != null)
            {
                if (target.Pawn.Ideo == __instance.parent.pawn.Ideo)
                {
                    Pawn realTarget = null;
                    if (!__instance.Props.onlyApplyToSelf && __instance.Props.applyToTarget)
                    {
                        realTarget = target.Pawn;
                    }
                    if (__instance.Props.applyToSelf || __instance.Props.onlyApplyToSelf)
                    {
                        realTarget = __instance.parent.pawn;
                    }
                    if (realTarget != null)
                    {
                        Hediff hediff = realTarget.health.hediffSet.GetFirstHediffOfDef(__instance.Props.hediffDef, false);
                        HediffComp_Disappears hediffComp_Disappears = hediff.TryGetComp<HediffComp_Disappears>();
                        if (hediffComp_Disappears != null)
                        {
                            int newDuration = (int)(hediffComp_Disappears.ticksToDisappear * realTarget.GetStatValue(HautsDefOf.Hauts_IdeoAbilityDurationSelf));
                            hediffComp_Disappears.ticksToDisappear = newDuration;
                        }
                    }
                }
            }
        }
    }
    //book comp: ideoligious tract
    public class BookOutcomeProperties_PromoteIdeo : BookOutcomeProperties
    {
        public override Type DoerClass
        {
            get
            {
                return typeof(BookOutcomeDoerPromoteIdeo);
            }
        }
        public float newIdeoChance = 0f;
        //for making new ideos
        public float notForSpecificFactionType = 1f;
        public float forPlayerFaction;
        public FactionDef forcedFactionDef;
        public List<PreceptDef> disallowedPrecepts;
        public List<MemeDef> disallowedMemes;
        public List<MemeDef> forcedMemes;
        public bool forceNoWeaponPreference;
        public List<StyleCategoryDef> styles;
        public bool hidden;
        public bool requiredPreceptsOnly;
        //book strength
        public SimpleCurve conversionPerHour = new SimpleCurve
            {
                {new CurvePoint(0f, 0.01f),true},
                {new CurvePoint(1f, 0.01f),true},
                {new CurvePoint(2f, 0.01f),true},
                {new CurvePoint(3f, 0.01f),true},
                {new CurvePoint(4f, 0.01f),true},
                {new CurvePoint(5f, 0.01f),true},
                {new CurvePoint(6f, 0.01f),true},
            };
        public SimpleCurve certaintyPerHour = new SimpleCurve
            {
                {new CurvePoint(0f, 0.01f),true},
                {new CurvePoint(1f, 0.01f),true},
                {new CurvePoint(2f, 0.01f),true},
                {new CurvePoint(3f, 0.01f),true},
                {new CurvePoint(4f, 0.01f),true},
                {new CurvePoint(5f, 0.01f),true},
                {new CurvePoint(6f, 0.01f),true},
            };
        public ThoughtDef badConversionThought;
        public SimpleCurve bctLikelihood = new SimpleCurve
            {
                {new CurvePoint(0f, 0.01f),true},
                {new CurvePoint(1f, 0.01f),true},
                {new CurvePoint(2f, 0.01f),true},
                {new CurvePoint(3f, 0.01f),true},
                {new CurvePoint(4f, 0.01f),true},
                {new CurvePoint(5f, 0.01f),true},
                {new CurvePoint(6f, 0.01f),true},
            };
    }
    public class BookOutcomeDoerPromoteIdeo : BookOutcomeDoer
    {
        public new BookOutcomeProperties_PromoteIdeo Props
        {
            get
            {
                return (BookOutcomeProperties_PromoteIdeo)this.props;
            }
        }
        public override bool DoesProvidesOutcome(Pawn reader)
        {
            if (reader.Ideo != null)
            {
                return true;
            }
            return false;
        }
        public override void OnBookGenerated(Pawn author = null)
        {
            if (Rand.Chance(this.Props.newIdeoChance))
            {
                Ideo ideo;
                IdeoGenerationParms parms = new IdeoGenerationParms(Rand.Chance(this.Props.notForSpecificFactionType) ? null : (Rand.Chance(this.Props.forPlayerFaction) ? Faction.OfPlayer.def : (this.Props.forcedFactionDef ?? Find.FactionManager.AllFactionsListForReading.RandomElement().def)), false, this.Props.disallowedPrecepts, this.Props.disallowedMemes, this.Props.forcedMemes, false, this.Props.forceNoWeaponPreference, false, false, "", this.Props.styles, null, this.Props.hidden, "", this.Props.requiredPreceptsOnly);
                if (parms.fixedIdeo)
                {
                    ideo = IdeoGenerator.MakeFixedIdeo(parms);
                } else {
                    ideo = IdeoGenerator.GenerateIdeo(parms);
                }
                ideo.primaryFactionColor = new Color(Rand.Value, Rand.Value, Rand.Value, 1f);
                this.ideo = ideo;
            } else {
                this.ideo = Find.IdeoManager.IdeosListForReading.RandomElement();
            }
        }
        public override void Reset()
        {
            this.OnBookGenerated(null);
        }
        public float ConversionPerHour
        {
            get
            {
                return this.Props.conversionPerHour.Evaluate((float)this.Quality);
            }
        }
        public float CertaintyPerHour
        {
            get
            {
                return this.Props.certaintyPerHour.Evaluate((float)this.Quality);
            }
        }
        public float ChanceToOffendPerHour
        {
            get
            {
                return this.Props.bctLikelihood.Evaluate((float)this.Quality);
            }
        }
        public float ConversionPowerFromReaderTraits(Pawn reader)
        {
            float num = 1f;
            if (ModsConfig.IdeologyActive && reader.Ideo != null && this.ideo != null)
            {
                foreach (MemeDef memeDef in reader.Ideo.memes)
                {
                    if (!memeDef.agreeableTraits.NullOrEmpty<TraitRequirement>())
                    {
                        foreach (TraitRequirement traitRequirement in memeDef.agreeableTraits)
                        {
                            if (traitRequirement.HasTrait(reader))
                            {
                                num -= 0.2f;
                            }
                        }
                    }
                    if (!memeDef.disagreeableTraits.NullOrEmpty<TraitRequirement>())
                    {
                        foreach (TraitRequirement traitRequirement2 in memeDef.disagreeableTraits)
                        {
                            if (traitRequirement2.HasTrait(reader))
                            {
                                num += 0.2f;
                            }
                        }
                    }
                }
                foreach (MemeDef memeDef in this.ideo.memes)
                {
                    if (!memeDef.agreeableTraits.NullOrEmpty<TraitRequirement>())
                    {
                        foreach (TraitRequirement traitRequirement in memeDef.agreeableTraits)
                        {
                            if (traitRequirement.HasTrait(reader))
                            {
                                num += 0.2f;
                            }
                        }
                    }
                    if (!memeDef.disagreeableTraits.NullOrEmpty<TraitRequirement>())
                    {
                        foreach (TraitRequirement traitRequirement2 in memeDef.disagreeableTraits)
                        {
                            if (traitRequirement2.HasTrait(reader))
                            {
                                num -= 0.2f;
                            }
                        }
                    }
                }
            }
            return Math.Max(num, 0f);
        }
        public override void OnReadingTick(Pawn reader, float factor)
        {
            if (this.Parent.IsHashIntervalTick(250))
            {
                if (reader.Ideo != null)
                {

                    if (reader.Ideo != this.ideo)
                    {
                        Ideo oldIdeo = reader.Ideo;
                        Precept_Role role = oldIdeo.GetRole(reader);
                        if (Rand.Chance(this.ChanceToOffendPerHour * Math.Min(reader.GetStatValue(StatDefOf.ReadingSpeed), 1f)) && !ThoughtUtility.ThoughtNullified(reader, ThoughtDefOf.FailedConvertIdeoAttemptResentment))
                        {
                            reader.needs.mood.thoughts.memories.TryGainMemory(this.Props.badConversionThought, null, null);
                        } else if (reader.ideo.IdeoConversionAttempt(this.ConversionPerHour * reader.GetStatValue(StatDefOf.CertaintyLossFactor) * reader.GetStatValue(StatDefOf.ReadingSpeed) * this.ConversionPowerFromReaderTraits(reader) / 10f, this.ideo, true)) {
                            if (PawnUtility.ShouldSendNotificationAbout(reader))
                            {
                                string letterLabel = "LetterLabelConvertIdeoAttempt_Success".Translate();
                                string title = this.Parent.Label;
                                Book book = this.Parent as Book;
                                if (book != null)
                                {
                                    title = book.Title;
                                }
                                string letterText = "Hauts_LetterConvertIdeoBook_Success".Translate(title, reader.Name.ToStringShort, oldIdeo.name, this.ideo.name).Resolve();
                                LetterDef letterDef = LetterDefOf.PositiveEvent;
                                LookTargets lookTargets = new LookTargets(new TargetInfo[] { reader });
                                if (role != null)
                                {
                                    letterText = letterText + "\n\n" + "LetterRoleLostLetterIdeoChangedPostfix".Translate(reader.Named("PAWN"), role.Named("ROLE"), ideo.Named("OLDIDEO")).Resolve();
                                }
                                Find.LetterStack.ReceiveLetter(letterLabel, letterText, letterDef, lookTargets ?? reader, null, null, null, null, 0, true);
                                if (!Find.IdeoManager.IdeosListForReading.Contains(this.ideo))
                                {
                                    Find.IdeoManager.Add(this.ideo);
                                }
                            }
                        }
                    } else {
                        reader.ideo.OffsetCertainty(this.CertaintyPerHour * reader.GetStatValue(StatDefOf.ReadingSpeed) * this.ConversionPowerFromReaderTraits(reader) / 10f);
                    }
                }
            }
        }
        public override string GetBenefitsString(Pawn reader = null)
        {
            if (this.ideo == null)
            {
                return null;
            }
            StringBuilder stringBuilder = new StringBuilder();
            string text = "Hauts_IdeoBookConversion".Translate(this.ideo.name.Colorize(this.ideo.Color), this.ConversionPerHour.ToStringDecimalIfSmall());
            stringBuilder.AppendLine(" - " + text);
            string text2 = "Hauts_IdeoBookCertainty".Translate(this.ideo.name.Colorize(this.ideo.Color), this.CertaintyPerHour.ToStringDecimalIfSmall());
            stringBuilder.AppendLine(" - " + text2);
            string text3 = "Hauts_IdeoBookOffendChance".Translate(this.ChanceToOffendPerHour.ToStringByStyle(ToStringStyle.PercentTwo));
            stringBuilder.AppendLine(" - " + text3);
            string text4 = "Hauts_IdeoBookMemeList".Translate(this.ideo.name.Colorize(this.ideo.Color), this.ideo.StructureMeme);
            if (!this.ideo.memes.NullOrEmpty())
            {
                for (int i = 0; i < this.ideo.memes.Count; i++)
                {
                    if (this.ideo.memes[i].category != MemeCategory.Structure)
                    {
                        text4 += this.ideo.memes[i].label;
                        if (this.ideo.memes.Count > i + 1)
                        {
                            text4 += ", ";
                        }
                    }
                }
            }
            stringBuilder.AppendLine(text4);
            return stringBuilder.ToString();
        }
        /*public override List<RulePack> GetTopicRulePacks()
        {
            if (this.ideo != null && !this.ideo.memes.NullOrEmpty())
            {
                List<RulePack> rulePacks = new List<RulePack>();
                rulePacks = this.ideo.memes.Select((MemeDef md) => md.generalRules).ToList<RulePack>();
                return rulePacks;
            }
            return null;
        }*/
        public override void PostExposeData()
        {
            Scribe_References.Look<Ideo>(ref this.ideo, "ideo", false);
        }
        public Ideo ideo;
    }
}
