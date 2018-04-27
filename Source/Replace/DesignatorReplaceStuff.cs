using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace Replace_Stuff
{
	[StaticConstructorOnStartup]
	public static class TexDefOf
	{
		public static Texture2D replaceIcon = ContentFinder<Texture2D>.Get("ReplaceStuff", true);
	}

	public class Designator_ReplaceStuff : Designator
	{
		private ThingDef stuffDef;

		public override int DraggableDimensions
		{
			get
			{
				return 2;
			}
		}

		private static readonly Vector2 DragPriceDrawOffset = new Vector2(19f, 17f);

		public Designator_ReplaceStuff()
		{
			this.soundDragSustain = SoundDefOf.DesignateDragBuilding;
			this.soundDragChanged = SoundDefOf.DesignateDragBuildingChanged;
			this.soundSucceeded = SoundDefOf.DesignatePlaceBuilding;

			this.defaultLabel = "Replace";
			this.defaultDesc = "Replace stuff of a thing, e.g. replace a wood wall with stone";
			this.icon = TexDefOf.replaceIcon;
			this.iconProportions = new Vector2(1f, 1f);
			this.iconDrawScale = 1f;
			this.ResetStuffToDefault();

			this.hotKey = KeyBindingDefOf.CommandColonistDraft;
		}

		public void ResetStuffToDefault()
		{
			stuffDef = ThingDefOf.WoodLog;
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft)
		{
			GizmoResult result = base.GizmoOnGUI(topLeft);

			Rect rect = new Rect(topLeft.x + Width / 2, topLeft.y, Width / 2, Height / 2);
			Widgets.ThingIcon(rect, stuffDef);

			return result;
		}

		public override void DrawMouseAttachments()
		{
			base.DrawMouseAttachments();
			if (!ArchitectCategoryTab.InfoRect.Contains(UI.MousePositionOnUIInverted))
			{
				DesignationDragger dragger = Find.DesignatorManager.Dragger;
				int cellCount = dragger.Dragging ? cellCount = dragger.DragCells.Count<IntVec3>() : 1;
				float yNext = 0f;
				Vector2 drawPoint = Event.current.mousePosition + DragPriceDrawOffset;

				List<ThingCountClass> list = new List<ThingCountClass>();
				list.Add(new ThingCountClass(ThingDefOf.WoodLog, 5));
				for (int i = 0; i < list.Count; i++)
				{
					ThingCountClass thingCountClass = list[i];
					float y = drawPoint.y + yNext;
					Rect position = new Rect(drawPoint.x, y, 27f, 27f);
					GUI.DrawTexture(position, thingCountClass.thingDef.uiIcon);
					Rect rect = new Rect(drawPoint.x + 29f, y, 999f, 29f);
					int num3 = cellCount * thingCountClass.count;

					string text = "Stuff";
					Text.Font = GameFont.Small;
					Text.Anchor = TextAnchor.MiddleLeft;
					Widgets.Label(rect, text);
					Text.Anchor = TextAnchor.UpperLeft;
					GUI.color = Color.white;
					yNext += 29f;
				}
			}
		}

		public override void ProcessInput(Event ev)
		{
			if (!CheckCanInteract())
			{
				return;
			}

			List<FloatMenuOption> list = new List<FloatMenuOption>();
			foreach (ThingDef current in Map.resourceCounter.AllCountedAmounts.Keys)
			{
				if (current.IsStuff && (DebugSettings.godMode || Map.listerThings.ThingsOfDef(current).Count > 0))
				{
					list.Add(new FloatMenuOption(current.LabelCap, delegate
					{
						base.ProcessInput(ev);
						Find.DesignatorManager.Select(this);
						stuffDef = current;
					}));
				}
			}
			if (list.Count == 0)
			{
				Messages.Message("NoStuffsToBuildWith".Translate(), MessageTypeDefOf.RejectInput);
			}
			else
			{
				FloatMenu floatMenu = new FloatMenu(list);
				floatMenu.vanishIfMouseDistant = true;
				Find.WindowStack.Add(floatMenu);
				Find.DesignatorManager.Select(this);
			}
		}

		public override void SelectedUpdate()
		{
			GenDraw.DrawNoBuildEdgeLines();
			if (!ArchitectCategoryTab.InfoRect.Contains(UI.MousePositionOnUIInverted))
			{
				IntVec3 intVec = UI.MouseCell();
				Color ghostCol;
				if (CanDesignateCell(intVec).Accepted)
				{
					ghostCol = new Color(0.5f, 1f, 0.6f, 0.4f);
				}
				else
				{
					ghostCol = new Color(1f, 0f, 0f, 0.4f);
				}
				DrawGhost(ghostCol);
			}
		}

		protected virtual void DrawGhost(Color ghostCol)
		{
			GhostDrawer.DrawGhostThing(UI.MouseCell(), Rot4.North, stuffDef, null, ghostCol, AltitudeLayer.Blueprint);
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 cell)
		{
			return CanReplaceStuffAt(stuffDef, cell, Map);
		}

		public static bool CanReplaceStuffAt(ThingDef stuff, IntVec3 cell, Map map)
		{
			return cell.GetThingList(map).Any(t => t.Position == cell && CanReplaceStuffFor(stuff, t));
		}

		public static bool CanReplaceStuffFor(ThingDef stuff, Thing thing)
		{
			if (thing.Faction != Faction.OfPlayer || thing.Stuff == stuff || !thing.def.IsBuildingArtificial || thing.def.IsFrame || thing.def.designationCategory == null)
				return false;

			foreach (ThingDef def in GenStuff.AllowedStuffsFor(thing.def))
				if (def == stuff)
					return true;
			return false;
		}

		public override void DesignateSingleCell(IntVec3 cell)
		{
			List<Thing> things = cell.GetThingList(Map).FindAll(t => CanReplaceStuffFor(stuffDef, t));
			for(int i=0; i<things.Count; i++)
			{
				Thing t = things[i];
				if (DebugSettings.godMode)
				{
					ReplaceFrame.FinalizeReplace(t, stuffDef);
				}
				else
				{
					ReplaceFrame frame = GenReplace.PlaceReplaceFrame(t, Faction.OfPlayer, stuffDef);
				}
			}
		}

		public override void DrawPanelReadout(ref float curY, float width)
		{
			Widgets.InfoCardButton(width - 24f - 6f, 6f, stuffDef);
			Text.Font = GameFont.Tiny;
		}
	}
}
