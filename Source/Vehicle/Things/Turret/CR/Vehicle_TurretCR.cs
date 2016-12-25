﻿#if CR
#if Headlights
using ppumkin.LEDTechnology.Managers;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Combat_Realism;
using RimWorld;
using ToolsForHaul.Components;
using ToolsForHaul.JobDefs;
using ToolsForHaul.StatDefs;
using ToolsForHaul.Utilities;
using UnityEngine;
using Verse;
using Verse.AI;
namespace ToolsForHaul
{
    [StaticConstructorOnStartup]
    public class Vehicle_Turret : Building_Reloadable, IAttackTarget
    {

#region Variables
        // ==================================

        public bool instantiated;


        public bool ThreatDisabled()
        {
            if (GetComp<CompExplosive>().wickStarted)
                return true;

            if (!mountableComp.IsMounted)
                return true;

            CompPowerTrader comp = GetComp<CompPowerTrader>();
            if (comp == null || !comp.PowerOn)
            {
                CompMannable comp2 = GetComp<CompMannable>();
                if (comp2 == null || !comp2.MannedNow)
                {
                    return !mountableComp.IsMounted;
                    return true;
                }
            }
            return false;

        }

        // TODO make vehicles break down & get repaired like buildings
        public override bool ClaimableBy(Faction faction)
        {
            if (!mountableComp.IsMounted)
            {
                return true;
            }

            CompPowerTrader comp = GetComp<CompPowerTrader>();
            if (comp == null || !comp.PowerOn)
            {
                CompMannable comp2 = GetComp<CompMannable>();
                if (comp2 == null || !comp2.MannedNow)
                {
                    return true;
                }
            }
            return false;
        }

        public float DesiredSpeed
        {
            get
            {
                return this.GetStatValue(HaulStatDefOf.VehicleSpeed);
            }
        }
        public bool IsCurrentlyMotorized()
        {
            return (refuelableComp != null && refuelableComp.HasFuel) || vehicleComp.MotorizedWithoutFuel();
        }

        public bool fueledByAI;
        private bool fuelSpilled;
        private int tickCheck = Find.TickManager.TicksGame;

        private int tickCooldown = 60;
        private float _puddleHitpointCounter;


        //Body and part location
        private Vector3 wheelLoc;
        private Vector3 bodyLoc;


        //mount and storage data
        // public ThingContainer storage;
        // public ThingContainer GetContainer() { return storage; }
        public IntVec3 GetPosition() { return Position; }

        //slotGroupParent Interface
        //    public ThingFilter allowances;

        public CompMountable mountableComp => GetComp<CompMountable>();

        public CompRefuelable refuelableComp => GetComp<CompRefuelable>();

        public CompExplosive explosiveComp => GetComp<CompExplosive>();

        public CompBreakdownable breakdownableComp => GetComp<CompBreakdownable>();

        public CompAxles axlesComp => GetComp<CompAxles>();

        public CompVehicle vehicleComp => GetComp<CompVehicle>();

#endregion

#region Setup Work


        public Vehicle_Turret()
        {
            // current spin degree
            //Inventory Initialize. It should be moved in constructor
            //storage = new ThingContainer(this);
            //allowances = new ThingFilter();
            //allowances.SetFromPreset(StorageSettingsPreset.DefaultStockpile);
            //allowances.SetFromPreset(StorageSettingsPreset.DumpingStockpile);
            stunner = new StunHandler(this);
        }

        static Vehicle_Turret()
        {
        }


        //    public static ListerVehicles listerVehicles = new ListerVehicles();

#if Headlights
        HeadLights flooder;
#endif
        public override void SpawnSetup()
        {
            base.SpawnSetup();

            ToolsForHaulUtility.CartTurret.Add(this);

   

        }

