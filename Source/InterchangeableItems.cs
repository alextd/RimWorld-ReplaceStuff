using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Replace_Stuff
{
	public class InterchangeableItems : Def
	{
		public List<ReplaceList> replaceLists = new();
	}

	public class ReplaceList
	{
		public string category = "";
		public List<ThingDef> items = new();
		public List<string> comps = new();
	}
}
