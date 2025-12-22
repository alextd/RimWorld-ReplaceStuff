using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Replace_Stuff
{
	public sealed class TickScheduler : GameComponent
	{
		private static TickScheduler _inst;

		private Queue<Action> _next = new();
		private Queue<Action> _current = new();

		private readonly SortedList<int, List<Action>> _due = new();

		public TickScheduler(Game game) : base()
		{
			_inst = this;
		}

		public static void NextTick(Action action)
		{
			if (action == null) return;
			_inst?._next.Enqueue(action);
		}

		public static void InTicks(int ticks, Action action)
		{
			if (action == null) return;
			if (ticks < 1) ticks = 1;
			int when = Find.TickManager.TicksGame + ticks;

			if (!_inst._due.TryGetValue(when, out var list))
			{
				list = new List<Action>(1);
				_inst._due.Add(when, list);
			}
			list.Add(action);
		}

		public override void GameComponentTick()
		{
			// 1) Run any actions scheduled for a specific tick.
			int now = Find.TickManager.TicksGame;
			while (_due.Count > 0 && _due.Keys[0] <= now)
			{
				var list = _due.Values[0];
				_due.RemoveAt(0);
				for (int i = 0; i < list.Count; i++) list[i]?.Invoke();
			}

			// 2) Swap queues so anything enqueued via NextTick during THIS tick
			//    will not run until the NEXT tick (true "next tick" behavior).
			(_current, _next) = (_next, _current);

			while (_current.Count > 0)
				_current.Dequeue()?.Invoke();
		}
	}
}
