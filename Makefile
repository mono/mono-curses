CURSES=ncurses
SOURCES = 		\
	handles.cs	\
	binding.cs

all: mono-curses.dll libmono-curses.so test.exe

test.exe: test.cs mono-curses.dll libmono-curses.so
	gmcs test.cs -r:mono-curses.dll

mono-curses.dll: $(SOURCES)
	gmcs -target:library -out:mono-curses.dll -debug $(SOURCES)

binding.cs: binding.cs.in
	sed -e 's/@CURSES@/$(CURSES)/' < binding.cs.in > binding.cs

libmono-curses.so: mono-curses.c
	gcc -shared -fPIC mono-curses.c -o libmono-curses.so

test: test.exe
	mono test.exe

clean:
	rm *.exe *dll binding.cs *.so