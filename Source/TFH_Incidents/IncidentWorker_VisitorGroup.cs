﻿namespace TFH_Incidents
{
    using System.Collections.Generic;

    using RimWorld;

    using TFH_VehicleBase;
    using TFH_VehicleBase.DefOfs_TFH;

    using TFH_Vehicles;
    using TFH_Vehicles.DefOfs_TFH;

    using Verse;
    using Verse.AI;
    using Verse.AI.Group;

    public class IncidentWorker_VisitorGroup : IncidentWorker_NeutralGroup
    {
        private const float TraderChance = 0.75f;

        public override bool TryExecute(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (!base.TryResolveParms(parms))
            {
                return false;
            }
            List<Pawn> list = base.SpawnPawns(parms);
            if (list.Count == 0)
            {
                return false;
            }
            IntVec3 chillSpot;
            RCellFinder.TryFindRandomSpotJustOutsideColony(list[0], out chillSpot);
            LordJob_VisitColony lordJob = new LordJob_VisitColony(parms.faction, chillSpot);
            LordMaker.MakeNewLord(parms.faction, lordJob, map, list);
            bool flag = false;
            if (Rand.Value < 0.75f)
            {
                flag = this.TryConvertOnePawnToSmallTrader(list, parms.faction, map);
            }
            Pawn pawn = list.Find((Pawn x) => parms.faction.leader == x);
            string label;
            string text3;
            if (list.Count == 1)
            {
                string text = (!flag) ? string.Empty : "SingleVisitorArrivesTraderInfo".Translate();
                string text2 = (pawn == null) ? string.Empty : "SingleVisitorArrivesLeaderInfo".Translate();
                label = "LetterLabelSingleVisitorArrives".Translate();
                text3 = "SingleVisitorArrives".Translate(new object[]
                                                             {
                                                                 list[0].story.Title.ToLower(),
                                                                 parms.faction.Name,
                                                                 list[0].Name,
                                                                 text,
                                                                 text2
                                                             });
                text3 = text3.AdjustedFor(list[0]);
            }
            else
            {
                string text4 = (!flag) ? string.Empty : "GroupVisitorsArriveTraderInfo".Translate();
                string text5 = (pawn == null) ? string.Empty : "GroupVisitorsArriveLeaderInfo".Translate(new object[]
                                                                                                             {
                                                                                                                 pawn.LabelShort
                                                                                                             });
                label = "LetterLabelGroupVisitorsArrive".Translate();
                text3 = "GroupVisitorsArrive".Translate(new object[]
                                                            {
                                                                parms.faction.Name,
                                                                text4,
                                                                text5
                                                            });
            }

            // Add vehicles
            foreach (Pawn current in list)
            {
                // Make vehicles
                if (current.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                    && parms.faction.def.techLevel >= TechLevel.Industrial
                    && current.RaceProps.FleshType != FleshTypeDefOf.Mechanoid && current.RaceProps.ToolUser && Rand.Value > 0.5f)
                {
                    CellFinder.RandomClosewalkCellNear(current.Position, current.Map, 5);

                    Pawn cart = PawnGenerator.GeneratePawn(VehicleKindDefOf.TFH_ATV, parms.faction);
                    var rand = Rand.Value;

                    if (rand >= 0.9f)
                    {
                        cart = PawnGenerator.GeneratePawn(VehicleKindDefOf.TFH_CombatATV, parms.faction);
                    }
                    else if (rand >= 0.8f)
                    {
                        cart = PawnGenerator.GeneratePawn(VehicleKindDefOf.TFH_Speeder, parms.faction);
                    }
                    GenSpawn.Spawn(cart, current.Position, map, Rot4.Random, false);
                 //   current.Map.reservationManager.ReleaseAllForTarget(cart);
                    Job job = new Job(VehicleJobDefOf.Mount) { targetA = cart };
                    current.jobs.StartJob(job, JobCondition.InterruptForced, null, true);
                }
            }

            PawnRelationUtility.Notify_PawnsSeenByPlayer(list, ref label, ref text3, "LetterRelatedPawnsNeutralGroup".Translate(), true);
            Find.LetterStack.ReceiveLetter(label, text3, LetterDefOf.Good, list[0], null);
            return true;
        }

        private bool TryConvertOnePawnToSmallTrader(List<Pawn> pawns, Faction faction, Map map)
        {
            if (faction.def.visitorTraderKinds.NullOrEmpty<TraderKindDef>())
            {
                return false;
            }
            Pawn pawn = pawns.RandomElement<Pawn>();
            Lord lord = pawn.GetLord();
            pawn.mindState.wantsToTradeWithColony = true;
            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, true);
            TraderKindDef traderKindDef = faction.def.visitorTraderKinds.RandomElementByWeight((TraderKindDef traderDef) => traderDef.commonality);
            pawn.trader.traderKind = traderKindDef;
            pawn.inventory.DestroyAll(DestroyMode.Vanish);
            ItemCollectionGeneratorParams parms = default(ItemCollectionGeneratorParams);
            parms.traderDef = traderKindDef;
            parms.forTile = map.Tile;
            parms.forFaction = faction;
            foreach (Thing current in ItemCollectionGeneratorDefOf.TraderStock.Worker.Generate(parms))
            {
                Pawn pawn2 = current as Pawn;
                if (pawn2 != null)
                {
                    if (pawn2.Faction != pawn.Faction)
                    {
                        pawn2.SetFaction(pawn.Faction, null);
                    }
                    IntVec3 loc = CellFinder.RandomClosewalkCellNear(pawn.Position, map, 5, null);
                    GenSpawn.Spawn(pawn2, loc, map);
                    lord.AddPawn(pawn2);
                }
                else if (!pawn.inventory.innerContainer.TryAdd(current, true))
                {
                    current.Destroy(DestroyMode.Vanish);
                }
            }
            PawnInventoryGenerator.GiveRandomFood(pawn);
            return true;
        }
    }
}
