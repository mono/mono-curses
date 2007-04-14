//
// gui.cs: Simple curses-based GUI toolkit, core
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
using System.Collections;
using Mono;
using System.IO;

namespace Mono.Terminal {

	[Flags]
	public enum WidgetFlags {
		CanFocus = 1,
		HasFocus = 2
	}
	
	public abstract class Widget {
		public Container Container;
		public int x, y, w, h;
		public WidgetFlags Flags;

		static StreamWriter l = new StreamWriter (File.OpenWrite ("log2"));
		
		public static void Log (string s)
		{
			l.WriteLine (s);
			l.Flush ();
		}

		public static void Log (string s, params object [] args)
		{
			Log (String.Format (s, args));
		}
		
		public Widget (int x, int y, int w, int h)
		{
			this.x = x;
			this.y = y;
			this.w = w;
			this.h = h;
			Flags = 0;
			Container = Application.EmptyContainer;
		}

		public bool CanFocus {
			get {
				return (Flags & WidgetFlags.CanFocus) != 0;
			}

			set {
				if (value)
					Flags |= WidgetFlags.CanFocus;
				else
					Flags &= ~WidgetFlags.CanFocus;
			}
		}

		public bool HasFocus {
			get {
				return (Flags & WidgetFlags.HasFocus) != 0;
			}

			set {
				if (value)
					Flags |= WidgetFlags.HasFocus;
				else
					Flags &= ~WidgetFlags.HasFocus;
				Redraw ();
			}
		}
		
		public void Move (int line, int col)
		{
			Container.ContainerMove (line, col);
		}
		
		public void Clear ()
		{
			for (int line = 0; line < h; line++){
				Move (y + line, x);
				for (int col = 0; col < w; col++){
					Curses.addch (' ');
				}
			}
		}
		
		public virtual void Redraw ()
		{
			for (int line = 0; line < h; line++){
				Move (y + line, x);
				for (int col = 0; col < w; col++){
					Curses.addch ('x');
				}
			}
		}

		public virtual bool ProcessKey (int key)
		{
			return false;
		}

		public virtual bool ProcessHotKey (int key)
		{
			return false;
		}
		
		public virtual void PositionCursor ()
		{
			Move (y, x);
		}
		
		static public void DrawFrame (int line, int col, int width, int height)
		{
			int b;
			Curses.move (line, col);
			Curses.addch (Curses.ACS_ULCORNER);
			for (b = 0; b < width-2; b++)
				Curses.addch (Curses.ACS_HLINE);
			Curses.addch (Curses.ACS_URCORNER);
			
			for (b = 1; b < height-1; b++){
				Curses.move (line+b, col);
				Curses.addch (Curses.ACS_VLINE);
			}
			for (b = 1; b < height-1; b++){
				Curses.move (line+b, col+width-1);
				Curses.addch (Curses.ACS_VLINE);
			}
			Curses.move (line + height-1, col);
			Curses.addch (Curses.ACS_LLCORNER);
			for (b = 0; b < width-2; b++)
				Curses.addch (Curses.ACS_HLINE);
			Curses.addch (Curses.ACS_LRCORNER);
		}

		public int ColorNormal {
			get {
				return Container.ContainerColorNormal;
			}
		}

		public int ColorFocus {
			get {
				return Container.ContainerColorFocus;
			}
		}

		public int ColorHotNormal {
			get {
				return Container.ContainerColorHotNormal;
			}
		}

		public int ColorHotFocus {
			get {
				return Container.ContainerColorHotFocus;
			}
		}
	}

	public class Label : Widget {
		string text;
		public Label (int x, int y, string s) : base (x, y, s.Length, 1)
		{
			text = s;
		}
		
		public override void Redraw ()
		{
			Curses.attrset (ColorNormal);
			Move (y, x);
			Curses.addstr (text);
		}
	}

	public class Entry : Widget {
		string text, kill;
		int first, point;

