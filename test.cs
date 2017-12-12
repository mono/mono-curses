using System;
using Mono;
using Unix.Terminal;

class Demo {
	static void Main ()
	{
		// Standard Init sequence
		Curses.initscr ();
		Curses.cbreak ();
		Curses.noecho ();

		// Recommended
		Curses.nonl ();

		Curses.addch ('ó');
		Curses.addch ('ó');
		Curses.addch ('ó');
		Curses.addch ('ó');
		Curses.addch ('ó');
		Curses.addch ('ó');
		Curses.addch ('ó');
		Curses.addstr ("acción");
		Curses.refresh ();
		Curses.getch ();
		
		Curses.Window.Standard.intrflush (false);
		Curses.Window.Standard.keypad (true);

		// Shutdown
		Curses.endwin ();
	}
}
