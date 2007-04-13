#include <stdio.h>
#include <ncurses.h>

#define put(x) fprintf (OUT, "\tpublic const int " #x " = %ld;\n", x)
#define put2(s,x) fprintf (OUT, "\tpublic const int %s = %d;\n", s, x)

int
main (int argc, char *argv [])
{
	FILE *OUT = fopen (argv [1], "w");
	
	int diff;

	initscr ();
	fprintf (OUT, "using System;\n\n"
		"namespace Mono.Terminal {\n"
		"public partial class Curses {\n");
	put (A_NORMAL);
	put (A_STANDOUT);
	put (A_UNDERLINE);
	put (A_REVERSE);
	put (A_BLINK);
	put (A_DIM);
	put (A_BOLD);
	put (A_PROTECT);
	put (A_INVIS);

	put (ACS_LLCORNER);
	put (ACS_LRCORNER);
	put (ACS_HLINE);
	put (ACS_ULCORNER);
	put (ACS_URCORNER);
	put (ACS_VLINE);

	put (COLOR_BLACK);
	put (COLOR_RED);
	put (COLOR_GREEN);
	put (COLOR_YELLOW);
	put (COLOR_BLUE);
	put (COLOR_MAGENTA);
	put (COLOR_CYAN);
	put (COLOR_WHITE);
	
	put2 ("Backspace", KEY_BACKSPACE);

	diff = COLOR_PAIR (1) - COLOR_PAIR(0);
	fprintf (OUT, "\n\n\tstatic public int ColorPair(int n){\n"
		"\t\treturn %d + n * %d;\n"
		"\t}\n\n", COLOR_PAIR (0), diff);
	fprintf (OUT,"}\n}\n");
	fclose (OUT);
	endwin ();
	return 0;
}