		public Entry (int x, int y, int w, string s) : base (x, y, w, 1)
		{
			if (s == null)
				s = "";
			
			text = s;
			point = s.Length;
			first = point > w ? point - w : 0;
			Flags = WidgetFlags.CanFocus;
		}

		public string Text {
			get {
				return text;
			}

			set {
				text = value;
				if (point > text.Length)
					point = text.Length;
				first = point > w ? point - w : 0;
				Redraw ();
			}
		}
		
		public override void PositionCursor ()
		{
			Move (y, x+point-first);
		}
		
		public override void Redraw ()
		{
			Curses.attrset (Application.ColorDialogFocus);
			Move (y, x);
			
			for (int i = 0; i < w; i++){
				int p = first + i;

				if (p < text.Length)
					Curses.addch (text [p]);
				else
					Curses.addch (' ' );
			}
			PositionCursor ();
		}

		void Adjust ()
		{
			if (point < first)
				first = point;
			else if (first + point >= w)
				first = point - (w / 3);
			Redraw ();
		}
		
		public override bool ProcessKey (int key)
		{
			switch (key){
			case Curses.KeyBackspace:
				if (point == 0)
					return true;
				
				text = text.Substring (0, point - 1) + text.Substring (point);
				point--;
				Adjust ();
				break;

			case Curses.KeyHome:
			case 1: // Control-a, Home
				point = 0;
				Adjust ();
				break;

			case Curses.KeyLeft:
			case 2: // Control-b, back character
				if (point > 0){
					point--;
					Adjust ();
				}
				break;

			case 4: // Control-d, Delete
				if (point == text.Length)
					break;
				text = text.Substring (0, point) + text.Substring (point+1);
				Adjust ();
				break;
				
			case 5: // Control-e, End
				point = text.Length;
				Adjust ();
				break;

			case Curses.KeyRight:
			case 6: // Control-f, forward char
				if (point == text.Length)
					break;
				point++;
				Adjust ();
				break;
				
			case 11: // Control-k, kill-to-end
				kill = text.Substring (point);
				text = text.Substring (0, point);
				Adjust ();
				break;

			case 25: // Control-y, yank
				if (kill == null)
					return true;
				
				if (point == text.Length){
					text = text + kill;
					point = text.Length;
				} else {
					text = text.Substring (0, point) + kill + text.Substring (point);
					point += kill.Length;
				}
				Adjust ();
				break;
				
			default:
				// Ignore other control characters.
				if (key < 32 || key > 255)
					return false;
				
				if (point == text.Length){
					text = text + (char) key;
				} else {
					text = text.Substring (0, point) + (char) key + text.Substring (point);
				}
				point++;
				Adjust ();
				return true;
			}
			return true;
		}
	}

	public class Button : Widget {
		string text;
		char hot_key;
		int  hot_pos = -1;
		bool is_default;
		
		public event EventHandler Clicked;

		public Button (int x, int y, string s) : this (x, y, s, false) {}
		
		public Button (int x, int y, string s, bool is_default)
			: base (x, y, s.Length + 4 + (is_default ? 2 : 0), 1)
		{
			Flags = WidgetFlags.CanFocus;

			this.is_default = is_default;
			if (is_default)
				text = "[< " + s + " >]";
			else
				text = "[ " + s + " ]";
			
			int i = 0;
			foreach (char c in text){
				if (Char.IsUpper (c)){
					hot_key = c;
					hot_pos = i;
					break;
				}
				i++;
			}
		}

		public override void Redraw ()
		{
			Curses.attrset (HasFocus ? ColorFocus : ColorNormal);
			Move (y, x);
			Curses.addstr (text);
			Move (y, x + hot_pos);
			Curses.attrset (HasFocus ? ColorHotFocus : ColorHotNormal);
			Curses.addch (hot_key);
		}

		public override void PositionCursor ()
		{
			Move (y, x + hot_pos);
		}

