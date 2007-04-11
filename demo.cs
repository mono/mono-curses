using System;
using Mono.Terminal;

class X {
	static void Main ()
	{
		Application.Init ();
		
		Container a = new Container (1, 1, Application.Cols, Application.Lines);
		//Dialog a = new Dialog (1, 1, 80, 25, "Demo");

		int midx = Application.Cols/2;
		int midy = Application.Lines/2;
		Frame f = new Frame (0,  0, midx, midy, "Torrents");
		a.Add (f);
		a.Add (new Frame (midx,  0, midx-1, midy, "Status"));
		a.Add (new Frame (0,  midy, midx, midy-1, "Details"));
		a.Add (new Frame (midx, midy, midx-1, midy-1, "Progress"));

		f.Add (new Label (8, 0, "Name:"));
		f.Add (new Entry (13, 0, 20, "First"));
		
		f.Add (new Label (4, 1, "Address:"));
		f.Add (new Entry (13, 1, 20, "Second"));

		
		Application.Run (a);
	}
}
