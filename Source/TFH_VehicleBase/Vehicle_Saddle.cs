﻿namespace TFH_VehicleBase
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using RimWorld;

    using TFH_VehicleBase.Components;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public class Vehicle_Saddle : ThingWithComps, IThingHolder
    {
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return this.innerContainer;
        }

        #region Variables

        // ==================================
        private const int maxNumBoarding = 1;

        // Graphic data
        private Graphic_Multi graphic_Saddle;

        // Body and part location
        private Vector3 saddleLoc;

        // mount and storage data
        public CompMountable MountableComp;

        public ThingOwner<Thing> innerContainer;

        public ThingOwner GetContainer()
        {
            return this.innerContainer;
        }

        public IntVec3 GetPosition()
        {
            return this.Position;
        }

        public int MaxItem
        {
            get
            {
                return (this.Rider != null) ? 3 : 2;
            }
        }

        public Pawn Rider
        {
            get
            {
                return (this.innerContainer.Where(x => x is Pawn).Count() > 0)
                           ? this.innerContainer.Where(x => x is Pawn).First() as Pawn
                           : null;
            }
        }

        public virtual void BoardOn(Pawn pawn)
        {
            if (this.MountableComp.IsMounted && (this.innerContainer.Count(x => x is Pawn) >= maxNumBoarding // No Space
                                            || (this.Faction != null
                                                && this.Faction != pawn.Faction))) // Not your vehicle
                return;

            if (pawn.Faction == Faction.OfPlayer && (pawn.needs.food.CurCategory == HungerCategory.Starving
                                                     || pawn.needs.rest.CurCategory == RestCategory.Exhausted))
            {
                Messages.Message(
                    pawn.LabelCap + "cannot board on " + this.LabelCap + ": " + pawn.LabelCap
                    + "is starving or exhausted",
                    MessageSound.RejectInput);
                return;
            }

            Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("StandBy"), this.MountableComp.Rider.Position, 4800);

            this.MountableComp.Rider.jobs.StartJob(jobNew, JobCondition.Incompletable);

            this.innerContainer.TryTransferToContainer(pawn, this.innerContainer, pawn.stackCount);

            // this.innerContainer.TryAdd(pawn);
            // pawn.holdingOwner = this.GetContainer();
            // pawn.holdingOwner.owner = this;
            pawn.jobs.StartJob(new Job(JobDefOf.WaitCombat));
        }

        public virtual void Unboard(Pawn pawn)
        {
            if (this.innerContainer.Count(x => x is Pawn) <= 0) return;

            Thing dummy;
            if (this.innerContainer.Contains(pawn))
            {
                pawn.holdingOwner = null;
                pawn.jobs.StopAll();
                this.innerContainer.TryDrop(pawn, this.Position, this.Map, ThingPlaceMode.Near, out dummy);
            }
        }

        public virtual void UnboardAll()
        {
            if (this.innerContainer.Count(x => x is Pawn) <= 0) return;

            Thing dummy;
            foreach (Pawn crew in this.innerContainer.Where(x => x is Pawn).ToList())
            {
                crew.holdingOwner = null;
                crew.jobs.StopAll();
                this.innerContainer.TryDrop(crew, this.Position, this.Map, ThingPlaceMode.Near, out dummy);
            }
        }

        #endregion

        #region Setup Work

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.MountableComp = this.GetComp<CompMountable>();
            this.innerContainer = new ThingOwner<Thing>(this, false);

            LongEventHandler.ExecuteWhenFinished(delegate { this.UpdateGraphics(); });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref this.innerContainer, "innerContainer", this);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            this.UnboardAll();
            this.innerContainer.TryDropAll(this.Position, this.Map, ThingPlaceMode.Near);

            if (mode == DestroyMode.Deconstruct) mode = DestroyMode.KillFinalize;
            base.Destroy(mode);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var baseGizmo in base.GetGizmos())
            {
                yield return baseGizmo;
            }

            if (this.MountableComp.IsMounted && this.innerContainer.Count(x => x is Pawn) < maxNumBoarding)
            {
                Designator_Board designatorBoard =
                    new Designator_Board
                        {
                            vehicle = this,
                            defaultLabel = "CommandRideLabel".Translate(),
                            defaultDesc = "CommandRideDesc".Translate(),
                            icon = Static.IconBoard,
                            activateSound = Static.ClickSound
                        };

                yield return designatorBoard;
            }

            if (this.MountableComp.IsMounted && this.innerContainer.Count(x => x is Pawn) >= maxNumBoarding)
            {
                Command_Action commandUnboardAll = new Command_Action();

                commandUnboardAll.defaultLabel = "CommandGetOffLabel".Translate();
                commandUnboardAll.defaultDesc = "CommandGetOffDesc".Translate();
                commandUnboardAll.icon = ContentFinder<Texture2D>.Get("UI/Commands/IconUnboardAll");
                commandUnboardAll.activateSound = Static.ClickSound;
                commandUnboardAll.action = () => { this.UnboardAll(); };

                yield return commandUnboardAll;

                // Designator_Move designator = new Designator_Move();
                // designator.driver = this.mountableComp.Driver;
                // designator.defaultLabel = "CommandMoveLabel".Translate();
                // designator.defaultDesc = "CommandMoveDesc".Translate();
                // designator.icon = ContentFinder<Texture2D>.Get("UI/Commands/ReleaseAnimals");
                // designator.activateSound = Static.ClickSound;
                // designator.hotKey = KeyBindingDefOf.Misc1;
                // yield return designator;
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            foreach (FloatMenuOption fmo in base.GetFloatMenuOptions(myPawn)) yield return fmo;

            if (this.MountableComp.IsMounted)
            {
                FloatMenuOption fmoBoard = new FloatMenuOption();

                fmoBoard.Label = "Ride".Translate(this.MountableComp.Rider.LabelCap);
                fmoBoard.Priority = MenuOptionPriority.High;
                fmoBoard.action = () =>
                    {
                        Job jobNew = new Job(DefDatabase<JobDef>.GetNamed("Board"), this);
                        myPawn.jobs.TryTakeOrderedJob(jobNew);
                    };
                if (this.innerContainer.Count(x => x is Pawn) >= 1)
                {
                    fmoBoard.Label = "AlreadyRide".Translate();
                    fmoBoard.Disabled = true;
                }

                yield return fmoBoard;
            }
        }

        /// <summary>
        /// Import the graphics
        /// </summary>
        private void UpdateGraphics()
        {
            this.graphic_Saddle = new Graphic_Multi();
            this.graphic_Saddle = GraphicDatabase.Get<Graphic_Multi>(
                                 "Things/Vehicles/VehicleSaddle/Saddle",
                                 this.def.graphic.Shader,
                                 this.def.graphic.drawSize,
                                 this.def.graphic.color,
                                 this.def.graphic.colorTwo) as Graphic_Multi;
        }

        #endregion

        #region Ticker

        // ==================================

        /// <summary>
        /// 
        /// </summary>
        public override void Tick()
        {
            base.Tick();

            if (this.innerContainer.Count == 0) return;

            foreach (Pawn crew in this.innerContainer.Where(x => x is Pawn))
            {
                if (crew.Downed || crew.Dead) this.Unboard(crew);
                crew.Position = this.Position;
            }

            if (!this.MountableComp.IsMounted) this.UnboardAll();
        }

        #endregion

        #region Graphics / Inspections

        // ==================================

        /// <summary>
        /// 
        /// </summary>
        public override Vector3 DrawPos
        {
            get
            {
                if (!this.MountableComp.IsMounted || !this.Spawned) return base.DrawPos;
                return this.MountableComp.drawPosition;
            }
        }

        public override void DrawAt(Vector3 drawLoc, bool flip)
        {
            if (!this.Spawned)
            {
                base.DrawAt(drawLoc);
                return;
            }

            this.saddleLoc = drawLoc;
            this.saddleLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn) + 0.01f;

            if (this.MountableComp.IsMounted)
            {
                this.graphic_Saddle.Draw(this.saddleLoc, this.Rotation, this);
                Vector3 crewLoc = drawLoc;
                crewLoc.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn);
                Vector3 crewsOffset = new Vector3(0.25f, 0.02f, -0.25f);
                if (this.Rotation == Rot4.North || this.Rotation == Rot4.South) crewsOffset.x = 0f;
                foreach (Pawn pawn in this.innerContainer.Where(x => x is Pawn).ToList())
                {
                    pawn.Rotation = this.Rotation;
                    pawn.DrawAt(crewLoc + crewsOffset.RotatedBy(this.Rotation.AsAngle));
                    Stance_Warmup stance_Warmup = null;
                    if (pawn.stances.curStance is Stance_Warmup && Find.Selector.IsSelected(this))
                    {
                        stance_Warmup = pawn.stances.curStance as Stance_Warmup;
                        float pieSizeFactor;
                        if (stance_Warmup.ticksLeft < 300) pieSizeFactor = 1f;
                        else if (stance_Warmup.ticksLeft < 450) pieSizeFactor = 0.75f;
                        else pieSizeFactor = 0.5f;
                        GenDraw.DrawAimPie(
                            stance_Warmup.stanceTracker.pawn,
                            stance_Warmup.focusTarg,
                            (int)(stance_Warmup.ticksLeft * (double)pieSizeFactor),
                            0.2f);
                    }
                }
            }
            else base.DrawAt(drawLoc);
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            stringBuilder.AppendLine("Rider".Translate());
            foreach (Pawn pawn in this.innerContainer.Where(x => x is Pawn).ToList())
                stringBuilder.Append(pawn.LabelCap.Translate() + ", ");
            stringBuilder.Remove(stringBuilder.Length - 3, 1);
            return stringBuilder.ToString();
        }

        #endregion
    }
}