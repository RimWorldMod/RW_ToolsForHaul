﻿namespace TFH_Incidents
{
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using Verse;
    using Verse.AI;

    public class IncidentWorker_RaidFriendly : IncidentWorker_Raid_TFH
    {
        protected override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            IEnumerable<Faction> source = (from p in map.attackTargetsCache.TargetsHostileToColony
                                           select ((Thing)p).Faction).Distinct<Faction>();
            return base.FactionCanBeGroupSource(f, map, desperate) && !f.def.hidden && !f.HostileTo(Faction.OfPlayer) && (!source.Any<Faction>() || source.Any((Faction hf) => hf.HostileTo(f)));
        }

        protected override bool CanFireNowSub(IIncidentTarget target)
        {
            if (!base.CanFireNowSub(target))
            {
                return false;
            }
            Map map = (Map)target;
            return (from p in map.attackTargetsCache.TargetsHostileToColony
                    where !p.ThreatDisabled()
                    select p).Sum(delegate (IAttackTarget p)
                {
                    Pawn pawn = p as Pawn;
                    if (pawn != null)
                    {
                        return pawn.kindDef.combatPower;
                    }
                    return 0f;
                }) > 120f;
        }

        protected override bool TryResolveRaidFaction(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (parms.faction != null)
            {
                return true;
            }
            if (!base.CandidateFactions(map, false).Any<Faction>())
            {
                return false;
            }
            parms.faction = base.CandidateFactions(map, false).RandomElementByWeight((Faction fac) => fac.PlayerGoodwill + 120.000008f);
            return true;
        }

        protected override void ResolveRaidStrategy(IncidentParms parms)
        {
            if (parms.raidStrategy != null)
            {
                return;
            }
            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttack;
        }

        protected override void ResolveRaidPoints(IncidentParms parms)
        {
            parms.points = (float)Rand.Range(400, 800);
        }

        protected override string GetLetterLabel(IncidentParms parms)
        {
            return parms.raidStrategy.letterLabelFriendly;
        }

        protected override string GetLetterText(IncidentParms parms, List<Pawn> pawns)
        {
            string text = null;
            switch (parms.raidArrivalMode)
            {
                case PawnsArriveMode.EdgeWalkIn:
                    text = "FriendlyRaidWalkIn".Translate(new object[]
                                                              {
                                                                  parms.faction.def.pawnsPlural,
                                                                  parms.faction.Name
                                                              });
                    break;
                case PawnsArriveMode.EdgeDrop:
                    text = "FriendlyRaidEdgeDrop".Translate(new object[]
                                                                {
                                                                    parms.faction.def.pawnsPlural,
                                                                    parms.faction.Name
                                                                });
                    break;
                case PawnsArriveMode.CenterDrop:
                    text = "FriendlyRaidCenterDrop".Translate(new object[]
                                                                  {
                                                                      parms.faction.def.pawnsPlural,
                                                                      parms.faction.Name
                                                                  });
                    break;
            }
            text += "\n\n";
            text += parms.raidStrategy.arrivalTextFriendly;
            Pawn pawn = pawns.Find((Pawn x) => x.Faction.leader == x);
            if (pawn != null)
            {
                text += "\n\n";
                text += "FriendlyRaidLeaderPresent".Translate(new object[]
                                                                  {
                                                                      pawn.Faction.def.pawnsPlural,
                                                                      pawn.LabelShort
                                                                  });
            }
            return text;
        }

        protected override LetterDef GetLetterDef()
        {
            return LetterDefOf.BadNonUrgent;
        }

        protected override string GetRelatedPawnsInfoLetterText(IncidentParms parms)
        {
            return "LetterRelatedPawnsRaidFriendly".Translate(new object[]
                                                                  {
                                                                      parms.faction.def.pawnsPlural
                                                                  });
        }
    }
}
