using Mono.Terminal;
using System;

class X {
	static void Main ()
	{
		MainLoop ml = new MainLoop ();
		Stamp ("Start");
		ml.AddTimeout (TimeSpan.FromSeconds (1), x => {
			Stamp ("second");
			return true;
		});

	int i = 0;
		ml.AddTimeout (TimeSpan.FromSeconds (3), x => {
			Stamp ("three");
			if (++i >= 3)
				return false;
			return true;
		});
		
		ml.Run ();
	}

	static void Stamp (string txt)
	{
		Console.WriteLine ("{0} At {1}", txt, DateTime.Now);
	}
}