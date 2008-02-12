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

	/// <summary>
	///   Base class for creating curses widgets
	/// </summary>
	public abstract class Widget {
		public Container Container;
		public int x, y, w, h;
		bool can_focus;
		bool has_focus;
		public Anchor Anchor;
		public Fill Fill;
		
		static StreamWriter l;
		
		/// <summary>
		///   Utility function to log messages
		/// </summary>
		/// <remarks>
		///    <para>This is a utility function that you can use to log messages
		///    that will be stored in a file (as curses has taken over the
		///    screen and you can not really log information there).</para>
		///    <para>
		///    The data is written to the file "log2" for now</para>
		/// </remarks>
		public static void Log (string s)
		{
			if (l == null)
				l = new StreamWriter (File.OpenWrite ("log2"));
			
			l.WriteLine (s);
			l.Flush ();
		}

		/// <summary>
		///   Utility function to log messages
		/// </summary>
		/// <remarks>
		///    <para>This is a utility function that you can use to log messages
		///    that will be stored in a file (as curses has taken over the
		///    screen and you can not really log information there). </para>
		///    <para>
		///    The data is written to the file "log2" for now</para>
		/// </remarks>
		public static void Log (string s, params object [] args)
		{
			Log (String.Format (s, args));
		}
		
		/// <summary>
		///   Public constructor for widgets
		/// </summary>
		/// <remarks>
		///   <para>
		///      Constructs a widget that starts at positio (x,y) and has width w and height h.
		///      These parameters are used by the methods <see cref="Clear"/> and <see cref="Redraw"/>
		///   </para>
		/// </remarks>
		public Widget (int x, int y, int w, int h)
		{
			this.x = x;
			this.y = y;
			this.w = w;
			this.h = h;
			Container = Application.EmptyContainer;
		}

		/// <summary>
		///   Focus status of this widget
		/// </summary>
		/// <remarks>
		///   <para>
		///     This is used typically by derived classes to flag whether
		///     this widget can receive focus or not.    Focus is activated
		///     by either clicking with the mouse on that widget or by using
		////    the tab key.
		///   </para>
		/// </remarks>
		public bool CanFocus {
			get {
				return can_focus;
			}

			set {
				can_focus = value;
			}
		}

		/// <summary>
		///   Gets or sets the current focus status.
		/// </summary>
		/// <remarks>
		///   <para>
		///     A widget can grab the focus by setting this value to true and
		///     the current focus status can be inquired by using this property.
		///   </para>
		/// </remarks>
		public bool HasFocus {
			get {
				return has_focus;
			}

			set {
				has_focus = value;
				Redraw ();
			}
		}

		/// <summary>
		///   Moves inside the first location inside the container
		/// </summary>
		/// <remarks>
		///     <para>This moves the current cursor position to the specified
		///     line and column relative to the container
		///     client area where this widget is located.</para>
		///   <para>The difference between this
		///     method and <see cref="BaseMove"/> is that this
		///     method goes to the beginning of the client area
		///     inside the container while <see cref="BaseMove"/> goes to the first
		///     position that container uses.</para>
		///   <para>
		///     For example, a Frame usually takes up a couple
		///     of characters for padding.   This method would
		///     position the cursor inside the client area,
		///     while <see cref="BaseMove"/> would position
		///     the cursor at the top of the frame.
		///   </para>
		/// </remarks>
		public void Move (int line, int col)
		{
			Container.ContainerMove (line, col);
		}
		

		/// <summary>
		///   Move relative to the top of the container
		/// </summary>
		/// <remarks>
		///     <para>This moves the current cursor position to the specified
		///     line and column relative to the start of the container
		///     where this widget is located.</para>
		///   <para>The difference between this
		///     method and <see cref="Move"/> is that this
		///     method goes to the beginning of the container,
		///     while <see cref="Move"/> goes to the first
		///     position that widgets should use.</para>
		///   <para>
		///     For example, a Frame usually takes up a couple
		///     of characters for padding.   This method would
		///     position the cursor at the beginning of the
		///     frame, while <see cref="Move"/> would position
		///     the cursor within the frame.
		///   </para>
		/// </remarks>
		public void BaseMove (int line, int col)
		{
			Container.ContainerBaseMove (line, col);
		}
		
		/// <summary>
		///   Clears the widget region withthe current color.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This clears the entire region used by this widget.
		///   </para>
		/// </remarks>
		public void Clear ()
		{
			for (int line = 0; line < h; line++){
				BaseMove (y + line, x);
				for (int col = 0; col < w; col++){
					Curses.addch (' ');
				}
			}
		}
		
		/// <summary>
		///   Redraws the current widget, must be overwritten.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This method should be overwritten by classes
		///     that derive from Widget.   The default
		///     implementation of this method just fills out
		///     the region with the character 'x'. 
		///   </para>
		///   <para>
		///     Widgets are responsible for painting the
		///     entire region that they have been allocated.
		///   </para>
		/// </remarks>
		public virtual void Redraw ()
		{
			for (int line = 0; line < h; line++){
				Move (y + line, x);
				for (int col = 0; col < w; col++){
					Curses.addch ('x');
				}
			}
		}

		/// <summary>
		///   If the widget is focused, gives the widget a
		///     chance to process the keystroke. 
		/// </summary>
		/// <remarks>
		///   <para>
		///     Widgets can override this method if they are
		///     interested in processing the given keystroke.
		///     If they consume the keystroke, they must
		///     return true to stop the keystroke from being
		///     processed by other widgets or consumed by the
		///     widget engine.    If they return false, the
		///     keystroke will be passed out to other widgets
		///     for processing. 
		///   </para>
		/// </remarks>
		public virtual bool ProcessKey (int key)
		{
			return false;
		}

		/// <summary>
		///   Gives widgets a chance to process the given
		///     mouse event. 
		/// </summary>
		/// <remarks>
		///     Widgets can inspect the value of
		///     ev.ButtonState to determine if this is a
		///     message they are interested in (typically
		///     ev.ButtonState &amp; Curses.BUTTON1_CLICKED).
		/// </remarks>
		public virtual void ProcessMouse (Curses.MouseEvent ev)
		{
		}

		/// <summary>
		///   This method can be overwritten by widgets that
		///     want to provide accelerator functionality
		///     (Alt-key for example).
		/// </summary>
		/// <remarks>
		///   <para>
		///     Before keys are sent to the widgets on the
		///     current Container, all the widgets are
		///     processed and the key is passed to the widgets
		///     to allow some of them to process the keystroke
		///     as a hot-key. </para>
		///  <para>
		///     For example, if you implement a button that
		///     has a hotkey ok "o", you would catch the
		///     combination Alt-o here.  If the event is
		///     caught, you must return true to stop the
		///     keystroke from being dispatched to other
		///     widgets.
		///  </para>
		///  <para>
		///    Typically to check if the keystroke is an
		///     Alt-key combination, you would use
		///     Curses.IsAlt(key) and then Char.ToUpper(key)
		///     to compare with your hotkey.
		///  </para>
		/// </remarks>
		public virtual bool ProcessHotKey (int key)
		{
			return false;
		}
		
		/// <summary>
		///   Moves inside the first location inside the container
		/// </summary>
		/// <remarks>
		///   <para>
		///     A helper routine that positions the cursor at
		///     the logical beginning of the widget.   The
		///     default implementation merely puts the cursor at
		///     the beginning, but derived classes should find a
		///     suitable spot for the cursor to be shown.
		///   </para>
		///   <para>
		///     This method must be overwritten by most
		///     widgets since screen repaints can happen at
		///     any point and it is important to leave the
		///     cursor in a position that would make sense for
		///     the user (as not all terminals support hiding
		///     the cursor), and give the user an impression of
		///     where the cursor is.   For a button, that
		///     would be the position where the hotkey is, for
		///     an entry the location of the editing cursor
		///     and so on.
		///   </para>
		/// </remarks>
		public virtual void PositionCursor ()
		{
			Move (y, x);
		}

		/// <summary>
		///   Method to relayout on size changes.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This method can be overwritten by widgets that
		///     might be interested in adjusting their
		///     contents or children (if they are
		///     containers). 
		///   </para>
		/// </remarks>
		public virtual void DoSizeChanged ()
		{
		}
		
		/// <summary>
		///   Utility function to draw frames
		/// </summary>
		/// <remarks>
		///    Draws a frame with the current color in the
		///    specified coordinates.
		/// </remarks>
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

		/// <summary>
		///   The color used for rendering an unfocused widget.
		/// </summary>
		public int ColorNormal {
			get {
				return Container.ContainerColorNormal;
			}
		}

		/// <summary>
		///   The color used for rendering a focused widget.
		/// </summary>
		public int ColorFocus {
			get {
				return Container.ContainerColorFocus;
			}
		}

		/// <summary>
		///   The color used for rendering the hotkey on an
		///     unfocused widget. 
		/// </summary>
		public int ColorHotNormal {
			get {
				return Container.ContainerColorHotNormal;
			}
		}

		/// <summary>
		///   The color used to render a hotkey in a focused widget.
		/// </summary>
		public int ColorHotFocus {
			get {
				return Container.ContainerColorHotFocus;
			}
		}
	}

	/// <summary>
	///   Label widget, displays a string at a given position.
	/// </summary>
	public class Label : Widget {
		protected string text;
		public int Color = -1;
		
		/// <summary>
		///   Public constructor: creates a label at the given
		///   coordinate with the given string.
		/// </summary>
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


		/// <summary>
		///   The text displayed by this widget.
		/// </summary>
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

	/// <summary>
	///   A label that can be trimmed to a given position
	/// </summary>
	/// <remarks>
	///   Just like a label, but it can be trimmed to a given
	///   position if the text being displayed overflows the
	///   specified width. 
	/// </remarks>
	public class TrimLabel : Label {
		string original;
		
		/// <summary>
		///   Public constructor.
		/// </summary>
		/// <remarks>
		/// </remarks>
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
	
	/// <summary>
	///   Text data entry widget
	/// </summary>
	/// <remarks>
	///   The Entry widget provides Emacs-like editing
	///   functionality,  and mouse support.
	/// </remarks>
	public class Entry : Widget {
		string text, kill;
		int first, point;

		/// <summary>
		///   Public constructor.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public Entry (int x, int y, int w, string s) : base (x, y, w, 1)
		{
			if (s == null)
				s = "";
			
			text = s;
			point = s.Length;
			first = point > w ? point - w : 0;
			CanFocus = true;
		}

		/// <summary>
		///   Sets or gets the text in the entry.
		/// </summary>
		/// <remarks>
		/// </remarks>
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

		public override void ProcessMouse (Curses.MouseEvent ev)
		{
			if ((ev.ButtonState & Curses.BUTTON1_CLICKED) == 0)
				return;

			Container.SetFocus (this);

			// We could also set the cursor position.
			point = first + (ev.X - x);
			if (point > text.Length)
				point = text.Length;
			if (point < first)
				point = 0;
			
			Container.Redraw ();
			Container.PositionCursor ();
		}
	}

	/// <summary>
	///   Button widget
	/// </summary>
	/// <remarks>
	///   Provides a button that can be clicked, or pressed with
	///   the enter key and processes hotkeys (the first uppercase
	///   letter in the button becomes the hotkey).
	/// </remarks>
	public class Button : Widget {
		public string text;
		char hot_key;
		int  hot_pos = -1;
		bool is_default;
		
		/// <summary>
		///   Clicked event, raised when the button is clicked.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the button is activated either with
		///   the mouse or the keyboard.
		/// </remarks>
		public event EventHandler Clicked;

		/// <summary>
		///   Public constructor, creates a button based on
		///   the given text at position 0,0
		/// </summary>
		/// <remarks>
		///   The size of the button is computed based on the
		///   text length.   This button is not a default button.
		/// </remarks>
		public Button (string s) : this (0, 0, s) {}
		
		/// <summary>
		///   Public constructor, creates a button based on
		///   the given text.
		/// </summary>
		/// <remarks>
		///   If the value for is_default is true, a special
		///   decoration is used, and the enter key on a
		///   dialog would implicitly activate this button.
		/// </remarks>
		public Button (string s, bool is_default) : this (0, 0, s, is_default) {}
		
		/// <summary>
		///   Public constructor, creates a button based on
		///   the given text at the given position.
		/// </summary>
		/// <remarks>
		///   The size of the button is computed based on the
		///   text length.   This button is not a default button.
		/// </remarks>
		public Button (int x, int y, string s) : this (x, y, s, false) {}
		
		/// <summary>
		///   Public constructor, creates a button based on
		///   the given text at the given position.
		/// </summary>
		/// <remarks>
		///   If the value for is_default is true, a special
		///   decoration is used, and the enter key on a
		///   dialog would implicitly activate this button.
		/// </remarks>
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

		public override void ProcessMouse (Curses.MouseEvent ev)
		{
			if ((ev.ButtonState & Curses.BUTTON1_CLICKED) != 0){
				Container.SetFocus (this);
				Container.Redraw ();
				if (Clicked != null)
					Clicked (this, EventArgs.Empty);
			}
		}
	}

	/// <summary>
	///   Model for the <see cref="ListView"/> widget.
	/// </summary>
	/// <remarks>
	///   Consumers of the <see cref="ListView"/> widget should
	///   implement this interface
	/// </remarks>
	public interface IListProvider {
		/// <summary>
		///   Number of items in the model.
		/// </summary>
		/// <remarks>
		///   This should return the number of items in the
		///   model. 
		/// </remarks>
		int Items { get; }

		/// <summary>
		///   Whether the ListView should allow items to be
		///   marked. 
		/// </summary>
		bool AllowMark { get; }

		/// <summary>
		///   Whether the given item is marked.
		/// </summary>
		bool IsMarked (int item);

		/// <summary>
		///   This should render the item at the given line,
		///   col with the specified width.
		/// </summary>
		void Render (int line, int col, int width, int item);

		/// <summary>
		///   Callback: this is the way that the model is
		///   hooked up to its actual view. 
		/// </summary>
		void SetListView (ListView target);

		/// <summary>
		///   Allows the model to process the given keystroke.
		/// </summary>
		/// <remarks>
		///   The model should return true if the key was
		///   processed, false otherwise.
		/// </remarks>
		bool ProcessKey (int ch);

		/// <summary>
		///   Callback: invoked when the selected item has changed.
		/// </summary>
		void SelectedChanged ();
	}
	
	/// <summary>
	///   A Listview widget.
	/// </summary>
	/// <remarks>
	///   This widget renders a list of data.   The actual
	///   rendering is implemented by an instance of the class
	///   IListProvider that must be supplied at construction time.
	/// </remarks>
	public class ListView : Widget {
		int items;
		int top;
		int selected;
		bool allow_mark;
		IListProvider provider;
		
		/// <summary>
		///   Public constructor.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public ListView (int x, int y, int w, int h, IListProvider provider) : base (x, y, w, h)
		{
			CanFocus = true;

			this.provider = provider;
			provider.SetListView (this);
			items = provider.Items;
			allow_mark = provider.AllowMark;
		}
		
		/// <summary>
		///   This method can be invoked by the model to
		///   notify the view that the contents of the model
		///   have changed.
		/// </summary>
		/// <remarks>
		///   Invoke this method to invalidate the contents of
		///   the ListView and force the ListView to repaint
		///   the contents displayed.
		/// </remarks>
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
					return true;
				} else
					return false;

			case 14: // Control-n
			case Curses.KeyDown:
				if (selected+1 < items){
					selected++;
					if (selected >= top + h){
						top++;
					}
					SelectedChanged ();
					Redraw ();
					return true;
				} else 
					return false;

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

		/// <summary>
		///   Returns the index of the currently selected item.
		/// </summary>
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
		
		public override void ProcessMouse (Curses.MouseEvent ev)
		{
			if ((ev.ButtonState & Curses.BUTTON1_CLICKED) == 0)
				return;

			ev.X -= x;
			ev.Y -= y;

			if (ev.Y < 0)
				return;
			if (ev.Y+top >= items)
				return;
			selected = ev.Y - top;
			SelectedChanged ();
			
			Redraw ();
		}
	}
	
	/// <summary>
	///   Container widget, can host other widgets.
	/// </summary>
	/// <remarks>
	///   This implements the foundation for other containers
	///   (like Dialogs and Frames) that can host other widgets
	///   inside their boundaries.   It provides focus handling
	///   and event routing.
	/// </remarks>
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
		
		/// <summary>
		///   Public constructor.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public Container (int x, int y, int w, int h) : base (x, y, w, h)
		{
			ContainerColorNormal = Application.ColorNormal;
			ContainerColorFocus = Application.ColorFocus;
			ContainerColorHotNormal = Application.ColorHotNormal;
			ContainerColorHotFocus = Application.ColorHotFocus;
		}

		/// <summary>
		///   Called on top-level container before starting up.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public virtual void Prepare ()
		{
		}

		/// <summary>
		///   Used to redraw all the children in this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
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

		/// <summary>
		///   Focuses the specified widget in this container.
		/// </summary>
		/// <remarks>
		///   Focuses the specified widge, taking the focus
		///   away from any previously focused widgets.   This
		///   method only works if the widget specified
		///   supports being focused.
		/// </remarks>
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

		/// <summary>
		///   Focuses the first possible focusable widget in
		///   the contained widgets.
		/// </summary>
		public void EnsureFocus ()
		{
			if (focused == null)
				FocusFirst();
		}
		
		/// <summary>
		///   Focuses the first widget in the contained widgets.
		/// </summary>
		public void FocusFirst ()
		{
			foreach (Widget w in widgets){
				if (w.CanFocus){
					SetFocus (w);
					return;
				}
			}
		}

		/// <summary>
		///   Focuses the last widget in the contained widgets.
		/// </summary>
		public void FocusLast ()
		{
			for (int i = widgets.Count; i > 0; ){
				i--;

				Widget w = (Widget) widgets [i];
				if (w.CanFocus){
					SetFocus (w);
					return;
				}
			}
		}

		/// <summary>
		///   Focuses the previous widget.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public bool FocusPrev ()
		{
			if (focused == null){
				FocusLast ();
				return true;
			}
			int focused_idx = -1;
			for (int i = widgets.Count; i > 0; ){
				i--;
				Widget w = (Widget)widgets [i];

				if (w.HasFocus){
					Container c = w as Container;
					if (c != null){
						if (c.FocusPrev ())
							return true;
					}
					focused_idx = i;
					continue;
				}
				if (w.CanFocus && focused_idx != -1){
					focused.HasFocus = false;

					Container c = w as Container;
					if (c != null && c.CanFocus){
						c.FocusLast ();
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

		/// <summary>
		///   Focuses the next widget.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public bool FocusNext ()
		{
			if (focused == null){
				FocusFirst ();
				return true;
			}
			int n = widgets.Count;
			int focused_idx = -1;
			for (int i = 0; i < n; i++){
				Widget w = (Widget)widgets [i];

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

		/// <summary>
		///   Returns the base position for child widgets to
		///   paint on.   
		/// </summary>
		/// <remarks>
		///   This method is typically overwritten by
		///   containers that want to have some padding (like
		///   Frames or Dialogs).
		/// </remarks>
		public virtual void GetBase (out int row, out int col)
		{
			row = 0;
			col = 0;
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
		
		/// <summary>
		///   Adds a widget to this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
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

		public override void ProcessMouse (Curses.MouseEvent ev)
		{
			int bx, by;

			GetBase (out bx, out by);
			ev.X -= x;
			ev.Y -= y;
			
			foreach (Widget w in widgets){
				int wx = w.x + bx;
				int wy = w.y + by;

				Log ("considering {0}", w);
				if ((ev.X < wx) || (ev.X > (wx + w.w)))
					continue;

				if ((ev.Y < wy) || (ev.Y > (wy + w.h)))
					continue;
				
				Log ("OK {0}", w);
				ev.X -= bx;
				ev.Y -= by;

				w.ProcessMouse (ev);
				return;
			}			
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

		/// <summary>
		///   Raised when the size of this container changes.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public event EventHandler SizeChangedEvent;
		
		/// <summary>
		///   This method is invoked when the size of this
		///   container changes. 
		/// </summary>
		/// <remarks>
		/// </remarks>
		public void SizeChanged ()
		{
			if (SizeChangedEvent != null)
				SizeChangedEvent (this, EventArgs.Empty);
			DoSizeChanged ();
		}
	}

	/// <summary>
	///   Framed-container widget.
	/// </summary>
	/// <remarks>
	///   A container that provides a frame around its children,
	///   and an optional title.
	/// </remarks>
	public class Frame : Container {
		public string Title;

		/// <summary>
		///   Creates an empty frame, with the given title
		/// </summary>
		/// <remarks>
		/// </remarks>
		public Frame (string title) : this (0, 0, 0, 0, title)
		{
		}
		
		/// <summary>
		///   Public constructor, a frame, with the given title.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public Frame (int x, int y, int w, int h, string title) : base (x, y, w, h)
		{
			Title = title;
			Border++;
		}

		public override void GetBase (out int row, out int col)
		{
			row = 1;
			col = 1;
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

	/// <summary>
	///   A Dialog is a container that can also have a number of
	///   buttons at the bottom
	/// </summary>
	/// <remarks>
	///   <para>Dialogs are containers that can have a set of buttons at
	///   the bottom.   Dialogs are automatically centered on the
	///   screen, and on screen changes the buttons are
	///   relaid out.</para>
	/// <para>
	///   To make the dialog box run until an option has been
	///   executed, you would typically create the dialog box and
	///   then call Application.Run on the Dialog instance.
	/// </para>
	/// </remarks>
	public class Dialog : Frame {
		int button_len;
		ArrayList buttons;

		const int button_space = 3;
		
		/// <summary>
		///   Public constructor.
		/// </summary>
		/// <remarks>
		/// </remarks>
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

		/// <summary>
		///   Adds a button to the dialog
		/// </summary>
		/// <remarks>
		/// </remarks>
		public void AddButton (Button b)
		{
			if (buttons == null)
				buttons = new ArrayList ();
			
			buttons.Add (b);
			button_len += b.w + button_space;

			Add (b);
		}
		
		public override void GetBase (out int row, out int col)
		{
			base.GetBase (out row, out col);
			row++;
			col++;
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
	
	/// <summary>
	///   gui.cs Application driver.
	/// </summary>
	/// <remarks>
	///   Before using gui.cs, you must call Application.Init, then
	///   you would create your toplevel container (typically by
	///   calling:  new Container (0, 0, Application.Cols,
	///   Application.Lines), adding widgets to it and finally
	///   calling Application.Run on the toplevel container. 
	/// </remarks>
	public class Application {
		/// <summary>
		///   Color used for unfocused widgets.
		/// </summary>
		public static int ColorNormal;
		/// <summary>
		///   Color used for focused widgets.
		/// </summary>
		public static int ColorFocus;
		/// <summary>
		///   Color used for hotkeys in unfocused widgets.
		/// </summary>
		public static int ColorHotNormal;
		/// <summary>
		///   Color used for hotkeys in focused widgets.
		/// </summary>
		public static int ColorHotFocus;
		
		/// <summary>
		///   Color used for marked entries.
		/// </summary>
		public static int ColorMarked;
		/// <summary>
		///   Color used for marked entries that are currently
		///   selected with the cursor.
		/// </summary>
		public static int ColorMarkedSelected;
		
		/// <summary>
		///   Color for unfocused widgets on a dialog.
		/// </summary>
		public static int ColorDialogNormal;
		/// <summary>
		///   Color for focused widgets on a dialog.
		/// </summary>
		public static int ColorDialogFocus;
		/// <summary>
		///   Color for hotkeys in an unfocused widget on a dialog.
		/// </summary>
		public static int ColorDialogHotNormal;
		/// <summary>
		///   Color for a hotkey in a focused widget on a dialog.
		/// </summary>
		public static int ColorDialogHotFocus;

		/// <summary>
		///   Color used for error text.
		/// </summary>
		public static int ColorError;
		
		/// <summary>
		///   The time before we timeout on a curses call.
		/// </summary>
		/// <remarks>
		///   This is needed for applications that need to
		///   poll or update other bits of information at
		///   specified intervals.
		///   <para>The default value -1, means to wait until
		///   an event is ready.   If the value is zero, then
		///   events are only processed if they are available,
		///   otherwise it is timeout in milliseconds to wait
		///   for an event to arrive before running an
		///   iteration on the main loop.   See <see cref="Iteration"/>.</para>
		/// </remarks>
		static public int Timeout = -1;

		/// <summary>
		///   This event is raised on each iteration of the
		///   main loop. 
		/// </summary>
		/// <remarks>
		///   See also <see cref="Timeout"/>
		/// </remarks>
		static public event EventHandler Iteration;

		// Private variables
		static ArrayList toplevels = new ArrayList ();
		static short last_color_pair;
		static bool inited;
		static Container empty_container;
		public static long MouseEventsAvailable;
		
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
		
		/// <summary>
		///    Initializes the runtime.   The boolean flag
		///   indicates whether we are forcing color to be off.
		/// </summary>
		public static void Init (bool disable_color)
		{
			empty_container = new Container (0, 0, Application.Cols, Application.Lines);

			try {
				Curses.initscr ();
			} catch (Exception e){
				Console.WriteLine ("Curses failed to initialize, the exception is: " + e);
				throw new Exception ("Application.Init failed");
			}

			if (inited)
				return;
			inited = true;

			long old = 0;
			MouseEventsAvailable = Curses.console_sharp_mouse_mask (
				Curses.BUTTON1_CLICKED | Curses.BUTTON1_DOUBLE_CLICKED, out old);
			
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

		/// <summary>
		///   The number of lines on the screen
		/// </summary>
		static public int Lines {	
			get {
				return Curses.Lines;
			}
		}

		/// <summary>
		///   The number of columns on the screen
		/// </summary>
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

		/// <summary>
		///   Displays a message on a modal dialog box.
		/// </summary>
		/// <remarks>
		///   The error boolean indicates whether this is an
		///   error message box or not.   
		/// </remarks>
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
		
		/// <summary>
		///   Displays an error message.
		/// </summary>
		/// <remarks>
		/// </remarks>
		static public void Error (string caption, string text)
		{
			Msg (true, caption, text);
		}
		
		/// <summary>
		///   Displays an error message.
		/// </summary>
		/// <remarks>
		///   Overload that allows for String.Format parameters.
		/// </remarks>
		static public void Error (string caption, string format, params object [] pars)
		{
			string t = String.Format (format, pars);

			Msg (true, caption, t);
		}

		/// <summary>
		///   Displays an informational message.
		/// </summary>
		/// <remarks>
		/// </remarks>
		static public void Info (string caption, string text)
		{
			Msg (false, caption, text);
		}
		
		/// <summary>
		///   Displays an informational message.
		/// </summary>
		/// <remarks>
		///   Overload that allows for String.Format parameters.
		/// </remarks>
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

		/// <summary>
		///   Forces a repaint of the screen.
		/// </summary>
		/// <remarks>
		/// </remarks>
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

		/// <summary>
		///   Runs the main loop on the given container.
		/// </summary>
		/// <remarks>
		///   This method is used to start processing events
		///   for the main application, but it is also used to
		///   run modal dialog boxes.
		/// </remarks>
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
				
				if (ch == Curses.KeyMouse){
					Curses.MouseEvent ev;
					
					Curses.console_sharp_getmouse (out ev);
					container.ProcessMouse (ev);
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
				if (ch == 9 || ch == Curses.KeyDown || ch == Curses.KeyRight){
					if (!container.FocusNext ())
						container.FocusNext ();
					continue;
				} else if (ch == Curses.KeyUp || ch == Curses.KeyLeft){
					if (!container.FocusPrev ())
						container.FocusPrev ();
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
