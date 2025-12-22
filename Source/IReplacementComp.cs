using Verse;

namespace Replace_Stuff
{
	public interface IReplacementComp
	{
		abstract void PreAction(Thing newThing, Thing oldThing);

		abstract void PostAction(Thing newThing, Thing oldThing);
	}
}