		public override bool ProcessHotKey (int key)
		{
			int k = Curses.IsAlt (key);
			if (k != 0){
				if (Char.ToUpper ((char)k) == hot_key){
					Container.SetFocus (this);
					if (Clicked != null)
						Clicked (this, EventArgs.Empty);
					return true;
				}
				return false;
			}

			if (is_default && key == '\n'){
				if (Clicked != null)
					Clicked (this, EventArgs.Empty);
				return true;
			}
			return false;
		}

		public override bool ProcessKey (int c)
		{
			if (c == '\n' || c == ' ' || Char.ToUpper ((char)c) == hot_key){
				if (Clicked != null)
					Clicked (this, EventArgs.Empty);
				return true;
			}
			return false;
		}
	}

	public interface IListProvider {
		int Items { get; }
		bool AllowMark { get; }
		bool IsMarked (int item);
		void Render (int line, int col, int width, int item);
		void SetListView (ListView target);
	}
	
	public class ListView : Widget {
		int items;
		int top;
		int selected;
		bool allow_mark;
		IListProvider provider;
		
		public ListView (int x, int y, int w, int h, IListProvider provider) : base (x, y, w, h)
		{
			Flags = WidgetFlags.CanFocus;

			this.provider = provider;
			provider.SetListView (this);
			items = provider.Items;
			allow_mark = provider.AllowMark;
		}

		public void ProviderChanged ()
		{
			if (provider.Items != items){
				items = provider.Items;
				if (top > items){
					if (items > 1)
						top = items-1;
					else
						top = 0;
				}
				if (selected > items){
					if (items > 1)
						selected = items - 1;
					else
						selected = 0;
				}
			}
			Redraw ();
		}

		public override bool ProcessKey (int c)
		{
			int n;
			
			switch (c){
			case 16: // Control-p
			case Curses.KeyUp:
				if (selected > 0){
					selected--;
					if (selected < top)
						top = selected;
					Redraw ();
				}
				return true;

			case 14: // Control-n
			case Curses.KeyDown:
				if (selected+1 < items){
					selected++;
					if (selected >= top + h){
						top++;
					}
					Redraw ();
				}
				return true;

			case 22: //  Control-v
			case Curses.KeyNPage:
				n = (selected + h);
				if (n > items)
					n = items-1;
				if (n != selected){
					selected = n;
					if (items >= h)
						top = selected;
					else
						top = 0;
					Redraw ();
				}
				return true;
				
			case Curses.KeyPPage:
				n = (selected - h);
				if (n < 0)
					n = 0;
				if (n != selected){
					selected = n;
					top = selected;
					Redraw ();
				}
				return true;
			}
			return false;
		}

		public override void PositionCursor ()
		{
			Move (y + (selected-top), x);
		}

		public override void Redraw ()
		{
			for (int l = 0; l < h; l++){
				Move (y + l, x);
				int item = l + top;

				if (item > items){
					Curses.attrset (ColorNormal);
					for (int c = 0; c < w; c++)
						Curses.addch (' ');
					continue;
				}

				bool marked = allow_mark ? provider.IsMarked (item) : false;

				if (item == selected){
					if (marked)
						Curses.attrset (ColorHotNormal);
					else
						Curses.attrset (ColorFocus);
				} else {
					if (marked)
						Curses.attrset (ColorHotFocus);
					else
						Curses.attrset (ColorNormal);
				}
				provider.Render (y + l, x, w, item);
			}
			PositionCursor ();
		}
	}
	
	public class Container : Widget {
		ArrayList widgets = new ArrayList ();
		Widget focused = null;
		public bool Running;
	
		public int ContainerColorNormal;
		public int ContainerColorFocus;
		public int ContainerColorHotNormal;
		public int ContainerColorHotFocus;
		
		static Container ()
		{
		}
		
