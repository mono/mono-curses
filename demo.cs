using System;
using Mono.Terminal;
using System.Collections;
class X {
	public class TorrentList : IListProvider {
		public ArrayList items = new ArrayList ();
		ListView view;

		public void SetListView (ListView v)
		{
			view = v;
		}
		
		public int Items {
			get {
				return items.Count;
			}
		}

		public void Add (string s)
		{
			items.Add (s);
			if (view != null)
				view.ProviderChanged ();
		}
		
		public bool AllowMark {
			get {
				return false;
			}
		}

		public bool IsMarked (int n)
		{
			return false;
		}

		public void Render (int line, int col, int width, int item)
		{
			string s = String.Format ("{0}. {1}", item, items [item]);
			if (s.Length > width){
				s = s.Substring (0, width);
				Curses.addstr (s);
			} else {
				Curses.addstr (s);
				for (int i = s.Length; i < width; i++)
					Curses.addch (' ');
			}
		}
	}
	
	static TorrentList tl;
	
	static void AddDialog ()
	{
		int cols = (int) (Application.Cols * 0.7);
		Dialog d = new Dialog (cols, 8, "Add");
		Entry e;
		string name = null;
		
		
		d.Add (new Label (1, 0, "Torrent file:"));
		e = new Entry (1, 1, cols - 6, Environment.CurrentDirectory);
		d.Add (e);

		// buttons
		Button b = new Button (0, 0, "Ok", true);
		b.Clicked += delegate {
			b.Container.Running = false;
			name = e.Text;
		};
		d.AddButton (b);
		b = new Button (0, 0, "Cancel");
		b.Clicked += delegate {
			b.Container.Running = false;
		};
		d.AddButton (b);
		
		Application.Run (d);

		if (name != null){
			tl.Add (name);
		}
	}

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

		tl = new TorrentList ();
		for (int i = 0; i < 20; i++)
			tl.Add ("Am torrent file #" + i);
		
		f.Add (new Label (1, 1, "Torrent: "));

		// Add
		Button badd = new Button (10, 1, "Add");
		badd.Clicked += delegate { AddDialog (); };
		f.Add (badd);

		// Pause
		Button bpause = new Button (18, 1, "Pause");
		f.Add (bpause);

		// Remote
		Button bremove = new Button (28, 1, "Remove");
		f.Add (bremove);

		// Random widget tests
		f.Add (new Label (7,  3, "Name:"));
		f.Add (new Entry (13, 3, 20, "First"));
		
		f.Add (new Label (4,  5, "Address:"));
		f.Add (new Entry (13, 5, 20, "Second"));

		f.Add (new ListView (0, 8, midx-2, 12, tl));
		
		f = new Frame (midx, midy, midx-1, midy-1, "Progress");
		a.Add (f);

		// For testing focus, not ready
		f.Add (new Label (0, 0, "->0<-"));
		f.Add (new Entry  (7, 0, 20, "Another"));
		
		Application.Run (a);
	}
}
