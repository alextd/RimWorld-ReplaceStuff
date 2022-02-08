using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace Replace_Stuff.CoolersOverWalls
{
	//Designator_Build and _Dropdown both create their own dropdown list but only one shows
	//That'll be _build designators with things made from stuff
	//So let's remove any dropdown list if it holds things made from stuff
	//(if a mod makes coolers made from stuff, overwall coolers and their 2-wide version fit this description)
	public static class DesignatorBuildDropdownStuffFix
	{
		public static void SanityCheck()
		{
			//Either patch the method that created the dropdown designator, or just undo it here:
			foreach (var catDef in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
				for (int i = 0; i < catDef.AllResolvedDesignators.Count; i++)
					if (catDef.AllResolvedDesignators[i] is Designator_Dropdown des
						&& des.Elements.Any(d => d is Designator_Build db && db.PlacingDef.MadeFromStuff))
					{
						catDef.AllResolvedDesignators.RemoveAt(i);
						foreach (var dropDes in des.Elements)
							catDef.AllResolvedDesignators.Insert(i, dropDes);
					}
		}
	}
}