		public Container (int x, int y, int w, int h) : base (x, y, w, h)
		{
			ContainerColorNormal = Application.ColorNormal;
			ContainerColorFocus = Application.ColorFocus;
			ContainerColorHotNormal = Application.ColorHotNormal;
			ContainerColorHotFocus = Application.ColorHotFocus;
		}

		// Called on top level containers before starting up.
		public virtual void Prepare ()
		{
		}

		public void RedrawChildren ()
		{
			foreach (Widget w in widgets){
				w.Redraw ();
			}
		}
		
		public override void Redraw ()
		{
			RedrawChildren ();
		}
		
		public override void PositionCursor ()
		{
			if (focused != null)
				focused.PositionCursor ();
		}

		public void SetFocus (Widget w)
		{
			if (!w.CanFocus)
				return;
			if (focused == w)
				return;
			if (focused != null)
				focused.HasFocus = false;
			focused = w;
			focused.HasFocus = true;
			Container wc = w as Container;
			if (wc != null)
				wc.EnsureFocus ();
			focused.PositionCursor ();
		}

		public void EnsureFocus ()
		{
			if (focused == null)
				FocusFirst();
		}
		
		public void FocusFirst ()
		{
			foreach (Widget w in widgets){
				if (w.CanFocus){
					SetFocus (w);
					return;
				}
			}
		}

		public bool FocusNext ()
		{
			if (focused == null){
				FocusFirst ();
				return true;
			}
			int n = widgets.Count;
			int focused_idx = -1;
			Log ("Count {0}", n);
			for (int i = 0; i < n; i++){
				Widget w = (Widget)widgets [i%n];

				if (w.HasFocus){
					Container c = w as Container;
					if (c != null){
						if (c.FocusNext ())
							return true;
					}
					focused_idx = i;
					continue;
				}
				if (w.CanFocus && focused_idx != -1){
					focused.HasFocus = false;

					Container c = w as Container;
					if (c != null && c.CanFocus){
						c.FocusFirst ();
					} 
					SetFocus (w);
					return true;
				}
			}
			if (focused != null){
				focused.HasFocus = false;
				focused = null;
			}
			return false;
		}

		public virtual void ContainerMove (int row, int col)
		{
			Curses.move (row + y, col + x);
		}
		
		public virtual void Add (Widget w)
		{
			widgets.Add (w);
			w.Container = this;
			if (w.CanFocus)
				this.CanFocus = true;
		}
		
		public override bool ProcessKey (int key)
		{
			if (focused != null){
				if (focused.ProcessKey (key))
					return true;
			}
			return false;
		}

		public override bool ProcessHotKey (int key)
		{
			if (focused != null)
				if (focused.ProcessHotKey (key))
					return true;
			
			foreach (Widget w in widgets){
				if (w == focused)
					continue;
				
				if (w.ProcessHotKey (key))
					return true;
			}
			return false;
		}

		public virtual void DoSizeChanged ()
		{
			// nothing
		}

		public event EventHandler SizeChangedEvent;
		
		public void SizeChanged ()
		{
			DoSizeChanged ();
			if (SizeChangedEvent != null)
				SizeChangedEvent (this, EventArgs.Empty);
		}
	}

	public class Frame : Container {
		public string Title;

		public Frame (int x, int y, int w, int h, string title) : base (x, y, w, h)
		{
			Title = title;
		}

		public override void ContainerMove (int row, int col)
		{
			base.ContainerMove (row + 1, col + 1);
		}

		public override void Redraw ()
		{
			Curses.attrset (ContainerColorNormal);
			Clear ();
			Widget.DrawFrame (y, x, w, h);
			Curses.attrset (Container.ContainerColorNormal);
			Curses.move (y, x + 1);
			if (HasFocus)
				Curses.attrset (Application.ColorDialogNormal);
			Curses.addch (' ');
			Curses.addstr (Title);
			Curses.addch (' ');
			RedrawChildren ();
		}

