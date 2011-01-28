using System;
using Mono;
using Mono.Terminal;

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
		
		Window.Standard.intrflush (false);
		Window.Standard.keypad (true);

		// Shutdown
		Curses.endwin ();
	}
}
