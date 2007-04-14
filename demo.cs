//
// demo.cs
//
// Authors:
//   Miguel de Icaza (miguel.de.icaza@gmail.com)
//
// Copyright (C) 2007 Novell (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using Mono.Terminal;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;

[XmlRoot ("config")]
public class Config {
	[XmlAttribute] public string DownloadDir;
	[XmlAttribute] public int ListenPort;
	[XmlAttribute] public float UploadSpeed;
	[XmlAttribute] public float DownloadSpeed;
}

class MonoTorrent {

	//
	// Configuration data, loaded at startup
	//
	static string config_file = Path.Combine (Environment.GetFolderPath (
							  Environment.SpecialFolder.ApplicationData), "monotorrent.config");

	static Config config;
	
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

	static bool ValidateSpeed (Entry e, out float value)
	{
		string t = e.Text;

		if (t == "0" || t == "" || t == "unlimited"){
			value = 0;
			return true;
		}

		if (float.TryParse (t, out value))
			return true;

		Application.Error ("Error", "Invalid speed `{0}' specified", t);
		return false;
	}

	static string RenderSpeed (float f)
	{
		if (f == 0)
			return "unlimited";
		return f.ToString (); 
	}
	
	static void OptionsDialog ()
	{
		Dialog d = new Dialog (62, 15, "Options");

		d.Add (new Label (1, 1, "  Download Directory:"));
		d.Add (new Label (1, 3, "         Listen Port:"));
		d.Add (new Label (1, 5, "  Upload Speed Limit:"));
		d.Add (new Label (35,5, "kB/s"));
		d.Add (new Label (1, 7, "Download Speed Limit:"));
		d.Add (new Label (35,7, "kB/s"));

		Entry download_dir = new Entry (24, 1, 30, config.DownloadDir);
		d.Add (download_dir);

		Entry listen_port = new Entry (24, 3, 6, config.ListenPort.ToString ());
		d.Add (listen_port);

		Entry upload_limit = new Entry (24, 5, 10, RenderSpeed (config.UploadSpeed));
		d.Add (upload_limit);

		Entry download_limit = new Entry (24, 7, 10, RenderSpeed (config.DownloadSpeed));
		d.Add (download_limit);
		
		bool ok = false;
		
		Button b = new Button (0, 0, "Ok", true);
		b.Clicked += delegate { ok = true; b.Container.Running = false; };
		d.AddButton (b);

		b = new Button (0, 0, "Cancel");
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

			if (!ValidateSpeed (upload_limit, out config.UploadSpeed))
				return;

			if (!ValidateSpeed (download_limit, out config.DownloadSpeed))
				return;

			config.DownloadDir = download_dir.Text;
			config.ListenPort = v;
		}
	}

	static void RunGui ()
	{
		Container a = new Container (1, 1, Application.Cols, Application.Lines);
		//Dialog a = new Dialog (1, 1, 80, 25, "Demo");

		int midx = Application.Cols/2;
		int midy = Application.Lines/2;
		Frame f = new Frame (0,  0, midx, midy, "Torrents");
		a.Add (f);

		tl = new TorrentList ();
		for (int i = 0; i < 20; i++)
			tl.Add ("Am torrent file #" + i);
		
		// Add
		Button badd = new Button (1, 1, "Add");
		badd.Clicked += delegate { AddDialog (); };
		f.Add (badd);

		// Pause
		Button bpause = new Button (9, 1, "Pause");
		f.Add (bpause);

		// Remote
		Button bremove = new Button (19, 1, "Remove");
		f.Add (bremove);

		// Options
		Button boptions = new Button (1, 2, "Options");
		boptions.Clicked += delegate { OptionsDialog (); };
		f.Add (boptions);

		// Quit
		Button bquit = new Button (13, 2, "Quit");
		bquit.Clicked += delegate {
			// FIXME: shut down torrent here
			bquit.Container.Running = false;
		};
		f.Add (bquit);
		
		// Random widget tests
		//f.Add (new Label (7,  3, "Name:"));
		//f.Add (new Entry (13, 3, 20, "First"));
		
		//f.Add (new Label (4,  5, "Address:"));
		//f.Add (new Entry (13, 5, 20, "Second"));

		f.Add (new ListView (1, 5, midx-3, 15, tl));
		
		f = new Frame (midx, midy, midx-1, midy-1, "Progress");
		a.Add (f);

		// For testing focus, not ready
		//f.Add (new Label (0, 0, "->0<-"));
		//f.Add (new Entry  (7, 0, 20, "Another"));


		// Details
		f = new Frame (0,  midy, midx, midy-1, "Details");
		int y = 1;
		f.Add (new Label (1, y++, "1. First File.avi"));
		f.Add (new Label (1, y++, "2. Second File.avi of Torrent"));
		a.Add (f);

		// Status
		Frame status = new Frame (midx,  0, midx-1, midy, "Status");
		y = 1;
		status.Add (new Label (1, y++, "Uploading to:      0"));
		status.Add (new Label (1, y++, "Half opens:        0"));
		status.Add (new Label (1, y++, "Max open:          150"));
		status.Add (new Label (1, y++, "Progress:          0.00"));
		status.Add (new Label (1, y++, "Download Speed:    0.00"));
		status.Add (new Label (1, y++, "Upload Speed:      0.00"));
		status.Add (new Label (1, y++, "Torrent State:     Downloading"));
		status.Add (new Label (1, y++, "Seeds = 0   Leechs  = 0"));
		status.Add (new Label (1, y++, "Total available:   0"));
		status.Add (new Label (1, y++, "Downloaded:        0.00"));
		status.Add (new Label (1, y++, "Uploaded:          0.00"));
		status.Add (new Label (1, y++, "Tracker Status:    Announcing"));
		status.Add (new Label (1, y++, "Protocol Download: 0.00"));
		status.Add (new Label (1, y++, "Protocol Upload:   0.00"));
		status.Add (new Label (1, y++, "Hashfails:         0"));
		status.Add (new Label (1, y++, "Scrape complete:   0"));
		status.Add (new Label (1, y++, "Scrape incomplete: 0"));
		status.Add (new Label (1, y++, "Scrape downloaded: 0"));
		status.Add (new Label (1, y++, "Warning Message:"));
		status.Add (new Label (1, y++, "Failure Message:"));
		status.Add (new Label (1, y++, "Endgame Mode:     False"));

		a.Add (status);
	
		Application.Run (a);
	}

	static void SetupDefaults ()
	{
		config = new Config ();
		config.ListenPort = 9876;
		config.UploadSpeed = 0;
		config.DownloadSpeed = 0;
		
		config.DownloadDir = Path.Combine (Environment.GetFolderPath (
							   Environment.SpecialFolder.Personal), "downloads");
		
		try {
			Directory.CreateDirectory (config.DownloadDir);
		} catch {
			config.DownloadDir = null;
		}
	}
	
	static void LoadSettings ()
	{
		if (!File.Exists (config_file)){
			try {
				Directory.CreateDirectory (Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData));
			} catch {
				return;
			}

			SetupDefaults ();
			return;
		}
		
		try {
			config = (Config)
				new XmlSerializer (typeof (Config)).Deserialize (new XmlTextReader (config_file));
		} catch {
			SetupDefaults ();
		}
	}

	static void SaveSettings ()
	{
		try {
			using (FileStream f = File.Create (config_file)){
				new XmlSerializer (typeof (Config)).Serialize (new XmlTextWriter (new StreamWriter (f)), config);
			}
		} catch {
		}
	}
	
	static void Main ()
	{
		LoadSettings ();
		
		Application.Init (false);

		RunGui ();
	}
}
