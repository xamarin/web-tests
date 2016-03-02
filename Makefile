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

.IOS-Debug-%::
	$(MAKE) IOS_CONFIGURATION=Debug .IOS-Run-$*

.IOS-DebugAppleTls-%::
	$(MAKE) IOS_CONFIGURATION=DebugAppleTls .IOS-Run-$*

.IOS-Build-Debug::
	$(MAKE) IOS_CONFIGURATION=Debug .IOS-Build

.IOS-Build-DebugAppleTls::
	$(MAKE) IOS_CONFIGURATION=DebugAppleTls .IOS-Build

.IOS-Run-Experimental::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=All .IOS-Run

.IOS-Run-All::
	$(MAKE) TEST_CATEGORY=All .IOS-Run

.IOS-Run-Work::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Work .IOS-Run

.IOS-Run-Martin::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Martin .IOS-Run

.IOS-Build::
	$(MONO) $(NUGET_EXE) restore Xamarin.WebTests.iOS.sln
	$(MDTOOL) build Xamarin.WebTests.iOS.sln -c:'$(IOS_CONFIGURATION)|$(IOS_TARGET)'

.IOS-Run::
	$(MONO) $(ASYNCTESTS_CONSOLE_EXE) $(ASYNCTESTS_ARGS) $(WRENCH_ARGS) --category=$(TEST_CATEGORY) \
		--stdout=$(STDOUT) --stderr=$(STDERR) --result=$(TEST_RESULT) $(EXTRA_ASYNCTESTS_ARGS) \
		$(ASYNCTESTS_COMMAND) $(WEBTESTS_IOS_APP)