        public override void DeSpawn()
        {
            //bool hasChair = false;
            //foreach (Pawn pawn in Find.MapPawns.AllPawnsSpawned)
            //{
            //    if (pawn.health.hediffSet.HasHediff(HediffDef.Named("HediffWheelChair")) &&
            //        !ToolsForHaulUtility.IsDriver(pawn) && base.Position.AdjacentTo8WayOrInside(pawn.Position))
            //    {
            //        mountableComp.MountOn(pawn);
            //        hasChair = true;
            //        break;
            //    }
            //}
            //if (!hasChair)
            //{
            if (ToolsForHaulUtility.CartTurret.Contains(this))
                ToolsForHaulUtility.CartTurret.Remove(this);

            if (mountableComp.IsMounted)
                if (MapComponent_ToolsForHaul.currentVehicle.ContainsKey(mountableComp.Driver))
                    MapComponent_ToolsForHaul.currentVehicle.Remove(mountableComp.Driver);

            if (mountableComp.sustainerAmbient != null)
                mountableComp.sustainerAmbient.End();
            base.DeSpawn();

            // not working
            //if (explosiveComp != null && explosiveComp.wickStarted)
            //    if (tankLeaking)
            //    {
            //        FireUtility.TryStartFireIn(Position, 0.3f);
            //        if (Rand.Value > 0.8f)
            //        {
            //            FireUtility.TryStartFireIn(Position.RandomAdjacentCell8Way(), 0.1f);
            //        }
            //        if (Rand.Value > 0.7f)
            //        {
            //            FireUtility.TryStartFireIn(Position.RandomAdjacentCell8Way(), 0.1f);
            //        }
            //        if (Rand.Value > 0.6f)
            //        {
            //            FireUtility.TryStartFireIn(Position.RandomAdjacentCell8Way(), 0.1f);
            //        }
            //        if (Rand.Value > 0.5f)
            //        {
            //            FireUtility.TryStartFireIn(Position.RandomAdjacentCell8Way(), 0.1f);
            //        }
            //
            //    }
            //}
        }


        public override void ExposeData()
        {
            base.ExposeData();
            //       Scribe_Deep.LookDeep(ref storage, "storage");
            //      Scribe_Deep.LookDeep(ref allowances, "allowances");
            Scribe_Values.LookValue(ref vehicleComp.tankLeaking, "tankLeaking");
            Scribe_Values.LookValue(ref _tankHitPos, "tankHitPos");

            Scribe_Deep.LookDeep(ref stunner, "stunner", this);
            Scribe_Values.LookValue(ref mountableComp.lastDrawAsAngle, "lastDrawAsAngle");
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {

            foreach (Gizmo baseGizmo in base.GetGizmos())
                yield return baseGizmo;

            if (Faction != Faction.OfPlayer)
                yield break;

            if (explosiveComp != null)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.icon = ContentFinder<Texture2D>.Get("UI/Commands/Detonate");
                command_Action.defaultDesc = "CommandDetonateDesc".Translate();
                command_Action.action = Command_Detonate;
                if (explosiveComp.wickStarted)
                {
                    command_Action.Disable();
                }
                command_Action.defaultLabel = "CommandDetonateLabel".Translate();
                yield return command_Action;
            }
            // yield return new Command_Action
            // {
            //     defaultLabel = "CommandBedSetOwnerLabel".Translate(),
            //     icon = ContentFinder<Texture2D>.Get("UI/Commands/AssignOwner", true),
            //     defaultDesc = "CommandGraveAssignColonistDesc".Translate(),
            //     action = delegate
            //     {
            //         Find.WindowStack.Add(new Dialog_AssignBuildingOwner(this));
            //     },
            //     hotKey = KeyBindingDefOf.Misc3
            // };
        }

        private void Command_Detonate()
        {
            explosiveComp.StartWick();
        }



        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            Action action_Dismount;
            Action action_Mount;
            Action action_DismountInBase;
            Action action_MakeMount;
            Action action_Deconstruct;

