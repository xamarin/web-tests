include $(TOP)/Make.config

IOS_TARGET = iPhoneSimulator
IOS_CONFIGURATION = Debug

CONSOLE_OUTPUT_DIR = Debug
IOS_OUTPUT_DIR = $(IOS_TARGET)/$(IOS_CONFIGURATION)

XAMARIN_ASYNCTESTS_CONSOLE_DIR = $(TOP)/Xamarin.AsyncTests.Console/bin/$(CONSOLE_OUTPUT_DIR)
XAMARIN_ASYNCTESTS_CONSOLE_EXE = $(XAMARIN_ASYNCTESTS_CONSOLE_DIR)/Xamarin.AsyncTests.Console.exe
XAMARIN_WEBTESTS_IOS_APP = $(TOP)/IOS/Xamarin.WebTests.iOS/bin/$(IOS_OUTPUT_DIR)/XamarinWebTestsIOS.app

XAMARIN_ASYNCTESTS_ARGS =

TEST_CATEGORY = All
TEST_RESULT = TestResult.xml

WORK_DEBUG_ARGS = --debug --log-level=5

.IOS-Clean::
	-rm .IOS-Build
	-rm -rf $(XAMARIN_ASYNCTESTS_CONSOLE_DIR)
	-rm -rf $(XAMARIN_WEBTESTS_IOS_APP)
	
$(XAMARIN_ASYNCTESTS_CONSOLE_EXE) $(XAMARIN_WEBTESTS_IOS_APP): .IOS-Build

.IOS-Build:
	$(MONO) $(NUGET_EXE) restore Xamarin.WebTests.iOS.sln
	$(MDTOOL) build Xamarin.WebTests.iOS.sln -c:'WrenchAppleTls|iPhoneSimulator'
	touch $@

.IOS-Simulator:: .IOS-Build
	$(MONO) $(XAMARIN_ASYNCTESTS_CONSOLE_EXE) $(XAMARIN_ASYNCTESTS_ARGS) --category=$(TEST_CATEGORY) --result=$(TEST_RESULT) simulator $(XAMARIN_WEBTESTS_IOS_APP)

