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
  OBJDIR     = ../../obj/linux_debug/static\ lib2
  TARGETDIR  = ../../lib/linux_debug/static\ lib2
  TARGET     = $(TARGETDIR)/libstatic\ lib2.a
  DEFINES   += -D "_DEBUG"
  INCLUDES  += -I../../../static\ lib2
  CPPFLAGS  += -MMD -MP $(DEFINES) $(INCLUDES)
  CFLAGS    += $(CPPFLAGS) -g -Wall 
  CXXFLAGS  += $(CFLAGS) -fno-exceptions -fno-rtti 
  LDFLAGS   +=  
  LDLIBS    +=  
  RESFLAGS  += $(DEFINES) $(INCLUDES)
  LDDEPS    += 
  LINKCMD    = $(AR) -rcs $(TARGET) $(OBJECTS)
  PCH        = 
  PCHOUT     = 
  GCH        = 
  PCHCMD     = 
  define PREBUILDCMDS
    
  endef
  define PRELINKCMDS
    
  endef
  define POSTBUILDCMDS
    
  endef
  define POSTFILECMDS
  endef
endif

ifeq ($(config),release)
  CXX        = g++
  AR         = ar
  OBJDIR     = ../../obj/linux_release/static\ lib2
  TARGETDIR  = ../../lib/linux_release/static\ lib2
  TARGET     = $(TARGETDIR)/libstatic\ lib2.a
  DEFINES   += -D "NDEBUG"
  INCLUDES  += -I../../../static\ lib2
  CPPFLAGS  += -MMD -MP $(DEFINES) $(INCLUDES)
  CFLAGS    += $(CPPFLAGS) -g -O3 -Wall 
  CXXFLAGS  += $(CFLAGS) -fno-exceptions -fno-rtti 
  LDFLAGS   +=  
  LDLIBS    +=  
  RESFLAGS  += $(DEFINES) $(INCLUDES)
  LDDEPS    += 
  LINKCMD    = $(AR) -rcs $(TARGET) $(OBJECTS)
  PCH        = 
  PCHOUT     = 
  GCH        = 
  PCHCMD     = 
  define PREBUILDCMDS
    
  endef
  define PRELINKCMDS
    
  endef
  define POSTBUILDCMDS
    
  endef
  define POSTFILECMDS
  endef
endif

ifeq ($(config),debug)
  OBJECTS += $(OBJDIR)/useless_static_lib2.o
  OBJECTS += $(OBJDIR)/util_static_lib2.o
endif

ifeq ($(config),release)
  OBJECTS += $(OBJDIR)/useless_static_lib2.o
  OBJECTS += $(OBJDIR)/util_static_lib2.o
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
	@echo Linking static lib2
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
	@echo Cleaning static lib2
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

$(OBJDIR)/useless_static_lib2.o: ../../../static\ lib2/sub\ folder/useless_static_lib2.cpp $(GCH) | $(OBJDIR)
	@echo $(notdir $<)
	$(SILENT) $(CXX) $(CXXFLAGS) $(PCHCMD) -o "$@" -c "$<"
	$(SILENT) $(POSTFILECMDS)

$(OBJDIR)/util_static_lib2.o: ../../../static\ lib2/util_static_lib2.cpp $(GCH) | $(OBJDIR)
	@echo $(notdir $<)
	$(SILENT) $(CXX) $(CXXFLAGS) $(PCHCMD) -o "$@" -c "$<"
	$(SILENT) $(POSTFILECMDS)

-include $(OBJECTS:%.o=%.d)
-include $(GCH:%.gch=%.d)