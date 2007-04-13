using System;
using Mono.Terminal;

class X {
	static void Main ()
	{
		Application.Init (false);
		
		Container a = new Container (1, 1, Application.Cols, Application.Lines);
		//Dialog a = new Dialog (1, 1, 80, 25, "Demo");

		int midx = Application.Cols/2;
		int midy = Application.Lines/2;
		Frame f = new Frame (0,  0, midx, midy, "Torrents");
		a.Add (f);
		a.Add (new Frame (midx,  0, midx-1, midy, "Status"));
		a.Add (new Frame (0,  midy, midx, midy-1, "Details"));
		a.Add (new Frame (midx, midy, midx-1, midy-1, "Progress"));

		f.Add (new Label (1, 1, "Torrent: "));
		f.Add (new Button (10, 1, "Add"));
		f.Add (new Button (18, 1, "Pause"));
		f.Add (new Button (28, 1, "Remove"));
		
		f.Add (new Label (8,  6, "Name:"));
		f.Add (new Entry (13, 6, 20, "First"));
		
		f.Add (new Label (4,  8, "Address:"));
		f.Add (new Entry (13, 8, 20, "Second"));

		
		Application.Run (a);
	}
}
