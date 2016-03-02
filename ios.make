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

ifeq (1,$(WRENCH))
WRENCH_ARGS = --wrench
else
WRENCH_ARGS =
endif

TEST_CATEGORY = All
TEST_RESULT = TestResult-$(TARGET_NAME).xml
STDOUT = stdout-$(TARGET_NAME).txt
STDERR = stderr-$(TARGET_NAME).txt

Build::
	$(MONO) $(NUGET_EXE) restore Xamarin.WebTests.iOS.sln
	$(MDTOOL) build Xamarin.WebTests.iOS.sln -c:'$(IOS_CONFIGURATION)|$(IOS_TARGET)'

Run::
	$(MONO) $(ASYNCTESTS_CONSOLE_EXE) $(ASYNCTESTS_ARGS) $(WRENCH_ARGS) --category=$(TEST_CATEGORY) \
		--stdout=$(STDOUT) --stderr=$(STDERR) --result=$(TEST_RESULT) $(EXTRA_ASYNCTESTS_ARGS) \
		$(ASYNCTESTS_COMMAND) $(WEBTESTS_IOS_APP)