		public override void Add (Widget w)
		{
			base.Add (w);
		}
	}

	//
	// A container with a border, and with a default set of colors
	//
	public class Dialog : Frame {
		int button_len;
		ArrayList buttons;

		const int button_space = 4;
		
		public Dialog (int w, int h, string title)
			: base ((Application.Cols - w) / 2, (Application.Lines-h)/2, w, h, title)
		{
			ContainerColorNormal = Application.ColorDialogNormal;
			ContainerColorFocus = Application.ColorDialogFocus;
			ContainerColorHotNormal = Application.ColorDialogHotNormal;
			ContainerColorHotFocus = Application.ColorDialogHotFocus;
		}

		public override void Prepare ()
		{
			LayoutButtons ();
		}

		void LayoutButtons ()
		{
			if (buttons == null)
				return;
			
			int p = (w - button_len) / 2;
			
			foreach (Button b in buttons){
				b.x = p;
				b.y = h - 5;

				p += b.w + button_space;
			}
		}

		public void AddButton (Button b)
		{
			if (buttons == null)
				buttons = new ArrayList ();
			
			buttons.Add (b);
			button_len += b.w + button_space;

			Add (b);
		}
		
		public override void ContainerMove (int row, int col)
		{
			base.ContainerMove (row + 1, col + 1);
		}

		public override void Redraw ()
		{
			Curses.attrset (ContainerColorNormal);
			Clear ();

			Widget.DrawFrame (y + 1, x + 1, w - 2, h - 2);
			Curses.move (y + 1, x + (w - Title.Length) / 2);
			Curses.addch (' ');
			Curses.addstr (Title);
			Curses.addch (' ');
			RedrawChildren ();
		}

		public override bool ProcessKey (int key)
		{
			if (key == 27){
				Running = false;
				return true;
			}

			return base.ProcessKey (key);
		}

		public override void DoSizeChanged ()
		{
			x = (Application.Cols - w) / 2;
			y = (Application.Lines-h) / 2;

			LayoutButtons ();
		}
	}
	
	public class Application {
		static ArrayList toplevels = new ArrayList ();

		static public int ColorNormal;
		static public int ColorFocus;
		static public int ColorHotNormal;
		static public int ColorHotFocus;
		
		static public int ColorMarked;
		static public int ColorMarkedSelected;
		
		static public int ColorDialogNormal;
		static public int ColorDialogFocus;
		static public int ColorDialogHotNormal;
		static public int ColorDialogHotFocus;

		static public int ColorError;
		
		static short last_color_pair;
		
		static int MakeColor (short f, short b)
		{
			Curses.init_pair (++last_color_pair, f, b);
			return Curses.ColorPair (last_color_pair);
		}

		static bool inited;

		static Container empty_container;
		static public Container EmptyContainer {
			get {
				return empty_container;
			}
		}
		
		public static void Init (bool disable_color)
		{
			empty_container = new Container (0, 0, Application.Cols, Application.Lines);
			
			Curses.initscr ();

			if (inited)
				return;
			inited = true;
			
			bool use_color = false;
			if (!disable_color){
				use_color = Curses.has_colors ();
				Console.WriteLine (use_color);
			}
			
			Curses.start_color ();
			if (use_color){
				ColorNormal = MakeColor (Curses.COLOR_WHITE, Curses.COLOR_BLUE);
				ColorFocus = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_CYAN);
				ColorHotNormal = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_BLUE);
				ColorHotFocus = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_CYAN);

				ColorMarked = ColorHotNormal;
				ColorMarkedSelected = ColorHotFocus;

