thisdir = class/System.Json
SUBDIRS = 
include ../../build/rules.make

#System.Json.Properties.Resources.resources: src/System.Json/Properties/Resources.resx
#	$(RESGEN) "$<" "$@"

LIBRARY = System.Json.dll
LIB_MCS_FLAGS = /d:ASPNETMVC -keyfile:../winfx.pub -delaysign \
	        /r:System.dll \
	        /r:System.Xml.dll \
	        /r:System.Core.dll \
		/r:System.Runtime.Serialization.dll \
		/r:System.ServiceModel.Web.dll 
#		/resource:System.Json.Properties.Resources.resources

ifeq (4, $(FRAMEWORK_VERSION_MAJOR))
LIB_MCS_FLAGS += /r:Microsoft.CSharp.dll
endif

TEST_MCS_FLAGS = $(LIB_MCS_FLAGS)

include ../../build/library.make
