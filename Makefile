TOP = .
include $(TOP)/Make.config

export TOP
export WRENCH
export IOS_TARGET

XBUILD_OPTIONS = /p:Configuration=Console
SOLUTION = Xamarin.WebTests.sln
OUTPUT = ./Xamarin.WebTests.Console/bin/Debug/Xamarin.WebTests.Console.exe
RUN_ARGS =

all::	build run

build::
	xbuild $(XBUILD_OPTIONS) $(SOLUTION)

clean::
	xbuild $(XBUILD_OPTIONS) /t:Clean $(SOLUTION)
	
run::
	$(MONO) $(OUTPUT) $(RUN_ARGS)

Hello::
	echo "Hello World!"

#
# Build
#

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
	$(MAKE) -f $(TOP)/ios.make Build IOS_CONFIGURATION=Debug

.IOS-Build-DebugAppleTls::
	$(MAKE) -f $(TOP)/ios.make Build IOS_CONFIGURATION=DebugAppleTls

.IOS-Run-Experimental::
	$(MAKE) -f $(TOP)/ios.make ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=All Run

.IOS-Run-All::
	$(MAKE) -f $(TOP)/ios.make TEST_CATEGORY=All Run

.IOS-Run-Work::
	$(MAKE) -f $(TOP)/ios.make ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Work Run

.IOS-Run-Martin::
	$(MAKE) -f $(TOP)/ios.make ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Martin Run

