CURSES=curses
MONO_CURSES=mono-curses

SOURCES = 		\
	handles.cs	\
	binding.cs	\
	gui.cs		\
	constants.cs

TORRENTDIR=/cvs/bitsharp/src
TORRENTLIBS=	\
	-r:$(TORRENTDIR)/bin/MonoTorrent.Common.dll 	\
	-r:$(TORRENTDIR)/bin/MonoTorrent.Client.dll 	\
	-r:$(TORRENTDIR)/bin/MonoTorrent.BEncoding.dll	\
	-r:$(TORRENTDIR)/Libs/Upnp.dll

all: mono-curses.dll libmono-curses.so demo.exe

test.exe: test.cs mono-curses.dll libmono-curses.so
	gmcs -debug test.cs -r:mono-curses.dll

demo.exe: demo.cs mono-curses.dll libmono-curses.so
	gmcs -debug demo.cs -r:mono-curses.dll $(TORRENTLIBS)

run: demo.exe
	DYLD_LIBRARY_PATH=. MONO_PATH=$(TORRENTDIR)/bin:$(TORRENTDIR)/Libs mono --debug demo.exe || stty sane

mono-curses.dll: $(SOURCES)
	gmcs -debug -target:library -out:mono-curses.dll -debug $(SOURCES)

binding.cs: binding.cs.in Makefile
	if test `uname` = Darwin; then make t-bugosx; else make binding; fi

t-bugosx: 
	make binding CURSES=libncurses.dylib MONO_CURSES=libmono-curses.dylib

binding:
	sed -e 's/@CURSES@/$(CURSES)/' -e 's/@MONO_CURSES@/$(MONO_CURSES)/' < binding.cs.in > binding.cs

constants.cs: attrib.c
	gcc -o attrib attrib.c  -lncurses
	./attrib constants.cs

libmono-curses.so: mono-curses.c
	if test `uname` = Darwin; then gcc -dynamiclib mono-curses.c -o libmono-curses.dylib -lncurses; else gcc -shared -fPIC mono-curses.c -o libmono-curses.so -lncurses; fi

test: test.exe
	mono test.exe

clean:
	rm *.exe *dll binding.cs *.so
