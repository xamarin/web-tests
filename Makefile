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