            // do nothing if not of colony
            if (Faction != Faction.OfPlayer)
                yield break;

            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn))
                yield return fmo;

            action_Mount = () =>
            {
                Job jobNew = new Job(HaulJobDefOf.Mount);
                Find.Reservations.ReleaseAllForTarget(this);
                jobNew.targetA = this;
                myPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
            };
            action_DismountInBase = () =>
            {
                Job jobNew = ToolsForHaulUtility.DismountInBase(mountableComp.Driver, MapComponent_ToolsForHaul.currentVehicle[mountableComp.Driver]);

                myPawn.jobs.StartJob(jobNew, JobCondition.InterruptForced);
            };
            action_Dismount = () =>
            {
                if (!myPawn.Position.InBounds())
                {
                    mountableComp.DismountAt(myPawn.Position);
                    return;
                }

                mountableComp.DismountAt(myPawn.Position - def.interactionCellOffset.RotatedBy(myPawn.Rotation));
                myPawn.Position = myPawn.Position.RandomAdjacentCell8Way();
                // mountableComp.DismountAt(myPawn.Position - VehicleDef.interactionCellOffset.RotatedBy(myPawn.Rotation));
            };

            action_MakeMount = () =>
            {
                Pawn worker = null;
                Job jobNew = new Job(HaulJobDefOf.MakeMount);
                Find.Reservations.ReleaseAllForTarget(this);
                jobNew.count = 1;
                jobNew.targetA = this;
                jobNew.targetB = myPawn;
                foreach (Pawn colonyPawn in Find.MapPawns.FreeColonistsSpawned)
                    if (colonyPawn.CurJob.def != jobNew.def && (worker == null || (worker.Position - myPawn.Position).LengthHorizontal > (colonyPawn.Position - myPawn.Position).LengthHorizontal))
                        worker = colonyPawn;
                if (worker == null)
                {
                    Messages.Message("NoWorkForMakeMount".Translate(), MessageSound.RejectInput);
                }
                else worker.jobs.StartJob(jobNew, JobCondition.InterruptForced);
            };



            action_Deconstruct = () =>
            {
                Find.Reservations.ReleaseAllForTarget(this);
                Find.Reservations.Reserve(myPawn, this);
                Find.DesignationManager.AddDesignation(new Designation(this, DesignationDefOf.Deconstruct));
                Job job = new Job(JobDefOf.Deconstruct, this);
                myPawn.jobs.StartJob(job, JobCondition.InterruptForced);
            };
            bool alreadyMounted = false;
            if (!mountableComp.IsMounted)
            {
                foreach (Vehicle_Turret cart in ToolsForHaulUtility.CartTurret)
                    if (cart.mountableComp.Driver == myPawn)
                        alreadyMounted = true;

                if (myPawn.Faction == Faction.OfPlayer && (myPawn.RaceProps.IsMechanoid || myPawn.RaceProps.Humanlike) && !alreadyMounted && !this.IsForbidden(myPawn.Faction))
                {
                    yield return new FloatMenuOption("Mount".Translate(LabelShort), action_Mount);
                }

                yield return new FloatMenuOption("Deconstruct".Translate(LabelShort), action_Deconstruct);

            }
            else if (myPawn == mountableComp.Driver)
            {
                yield return new FloatMenuOption("Dismount".Translate(LabelShort), action_Dismount);
            }
            yield return new FloatMenuOption("DismountInBase".Translate(LabelShort), action_DismountInBase);


        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode == DestroyMode.Deconstruct)
                mode = DestroyMode.Kill;
            //else if (explosiveComp != null && explosiveComp.wickStarted)
            //{
            //    storage.ClearAndDestroyContents();
            //
            //}

            //     storage.TryDropAll(Position, ThingPlaceMode.Near);

            base.Destroy(mode);
        }
        private ThingDef fuelDefName = ThingDef.Named("Puddle_BioDiesel_Fuel");

        private int tankHitCount;

        /// <summary>
        /// PreApplyDamage from Building_Turret - not sure what the stunner does
        /// </summary>
        /// <param name="dinfo"></param>
        /// <param name="absorbed"></param>
        public override void PreApplyDamage(DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(dinfo, out absorbed);
            if (absorbed)
            {
                return;
            }
            stunner.Notify_DamageApplied(dinfo, true);
            absorbed = false;
        }





#endregion

