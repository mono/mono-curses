using Unix.Terminal;
using Mono.Terminal;
using System;
using System.IO;

class GuiTest {
	
	static void OptionsDialog ()
	{
		Dialog d = new Dialog (62, 15, "Options");

		d.Add (new Label (1, 1, "  Download Directory:"));
		d.Add (new Label (1, 3, "         Listen Port:"));
		d.Add (new Label (1, 5, "  Upload Speed Limit:"));
		d.Add (new Label (35,5, "kB/s"));
		d.Add (new Label (1, 7, "Download Speed Limit:"));
		d.Add (new Label (35,7, "kB/s"));

		Entry download_dir = new Entry (24, 1, 30, "~/Download");
		d.Add (download_dir);

		Entry listen_port = new Entry (24, 3, 6, "34");
		d.Add (listen_port);

		Entry upload_limit = new Entry (24, 5, 10, "1024");
		d.Add (upload_limit);

		Entry download_limit = new Entry (24, 7, 10, "1024");
		d.Add (download_limit);
		
		bool ok = false;
		
		Button b = new Button ("Ok", true);
		b.Clicked += delegate { ok = true; b.Container.Running = false; };
		d.AddButton (b);

		b = new Button ("Cancel");
		b.Clicked += delegate { b.Container.Running = false; };
		d.AddButton (b);
		
		Application.Run (d);

		if (ok){
			int v;
			
			if (!Int32.TryParse (listen_port.Text, out v)){
				Application.Error ("Error", "The value `{0}' is not a valid port number", listen_port.Text);
				return;
			}

			if (!Directory.Exists (download_dir.Text)){
				Application.Error ("Error", "The directory\n{0}\ndoes not exist", download_dir.Text);
				return;
			}
		}
	}
	
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
		Button b = new Button ("Ok", true);
		b.Clicked += delegate {
			b.Container.Running = false;
			name = e.Text;
		};
		d.AddButton (b);
		b = new Button ("Cancel");
		b.Clicked += delegate {
			b.Container.Running = false;
		};
		d.AddButton (b);
		
		Application.Run (d);

