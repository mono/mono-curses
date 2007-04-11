CURSES=ncurses
SOURCES = 		\
	handles.cs	\
	binding.cs	\
	gui.cs		\
	constants.cs

all: mono-curses.dll libmono-curses.so demo.exe

test.exe: test.cs mono-curses.dll libmono-curses.so
	gmcs -debug test.cs -r:mono-curses.dll

demo.exe: demo.cs mono-curses.dll libmono-curses.so
	gmcs -debug demo.cs -r:mono-curses.dll

mono-curses.dll: $(SOURCES)
	gmcs -debug -target:library -out:mono-curses.dll -debug $(SOURCES)

binding.cs: binding.cs.in
	sed -e 's/@CURSES@/$(CURSES)/' < binding.cs.in > binding.cs

constants.cs: attrib.c
	gcc -o attrib attrib.c  -lncurses
	./attrib constants.cs

libmono-curses.so: mono-curses.c
	gcc -shared -fPIC mono-curses.c -o libmono-curses.so

test: test.exe
	mono test.exe

clean:
	rm *.exe *dll binding.cs *.so
