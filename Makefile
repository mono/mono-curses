CURSES=ncurses
MONO_CURSES=mono-curses

SOURCES = 		\
	handles.cs	\
	binding.cs	\
	gui.cs		\
	constants.cs

TORRENTDIR=/cvs/bitsharp/src
TORRENTLIBS=`pkg-config --libs monotorrent`

all: config.make mono-curses.dll libmono-curses.so monotorrent.exe monotorrent

monotorrent: monotorrent.in Makefile
	sed "s,@prefix@,$(prefix)," < monotorrent.in > monotorrent
	chmod +x monotorrent

test.exe: test.cs mono-curses.dll libmono-curses.so
	gmcs -debug test.cs -r:mono-curses.dll

monotorrent.exe: monotorrent.cs mono-curses.dll libmono-curses.so MonoTorrent.dll Upnp.dll
	gmcs -debug monotorrent.cs -r:mono-curses.dll $(TORRENTLIBS)

MonoTorrent.dll Upnp.dll:
	cp `pkg-config --variable=Libraries monotorrent` .

run: monotorrent.exe
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

install: all
	mkdir -p $(prefix)/bin
	mkdir -p $(prefix)/lib/monotorrent
	cp mono-curses.dll MonoTorrent.dll Upnp.dll monotorrent.exe $(prefix)/lib/monotorrent
	cp libmono-curses* $(prefix)/lib/monotorrent
	cp monotorrent $(prefix)/bin

config.make:
	echo You must run configure first
	exit 1

include config.make
