CURSES=ncurses
DESTDIR=/tmp
MONO_CURSES=mono-curses

CORE_SOURCES = 		\
	handles.cs	\
	binding.cs	\
	mainloop.cs	\
	constants.cs

SOURCES = \
	$(CORE_SOURCES) \
	AssemblyInfo.cs	\
	gui.cs

EXTRA_DIST = 	\
	mono-curses.snk	\
	configure	\
	Makefile	\
	binding.cs.in	\
	attrib.c	\
	mono-curses.c	\
	mono-curses.source	\
	mono-curses.pc.in


DOCS_DIST = \
	docs/ns-Mono.Terminal.xml \
	docs/index.xml

TESTS = test.exe 	\
	gtest.exe	\
	guitest.exe	\
	mltest.exe

all: config.make mono-curses.dll mono-curses.zip mono-curses.pc $(TESTS)

test.exe: test.cs mono-curses.dll
	mcs -debug  test.cs -r:mono-curses.dll

grun: gtest.exe guitest.exe
	MONO_PATH=. mono --debug guitest.exe

gtest.exe: gtest.cs mono-curses.dll
	dmcs -debug gtest.cs -r:mono-curses.dll

guitest.exe: guitest.cs mono-curses.dll
	mcs -debug guitest.cs -r:mono-curses.dll

mltest.exe: mltest.cs mono-curses.dll
	dmcs -debug mltest.cs -r:mono-curses.dll

mlrun: mltest.exe
	mono --debug mltest.exe

mono-curses.pc: mono-curses.pc.in Makefile
	sed -e 's,@PREFIX@,$(prefix),' -e 's/@VERSION@/$(VERSION)/' < mono-curses.pc.in > mono-curses.pc

mono-curses.dll mono-curses.xml: $(SOURCES)
	mcs -nowarn:1591 -doc:mono-curses.xml -debug -target:library -out:mono-curses.dll -debug $(SOURCES)


publish-internal:
	for i in $(CORE_SOURCES); do sed -e 's/public partial/internal partial/' -e 's/public struct /internal struct /g' -e 's/public class/internal class/' -e 's/public enum/internal enum/' -e 's/public interface/internal interface/' -e 's/internal class MainLoop/public class MainLoop/' -e 's/internal enum Condition/public enum Condition/'  < $$i > $(DESTDIR)/$$i; done

publish-to-gui:
	make publish-internal DESTDIR=../gui.cs/Terminal.Gui/MonoCurses/

#
mono-curses.tree mono-curses.zip: mono-curses.xml mono-curses.dll docs/ns-Mono.Terminal.xml docs/index.xml
	monodocer -importslashdoc:mono-curses.xml -path:docs -assembly:mono-curses.dll
	mdassembler --ecma docs/ --out mono-curses

#cute hack to avoid depending on ncurses-devel on Linux
detect:
	echo "main () {initscr();}" > tmp.c
	gcc tmp.c -lncursesw -o tmp
	make binding CURSES=`ldd ./tmp  | grep ncurses | awk '{print $$3}' | sed 's#.*libncurses#ncurses#'`

constants.cs: attrib.c
	gcc -o attrib attrib.c  -lncurses
	./attrib constants.cs

test: test.exe
	mono test.exe

clean:
	-rm -f *.exe *dll *.so *dylib

install: all
	mkdir -p $(prefix)/bin
	mkdir -p $(prefix)/lib/mono-curses
	mkdir -p $(prefix)/lib/pkgconfig
	gacutil -i mono-curses.dll -package mono-curses -root $(DESTDIR)$(prefix)/lib
	cp mono-curses.pc $(prefix)/lib/pkgconfig/
	cp mono-curses.tree mono-curses.zip mono-curses.source  `pkg-config --variable sourcesdir monodoc`

config.make:
	@echo You must run configure first
	@exit 1

include config.make

dist: 
	rm -rf mono-curses-$(VERSION)
	mkdir mono-curses-$(VERSION)
	mkdir mono-curses-$(VERSION)/docs
	cp -a $(SOURCES) $(EXTRA_DIST) mono-curses-$(VERSION)
	cp -a $(DOCS_DIST) mono-curses-$(VERSION)/docs
	tar czvf mono-curses-$(VERSION).tar.gz mono-curses-$(VERSION)
	rm -rf mono-curses-$(VERSION)

distcheck: dist
	rm -rf test
	(mkdir test; cd test; tar xzvf ../mono-curses-$(VERSION).tar.gz; cd mono-curses-$(VERSION); \
	 ./configure --prefix=$$(cd `pwd`/..; pwd); \
	 make && make install && make dist);
	rm -rf test
	echo mono-curses-$(VERSION).tar.gz is ready for release

push:
	scp mono-curses.tree mono-curses.zip mono-curses.source root@www.go-mono.com:/usr/lib/monodoc/sources/
