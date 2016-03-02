TOP = .
include $(TOP)/Make.config

export TOP

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

IOS-Build-Sim::
	$(MAKE) -f $(TOP)/ios.make .IOS-Build-Sim

IOS-Build-Dev::
	$(MAKE) -f $(TOP)/ios.make .IOS-Build-Dev

#
# Simulator
#
	
IOS-Sim-Work::
	$(MAKE) -f $(TOP)/ios.make .IOS-Simulator \
		ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" \
		TEST_CATEGORY=Work TEST_OUTPUT=TestResult-$@.xml \
		IOS_CONFIGURATION=Debug IOS_TARGET=iPhoneSimulator
		
IOS-Sim-All::
	$(MAKE) -f $(TOP)/ios.make .IOS-Simulator \
		ASYNCTESTS_ARGS="--wrench" \
		TEST_CATEGORY=All TEST_OUTPUT=TestResult-$@.xml \
		IOS_CONFIGURATION=Debug IOS_TARGET=iPhoneSimulator

IOS-Sim-Experimental::
	$(MAKE) -f $(TOP)/ios.make .IOS-Simulator \
		ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" \
		TEST_CATEGORY=All TEST_OUTPUT=TestResult-$@.xml \
		IOS_CONFIGURATION=Debug IOS_TARGET=iPhoneSimulator

IOS-Sim-Work-AppleTls::
	$(MAKE) -f $(TOP)/ios.make .IOS-Simulator \
		ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" \
		TEST_CATEGORY=Work TEST_OUTPUT=TestResult-$@.xml \
		IOS_CONFIGURATION=DebugAppleTls IOS_TARGET=iPhoneSimulator
		
IOS-Sim-All-AppleTls::
	$(MAKE) -f $(TOP)/ios.make .IOS-Simulator \
		ASYNCTESTS_ARGS="--wrench" \
		TEST_CATEGORY=All TEST_OUTPUT=TestResult-$@.xml \
		IOS_CONFIGURATION=DebugAppleTls IOS_TARGET=iPhoneSimulator

IOS-Sim-Experimental-AppleTls::
	$(MAKE) -f $(TOP)/ios.make .IOS-Simulator \
		ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" \
		TEST_CATEGORY=All TEST_OUTPUT=TestResult-$@.xml \
		IOS_CONFIGURATION=DebugAppleTls IOS_TARGET=iPhoneSimulator
		
#
# Device
#

IOS-Dev-Work::
	$(MAKE) -f $(TOP)/ios.make .IOS-Device \
		ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" \
		TEST_CATEGORY=Work TEST_OUTPUT=TestResult-$@.xml \
		IOS_CONFIGURATION=Debug IOS_TARGET=iPhone
		
IOS-Dev-All::
	$(MAKE) -f $(TOP)/ios.make .IOS-Device \
		ASYNCTESTS_ARGS="--wrench" \
		TEST_CATEGORY=All TEST_OUTPUT=TestResult-$@.xml \
		IOS_CONFIGURATION=Debug IOS_TARGET=iPhone

IOS-Dev-Experimental::
	$(MAKE) -f $(TOP)/ios.make .IOS-Device \
		ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" \
		TEST_CATEGORY=All TEST_OUTPUT=TestResult-$@.xml \
		IOS_CONFIGURATION=Debug IOS_TARGET=iPhone

IOS-Dev-Work-AppleTls::
	$(MAKE) -f $(TOP)/ios.make .IOS-Device \
		ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" \
		TEST_CATEGORY=Work TEST_OUTPUT=TestResult-$@.xml \
		IOS_CONFIGURATION=DebugAppleTls IOS_TARGET=iPhone
		
IOS-Dev-All-AppleTls::
	$(MAKE) -f $(TOP)/ios.make .IOS-Device \
		ASYNCTESTS_ARGS="--wrench" \
		TEST_CATEGORY=All TEST_OUTPUT=TestResult-$@.xml \
		IOS_CONFIGURATION=DebugAppleTls IOS_TARGET=iPhone

IOS-Dev-Experimental-AppleTls::
	$(MAKE) -f $(TOP)/ios.make .IOS-Device \
		ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" \
		TEST_CATEGORY=All TEST_OUTPUT=TestResult-$@.xml \
		IOS_CONFIGURATION=DebugAppleTls IOS_TARGET=iPhone

