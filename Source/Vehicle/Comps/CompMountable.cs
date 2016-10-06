﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace ToolsForHaul
{
    public class CompMountable : ThingComp
    {
        private const string txtCommandDismountLabel = "CommandDismountLabel";
        private const string txtCommandDismountDesc = "CommandDismountDesc";
        private const string txtCommandMountLabel = "CommandMountLabel";
        private const string txtCommandMountDesc = "CommandMountDesc";
        private const string txtMountOn = "MountOn";
        private const string txtDismount = "Dismount";

        protected Pawn driver = null;
        private Building_Door lastPassedDoor = null;
        private int tickLastDoorCheck = Find.TickManager.TicksGame;
        private const int TickCooldownDoorCheck = 96;
        private int tickCheck = Find.TickManager.TicksGame;
        private int tickCooldown = 60;

        public void MountOn(Pawn pawn)
        {
            if (driver != null)
                return;
            driver = pawn;
        }
        public bool IsMounted => driver != null;
        public Pawn Driver => driver;

        public void Dismount()
        {
            //if (Find.Reservations.IsReserved(parent, driver.Faction))
            Find.Reservations.ReleaseAllForTarget(parent);
            driver = null;
        }
        public void DismountAt(IntVec3 dismountPos)
        {
            //if (driver.Position.IsAdjacentTo8WayOrInside(dismountPos, driver.Rotation, new IntVec2(1,1)))
            if (dismountPos != IntVec3.Invalid)
            {
                Dismount();
                parent.Position = dismountPos;
                return;
            }
            Log.Warning("Tried dismount at " + dismountPos);
        }

        public Vector3 InteractionOffset => parent.def.interactionCellOffset.ToVector3().RotatedBy(driver.Rotation.AsAngle);

        public Vector3 Position
        {
            get
            {
                Vector3 position = driver.DrawPos - InteractionOffset * 1.3f;
                //No driver
                if (driver == null)
                    return parent.DrawPos;
                //Out of bound or Preventing cart from stucking door
                if (!position.InBounds())
                    return driver.DrawPos;

                return driver.DrawPos - InteractionOffset * 1.3f;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.LookReference(ref driver, "driver");
            Scribe_References.LookReference(ref lastPassedDoor, "lastPassedDoor");
        }

        public override void CompTick()
        {
            base.CompTick();
            if (IsMounted)
            {
                if (driver.Dead || driver.Downed || driver.health.InPainShock || driver.MentalStateDef == MentalStateDefOf.WanderPsychotic || (ForbidUtility.IsForbidden(parent, Faction.OfPlayer) && driver.Faction == Faction.OfPlayer))
                {
                    parent.Position = (IntVec3Utility.ToIntVec3(Position));
                    parent.Rotation = (driver.Rotation);
                    Dismount();
                    return;
                }
                if (Find.TickManager.TicksGame - tickCheck >= tickCooldown)
                {
                    if (driver.Faction == Faction.OfPlayer && driver.CurJob != null && driver.CurJob.def.playerInterruptible && (driver.CurJob.def == JobDefOf.DoBill || driver.CurJob.def == JobDefOf.EnterCryptosleepCasket || driver.CurJob.def == JobDefOf.LayDown || driver.CurJob.def == JobDefOf.Lovin || driver.CurJob.def == JobDefOf.MarryAdjacentPawn || driver.CurJob.def == JobDefOf.Mate || driver.CurJob.def == JobDefOf.PrisonerAttemptRecruit || driver.CurJob.def == JobDefOf.Research || driver.CurJob.def == JobDefOf.SocialRelax || driver.CurJob.def == JobDefOf.SpectateCeremony || driver.CurJob.def == JobDefOf.StandAndBeSociallyActive || driver.CurJob.def == JobDefOf.TakeToBedToOperate || driver.CurJob.def == JobDefOf.TendPatient || driver.CurJob.def == JobDefOf.UseCommsConsole || driver.CurJob.def == JobDefOf.UseNeurotrainer || driver.CurJob.def == JobDefOf.VisitSickPawn || driver.CurJob.def == JobDefOf.Shear || driver.CurJob.def == JobDefOf.Ingest) && GridsUtility.Roofed(driver.Position))
                    {
                        parent.Position = (IntVec3Utility.ToIntVec3(Position));
                        parent.Rotation = (driver.Rotation);
                        if (!GenGrid.InBounds(driver.Position))
                        {
                            DismountAt(driver.Position);
                            return;
                        }
                        DismountAt(driver.Position - IntVec3Utility.ToIntVec3(InteractionOffset));
                        return;
                    }
                    else
                    {
                        tickCheck = Find.TickManager.TicksGame;
                        tickCooldown = Rand.RangeInclusive(60, 180);
                    }
                }
                if (Find.TickManager.TicksGame - tickLastDoorCheck >= 96 && (GridsUtility.GetEdifice(driver.Position) is Building_Door || GridsUtility.GetEdifice(parent.Position) is Building_Door))
                {
                    lastPassedDoor = (((GridsUtility.GetEdifice(driver.Position) is Building_Door) ? GridsUtility.GetEdifice(driver.Position) : GridsUtility.GetEdifice(parent.Position)) as Building_Door);
                    lastPassedDoor.StartManualOpenBy(driver);
                    tickLastDoorCheck = Find.TickManager.TicksGame;
                }
                else if (Find.TickManager.TicksGame - tickLastDoorCheck >= 96 && lastPassedDoor != null)
                {
                    lastPassedDoor.StartManualCloseBy(driver);
                    lastPassedDoor = null;
                }
                parent.Position = (IntVec3Utility.ToIntVec3(Position));
                parent.Rotation = (driver.Rotation);
            }
        }

        public override IEnumerable<Command> CompGetGizmosExtra()
        {
            foreach (Command compCom in base.CompGetGizmosExtra())
                yield return compCom;

            Command_Action com = new Command_Action();

            if (IsMounted)
            {
                com.defaultLabel = txtCommandDismountLabel.Translate();
                com.defaultDesc = txtCommandDismountDesc.Translate();
                com.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconUnmount");
                com.activateSound = SoundDef.Named("Click");
                com.action = () => { Dismount(); };

                yield return com;
            }
            else
            {
                Designator_Mount designator = new Designator_Mount();

                designator.vehicle = parent;
                designator.defaultLabel = txtCommandMountLabel.Translate();
                designator.defaultDesc = txtCommandMountDesc.Translate();
                designator.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconMount");
                designator.activateSound = SoundDef.Named("Click");

                yield return designator;
            }
        }

        public IEnumerable<FloatMenuOption> CompGetFloatMenuOptionsForExtra(Pawn myPawn)
        {
            // order to drive
            Action action_Order;
            string verb;
            if (!IsMounted)
            {
                action_Order = () =>
                {
                    Find.Reservations.ReleaseAllForTarget(parent);
                    Find.Reservations.Reserve(myPawn, parent);
                    Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("Mount"), parent);
                    myPawn.drafter.TakeOrderedJob(jobNew);
                };
                verb = txtMountOn;
                yield return new FloatMenuOption(verb.Translate(parent.LabelShort), action_Order);
            }
            else if (IsMounted && myPawn == driver)
            {
                action_Order = () =>
                {
                    Dismount();
                };
                verb = txtDismount;
                yield return new FloatMenuOption(verb.Translate(parent.LabelShort), action_Order);
            }
        }

    }
}