#region Ticker
        // ==================================

        /// <summary>
        /// 
        /// </summary>
        public override void Tick()
        {
            if (!instantiated)
            {
                //foreach (Thing thing in GetContainer())
                //{
                //    thing.holdingContainer.owner = this;
                //}

                currentDriverSpeed = VehicleSpeed;
                instantiated = true;
            }

            base.Tick();

#region Headlights
#if Headlights
            {
                if (Find.GlowGrid.GameGlowAt(Position - Rotation.FacingCell - Rotation.FacingCell) < 0.4f)
                {
                    // TODO Add headlights to xml & move the flooder initialization to mountableComp
                    if (mountableComp.Driver != null && !compVehicles.AnimalsCanDrive() && flooder == null)
                    {
                        flooder = new HeadLights(Position, Rotation, this);
                        CustomGlowFloodManager.RegisterFlooder(flooder);
                        CustomGlowFloodManager.RefreshGlowFlooders();
                    }
                    if (mountableComp.Driver == null && flooder != null)
                    {
                        flooder.Clear();
                        CustomGlowFloodManager.DeRegisterGlower(flooder);
                        CustomGlowFloodManager.RefreshGlowFlooders();
                        flooder = null;
                    }
                    // TODO optimized performance, lights only at night and when driver is mounted => light switch gizmo?
                    if (flooder != null)
                    {
                        flooder.Position = Position + Rotation.FacingCell + Rotation.FacingCell;
                        flooder.Orientation = Rotation;
                        flooder.Clear();
                        flooder.CalculateGlowFlood();
                    }
                }
                if (mountableComp.Driver == null && flooder != null || flooder != null)
                {
                    CustomGlowFloodManager.DeRegisterGlower(flooder);
                    CustomGlowFloodManager.RefreshGlowFlooders();
                    flooder = null;
                }
            }
#endif
#endregion

            if (mountableComp.IsMounted)
            {

                if (refuelableComp != null)
                {
                    if (mountableComp.Driver.Faction != Faction.OfPlayer)
                        if (!fueledByAI)
                        {
                            if (refuelableComp.FuelPercentOfMax < 0.550000011920929)
                                refuelableComp.Refuel(
                                    ThingMaker.MakeThing(refuelableComp.Props.fuelFilter.AllowedThingDefs.FirstOrDefault()));
                            else
                                fueledByAI = true;
                        }
                }

                



                //Exhaustion fumes - basic
                // only fumes on vehicles with combustion and no animals driving
                if (!vehicleComp.MotorizedWithoutFuel() && !vehicleComp.AnimalsCanDrive())
                    MoteMaker.ThrowSmoke(DrawPos + FumesOffset, 0.05f + currentDriverSpeed * 0.01f);

                if (Find.TickManager.TicksGame - tickCheck >= tickCooldown)
                {
                    if (mountableComp.Driver.pather.Moving)
                    {
                        if (!mountableComp.Driver.stances.FullBodyBusy)
                        {
                            if (refuelableComp != null)
                                refuelableComp.Notify_UsedThisTick();
                            damagetick -= 1;

                            if (axlesComp.HasAxles())
                                currentDriverSpeed = ToolsForHaulUtility.GetMoveSpeed(mountableComp.Driver);
                        }
                        if (breakdownableComp != null && breakdownableComp.BrokenDown ||
                            refuelableComp != null && !refuelableComp.HasFuel)
                            VehicleSpeed = 0.75f;
                        else VehicleSpeed = DesiredSpeed;
                        tickCheck = Find.TickManager.TicksGame;
                    }

                    if (Position.InNoBuildEdgeArea() && compvehicles.despawnAtEdge && Spawned && (mountableComp.Driver.Faction != Faction.OfPlayer || mountableComp.Driver.MentalState.def == MentalStateDefOf.PanicFlee))
                        DeSpawn();
                }

            }

            //if (Find.TickManager.TicksGame >= damagetick)
            //{
            //    TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 1, null, null, null));
            //    damagetick = Find.TickManager.TicksGame + 3600;
            //}

            if (vehicleComp.tankLeaking)
            {
                if (Find.TickManager.TicksGame > _tankSpillTick)
                {
                    if (refuelableComp.FuelPercentOfMax > _tankHitPos)
                    {
                        refuelableComp.ConsumeFuel(0.15f);

                        FilthMaker.MakeFilth(Position, fuelDefName, LabelCap);
                        _tankSpillTick = Find.TickManager.TicksGame + 15;
                    }
                }
            }
        }

        private static readonly Vector3 TrailOffset = new Vector3(0f, 0f, -0.3f);
        private static readonly Vector3 FumesOffset = new Vector3(-0.3f, 0f, 0f);
        private static readonly Vector3 DustOffset = new Vector3(-0.3f, 0f, -0.3f);

        private Vector3 _lastFootprintPlacePos;
        private const float FootprintIntervalDist = 0.7f;

        private float _tankHitPos = 1f;
        int damagetick = -5000;

        private int _tankSpillTick = -5000;

