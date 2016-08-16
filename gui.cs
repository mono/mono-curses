//
// gui.cs: Simple curses-based GUI toolkit, core
//
// Authors:
//   Miguel de Icaza (miguel.de.icaza@gmail.com)
//
// Copyright (C) 2007-2011 Novell (http://www.novell.com)
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
using System.Collections.Generic;
using Mono;
using System.IO;

namespace Mono.Terminal {

	/// <summary>
	/// The fill values apply from the given x, y values, they will not do
	/// a full fill, you must compute x, y yourself.
	/// </summary>
	[Flags]
	public enum Fill {
		None = 0,
		Horizontal = 1,
		Vertical = 2
	}


	// Keys in addition to what Curses constants provide
	class Keys
	{
		public const int CtrlA = 1;
		public const int CtrlB = 2;
		public const int CtrlC = 3;
		public const int CtrlD = 4;
		public const int CtrlE = 5;
		public const int CtrlF = 6;
		public const int Tab = 9;
		public const int CtrlK = 11;
		public const int CtrlN = 14;
		public const int CtrlP = 16;
		public const int CtrlV = 22;
		public const int CtrlY = 25;
		public const int CtrlZ = 26;

		public const int Esc = 27;
		public const int Enter = '\n';

		public const int ShiftTab = 353;
	}


	public delegate void Action ();
	
	/// <summary>
	///   Base class for creating curses widgets
	/// </summary>
	public abstract class Widget {
		/// <summary>
		///    Points to the container of this widget
		/// </summary>
		public Container Container;
		
		/// <summary>
		///    The x position of this widget
		/// </summary>
		public int x;

		/// <summary>
		///    The y position of this widget
		/// </summary>
		public int y;

		/// <summary>
		///    The width of this widget, it is the area that receives mouse events and that must be repainted.
		/// </summary>
		public int w;

		/// <summary>
		///    The height of this widget, it is the area that receives mouse events and that must be repainted.
		/// </summary>
		public int h;
		
		bool can_focus;
		bool has_focus;
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
				l = new StreamWriter (File.Create ("log2"));
			
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
		public virtual bool HasFocus {
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
		///     ev.ButtonState &amp; Curses.Event.Button1Clicked).
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
		///   This method can be overwritten by widgets that
		///     want to provide accelerator functionality
		///     (Alt-key for example), but without
		///     interefering with normal ProcessKey behavior.
		/// </summary>
		/// <remarks>
		///   <para>
		///     After keys are sent to the widgets on the
		///     current Container, all the widgets are
		///     processed and the key is passed to the widgets
		///     to allow some of them to process the keystroke
		///     as a cold-key. </para>
		///  <para>
		///    This functionality is used, for example, by
		///    default buttons to act on the enter key.
		///    Processing this as a hot-key would prevent
		///    non-default buttons from consuming the enter
		///    keypress when they have the focus.
		///  </para>
		/// </remarks>
		public virtual bool ProcessColdKey (int key)
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
			DrawFrame (col, line, width, height, false);
		}

		/// <summary>
		///   Utility function to draw strings that contain a hotkey
		/// </summary>
		/// <remarks>
		///    Draws a string with the given color.   If a character "_" is
		///    found, then the next character is drawn using the hotcolor.
		/// </remarks>
		static public void DrawHotString (string s, int hotcolor, int color)
		{
			Curses.attrset (color);
			foreach (char c in s){
				if (c == '_'){
					Curses.attrset (hotcolor);
					continue;
				}
				Curses.addch (c);
				Curses.attrset (color);
			}
		}

		/// <summary>
		///   Utility function to draw frames
		/// </summary>
		/// <remarks>
		///    Draws a frame with the current color in the
		///    specified coordinates.
		/// </remarks>
		static public void DrawFrame (int col, int line, int width, int height, bool fill)
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
				if (fill){
					for (int x = 1; x < width-1; x++)
						Curses.addch (' ');
				} else
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

