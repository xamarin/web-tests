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
	
CleanAll::
	git clean -xffd

IOS-Build::
	$(MAKE) -f $(TOP)/ios.make .IOS-Build
	
IOS-Sim-Work::
	$(MAKE) -f $(TOP)/ios.make .IOS-Simulator \
		XAMARIN_ASYNCTESTS_ARGS="--wrench --features=+Experimental --debug --log-level=5" \
		TEST_CATEGORY=Work TEST_OUTPUT=TestResult-Work.xml \
		IOS_CONFIGURATION=Wrench
		
IOS-Sim-All::
	$(MAKE) -f $(TOP)/ios.make .IOS-Simulator \
		XAMARIN_ASYNCTESTS_ARGS="--wrench" \
		TEST_CATEGORY=All TEST_OUTPUT=TestResult-All.xml \
		IOS_CONFIGURATION=Wrench

IOS-Sim-Experimental::
	$(MAKE) -f $(TOP)/ios.make .IOS-Simulator \
		XAMARIN_ASYNCTESTS_ARGS="--wrench --features=+Experimental --debug --log-level=5" \
		TEST_CATEGORY=All TEST_OUTPUT=TestResult-Experimental.xml \
		IOS_CONFIGURATION=Wrench

IOS-Sim-Work-AppleTls::
	$(MAKE) -f $(TOP)/ios.make .IOS-Simulator \
		XAMARIN_ASYNCTESTS_ARGS="--wrench --features=+Experimental --debug --log-level=5" \
		TEST_CATEGORY=Work TEST_OUTPUT=TestResult-Work-AppleTls.xml \
		IOS_CONFIGURATION=WrenchAppleTls
		
IOS-Sim-All-AppleTls::
	$(MAKE) -f $(TOP)/ios.make .IOS-Simulator \
		XAMARIN_ASYNCTESTS_ARGS="--wrench" \
		TEST_CATEGORY=All TEST_OUTPUT=TestResult-All-AppleTls.xml \
		IOS_CONFIGURATION=WrenchAppleTls

IOS-Sim-Experimental-AppleTls::
	$(MAKE) -f $(TOP)/ios.make .IOS-Simulator \
		XAMARIN_ASYNCTESTS_ARGS="--wrench --features=+Experimental --debug --log-level=5" \
		TEST_CATEGORY=All TEST_OUTPUT=TestResult-Experimental-AppleTls.xml \
		IOS_CONFIGURATION=WrenchAppleTls

