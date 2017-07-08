﻿namespace TFH_VehicleBase.WorkGivers
{
    using System.Collections.Generic;
    using System.Linq;

    using RimWorld;

    using TFH_VehicleBase;

    using Verse;
    using Verse.AI;

    public class WorkGiver_Haul_ReturnCart : WorkGiver_Scanner
    {


        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            // return TFH_Utility.Cart();
            // noParking.SortBy(x => pawn.Position.DistanceTo(x.Position));
            return pawn.AvailableVehiclesForPawnFaction(999f)
                .Where(vehicle => !((Vehicle_Cart)vehicle).InParkingLot).ToList();

            // pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling();
        }

        public override bool ShouldSkip(Pawn pawn)
        {
            if (pawn.IsDriver())
            {
                return true;
            }

            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced)
        {
            Vehicle_Cart cart = t as Vehicle_Cart;

            if (cart.RefuelableComp != null && !cart.RefuelableComp.HasFuel)
            {
                JobFailReason.Is("EmptyTank".Translate());
                return null;
            }

            return pawn.DismountAtParkingLot("WG Haul", cart);
        }
    }

}