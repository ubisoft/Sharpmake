# Generated by Sharpmake -- Do not edit !
ifndef config
  config=debug
endif

ifndef verbose
  SILENT = @
endif

ifeq ($(config),debug)
  CXX        = g++
  AR         = ar
  OBJDIR     = ../../obj/linux_debug/lib_group
  TARGETDIR  = ../../bin/linux_debug
  TARGET     = $(TARGETDIR)/liblib_group.so
  DEFINES   += -D "UTIL_DLL_EXPORT" -D "UTIL_DLL_IMPORT" -D "_DEBUG"
  INCLUDES  += -I../../../dll1 -I../../../lib_group -I../../../static_lib1
  CPPFLAGS  += -MMD -MP $(DEFINES) $(INCLUDES)
  CFLAGS    += $(CPPFLAGS) -g -Wall -fPIC
  CXXFLAGS  += $(CFLAGS) -fno-exceptions -fno-rtti 
  LDFLAGS   += -L../../bin/linux_debug -L../../lib/linux_debug/curl -L../../lib/linux_debug/static_lib1 
  LDLIBS    +=  -Wl,--start-group -l:libcurl.a -l:libdll1.so -l:libstatic_lib1.a -l:libm.a -Wl,--end-group 
  RESFLAGS  += $(DEFINES) $(INCLUDES)
  LDDEPS    += ../../bin/linux_debug/libdll1.so ../../lib/linux_debug/static_lib1/libstatic_lib1.a
  LINKCMD    = $(CXX) -shared -o $(TARGET) $(OBJECTS) $(LDFLAGS) $(RESOURCES) $(LDLIBS)
  PCH        = ../../../lib_group/precomp.h
  PCHOUT     = $(OBJDIR)/precomp.h
  GCH        = $(OBJDIR)/precomp.h.gch
  PCHCMD     = -include $(PCHOUT)
  define PREBUILDCMDS
    mkdir -p $(TARGETDIR)/../../package
  endef
  define PRELINKCMDS
    
  endef
  define POSTBUILDCMDS
    cp $(TARGET) $(TARGETDIR)/../../package
  endef
  define POSTFILECMDS
  endef
endif

ifeq ($(config),release)
  CXX        = g++
  AR         = ar
  OBJDIR     = ../../obj/linux_release/lib_group
  TARGETDIR  = ../../bin/linux_release
  TARGET     = $(TARGETDIR)/liblib_group.so
  DEFINES   += -D "NDEBUG" -D "UTIL_DLL_EXPORT" -D "UTIL_DLL_IMPORT"
  INCLUDES  += -I../../../dll1 -I../../../lib_group -I../../../static_lib1
  CPPFLAGS  += -MMD -MP $(DEFINES) $(INCLUDES)
  CFLAGS    += $(CPPFLAGS) -g -O3 -Wall -fPIC
  CXXFLAGS  += $(CFLAGS) -fno-exceptions -fno-rtti 
  LDFLAGS   += -L../../bin/linux_release -L../../lib/linux_release/curl -L../../lib/linux_release/static_lib1 
  LDLIBS    +=  -Wl,--start-group -l:libcurl.a -l:libdll1.so -l:libstatic_lib1.a -l:libm.a -Wl,--end-group 
  RESFLAGS  += $(DEFINES) $(INCLUDES)
  LDDEPS    += ../../bin/linux_release/libdll1.so ../../lib/linux_release/static_lib1/libstatic_lib1.a
  LINKCMD    = $(CXX) -shared -o $(TARGET) $(OBJECTS) $(LDFLAGS) $(RESOURCES) $(LDLIBS)
  PCH        = ../../../lib_group/precomp.h
  PCHOUT     = $(OBJDIR)/precomp.h
  GCH        = $(OBJDIR)/precomp.h.gch
  PCHCMD     = -include $(PCHOUT)
  define PREBUILDCMDS
    mkdir -p $(TARGETDIR)/../../package
  endef
  define PRELINKCMDS
    
  endef
  define POSTBUILDCMDS
    cp $(TARGET) $(TARGETDIR)/../../package
  endef
  define POSTFILECMDS
  endef
endif

ifeq ($(config),debug)
  OBJECTS += $(OBJDIR)/precomp.o
  OBJECTS += $(OBJDIR)/util_dll.o
endif

ifeq ($(config),release)
  OBJECTS += $(OBJDIR)/precomp.o
  OBJECTS += $(OBJDIR)/util_dll.o
endif

RESOURCES := \

SHELLTYPE := msdos
ifeq (,$(ComSpec)$(COMSPEC))
  SHELLTYPE := posix
endif
ifeq (/bin,$(findstring /bin,$(SHELL)))
  SHELLTYPE := posix
endif

.PHONY: clean prebuild prelink

all: $(TARGETDIR) $(OBJDIR) prebuild prelink $(TARGET)
	@:

$(TARGET): $(GCH) $(OBJECTS) $(LDDEPS) $(RESOURCES) | $(TARGETDIR)
	@echo Linking lib_group
	$(SILENT) $(LINKCMD)
	$(POSTBUILDCMDS)

$(TARGETDIR):
	@echo Creating $(TARGETDIR)
ifeq (posix,$(SHELLTYPE))
	$(SILENT) mkdir -p $(TARGETDIR)
else
	$(SILENT) if not exist $(subst /,\\,$(TARGETDIR)) mkdir $(subst /,\\,$(TARGETDIR))
endif

ifneq ($(OBJDIR),$(TARGETDIR))
$(OBJDIR):
	@echo Creating $(OBJDIR)
ifeq (posix,$(SHELLTYPE))
	$(SILENT) mkdir -p $(OBJDIR)
else
	$(SILENT) if not exist $(subst /,\\,$(OBJDIR)) mkdir $(subst /,\\,$(OBJDIR))
endif
endif

clean:
	@echo Cleaning lib_group
ifeq (posix,$(SHELLTYPE))
	$(SILENT) rm -f  $(TARGET)
	$(SILENT) rm -rf $(OBJDIR)
else
	$(SILENT) if exist $(subst /,\\,$(TARGET)) del $(subst /,\\,$(TARGET))
	$(SILENT) if exist $(subst /,\\,$(OBJDIR)) rmdir /s /q $(subst /,\\,$(OBJDIR))
endif

prebuild:
	$(PREBUILDCMDS)

prelink:
	$(PRELINKCMDS)

ifneq (,$(PCH))
$(GCH): $(PCH) | $(OBJDIR)
	@echo $(notdir $<)
	-$(SILENT) cp $< $(OBJDIR)
	$(SILENT) $(CXX) $(CXXFLAGS) -xc++-header -o "$@" -c "$<"
	$(SILENT) $(POSTFILECMDS)
endif

$(OBJDIR)/precomp.o: ../../../lib_group/precomp.cpp $(GCH) | $(OBJDIR)
	@echo $(notdir $<)
	$(SILENT) $(CXX) $(CXXFLAGS) $(PCHCMD) -o "$@" -c "$<"
	$(SILENT) $(POSTFILECMDS)

$(OBJDIR)/util_dll.o: ../../../lib_group/util_dll.cpp $(GCH) | $(OBJDIR)
	@echo $(notdir $<)
	$(SILENT) $(CXX) $(CXXFLAGS) $(PCHCMD) -o "$@" -c "$<"
	$(SILENT) $(POSTFILECMDS)

-include $(OBJECTS:%.o=%.d)
-include $(GCH:%.gch=%.d)