		public Label (int x, int y, string s, params object [] args) : base (x, y, String.Format (s, args).Length, 1)
		{
			text = String.Format (s, args);
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

		/// <summary>
		///   The text displayed by this widget.
		/// </summary>
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
		int color;
		bool used;
		
		/// <summary>
		///   Changed event, raised when the text has clicked.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the text in the entry changes.
		/// </remarks>
		public event EventHandler Changed;
		
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
			Color = Application.ColorDialogFocus;
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

		/// <summary>
		///   Sets the secret property.
		/// </summary>
		/// <remarks>
		///   This makes the text entry suitable for entering passwords. 
		/// </remarks>
		public bool Secret { get; set; }

		/// <summary>
		///    The color used to display the text
		/// </summary>
		public int Color {
			get { return color; }
			set { color = value; Container.Redraw (); }
		}

		/// <summary>
		///    The current cursor position.
		/// </summary>
		public int CursorPosition { get { return point; }}
		
		/// <summary>
		///   Sets the cursor position.
		/// </summary>
		public override void PositionCursor ()
		{
			Move (y, x+point-first);
		}
		
		public override void Redraw ()
		{
			Curses.attrset (Color);
			Move (y, x);
			
			for (int i = 0; i < w; i++){
				int p = first + i;

				if (p < text.Length){
					Curses.addch (Secret ? '*' : text [p]);
				} else
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
			Curses.refresh ();
		}

		void SetText (string new_text)
		{
			text = new_text;
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		public override bool ProcessKey (int key)
		{
			switch (key){
			case 127:
			case Curses.KeyBackspace:
				if (point == 0)
					return true;
				
				SetText (text.Substring (0, point - 1) + text.Substring (point));
				point--;
				Adjust ();
				break;

			case Curses.KeyHome:
			case Keys.CtrlA: // Home
				point = 0;
				Adjust ();
				break;

			case Curses.KeyLeft:
			case Keys.CtrlB: // back character
				if (point > 0){
					point--;
					Adjust ();
				}
				break;

			case Keys.CtrlD: // Delete
				if (point == text.Length)
					break;
				SetText (text.Substring (0, point) + text.Substring (point+1));
				Adjust ();
				break;
				
			case Keys.CtrlE: // End
				point = text.Length;
				Adjust ();
				break;

			case Curses.KeyRight:
			case Keys.CtrlF: // Control-f, forward char
				if (point == text.Length)
					break;
				point++;
				Adjust ();
				break;

			case Keys.CtrlK: // kill-to-end
				kill = text.Substring (point);
				SetText (text.Substring (0, point));
				Adjust ();
				break;

			case Keys.CtrlY: // Control-y, yank
				if (kill == null)
					return true;
				
				if (point == text.Length){
					SetText (text + kill);
					point = text.Length;
				} else {
					SetText (text.Substring (0, point) + kill + text.Substring (point));
					point += kill.Length;
				}
				Adjust ();
				break;

			case (int) 'b' + Curses.KeyAlt:
				int bw = WordBackward (point);
				if (bw != -1)
					point = bw;
				Adjust ();
				break;

			case (int) 'f' + Curses.KeyAlt:
				int fw = WordForward (point);
				if (fw != -1)
					point = fw;
				Adjust ();
				break;
			
			default:
				// Ignore other control characters.
				if (key < 32 || key > 255)
					return false;

				if (used){
					if (point == text.Length){
						SetText (text + (char) key);
					} else {
						SetText (text.Substring (0, point) + (char) key + text.Substring (point));
					}
					point++;
				} else {
					SetText ("" + (char) key);
					first = 0;
					point = 1;
				}
				used = true;
				Adjust ();
				return true;
			}
			used = true;
			return true;
		}

		int WordForward (int p)
		{
			if (p >= text.Length)
				return -1;

			int i = p;
			if (Char.IsPunctuation (text [p]) || Char.IsWhiteSpace (text[p])){
				for (; i < text.Length; i++){
					if (Char.IsLetterOrDigit (text [i]))
					    break;
				}
				for (; i < text.Length; i++){
					if (!Char.IsLetterOrDigit (text [i]))
					    break;
				}
			} else {
				for (; i < text.Length; i++){
					if (!Char.IsLetterOrDigit (text [i]))
					    break;
				}
			}
			if (i != p)
				return i;
			return -1;
		}

		int WordBackward (int p)
		{
			if (p == 0)
				return -1;

			int i = p-1;
			if (i == 0)
				return 0;
			
			if (Char.IsPunctuation (text [i]) || Char.IsSymbol (text [i]) || Char.IsWhiteSpace (text[i])){
				for (; i >= 0; i--){
					if (Char.IsLetterOrDigit (text [i]))
						break;
				}
				for (; i >= 0; i--){
					if (!Char.IsLetterOrDigit (text[i]))
						break;
				}
			} else {
				for (; i >= 0; i--){
					if (!Char.IsLetterOrDigit (text [i]))
						break;
				}
			}
			i++;
			
			if (i != p)
				return i;

			return -1;
		}
		
		
		public override void ProcessMouse (Curses.MouseEvent ev)
		{
			if ((ev.ButtonState & Curses.Event.Button1Clicked) == 0)
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
		string text;
		string shown_text;
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
		///   The text displayed by this widget.
		/// </summary>
		public string Text {
			get {
				return text;
			}

			set {
				text = value;
				if (is_default)
					shown_text = "[< " + value + " >]";
				else
					shown_text = "[ " + value + " ]";

				hot_pos = -1;
				hot_key = (char) 0;
				int i = 0;
				foreach (char c in shown_text){
					if (Char.IsUpper (c)){
						hot_key = c;
						hot_pos = i;
						break;
					}
					i++;
				}
			}
		}

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
			Text = s;
		}

		public override void Redraw ()
		{
			Curses.attrset (HasFocus ? ColorFocus : ColorNormal);
			Move (y, x);
			Curses.addstr (shown_text);

			if (hot_pos != -1){
				Move (y, x + hot_pos);
				Curses.attrset (HasFocus ? ColorHotFocus : ColorHotNormal);
				Curses.addch (hot_key);
			}
		}

		public override void PositionCursor ()
		{
			Move (y, x + hot_pos);
		}

		bool CheckKey (int key)
		{
			if (Char.ToUpper ((char)key) == hot_key){
				Container.SetFocus (this);
				if (Clicked != null)
					Clicked (this, EventArgs.Empty);
				return true;
			}
			return false;
		}
			
		public override bool ProcessHotKey (int key)
		{
			int k = Curses.IsAlt (key);
			if (k != 0)
				return CheckKey (k);

			return false;
		}

		public override bool ProcessColdKey (int key)
		{
			if (is_default && key == '\n'){
				if (Clicked != null)
					Clicked (this, EventArgs.Empty);
				return true;
			}
			return CheckKey (key);
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
			if ((ev.ButtonState & Curses.Event.Button1Clicked) != 0){
				Container.SetFocus (this);
				Container.Redraw ();
				if (Clicked != null)
					Clicked (this, EventArgs.Empty);
			}
		}
	}

	public class CheckBox : Widget {
		string text;
		int hot_pos = -1;
		char hot_key;
		
		/// <summary>
		///   Toggled event, raised when the CheckButton is toggled.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the checkbutton is activated either with
		///   the mouse or the keyboard.
		/// </remarks>
		public event EventHandler Toggled;

		/// <summary>
		///   Public constructor, creates a CheckButton based on
		///   the given text at the given position.
		/// </summary>
		/// <remarks>
		///   The size of CheckButton is computed based on the
		///   text length. This CheckButton is not toggled.
		/// </remarks>
		public CheckBox (int x, int y, string s) : this (x, y, s, false)
		{
		}

		/// <summary>
		///   Public constructor, creates a CheckButton based on
		///   the given text at the given position and a state.
		/// </summary>
		/// <remarks>
		///   The size of CheckButton is computed based on the
		///   text length. 
		/// </remarks>
		public CheckBox (int x, int y, string s, bool is_checked) : base (x, y, s.Length + 4, 1)
		{
			Checked = is_checked;
			Text = s;

			CanFocus = true;
		}

		/// <summary>
		///    The state of the checkbox.
		/// </summary>
		public bool Checked { get; set; }

		/// <summary>
		///   The text displayed by this widget.
		/// </summary>
		public string Text {
			get {
				return text;
			}

			set {
				text = value;

				int i = 0;
				hot_pos = -1;
				hot_key = (char) 0;
				foreach (char c in text){
					if (Char.IsUpper (c)){
						hot_key = c;
						hot_pos = i;
						break;
					}
					i++;
				}
			}
		}
		
		public override void Redraw ()
		{
			Curses.attrset (ColorNormal);
			Move (y, x);
			Curses.addstr (Checked ? "[X] " : "[ ] ");
			Curses.attrset (HasFocus ? ColorFocus : ColorNormal);
			Move (y, x + 3);
			Curses.addstr (Text);
			if (hot_pos != -1){
				Move (y, x + 3 + hot_pos);
				Curses.attrset (HasFocus ? ColorHotFocus : ColorHotNormal);
				Curses.addch (hot_key);
			}
			PositionCursor();
		}

		public override void PositionCursor ()
		{
			Move (y, x + 1);
		}

		public override bool ProcessKey (int c)
		{
			if (c == ' '){
				Checked = !Checked;

				if (Toggled != null)
					Toggled (this, EventArgs.Empty);

				Redraw();
				return true;
			}
			return false;
		}

		public override void ProcessMouse (Curses.MouseEvent ev)
		{
			if ((ev.ButtonState & Curses.Event.Button1Clicked) != 0){
				Container.SetFocus (this);
				Container.Redraw ();

				Checked = !Checked;
				
				if (Toggled != null)
					Toggled (this, EventArgs.Empty);
				Redraw ();
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
			if (top > provider.Items){
				if (provider.Items > 1)
					top = provider.Items -1;
				else
					top = 0;
			}
			if (selected > provider.Items){
				if (provider.Items > 1)
					selected = provider.Items - 1;
				else
					selected = 0;
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
			case Keys.CtrlP:
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

			case Keys.CtrlN:
			case Curses.KeyDown:
				if (selected+1 < provider.Items){
					selected++;
					if (selected >= top + h){
						top++;
					}
					SelectedChanged ();
					Redraw ();
					return true;
				} else 
					return false;

			case Keys.CtrlV:
			case Curses.KeyNPage:
				n = (selected + h);
				if (n > provider.Items)
					n = provider.Items -1;
				if (n != selected){
					selected = n;
					if (provider.Items >= h)
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

				if (item >= provider.Items){
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
				if (provider.Items == 0)
					return -1;
				return selected;
			}

			set {
				if (value >= provider.Items)
					throw new ArgumentException ("value");

				selected = value;
				SelectedChanged();

				Redraw ();
			}
		}
		
		public override void ProcessMouse (Curses.MouseEvent ev)
		{
			if ((ev.ButtonState & Curses.Event.Button1Clicked) == 0)
				return;

			ev.X -= x;
			ev.Y -= y;

			if (ev.Y < 0)
				return;
			if (ev.Y+top >= provider.Items)
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
	public class Container : Widget, IEnumerable {
		List<Widget> widgets = new List<Widget> ();
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

		public IEnumerator GetEnumerator ()
		{
			return widgets.GetEnumerator ();
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
				return focused != null;
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

		/// <summary>
		///   Removes all the widgets from this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public virtual void RemoveAll ()
		{
			List<Widget> tmp = new List<Widget>();
			foreach (Widget w in widgets)
				tmp.Add(w);
			foreach (Widget w in tmp)
				Remove(w);
		}

		/// <summary>
		///   Removes a widget from this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public virtual void Remove (Widget w)
		{
			if (w == null)
				return;
			
			widgets.Remove (w);
			w.Container = null;
			
			if (widgets.Count < 1)
				this.CanFocus = false;
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

		public override bool ProcessColdKey (int key)
		{
			if (focused != null)
				if (focused.ProcessColdKey (key))
					return true;
			
			foreach (Widget w in widgets){
				if (w == focused)
					continue;
				
				if (w.ProcessColdKey (key))
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

				if ((ev.X < wx) || (ev.X > (wx + w.w)))
					continue;

				if ((ev.Y < wy) || (ev.Y > (wy + w.h)))
					continue;
				
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
			if (Title != null){
				Curses.addch (' ');
				Curses.addstr (Title);
				Curses.addch (' ');
			}
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
		List<Button> buttons;

		const int button_space = 3;
		
		/// <summary>
		///   Public constructor.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public Dialog (int w, int h, string title)
			: base ((Application.Cols - w) / 2, (Application.Lines-h)/3, w, h, title)
		{
			ContainerColorNormal = Application.ColorDialogNormal;
			ContainerColorFocus = Application.ColorDialogFocus;
			ContainerColorHotNormal = Application.ColorDialogHotNormal;
			ContainerColorHotFocus = Application.ColorDialogHotFocus;

			Border++;
		}

		/// <summary>
		///   Makes the default style for the dialog use the error colors.
		/// </summary>
		public void ErrorColors ()
		{
			ContainerColorNormal = Application.ColorError;
			ContainerColorFocus = Application.ColorErrorFocus;
			ContainerColorHotFocus = Application.ColorErrorHotFocus;
			ContainerColorHotNormal = Application.ColorErrorHot;
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
				buttons = new List<Button> ();
			
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
			Curses.attrset (Application.ColorDialogHotNormal);
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
			y = (Application.Lines-h) / 3;

			LayoutButtons ();
		}
	}

	public class MessageBox {
		public static int Query (int width, int height, string title, string message, params string [] buttons)
		{
			var d = new Dialog (width, height, title);
			int clicked = -1, count = 0;
			
			foreach (var s in buttons){
				int n = count++;
				var b = new Button (s);
				b.Clicked += delegate {
					clicked = n;
					d.Running = false;
				};
				d.AddButton (b);
			}
			if (message != null){
				var l = new Label ((width - 4 - message.Length)/2, 0, message);
				d.Add (l);
			}
			
			Application.Run (d);
			return clicked;
		}
	}
	
	public class MenuItem {
		public MenuItem (string title, string help, Action action)
		{
			Title = title ?? "";
			Help = help ?? "";
			Action = action;
			Width = Title.Length + Help.Length + 1;
		}
		public string Title { get; set; }
		public string Help { get; set; }
		public Action Action { get; set; }
		public int Width { get; set; }
	}
	
	public class MenuBarItem {
		public MenuBarItem (string title, MenuItem [] children) 
		{
			Title = title ?? "";
			Children = children;
		}

		public string Title { get; set; }
		public MenuItem [] Children { get; set; }
		public int Current { get; set; }
	}
	
	public class MenuBar : Container {
		public MenuBarItem [] Menus { get; set; }
		int selected;
		Action action;
		
		public MenuBar (MenuBarItem [] menus) : base (0, 0, Application.Cols, 1)
		{
			Menus = menus;
			CanFocus = false;
			selected = -1;
		}

		/// <summary>
		///   Activates the menubar
		/// </summary>
		public void Activate (int idx)
		{
			if (idx < 0 || idx > Menus.Length)
				throw new ArgumentException ("idx");

			action = null;
			selected = idx;

			foreach (var m in Menus)
				m.Current = 0;
			
			Application.Run (this);
			selected = -1;
			Container.Redraw ();
			
			if (action != null)
				action ();
		}

		void DrawMenu (int idx, int col, int line)
		{
			int max = 0;
			var menu = Menus [idx];

			if (menu.Children == null)
				return;

			foreach (var m in menu.Children){
				if (m == null)
					continue;
				
				if (m.Width > max)
					max = m.Width;
			}
			max += 4;
			DrawFrame (col + x, line, max, menu.Children.Length + 2, true);
			for (int i = 0; i < menu.Children.Length; i++){
				var item = menu.Children [i];

				Move (line + 1 + i, col + 1);
				Curses.attrset (item == null ? Application.ColorFocus : i == menu.Current ? Application.ColorMenuSelected : Application.ColorMenu);
				for (int p = 0; p < max - 2; p++)
					Curses.addch (item == null ? Curses.ACS_HLINE : ' ');

				if (item == null)
					continue;
				
				Move (line + 1 + i, col + 2);
				DrawHotString (item.Title,
					       i == menu.Current ? Application.ColorMenuHotSelected : Application.ColorMenuHot,
					       i == menu.Current ? Application.ColorMenuSelected : Application.ColorMenu);

				// The help string
				var l = item.Help.Length;
				Move (line + 1 + i, col + x + max - l - 2);
				Curses.addstr (item.Help);
			}
		}
		
		public override void Redraw ()
		{
			Move (y, 0);
			Curses.attrset (Application.ColorFocus);
			for (int i = 0; i < Application.Cols; i++)
				Curses.addch (' ');

			Move (y, 1);
			int pos = 0;
			for (int i = 0; i < Menus.Length; i++){
				var menu = Menus [i];
				if (i == selected){
					DrawMenu (i, pos, y+1);
					Curses.attrset (Application.ColorMenuSelected);
				} else
					Curses.attrset (Application.ColorFocus);

				Move (y, pos);
				Curses.addch (' ');
				Curses.addstr (menu.Title);
				Curses.addch (' ');
				if (HasFocus && i == selected)
					Curses.attrset (Application.ColorMenuSelected);
				else
					Curses.attrset (Application.ColorFocus);
				Curses.addstr ("  ");
				
				pos += menu.Title.Length + 4;
			}
			PositionCursor ();
		}

		public override void PositionCursor ()
		{
			int pos = 0;
			for (int i = 0; i < Menus.Length; i++){
				if (i == selected){
					pos++;
					Move (y, pos);
					return;
				} else {
					pos += Menus [i].Title.Length + 4;
				}
			}
			Move (y, 0);
		}

		void Selected (MenuItem item)
		{
			Running = false;
			action = item.Action;
		}
		
		public override bool ProcessKey (int key)
		{
			switch (key){
			case Curses.KeyUp:
				if (Menus [selected].Children == null)
					return false;

				int current = Menus [selected].Current;
				do {
					current--;
					if (current < 0)
						current = Menus [selected].Children.Length-1;
				} while (Menus [selected].Children [current] == null);
				Menus [selected].Current = current;
				
				Redraw ();
				Curses.refresh ();
				return true;
				
			case Curses.KeyDown:
				if (Menus [selected].Children == null)
					return false;

				do {
					Menus [selected].Current = (Menus [selected].Current+1) % Menus [selected].Children.Length;
				} while (Menus [selected].Children [Menus [selected].Current] == null);
				
				Redraw ();
				Curses.refresh ();
				break;
				
			case Curses.KeyLeft:
				selected--;
				if (selected < 0)
					selected = Menus.Length-1;
				break;
			case Curses.KeyRight:
				selected = (selected + 1) % Menus.Length;
				break;

			case Keys.Enter:
				if (Menus [selected].Children == null)
					return false;

				Selected (Menus [selected].Children [Menus [selected].Current]);
				break;

			case Keys.Esc:
			case Keys.CtrlC:
				Running = false;
				break;

			default:
				if ((key >= 'a' && key <= 'z') || (key >= 'A' && key <= 'Z') || (key >= '0' && key <= '9')){
					char c = Char.ToUpper ((char)key);

					if (Menus [selected].Children == null)
						return false;
					
					foreach (var mi in Menus [selected].Children){
						int p = mi.Title.IndexOf ('_');
						if (p != -1 && p+1 < mi.Title.Length){
							if (mi.Title [p+1] == c){
								Selected (mi);
								return true;
							}
						}
					}
				}
				    
				return false;
			}
			Container.Redraw ();
			Curses.refresh ();
			return true;
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
		///   Color used for focused widgets on an error dialog.
		/// </summary>
		public static int ColorErrorFocus;
		
		/// <summary>
		///   Color used for hotkeys in error dialogs
		/// </summary>
		public static int ColorErrorHot;
		
		/// <summary>
		///   Color used for hotkeys in a focused widget in an error dialog
		/// </summary>
		public static int ColorErrorHotFocus;
		
		/// <summary>
		///   The basic color of the terminal.
		/// </summary>
		public static int ColorBasic;

		/// <summary>
		///   The regular color for a selected item on a menu
		/// </summary>
		public static int ColorMenuSelected;

		/// <summary>
		///   The hot color for a selected item on a menu
		/// </summary>
		public static int ColorMenuHotSelected;

		/// <summary>
		///   The regular color for a menu entry
		/// </summary>
		public static int ColorMenu;
		
		/// <summary>
		///   The hot color for a menu entry
		/// </summary>
		public static int ColorMenuHot;
		
		/// <summary>
		///   This event is raised on each iteration of the
		///   main loop. 
		/// </summary>
		/// <remarks>
		///   See also <see cref="Timeout"/>
		/// </remarks>
		static public event EventHandler Iteration;

		// Private variables
		static List<Container> toplevels = new List<Container> ();
		static short last_color_pair;
		static bool inited;
		static Container empty_container;
		
		/// <summary>
		///    A flag indicating which mouse events are available
		/// </summary>
		public static Curses.Event MouseEventsAvailable;
		
		/// <summary>
		///    Creates a new Curses color to be used by Gui.cs apps
		/// </summary>
		public static int MakeColor (short f, short b)
		{
			Curses.init_pair (++last_color_pair, f, b);
			return Curses.ColorPair (last_color_pair);
		}

		/// <summary>
		///    The singleton EmptyContainer that covers the entire screen.
		/// </summary>
		static public Container EmptyContainer {
			get {
				return empty_container;
			}
		}

		static Window main_window;
		static MainLoop mainloop;
		public static MainLoop MainLoop {
			get {
				return mainloop;
			}
		}
		
		public static bool UsingColor { get; private set; }
		
		/// <summary>
		///    Initializes the runtime.   The boolean flag
		///   indicates whether we are forcing color to be off.
		/// </summary>
		public static void Init (bool disable_color)
		{
			if (inited)
				return;
			inited = true;

			empty_container = new Container (0, 0, Application.Cols, Application.Lines);

			try {
				main_window = Curses.initscr ();
			} catch (Exception e){
				Console.WriteLine ("Curses failed to initialize, the exception is: " + e);
				throw new Exception ("Application.Init failed");
			}
			Curses.raw ();
			Curses.noecho ();
			//Curses.nonl ();
			Window.Standard.keypad (true);

#if BREAK_UTF8_RENDERING
			Curses.Event old = 0;
			MouseEventsAvailable = Curses.console_sharp_mouse_mask (
				Curses.Event.Button1Clicked | Curses.Event.Button1DoubleClicked, out old);
#endif
	
			UsingColor = false;
			if (!disable_color)
				UsingColor = Curses.has_colors ();
			
			Curses.start_color ();
			Curses.use_default_colors ();
			if (UsingColor){
				ColorNormal = MakeColor (Curses.COLOR_WHITE, Curses.COLOR_BLUE);
				ColorFocus = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_CYAN);
				ColorHotNormal = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_BLUE);
				ColorHotFocus = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_CYAN);

				ColorMenu = Curses.A_BOLD | MakeColor (Curses.COLOR_WHITE, Curses.COLOR_CYAN);
				ColorMenuHot = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_CYAN);
				ColorMenuSelected = Curses.A_BOLD | MakeColor (Curses.COLOR_WHITE, Curses.COLOR_BLACK);
				ColorMenuHotSelected = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_BLACK);
				
				ColorMarked = ColorHotNormal;
				ColorMarkedSelected = ColorHotFocus;

				ColorDialogNormal    = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_WHITE);
				ColorDialogFocus     = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_CYAN);
				ColorDialogHotNormal = MakeColor (Curses.COLOR_BLUE,  Curses.COLOR_WHITE);
				ColorDialogHotFocus  = MakeColor (Curses.COLOR_BLUE,  Curses.COLOR_CYAN);

				ColorError = Curses.A_BOLD | MakeColor (Curses.COLOR_WHITE, Curses.COLOR_RED);
				ColorErrorFocus = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_WHITE);
				ColorErrorHot = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_RED);
				ColorErrorHotFocus = ColorErrorHot;
			} else {
				ColorNormal = Curses.A_NORMAL;
				ColorFocus = Curses.A_REVERSE;
				ColorHotNormal = Curses.A_BOLD;
				ColorHotFocus = Curses.A_REVERSE | Curses.A_BOLD;

				ColorMenu = Curses.A_REVERSE;
				ColorMenuHot = Curses.A_NORMAL;
				ColorMenuSelected = Curses.A_BOLD;
				ColorMenuHotSelected = Curses.A_NORMAL;
				
				ColorMarked = Curses.A_BOLD;
				ColorMarkedSelected = Curses.A_REVERSE | Curses.A_BOLD;

				ColorDialogNormal = Curses.A_REVERSE;
				ColorDialogFocus = Curses.A_NORMAL;
				ColorDialogHotNormal = Curses.A_BOLD;
				ColorDialogHotFocus = Curses.A_NORMAL;

				ColorError = Curses.A_BOLD;
			}
			ColorBasic = MakeColor (-1, -1);
			mainloop = new MainLoop ();
			mainloop.AddWatch (0, MainLoop.Condition.PollIn, x => {
				Container top = toplevels.Count > 0 ? toplevels [toplevels.Count-1] : null;
				if (top != null)
					ProcessChar (top);

				return true;
			});
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

		/// <summary>
		///   Displays a message on a modal dialog box.
		/// </summary>
		/// <remarks>
		///   The error boolean indicates whether this is an
		///   error message box or not.   
		/// </remarks>
		static public void Msg (bool error, string caption, string t)
		{
			var lines = new List<string> ();
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
			if (error)
				d.ErrorColors ();
			
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

			Curses.redrawwin (main_window.Handle);
			foreach (Container c in toplevels){
				c.Redraw ();
				last = c;
			}
			Curses.refresh ();
			if (last != null)
				last.PositionCursor ();
		}

		/// <summary>
		///   Starts running a new container or dialog box.
		/// </summary>
		/// <remarks>
		///   Use this method if you want to start the dialog, but
		///   you want to control the main loop execution manually
		///   by calling the RunLoop method (for example, to start
		///   the dialog, but continuing to process events).
		///
		///    Use the returned value as the argument to RunLoop
		///    and later to the End method to remove the container
		///    from the screen.
		/// </remarks>
		static public RunState Begin (Container container)
		{
			if (container == null)
				throw new ArgumentNullException ("container");
			var rs = new RunState (container);
			
			Init (false);
			
			Curses.timeout (-1);

			toplevels.Add (container);

			container.Prepare ();
			container.SizeChanged ();			
			container.FocusFirst ();
			Redraw (container);
			container.PositionCursor ();
			Curses.refresh ();
			
			return rs;
		}

		/// <summary>
		///   Runs the main loop for the created dialog
		/// </summary>
		/// <remarks>
		///   Calling this method will block until the
		///   dialog has completed execution.
		/// </remarks>
		public static void RunLoop (RunState state)
		{
			RunLoop (state, true);
		}
		
		/// <summary>
		///   Runs the main loop for the created dialog
		/// </summary>
		/// <remarks>
		///   Use the wait parameter to control whether this is a
		///   blocking or non-blocking call.
		/// </remarks>
		public static void RunLoop (RunState state, bool wait)
		{
			if (state == null)
				throw new ArgumentNullException ("state");
			if (state.Container == null)
				throw new ObjectDisposedException ("state");
			
			for (state.Container.Running = true; state.Container.Running; ){
				if (mainloop.EventsPending (wait)){
					mainloop.MainIteration ();
					if (Iteration != null)
						Iteration (null, EventArgs.Empty);
				} else if (wait == false)
					return;
			}
		}

		public static void Stop ()
		{
			if (toplevels.Count == 0)
				return;
			toplevels [toplevels.Count-1].Running = false;
			MainLoop.Stop ();
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
			var runToken = Begin (container);
			RunLoop (runToken);
			End (runToken);
		}

		/// <summary>
		///   Use this method to complete an execution started with Begin
		/// </summary>
		static public void End (RunState state)
		{
			if (state == null)
				throw new ArgumentNullException ("state");
			state.Dispose ();
		}

		// Called by the Dispose handler.
		internal static void End (Container container)
		{
			toplevels.Remove (container);
			if (toplevels.Count == 0)
				Shutdown ();
			else
				Refresh ();
		}
				
		static void ProcessChar (Container container)
		{
			int ch = Curses.getch ();

			if ((ch == -1) || (ch == Curses.KeyResize)){
				if (Curses.CheckWinChange ()){
					EmptyContainer.Clear ();
					foreach (Container c in toplevels)
						c.SizeChanged ();
					Refresh ();
				}
				return;
			}
				
			if (ch == Curses.KeyMouse){
				Curses.MouseEvent ev;
				
				Curses.console_sharp_getmouse (out ev);
				container.ProcessMouse (ev);
				return;
			}
				
			if (ch == Keys.Esc){
				Curses.timeout (100);
				int k = Curses.getch ();
				if (k != Curses.ERR && k != Keys.Esc)
					ch = Curses.KeyAlt | k;
				Curses.timeout (-1);
			}
			
			if (container.ProcessHotKey (ch))
				return;
			
			if (container.ProcessKey (ch))
				return;

			if (container.ProcessColdKey (ch))
				return;
			
			// Control-c, quit the current operation.
			if (ch == Keys.CtrlC){
				container.Running = false;
				return;
			}
			
			// Control-z, suspend execution, then repaint.
			if (ch == Keys.CtrlZ){
				Curses.console_sharp_sendsigtstp ();
				Window.Standard.redrawwin ();
				Curses.refresh ();
			}
			
			//
			// Focus handling
			//
			if (ch == Keys.Tab){
				if (!container.FocusNext ())
					container.FocusNext ();
				Curses.refresh ();
			} else if (ch == Keys.ShiftTab){
				if (!container.FocusPrev ())
					container.FocusPrev ();
				Curses.refresh ();
			}
		}
	}


	public class RunState : IDisposable {
		internal RunState (Container container)
		{
			Container = container;
		}
		internal Container Container;

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize(this);
		}

		public virtual void Dispose (bool disposing)
		{
			if (Container != null){
				Application.End (Container);
				Container = null;
			}
		}
	}
}
