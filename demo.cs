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

		f.Add (new Label (1, 1, "Torrent: "));
		f.Add (new Button (10, 1, "Add"));
		f.Add (new Button (18, 1, "Pause"));
		f.Add (new Button (28, 1, "Remove"));
		
		f.Add (new Label (7,  3, "Name:"));
		f.Add (new Entry (13, 3, 20, "First"));
		
		f.Add (new Label (4,  5, "Address:"));
		f.Add (new Entry (13, 5, 20, "Second"));

		f.Add (new ListView (10, 10, 10, 10));
		
		f = new Frame (midx, midy, midx-1, midy-1, "Progress");
		a.Add (f);

		// For testing focus, not ready
		//f.Add (new Label (0, 0, "->0<-"));
		//f.Add (new Entry  (7, 0, 20, "Another"));
		
		Application.Run (a);
	}
}
