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
			this.soundDragSustain = SoundDefOf.Designate_DragBuilding;
			this.soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			this.soundSucceeded = SoundDefOf.Designate_PlaceBuilding;

			this.defaultLabel = "TD.Replace".Translate();
			this.defaultDesc = "TD.ReplaceDesc".Translate();
			this.icon = TexDefOf.replaceIcon;
			this.iconProportions = new Vector2(1f, 1f);
			this.iconDrawScale = 1f;
			this.ResetStuffToDefault();

			this.hotKey = KeyBindingDefOf.Command_ColonistDraft;
		}

		public void ResetStuffToDefault()
		{
			stuffDef = ThingDefOf.WoodLog;
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth)
		{
			GizmoResult result = base.GizmoOnGUI(topLeft, maxWidth);

			float w = GetWidth(maxWidth);
			Rect rect = new Rect(topLeft.x + w / 2, topLeft.y, w / 2, Height / 2);
			Widgets.ThingIcon(rect, stuffDef);

			return result;
		}

		public override void DrawMouseAttachments()
		{
			base.DrawMouseAttachments();
			if (!ArchitectCategoryTab.InfoRect.Contains(UI.MousePositionOnUIInverted))
			{
				int cost = 0;
				foreach (IntVec3 cell in Find.DesignatorManager.Dragger.DragCells)
					cost += cell.GetThingList(Map).FindAll(t => CanReplaceStuffFor(stuffDef, t)).Sum(t => Mathf.RoundToInt((float)t.def.costStuffCount / stuffDef.VolumePerUnit));
				Vector2 drawPoint = Event.current.mousePosition + DragPriceDrawOffset;
				Rect iconRect = new Rect(drawPoint.x, drawPoint.y, 27f, 27f);
				GUI.DrawTexture(iconRect, stuffDef.uiIcon);

				Rect textRect = new Rect(drawPoint.x + 29f, drawPoint.y, 999f, 29f);
				string text = cost.ToString();
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.MiddleLeft;
				Widgets.Label(textRect, text);
				Text.Anchor = TextAnchor.UpperLeft;
				GUI.color = Color.white;
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
				IntVec3 mousePos = UI.MouseCell();
				if (mousePos.InBounds(Map))
					DrawGhost(CanDesignateCell(mousePos).Accepted ? new Color(0.5f, 1f, 0.6f, 0.4f) : new Color(1f, 0f, 0f, 0.4f));
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
			if (thing.Faction != Faction.OfPlayer && thing.Faction != null)
				return false;

			if (thing.Stuff == stuff || !thing.def.IsBuildingArtificial || thing.def.IsFrame || thing.def.designationCategory == null)
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
				t.SetFaction(Faction.OfPlayer);
				if (DebugSettings.godMode)
				{
					ReplaceFrame.FinalizeReplace(t, stuffDef);
				}
				else
				{
					GenReplace.PlaceReplaceFrame(t, stuffDef);
				}
			}
		}

		public override void DrawPanelReadout(ref float curY, float width)
		{
			Widgets.InfoCardButton(width - 24f - 6f, 6f, stuffDef);
			Text.Font = GameFont.Tiny;
		}

		public override void RenderHighlight(List<IntVec3> dragCells)
		{
			DesignatorUtility.RenderHighlightOverSelectableCells(this, dragCells);
		}
	}
}
