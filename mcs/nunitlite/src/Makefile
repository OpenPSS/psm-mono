thisdir = nunitlite/src
SUBDIRS =
include ../../build/rules.make

LIBRARY = NUnitLite.dll
LIBRARY_NAME = nunitlite.dll
LIBRARY_SNK = $(topdir)/nunitlite/nunitlite.snk

LIB_MCS_FLAGS = /r:System.Xml.dll /r:System.dll /d:NUNITLITE
NO_TEST = yo

ifneq (net_2_0, $(PROFILE))
NO_INSTALL = yes
install-local: install-symlink
uninstall-local: uninstall-symlink
endif

include ../../build/library.make

symlinkdir = $(mono_libdir)/mono/$(FRAMEWORK_VERSION)
install-symlink:
	$(MKINSTALLDIRS) $(DESTDIR)$(symlinkdir)
	cd $(DESTDIR)$(symlinkdir) && rm -f $(LIBRARY_NAME) && ln -s ../2.0/$(LIBRARY_NAME) $(LIBRARY_NAME)

uninstall-symlink:
	rm -f $(DESTDIR)$(symlinkdir)/$(LIBRARY_NAME)