				ColorDialogNormal    = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_WHITE);
				ColorDialogFocus     = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_CYAN);
				ColorDialogHotNormal = MakeColor (Curses.COLOR_BLUE,  Curses.COLOR_WHITE);
				ColorDialogHotFocus  = MakeColor (Curses.COLOR_BLUE,  Curses.COLOR_CYAN);

				ColorError = Curses.A_BOLD | MakeColor (Curses.COLOR_WHITE, Curses.COLOR_RED);
			} else {
				ColorNormal = Curses.A_NORMAL;
				ColorFocus = Curses.A_REVERSE;
				ColorHotNormal = Curses.A_BOLD;
				ColorHotFocus = Curses.A_REVERSE | Curses.A_BOLD;

				ColorMarked = Curses.A_BOLD;
				ColorMarkedSelected = Curses.A_REVERSE | Curses.A_BOLD;

				ColorDialogNormal = Curses.A_REVERSE;
				ColorDialogFocus = Curses.A_NORMAL;
				ColorDialogHotNormal = Curses.A_BOLD;
				ColorDialogHotFocus = Curses.A_NORMAL;

				ColorError = Curses.A_BOLD;
			}
		}

		static public int Lines {	
			get {
				return Curses.Lines;
			}
		}

		static public int Cols {
			get {
				return Curses.Cols;
			}
		}

		static void InitApp ()
		{
			Curses.cbreak ();
			Curses.noecho ();
			//Curses.nonl ();
			Window.Standard.keypad (true);
		}

		static public void Error (string caption, string format, params object [] pars)
		{
			ArrayList lines = new ArrayList ();
			string t = String.Format (format, pars);

			int last = 0;
			int max_w = 0;
			string x;
			for (int i = 0; i < t.Length; i++){
				if (t [i] == '\n'){
					x = t.Substring (last, i-last);
					lines.Add (x);
					last = i + 1;
					if (x.Length > max_w)
						max_w = x.Length;
				}
			}
			x = t.Substring (last);
			if (x.Length > max_w)
				max_w = x.Length;
			lines.Add (x);

			Dialog d = new Dialog (System.Math.Max (caption.Length + 8, max_w + 8), lines.Count + 7, caption);
			d.ContainerColorNormal = Application.ColorError;
			d.ContainerColorFocus = Application.ColorError;
			d.ContainerColorHotFocus = Application.ColorError;
			
			for (int i = 0; i < lines.Count; i++)
				d.Add (new Label (1, i + 1, (string) lines [i]));

			Button b = new Button (0, 0, "Ok", true);
			d.AddButton (b);
			b.Clicked += delegate { b.Container.Running = false; };

			Application.Run (d);
		}
		
		static void Shutdown ()
		{
			Curses.endwin ();
		}

		static void Redraw (Container container)
		{
			container.Redraw ();
			Curses.refresh ();
		}

		static void Refresh ()
		{
			foreach (Container c in toplevels)
				c.Redraw ();
			Curses.refresh ();
		}
		
		static public void Run (Container container)
		{
			Init (false);
			
			Curses.timeout (-1);
			if (toplevels.Count == 0)
				InitApp ();

			toplevels.Add (container);

			container.Prepare ();
			
			container.FocusFirst ();
			Redraw (container);
			container.PositionCursor ();
			
			int ch;
			for (container.Running = true; container.Running; ){
				ch = Curses.getch ();

				if (ch == -1){
					if (Curses.CheckWinChange ()){
						foreach (Container c in toplevels)
							c.SizeChanged ();
						Refresh ();
					}
				}
				if (ch == 27){
					Curses.timeout (0);
					int k = Curses.getch ();
					if (k != Curses.ERR)
						ch = Curses.KeyAlt | k;
					Curses.timeout (-1);
				}
				
				if (container.ProcessHotKey (ch))
					continue;

				if (container.ProcessKey (ch))
					continue;

				//
				// Focus handling
				//
				if (ch == 9 || ch == Curses.KeyDown){
					if (!container.FocusNext ())
						container.FocusNext ();
					continue;
				}
				
			}

			toplevels.Remove (toplevels.Count-1);
			if (toplevels.Count == 0)
				Shutdown ();
			else
				Refresh ();
		}
	}
}
