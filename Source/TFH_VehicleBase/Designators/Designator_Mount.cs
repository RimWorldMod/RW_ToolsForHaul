﻿namespace TFH_VehicleBase.Designators
{
    using System.Collections.Generic;

    using RimWorld;

    using TFH_VehicleBase.DefOfs_TFH;

    using Verse;
    using Verse.AI;

    public class Designator_Mount : Designator
    {
        public Vehicle_Cart vehicle;

        public Designator_Mount()
        {
            this.useMouseIcon = true;
            this.soundSucceeded = SoundDefOf.Click;
        }

        public override int DraggableDimensions => 2;

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            Pawn pawn = loc.GetThingList(this.Map).Find(t => t is Pawn) as Pawn;
            if (pawn == null)
                return new AcceptanceReport("CannotMount".Translate() + ": " + "NotPawn".Translate());
            if (pawn.Faction != Faction.OfPlayer)
                return new AcceptanceReport("CannotMount".Translate() + ": " + "NotColonyFaction".Translate());
#if Saddle
            if (!pawn.RaceProps.Animal && vehicle is Vehicle_Saddle)
                return new AcceptanceReport("CannotMount".Translate() + ": " + "NotHumanlikeOrMechanoid".Translate());
#endif
            if (pawn.RaceProps.Animal && !pawn.training.IsCompleted(TrainableDefOf.Obedience))
                return new AcceptanceReport("CannotMount".Translate() + ": " + "NotTrainedAnimal".Translate());
            if (pawn.RaceProps.Animal && !(pawn.RaceProps.baseBodySize >= 1.0))
                return new AcceptanceReport("CannotMount".Translate() + ": " + "TooSmallAnimal".Translate());
            return true;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            List<Thing> thingList = c.GetThingList(this.Map);
            foreach (Thing thing in thingList)
            {
                Pawn pawn = thing as Pawn;

                if (pawn == null)
                {
                    return;

                }

                if (pawn.Faction == Faction.OfPlayer && (pawn.RaceProps.IsMechanoid || pawn.RaceProps.Humanlike) && !pawn.IsDriver())
                {
                    Job jobNew = new Job(VehicleJobDefOf.Mount);
                    this.Map.reservationManager.ReleaseAllForTarget(this.vehicle);
                    jobNew.targetA = this.vehicle;
                    pawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                    break;
                }

                if (pawn.Faction == Faction.OfPlayer && pawn.RaceProps.Animal && pawn.training.IsCompleted(TrainableDefOf.Obedience) && pawn.RaceProps.baseBodySize >= 1.0 && !pawn.IsDriver())
                {
                    Pawn worker = null;
                    Job jobNew = new Job(VehicleJobDefOf.MakeMount);
                    this.Map.reservationManager.ReleaseAllForTarget(this.vehicle);
                    jobNew.count = 1;
                    jobNew.targetA = this.vehicle;
                    jobNew.targetB = pawn;
                    foreach (Pawn colonyPawn in PawnsFinder.AllMaps_FreeColonistsSpawned)
                        if (colonyPawn.CurJob.def != jobNew.def
                            && (worker == null || (worker.Position - pawn.Position).LengthHorizontal
                                > (colonyPawn.Position - pawn.Position).LengthHorizontal))
                        {
                            worker = colonyPawn;
                        }

                    if (worker == null)
                    {
                        Messages.Message("NoWorkForMakeMount".Translate(), MessageSound.RejectInput);
                        break;
                    }

                    worker.jobs.StartJob(jobNew, JobCondition.InterruptForced);
                    break;
                }
            }

            Find.DesignatorManager.Deselect();
        }
    }
}