		if (name != null){
			if (!File.Exists (name)){
				Application.Error ("Missing File", "Torrent file:\n" + name + "\ndoes not exist");
				return;
			}
		}
	}

	public class TorrentDetailsList : IListProvider {
		public ListView view;
		
		void IListProvider.SetListView (ListView v)
		{
			view = v;
		}
		
		int IListProvider.Items => 5;
		bool IListProvider.AllowMark => false;

		bool IListProvider.IsMarked (int n)
		{
			return false;
		}

		void IListProvider.Render (int line, int col, int width, int item)
		{
			string s = $"{item} This is item {item}";
			if (s.Length > width){
				s = s.Substring (0, width);
				Curses.addstr (s);
			} else {
				Curses.addstr (s);
				for (int i = s.Length; i < width; i++)
					Curses.addch (' ');
			}
		}

		bool IListProvider.ProcessKey (int ch)
		{
			return false;
		}

		void IListProvider.SelectedChanged ()
		{
		}
	}
	
	public class LogWidget : Widget {
		string [] messages = new string [80];
		int head, tail;
		int count;
		
		public LogWidget (int x, int y) : base (x, y, 0, 0)
		{
			Fill = Fill.Horizontal | Fill.Vertical;
			AddText ("Started");
		}

		public void AddText (string s)
		{
			messages [head] = s;
			head++;
			if (head == messages.Length)
				head = 0;
			if (head == tail)
				tail = (tail+1) % messages.Length;
		}
		
		public override void Redraw ()
		{
			Curses.attrset (ColorNormal);

			int i = 0;
			int l;
			int n = head > tail ? head-tail : (head + messages.Length) - tail;
			for (l = h-1; l >= 0 && n-- > 0; l--){
				int item = head-1-i;
				if (item < 0)
					item = messages.Length+item;

				Move (y+l, x);

				int sl = messages [item].Length;
				if (sl < w){
					Curses.addstr (messages [item]);
					for (int fi = 0; fi < w-sl; fi++)
						Curses.addch (' ');
				} else {
					Curses.addstr (messages [item].Substring (0, sl));
				}
				i++;
			}

			for (; l >= 0; l--){
				Move (y+l, x);
				for (i = 0; i < w; i++)
					Curses.addch (' ');
			}
		}
	}

	static Label status_progress, status_state, status_peers, status_tracker, status_up, status_up_speed;
	static Label status_down, status_down_speed, status_warnings, status_failures, iteration;
	static Frame SetupStatus ()
	{
		Frame fstatus = new Frame ("Status");
		int y = 0;
		int x = 13;
		string init = "<init>";
		
		fstatus.Add (status_progress = new Label (x, y, "0%"));
		status_progress.Color = status_progress.ColorHotNormal;
		fstatus.Add (new Label (1, y++, "Progress:"));
		
		fstatus.Add (status_state = new Label (x, y, init));
		fstatus.Add (new Label (1, y++, "State:"));

		fstatus.Add (status_peers = new Label (x, y, init));
		fstatus.Add (new Label (1, y++, "Peers:"));

		fstatus.Add (status_tracker = new Label (x, y, init));
		fstatus.Add (new Label (1, y++, "Tracker: "));
		y++;

		fstatus.Add (new Label (1, y++, "Upload:"));
		fstatus.Add (new Label (16, y, "KB   Speed: "));
		fstatus.Add (status_up = new Label (1, y, init));
		fstatus.Add (status_up_speed = new Label (28, y, init));
		y++;
		fstatus.Add (new Label (1, y++, "Download:"));
		fstatus.Add (new Label (16, y, "KB   Speed: "));
		fstatus.Add (status_down = new Label (1, y, init));
		fstatus.Add (status_down_speed = new Label (28, y, init));
		y += 2;
		fstatus.Add (status_warnings = new Label (11, y, init));
		fstatus.Add (new Label (1, y++, "Warnings: "));
		fstatus.Add (status_failures = new Label (11, y, init));
		fstatus.Add (new Label (1, y++, "Failures: "));
		y += 2;

		return fstatus;
	}

	//
	// We split this, so if the terminal resizes, we resize accordingly
	//
	static void LayoutDialogs (Frame ftorrents, Frame fstatus, Frame fdetails, Frame fprogress)
	{
		int cols = Application.Cols;
		int lines = Application.Lines;
		
		int midx = Application.Cols/2;
		int midy = Application.Lines/2;

		// Torrents
		ftorrents.x = 0;
		ftorrents.y = 0;
		ftorrents.w = cols - 40;
		ftorrents.h = midy;

		// Status: Always 40x12
		fstatus.x = cols - 40;
		fstatus.y = 0;
		fstatus.w = 40;
		fstatus.h = midy;

		// Details
		fdetails.x = 0;
		fdetails.y = midy;
		fdetails.w = midx;
		fdetails.h = midy;

		// fprogress
		fprogress.x = midx;
		fprogress.y = midy;
		fprogress.w = midx + Application.Cols % 2;
		fprogress.h = midy;
	}

	static void UpdateStatus ()
	{
		status_progress.Text = $"{DateTime.Now}";
		status_state.Text = $"{DateTime.Now}";
		status_peers.Text = $"{DateTime.Now}";
		status_up.Text = "1000";
		status_up_speed.Text = "Lots";
	}
	
	static void Main ()
	{
		Application.Init (false);

		var frame = new Frame (0, 0, Application.Cols, Application.Lines, "List");
		var top = new Container (0, 0, Application.Cols, Application.Lines) {
			frame
		};
		// Add
		Button badd = new Button (1, 1, "Add");
		badd.Clicked += delegate { AddDialog (); };
		frame.Add (badd);

		// Options
		Button boptions = new Button (9, 1, "Options");
		boptions.Clicked += delegate { OptionsDialog (); };
		frame.Add (boptions);

		// Quit
		Button bquit = new Button (21, 1, "Quit");
		bquit.Clicked += delegate {
			// FIXME: shut down torrent here
			top.Running = false;
		};
		frame.Add (bquit);

		ListView list = new ListView (1, 5, 0, 0, new TorrentDetailsList ());
		list.Fill = Fill.Horizontal | Fill.Vertical;
		frame.Add (list);
		
		Frame fprogress = new Frame ("Messages");
		LogWidget log_widget = new LogWidget (0, 0);
		fprogress.Add (log_widget);
		top.Add (fprogress);

		// For testing focus, not ready
		//f.Add (new Label (0, 0, "->0<-"));
		//f.Add (new Entry  (7, 0, 20, "Another"));


		// Details
		Frame fdetails = new Frame ("Details");
		fdetails.Add (new Label (1, 1, "Files for: "));
		var torrent_name = new TrimLabel (12, 1, 10, "");
		torrent_name.Fill = Fill.Horizontal;
		fdetails.Add (torrent_name);
			      
		var details_list = new TorrentDetailsList ();
		var list_details = new ListView (1, 3, 0, 0, details_list);
		list_details.Fill = Fill.Horizontal | Fill.Vertical;
		fdetails.Add (list_details);
		
		top.Add (fdetails);

		// Status
		Frame fstatus = SetupStatus ();
	        top.Add (fstatus);

		iteration = new Label (35, 0, "0");
		fstatus.Add (iteration);

		int it = 0;
		Application.MainLoop.AddTimeout (TimeSpan.FromSeconds (1), (mainloop) => {
			iteration.Text = (it++).ToString ();
			UpdateStatus ();
			log_widget.AddText ("Iteration " + it);
			Application.Refresh ();
			return true;
		});
		
		LayoutDialogs (frame, fstatus, fdetails, fprogress);
		top.SizeChangedEvent += delegate {
			LayoutDialogs (frame, fstatus, fdetails, fprogress);
		};

		UpdateStatus ();
		
		Application.Run (top);
	}
}