TOP = .
include $(TOP)/Make.config

CleanAll::
	git clean -xffd

Wrench-%::
	$(MAKE) WRENCH=1 $*

IOS-Sim-%::
	$(MAKE) IOS_TARGET=iPhoneSimulator ASYNCTESTS_COMMAND=simulator TARGET_NAME=$@ .IOS-$*

IOS-Dev-%::
	$(MAKE) IOS_TARGET=iPhone ASYNCTESTS_COMMAND=device TARGET_NAME=$@ .IOS-$*

Console-%::
	$(MAKE) ASYNCTESTS_COMMAND=local TARGET_NAME=$@ .Console-$*

#
# Internal make targets below
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
	$(MDTOOL) build Xamarin.WebTests.iOS.sln -c:'$(IOS_CONFIGURATION)|$(IOS_TARGET)'

.IOS-Internal-Run::
	$(MONO) $(ASYNCTESTS_CONSOLE_EXE) $(ASYNCTESTS_ARGS) $(WRENCH_ARGS) --category=$(TEST_CATEGORY) \
		--stdout=$(STDOUT) --stderr=$(STDERR) --result=$(TEST_RESULT) $(EXTRA_ASYNCTESTS_ARGS) \
		$(ASYNCTESTS_COMMAND) $(WEBTESTS_IOS_APP)

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
	$(MDTOOL) build Xamarin.WebTests.Console.sln -c:'$(CONSOLE_CONFIGURATION)'

.Console-Internal-Run::
	$(MONO) $(WEBTESTS_CONSOLE_EXE) $(ASYNCTESTS_ARGS) $(WRENCH_ARGS) --category=$(TEST_CATEGORY) \
		--result=$(TEST_RESULT) $(EXTRA_ASYNCTESTS_ARGS) $(ASYNCTESTS_COMMAND)

