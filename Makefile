TOP = .
include $(TOP)/Make.config

CleanAll::
	git clean -xffd

Wrench-%::
	$(MAKE) WRENCH=1 $*

ALL_WRENCH_BUILD_TARGETS = \
	Wrench-IOS-Sim-Build-Debug Wrench-IOS-Sim-Build-DebugAppleTls \
	Wrench-Console-Build-Debug \
	Wrench-Mac-Build-Debug Wrench-Mac-Build-DebugAppleTls

Wrench-Build-All:: $(ALL_WRENCH_BUILD_TARGETS)
	@echo "Build done."

IOS-Sim-%::
	$(MAKE) IOS_TARGET=iPhoneSimulator ASYNCTESTS_COMMAND=simulator TARGET_NAME=$@ .IOS-$*

IOS-Dev-%::
	$(MAKE) IOS_TARGET=iPhone ASYNCTESTS_COMMAND=device TARGET_NAME=$@ .IOS-$*

Console-%::
	$(MAKE) ASYNCTESTS_COMMAND=local TARGET_NAME=$@ .Console-$*

Mac-%::
	$(MAKE) ASYNCTESTS_COMMAND=mac TARGET_NAME=$@ .Mac-$*

#
# Internal IOS make targets
#

.IOS-Debug-%::
	$(MAKE) IOS_CONFIGURATION=Debug .IOS-Run-$*

.IOS-DebugAppleTls-%::
	$(MAKE) IOS_CONFIGURATION=DebugAppleTls .IOS-Run-$*

.IOS-Build-Debug::
	$(MAKE) IOS_CONFIGURATION=Debug .IOS-Internal-Build

.IOS-Build-DebugAppleTls::
	$(MAKE) IOS_CONFIGURATION=DebugAppleTls .IOS-Internal-Build

.IOS-Run-Experimental::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=All .IOS-Internal-Run

.IOS-Run-All::
	$(MAKE) TEST_CATEGORY=All .IOS-Internal-Run

.IOS-Run-Work::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Work .IOS-Internal-Run

.IOS-Run-Martin::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Martin .IOS-Internal-Run

.IOS-Internal-Build::
	$(MONO) $(NUGET_EXE) restore Xamarin.WebTests.iOS.sln
	$(XBUILD) /p:Configuration='$(IOS_CONFIGURATION)' /p:Platform='$(IOS_TARGET)' Xamarin.WebTests.iOS.sln

.IOS-Internal-Run::
	$(MONO) $(ASYNCTESTS_CONSOLE_EXE) $(ASYNCTESTS_ARGS) $(WRENCH_ARGS) --category=$(TEST_CATEGORY) \
		--stdout=$(STDOUT) --stderr=$(STDERR) --result=$(TEST_RESULT) $(EXTRA_ASYNCTESTS_ARGS) \
		$(ASYNCTESTS_COMMAND) $(WEBTESTS_IOS_APP)

#
# Internal Console make targets
#

.Console-Build-Debug::
	$(MAKE) CONSOLE_CONFIGURATION=Debug .Console-Internal-Build

.Console-Debug-%::
	$(MAKE) CONSOLE_CONFIGURATION=Debug .Console-Run-$*

.Console-Run-Experimental::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=All .Console-Internal-Run

.Console-Run-All::
	$(MAKE) TEST_CATEGORY=All .Console-Internal-Run

.Console-Run-Work::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Work .Console-Internal-Run

.Console-Run-Martin::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Martin .Console-Internal-Run

.Console-Internal-Build::
	$(XBUILD) /p:Configuration='$(CONSOLE_CONFIGURATION)' Xamarin.WebTests.Console.sln

.Console-Internal-Run::
	$(MONO) $(WEBTESTS_CONSOLE_EXE) $(ASYNCTESTS_ARGS) $(WRENCH_ARGS) --category=$(TEST_CATEGORY) \
		--result=$(TEST_RESULT) $(EXTRA_ASYNCTESTS_ARGS) $(ASYNCTESTS_COMMAND)

#
# Internal Mac make targets
#

.Mac-Build-Debug::
	$(MAKE) MAC_CONFIGURATION=Debug .Mac-Internal-Build

.Mac-Build-DebugAppleTls::
	$(MAKE) MAC_CONFIGURATION=DebugAppleTls .Mac-Internal-Build

.Mac-Debug-%::
	$(MAKE) MAC_CONFIGURATION=Debug .Mac-Run-$*

.Mac-DebugAppleTls-%::
	$(MAKE) MAC_CONFIGURATION=DebugAppleTls .Mac-Run-$*

.Mac-Run-Experimental::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=All .Mac-Internal-Run

.Mac-Run-All::
	$(MAKE) TEST_CATEGORY=All .Mac-Internal-Run

.Mac-Run-Work::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Work .Mac-Internal-Run

.Mac-Run-Martin::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Martin .Mac-Internal-Run

.Mac-Internal-Build::
	$(MONO) $(NUGET_EXE) restore Xamarin.WebTests.Mac.sln
	$(XBUILD) /p:Configuration='$(MAC_CONFIGURATION)' Xamarin.WebTests.Mac.sln

.Mac-Internal-Run::
	$(MONO) $(ASYNCTESTS_CONSOLE_EXE) $(ASYNCTESTS_ARGS) $(WRENCH_ARGS) --category=$(TEST_CATEGORY) \
		--stdout=$(STDOUT) --stderr=$(STDERR) --result=$(TEST_RESULT) $(EXTRA_ASYNCTESTS_ARGS) \
		$(ASYNCTESTS_COMMAND) $(WEBTESTS_MAC_APP_BIN)

