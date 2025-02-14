using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TweaksGalore;
using Verse;

namespace HautsF_TG
{
    public class StatPart_MaxOutCommandRange : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null || !TGTweakDefOf.Tweak_MechanitorTweaks.BoolValue || !TGTweakDefOf.Tweak_MechanitorDisableRange.BoolValue)
            {
                return;
            }
            val = this.setBaseTo;
        }
        public override string ExplanationPart(StatRequest req)
        {
            Pawn pawn;
            if ((pawn = (req.Thing as Pawn)) == null || !TGTweakDefOf.Tweak_MechanitorTweaks.BoolValue || !TGTweakDefOf.Tweak_MechanitorDisableRange.BoolValue)
            {
                return null;
            }
            return this.label + ": " + this.setBaseTo;
        }
        private readonly float setBaseTo;
        [MustTranslate]
        private readonly string label;
    }
}
