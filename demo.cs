using System;
using Mono.Terminal;

class X {
	static void Main ()
	{
		Dialog a = new Dialog (10, 10, 80, 25, "Place");
		
		a.Add (new Label (0, 0, "Name:"));
		a.Add (new Entry (13, 10, 20, ""));
		
		a.Add (new Label (4, 11, "Address:"));
		a.Add (new Entry (13, 11, 20, ""));
		Application.Run (a);
	}
}
