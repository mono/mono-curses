#include <ncurses.h>
#include <sys/types.h>
#include <signal.h>
#include <stdint.h>

void *console_sharp_get_stdscr ()
{
	return stdscr;
}

void *console_sharp_get_curscr ()
{
	return curscr;
}

void *console_sharp_get_newscr ()
{
	return newscr;
}

void console_sharp_get_dims (int *lines, int *cols)
{
	*lines = LINES;
	*cols = COLS;
}

void console_sharp_sendsigtstp ()
{
	killpg (0, SIGTSTP);
}

int64_t console_sharp_mouse_mask (int64_t newmask, int64_t *oldmask)
{
	mmask_t old;
	mmask_t ret;

	ret = mousemask (newmask, &old);
	*oldmask = old;
	
	return ret;
}

typedef struct {
	int ID;
	int X, Y, Z;
	int64_t ButtonState;
	
} MouseEvent;

int console_sharp_getmouse (MouseEvent *event)
{
	MEVENT m;
	
	int c = getmouse (&m);
	if (c == -1)
		return -1;
	event->ID = m.id;
	event->X = m.x;
	event->Y = m.y;
	event->Z = m.z;
	event->ButtonState = m.bstate;
	
	return c;
}

int console_sharp_ungetmouse (MouseEvent *event)
{
	MEVENT m;

	m.x = event->X;
	m.y = event->Y;
	m.z = event->Z;
	m.id = event->ID;
	m.bstate = event->ButtonState;
	
	return ungetmouse (&m);
}
