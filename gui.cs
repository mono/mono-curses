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
	public enum Anchor {
		None = 0,
		Left = 1,
		Right = 2,
		Top = 4,
		Bottom = 8
	}

	//
	// The fill values apply from the given x, y values, they will not do
	// a full fill, you must compute x, y yourself.
	//
	[Flags]
	public enum Fill {
		None = 0,
		Horizontal = 1,
		Vertical = 2
	}
	
	public abstract class Widget {
		public Container Container;
		public int x, y, w, h;
		bool can_focus;
		bool has_focus;
		public Anchor Anchor;
		public Fill Fill;
		
		static StreamWriter l;
		
		public static void Log (string s)
		{
			if (l == null)
				l = new StreamWriter (File.OpenWrite ("log2"));
			
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
			Container = Application.EmptyContainer;
		}

		public bool CanFocus {
			get {
				return can_focus;
			}

			set {
				can_focus = value;
			}
		}

		public bool HasFocus {
			get {
				return has_focus;
			}

			set {
				has_focus = value;
				Redraw ();
			}
		}

		// Moves inside the first location inside the container
		public void Move (int line, int col)
		{
			Container.ContainerMove (line, col);
		}
		

		// Move relative to the top of the container
		public void BaseMove (int line, int col)
		{
			Container.ContainerBaseMove (line, col);
		}
		
		public void Clear ()
		{
			for (int line = 0; line < h; line++){
				BaseMove (y + line, x);
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

		public virtual void DoSizeChanged ()
		{
		}
		
		static public void DrawFrame (int col, int line, int width, int height)
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
		protected string text;
		public int Color = -1;
		
		public Label (int x, int y, string s) : base (x, y, s.Length, 1)
		{
			text = s;
		}
		
		public override void Redraw ()
		{
			if (Color != -1)
				Curses.attrset (Color);
			else
				Curses.attrset (ColorNormal);

			Move (y, x);
			Curses.addstr (text);
		}

		public virtual string Text {
			get {
				return text;
			}
			set {
				Curses.attrset (ColorNormal);
				Move (y, x);
				for (int i = 0; i < text.Length; i++)
					Curses.addch (' ');
				text = value;
				Redraw ();
			}
		}
	}

	public class TrimLabel : Label {
		string original;
		
		public TrimLabel (int x, int y, int w, string s) : base (x, y, s)
		{
			original = s;

			SetString (w, s);
		}

		void SetString (int w, string s)
		{
			if ((Fill & Fill.Horizontal) != 0)
				w = Container.w - Container.Border * 2 - x;
			
			this.w = w;
			if (s.Length > w){
				if (w < 5)
					text = s.Substring (0, w);
				else {
					text = s.Substring (0, w/2-2) + "..." + s.Substring (s.Length - w/2+1);
				}
			} else
				text = s;
		}

		public override void DoSizeChanged ()
		{
			if ((Fill & Fill.Horizontal) != 0)
				SetString (0, original);
		}

		public override string Text {
			get {
				return original;
			}

			set {
				SetString (w, value);
				base.Text = text;
			}
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
			CanFocus = true;
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
			case 127:
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
		public string text;
		char hot_key;
		int  hot_pos = -1;
		bool is_default;
		
		public event EventHandler Clicked;

		public Button (string s) : this (0, 0, s) {}
		
		public Button (string s, bool is_default) : this (0, 0, s, is_default) {}
		
		public Button (int x, int y, string s) : this (x, y, s, false) {}
		
		public Button (int x, int y, string s, bool is_default)
			: base (x, y, s.Length + 4 + (is_default ? 2 : 0), 1)
		{
			CanFocus = true;

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
		bool ProcessKey (int ch);
		void SelectedChanged ();
	}
	
	public class ListView : Widget {
		int items;
		int top;
		int selected;
		bool allow_mark;
		IListProvider provider;
		
		public ListView (int x, int y, int w, int h, IListProvider provider) : base (x, y, w, h)
		{
			CanFocus = true;

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

		void SelectedChanged ()
		{
			provider.SelectedChanged ();
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
					SelectedChanged ();
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
					SelectedChanged ();
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
					SelectedChanged ();
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
					SelectedChanged ();
					Redraw ();
				}
				return true;

			default:
				return provider.ProcessKey (c);
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

				if (item >= items){
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

		public int Selected {
			get {
				if (items == 0)
					return -1;
				return selected;
			}

			set {
				if (value >= items)
					throw new ArgumentException ("value");

				selected = value;
				Redraw ();
			}
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

		public int Border;
		
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
				// Poor man's clipping.
				if (w.x >= this.w - Border * 2)
					continue;
				if (w.y >= this.h - Border * 2)
					continue;
				
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
			if (Container != Application.EmptyContainer && Container != null)
				Container.ContainerMove (row + y, col + x);
			else
				Curses.move (row + y, col + x);
		}
		
		public virtual void ContainerBaseMove (int row, int col)
		{
			if (Container != Application.EmptyContainer && Container != null)
				Container.ContainerBaseMove (row + y, col + x);
			else
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

		public override void DoSizeChanged ()
		{
			foreach (Widget widget in widgets){
				widget.DoSizeChanged ();

				//Anchor a = w.Anchor;
				//
				//// Left/Top anchors are just x, y
				//if ((a & Anchor.Right) != 0)
				//	w.x = w - border - w.w;
				//
				//if ((a & Anchor.Bottom) != 0)
				//	w.y = h - border - w.h;

				if ((widget.Fill & Fill.Horizontal) != 0){
					widget.w = w - (Border*2) - widget.x;
				}

				if ((widget.Fill & Fill.Vertical) != 0)
					widget.h = h - (Border * 2) - widget.y;
			}
		}

		public event EventHandler SizeChangedEvent;
		
		public void SizeChanged ()
		{
			if (SizeChangedEvent != null)
				SizeChangedEvent (this, EventArgs.Empty);
			DoSizeChanged ();
		}
	}

	public class Frame : Container {
		public string Title;

		public Frame (string title) : this (0, 0, 0, 0, title)
		{
		}
		
		public Frame (int x, int y, int w, int h, string title) : base (x, y, w, h)
		{
			Title = title;
			Border++;
		}

		public override void ContainerMove (int row, int col)
		{
			base.ContainerMove (row + 1, col + 1);
		}

		public override void Redraw ()
		{
			Curses.attrset (ContainerColorNormal);
			Clear ();
			
			Widget.DrawFrame (x, y, w, h);
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

		const int button_space = 3;
		
		public Dialog (int w, int h, string title)
			: base ((Application.Cols - w) / 2, (Application.Lines-h)/2, w, h, title)
		{
			ContainerColorNormal = Application.ColorDialogNormal;
			ContainerColorFocus = Application.ColorDialogFocus;
			ContainerColorHotNormal = Application.ColorDialogHotNormal;
			ContainerColorHotFocus = Application.ColorDialogHotFocus;

			Border++;
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

			Widget.DrawFrame (x + 1, y + 1, w - 2, h - 2);
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
			base.DoSizeChanged ();
			
			x = (Application.Cols - w) / 2;
			y = (Application.Lines-h) / 2;

			LayoutButtons ();
		}
	}
	
	public class Application {
		public static int ColorNormal;
		public static int ColorFocus;
		public static int ColorHotNormal;
		public static int ColorHotFocus;
		
		public static int ColorMarked;
		public static int ColorMarkedSelected;
		
		public static int ColorDialogNormal;
		public static int ColorDialogFocus;
		public static int ColorDialogHotNormal;
		public static int ColorDialogHotFocus;

		public static int ColorError;
		
		// Every timeout milliseconds to wait
		static public int Timeout = -1;

		// Iteration, on each cycle of Application.Run
		static public event EventHandler Iteration;

		// Private variables
		static ArrayList toplevels = new ArrayList ();
		static short last_color_pair;
		static bool inited;
		static Container empty_container;
		
		static int MakeColor (short f, short b)
		{
			Curses.init_pair (++last_color_pair, f, b);
			return Curses.ColorPair (last_color_pair);
		}

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
			if (!disable_color)
				use_color = Curses.has_colors ();
			
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
			Curses.raw ();
			Curses.noecho ();
			//Curses.nonl ();
			Window.Standard.keypad (true);
		}

		static public void Msg (bool error, string caption, string t)
		{
			ArrayList lines = new ArrayList ();
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
			if (error){
				d.ContainerColorNormal = Application.ColorError;
				d.ContainerColorFocus = Application.ColorError;
				d.ContainerColorHotFocus = Application.ColorError;
			}
			
			for (int i = 0; i < lines.Count; i++)
				d.Add (new Label (1, i + 1, (string) lines [i]));

			Button b = new Button (0, 0, "Ok", true);
			d.AddButton (b);
			b.Clicked += delegate { b.Container.Running = false; };

			Application.Run (d);
		}
		
		static public void Error (string caption, string text)
		{
			Msg (true, caption, text);
		}
		
		static public void Error (string caption, string format, params object [] pars)
		{
			string t = String.Format (format, pars);

			Msg (true, caption, t);
		}

		static public void Info (string caption, string text)
		{
			Msg (false, caption, text);
		}
		
		static public void Info (string caption, string format, params object [] pars)
		{
			string t = String.Format (format, pars);

			Msg (false, caption, t);
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

		public static void Refresh ()
		{
			Container last = null;
			
			foreach (Container c in toplevels){
				c.Redraw ();
				last = c;
			}
			Curses.refresh ();
			if (last != null)
				last.PositionCursor ();
		}

		static public void Run (Container container)
		{
			Init (false);
			
			Curses.timeout (-1);
			if (toplevels.Count == 0)
				InitApp ();

			toplevels.Add (container);

			container.Prepare ();
			container.SizeChanged ();			
			container.FocusFirst ();
			Redraw (container);
			container.PositionCursor ();
			
			int ch;
			Curses.timeout (Timeout);

			for (container.Running = true; container.Running; ){
				ch = Curses.getch ();

				if (Iteration != null)
					Iteration (null, EventArgs.Empty);
				
				if (ch == -1){
					if (Curses.CheckWinChange ()){
						EmptyContainer.Clear ();
						foreach (Container c in toplevels)
							c.SizeChanged ();
						Refresh ();
					}
					continue;
				}
				if (ch == 27){
					Curses.timeout (0);
					int k = Curses.getch ();
					if (k != Curses.ERR)
						ch = Curses.KeyAlt | k;
					Curses.timeout (Timeout);
				}
				
				if (container.ProcessHotKey (ch))
					continue;

				if (container.ProcessKey (ch))
					continue;

				// Control-c, quit the current operation.
				if (ch == 3)
					break;

				// Control-z, suspend execution, then repaint.
				if (ch == 26){
					Curses.console_sharp_sendsigtstp ();
					Window.Standard.redrawwin ();
					Curses.refresh ();
				}
				
				//
				// Focus handling
				//
				if (ch == 9 || ch == Curses.KeyDown){
					if (!container.FocusNext ())
						container.FocusNext ();
					continue;
				}
			}

			toplevels.Remove (container);
			if (toplevels.Count == 0)
				Shutdown ();
			else
				Refresh ();
		}
	}
}
