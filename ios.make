include $(TOP)/Make.config

IOS_TARGET = iPhoneSimulator
IOS_CONFIGURATION = Debug

CONSOLE_OUTPUT_DIR = Debug
IOS_OUTPUT_DIR = $(IOS_TARGET)/$(IOS_CONFIGURATION)

ASYNCTESTS_CONSOLE_DIR = $(TOP)/Xamarin.AsyncTests.Console/bin/$(CONSOLE_OUTPUT_DIR)
ASYNCTESTS_CONSOLE_EXE = $(ASYNCTESTS_CONSOLE_DIR)/Xamarin.AsyncTests.Console.exe
WEBTESTS_IOS_APP = $(TOP)/IOS/Xamarin.WebTests.iOS/bin/$(IOS_OUTPUT_DIR)/XamarinWebTestsIOS.app

ASYNCTESTS_ARGS =
EXTRA_ASYNCTESTS_ARGS =

TEST_CATEGORY = All
TEST_RESULT = TestResult.xml

.IOS-Clean::
	-rm .IOS-Build-Sim
	-rm .IOS-Build-Dev
	-rm -rf $(ASYNCTESTS_CONSOLE_DIR)
	-rm -rf $(WEBTESTS_IOS_APP)

.IOS-Build-Sim:
	$(MONO) $(NUGET_EXE) restore Xamarin.WebTests.iOS.sln
	$(MDTOOL) build Xamarin.WebTests.iOS.sln -c:'WrenchAppleTls|iPhoneSimulator'
	touch $@

.IOS-Build-Dev:
	$(MONO) $(NUGET_EXE) restore Xamarin.WebTests.iOS.sln
	$(MDTOOL) build Xamarin.WebTests.iOS.sln -c:'WrenchAppleTls|iPhone'
	touch $@

.IOS-Simulator:: .IOS-Build-Sim
	$(MONO) $(ASYNCTESTS_CONSOLE_EXE) $(ASYNCTESTS_ARGS) --category=$(TEST_CATEGORY) --result=$(TEST_RESULT) $(EXTRA_ASYNCTESTS_ARGS) simulator $(WEBTESTS_IOS_APP)

.IOS-Device:: .IOS-Build-Dev
	$(MONO) $(ASYNCTESTS_CONSOLE_EXE) $(ASYNCTESTS_ARGS) --category=$(TEST_CATEGORY) --result=$(TEST_RESULT) $(EXTRA_ASYNCTESTS_ARGS) device $(WEBTESTS_IOS_APP)