#endregion

#region Graphics / Inspections
        // ==================================

        //private void UpdateGraphics()
        //{
        //}


        public override Vector3 DrawPos
        {
            get
            {
                if (!Spawned || !mountableComp.IsMounted || !instantiated || vehicleComp==null)
                {
                    return base.DrawPos;
                }
                float num = mountableComp.Driver.Drawer.renderer.graphics.nakedGraphic.drawSize.x - 1f;
                num *= mountableComp.Driver.Rotation.AsInt % 2 == 1 ? 0.5f : 0.25f;
                Vector3 vector = new Vector3(0f, 0f, -num);
                return mountableComp.Position + vector.RotatedBy(mountableComp.Driver.Rotation.AsAngle);
            }
        }

        public override void DrawAt(Vector3 drawLoc)
        {
            if (!Spawned)
            {
                base.DrawAt(drawLoc);
                return;
            }

            wheelLoc = drawLoc;
            bodyLoc = drawLoc;
            //Vertical
            if (Rotation.AsInt % 2 == 0)
                wheelLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Item) + 0.02f;

            //horizontal 
            if (axlesComp.HasAxles() && Rotation.AsInt % 2 == 1)
            {
                wheelLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.04f;
                bodyLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.03f;

                Vector2 drawSize = def.graphic.drawSize;
                int num = Rotation == Rot4.West ? -1 : 1;
                Vector3 vector3 = new Vector3(1f * drawSize.x, 1f, 1f * drawSize.y);
                Quaternion asQuat = Rotation.AsQuat;
                float x = 1f * Mathf.Sin(num * (axlesComp.wheelRotation * 0.05f) % (2 * Mathf.PI));
                float z = 1f * Mathf.Cos(num * (axlesComp.wheelRotation * 0.05f) % (2 * Mathf.PI));

                asQuat.SetLookRotation(new Vector3(x, 0f, z), Vector3.up);


                wheelLoc.z = wheelLoc.z + axlesComp.wheel_shake;

                Vector3 mountThingLoc = drawLoc;
                mountThingLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.06f;
                Vector3 mountThingOffset = new Vector3(0, 0, 1).RotatedBy(Rotation.AsAngle);

                List<Vector3> list;
                if (axlesComp.GetAxleLocations(drawSize, num, out list))
                {
                    foreach (Vector3 current in list)
                    {
                        Matrix4x4 matrix = default(Matrix4x4);
                        matrix.SetTRS(wheelLoc + current, asQuat, vector3);
                        Graphics.DrawMesh(MeshPool.plane10, matrix, axlesComp.graphic_Wheel_Single.MatAt(Rotation), 0);
                    }
                }
            }
            base.DrawAt(bodyLoc);
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());

            string currentDriverString;
            if (mountableComp.Driver != null)
                currentDriverString = mountableComp.Driver.LabelCap;
            else
                currentDriverString = "NoDriver".Translate();

            stringBuilder.AppendLine("Driver".Translate() + ": " + currentDriverString);
            if (vehicleComp.tankLeaking)
                stringBuilder.AppendLine("TankLeaking".Translate());
            //string text = storage.ContentsString;
            //stringBuilder.AppendLine(string.Concat(new object[]
            //{
            //"InStorage".Translate(),
            //": ",
            //text
            //}));
            return stringBuilder.ToString();
        }
#endregion

        public IEnumerable<Pawn> AssigningCandidates
        {
            get
            {
                return Find.MapPawns.FreeColonists;
            }
        }



        public int MaxAssignedPawnsCount
        {
            get
            {
                return 2;
            }
        }
    }
}
#endif