MDTOOL = "/Applications/Xamarin Studio.app/Contents/MacOS/mdtool"
MONO_FRAMEWORK = /Library/Frameworks/Mono.framework/Versions/Current
NUGET_EXE = $(MONO_FRAMEWORK)/lib/mono/nuget/NuGet.exe

XBUILD_OPTIONS = /p:Configuration=Console
SOLUTION = Xamarin.WebTests.sln
MONO = mono
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
	$(MONO) $(NUGET_EXE) restore Xamarin.WebTests.iOS.sln
	$(MDTOOL) build Xamarin.WebTests.iOS.sln -c:'WrenchAppleTls|iPhoneSimulator'

