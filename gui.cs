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
		int color;
		
		public Widget (int x, int y, int w, int h)
		{
			this.x = x;
			this.y = y;
			this.w = w;
			this.h = h;
			Flags = 0;
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
			}
		}
		
		public void Move (int line, int col)
		{
			if (Container != null)
				Container.ContainerMove (line, col);
			else
				Curses.move (line, col);
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

		public int Color {
			get {
				return color;
			}

			set {
				color = value;
			}
		}

		public virtual bool ProcessKey (int key)
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
	}

	public class Label : Widget {
		string text;
		public Label (int x, int y, string s) : base (x, y, s.Length, 1)
		{
			text = s;
		}
		
		public override void Redraw ()
		{
			Curses.attrset (Color);
			Move (y, x);
			int pos = 0;
			
			foreach (char c in text){
				if (++pos > w)
					break;
				Curses.addch (c);
			}
			while (pos++ < w)
				Curses.addch (' ');
		}
	}

	public class Entry : Widget {
		string text, kill;
		int first, point;
		
		public Entry (int x, int y, int w, string s) : base (x, y, w, 1)
		{
			text = s;
			point = s.Length;
			first = point > w ? point - w : 0;
			Flags = WidgetFlags.CanFocus;
		}

		public override void PositionCursor ()
		{
			Move (y, x+point-first);
		}
		
		public override void Redraw ()
		{
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
			case Curses.Backspace:
				if (point == 0)
					return true;
				
				text = text.Substring (0, point - 1) + text.Substring (point);
				point--;
				Adjust ();
				break;

			case 1: // Control-a, Home
				point = 0;
				Adjust ();
				break;

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
				if (key < 32)
					break;
				
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

	public class Container : Widget {
		ArrayList widgets = new ArrayList ();
		Widget focused = null;
		public bool Running;
		
		public Container (int x, int y, int w, int h) : base (x, y, w, h)
		{
		}
		
		public override void Redraw ()
		{
			foreach (Widget w in widgets){
				w.Redraw ();
			}
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

		public void FocusNext ()
		{
			if (focused == null){
				FocusFirst ();
				return;
			}
			int n = widgets.Count;
			int focused_idx = -1;
			int top = n*2-1;
			for (int i = 0; i < top; i++){
				Widget w = (Widget)widgets [i%n];
				
				if (w.HasFocus){
					focused_idx = i;
					continue;
				}
				if (w.CanFocus && focused_idx != -1){
					focused.HasFocus = false;
					SetFocus (w);
					break;
				}
			}
		}

		public virtual void ContainerMove (int row, int col)
		{
			Curses.move (row + y, col + x);
		}
		
		public virtual void Add (Widget w)
		{
			widgets.Add (w);
			w.Container = this;
		}
		
		public override bool ProcessKey (int key)
		{
			if (focused != null)
				focused.ProcessKey (key);
			
			if (key == 9){
				FocusNext ();
				return true;
			}
			return false;
		}
	}

	public class Frame : Container {
		string title;
		
		public Frame (int x, int y, int w, int h, string title) : base (x, y, w, h)
		{
			this.title = title;
		}

		public override void ContainerMove (int row, int col)
		{
			base.ContainerMove (y + row + 1, x + col + 1);
		}

		public override void Redraw ()
		{
			Clear ();

			Widget.DrawFrame (y, x, w, h);
			Curses.move (y, x + 1);
			Curses.addch (' ');
			Curses.addstr (title);
			Curses.addch (' ');
			base.Redraw ();
		}

		public override void Add (Widget w)
		{
			base.Add (w);
			if (w.CanFocus){
				this.CanFocus = true;
			}
		}
	}
	
	public class Dialog : Container {
		string title;
		
		public Dialog (int x, int y, int w, int h, string title) : base (x, y, w, h)
		{
			this.title = title;
		}

		public override void ContainerMove (int row, int col)
		{
			base.ContainerMove (y + row + 2, x + col + 2);
		}

		public override void Redraw ()
		{
			Curses.attrset (Curses.A_REVERSE);
			Clear ();

			Widget.DrawFrame (y + 1, x + 1, w - 2, h - 2);
			Curses.move (y + 1, x + (w - title.Length) / 2);
			Curses.addch (' ');
			Curses.addstr (title);
			Curses.addch (' ');
			base.Redraw ();
		}
	}
	
	public class Application {
		static Stack toplevels = new Stack ();
		
		public static void Init ()
		{
			Curses.initscr ();
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

		static void Shutdown ()
		{
			Curses.endwin ();
		}

		static void Redraw (Container container)
		{
			container.Redraw ();
			Curses.refresh ();
		}

		static public void Run (Container container)
		{
			Init ();
			
			if (toplevels.Count == 0)
				InitApp ();

			toplevels.Push (container);
			
			container.FocusFirst ();
			Redraw (container);
			container.PositionCursor ();
			
			int ch;
			for (container.Running = true; container.Running; ){
				ch = Curses.getch ();

				if (container.ProcessKey (ch))
					continue;
			}

			toplevels.Pop ();
			if (toplevels.Count == 0)
				Shutdown ();
		}
	}
}
