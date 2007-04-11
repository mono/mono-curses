using System;
using Mono.Terminal;

class X {
	static void Main ()
	{
		Container a = new Container (1, 1, 80, 25);
		//Dialog a = new Dialog (1, 1, 80, 25, "Demo");

		Frame f = new Frame (0,  0, 40, 15, "Torrents");
		a.Add (f);
		a.Add (new Frame (40,  0, 40, 15, "Status"));
		a.Add (new Frame (0,  15, 40, 10, "Details"));
		a.Add (new Frame (40, 15, 40, 10, "Progress"));

		f.Add (new Label (8, 0, "Name:"));
		f.Add (new Entry (13, 0, 20, "First"));
		
		f.Add (new Label (4, 1, "Address:"));
		f.Add (new Entry (13, 1, 20, "Second"));

		
		Application.Run (a);
	}
}
