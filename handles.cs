using System;
using System.Runtime.InteropServices;

namespace Mono.Terminal {

	public class Window {
		public readonly IntPtr Handle;
		static Window curscr;
		static Window stdscr;

		static Window ()
		{
			Curses.initscr ();
			stdscr = new Window (Curses.console_sharp_get_stdscr ());
			curscr = new Window (Curses.console_sharp_get_curscr ());
		}
		
		internal Window (IntPtr handle) 
		{
			Handle = handle;
		}
		
		static public Window Standard {
			get {
				return stdscr;
			}
		}

		static public Window Current {
			get {
				return curscr;
			}
		}

		
		public int wtimeout (int delay)
		{
			return Curses.wtimeout (Handle, delay);
		}

		public int notimeout (bool bf)
		{
			return Curses.notimeout (Handle, bf);
		}

		public int keypad (bool bf)
		{
			return Curses.keypad (Handle, bf);
		}

		public int meta (bool bf)
		{
			return Curses.meta (Handle, bf);
		}

		public int intrflush (bool bf)
		{
			return Curses.intrflush (Handle, bf);
		}

		public int clearok (bool bf)
		{
			return Curses.clearok (Handle, bf);
		}
		
		public int idlok (bool bf)
		{
			return Curses.idlok (Handle, bf);
		}
		
		public void idcok (bool bf)
		{
			Curses.idcok (Handle, bf);
		}
		
		public void immedok (bool bf)
		{
			Curses.immedok (Handle, bf);
		}
		
		public int leaveok (bool bf)
		{
			return Curses.leaveok (Handle, bf);
		}
		
		public int setscrreg (int top, int bot)
		{
			return Curses.wsetscrreg (Handle, top, bot);
		}
		
		public int scrollok (bool bf)
		{
			return Curses.scrollok (Handle, bf);
		}
		
		public int wrefresh ()
		{
			return Curses.wrefresh (Handle);
		}

		public int redrawwin ()
		{
			return Curses.redrawwin (Handle);
		}
		
		public int wredrawwin (int beg_line, int num_lines)
		{
			return Curses.wredrawwin (Handle, beg_line, num_lines);
		}

		public int wnoutrefresh ()
		{
			return Curses.wnoutrefresh (Handle);
		}

		public int move (int line, int col)
		{
			return Curses.wmove (Handle, line, col);
		}

		public int addch (char ch)
		{
			return Curses.waddch (Handle, ch);
		}

		public int refresh ()
		{
			return Curses.wrefresh (Handle);
		}
	}

	// Currently unused, to do later
	public class Screen {
		public readonly IntPtr Handle;
		
		internal Screen (IntPtr handle)
		{
			Handle = handle;
		}
	}
}
