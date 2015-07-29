Xamarin.WebTests - Quick Start Guide
====================================

I have added a very simple `Makefile` to help quickly and easily run all the tests.

Use the branch called [stable](https://github.com/xamarin/web-tests/tree/stable), "master"
is outdated and won't be updated anymore.  There is a Xamarin Studio configuration called
"Console", which will only build the Console tests (no GUI, Android or iOS).

To run, simply run the `Xamarin.WebTests.Console.exe` without arguments.

When filing any bugs or problems, please make sure to attach both the full test output
and the `TestResult.xml` file which is generated in the current working directory.

If possible, then also re-run the tests with debugging:

    $ mono --debug ./Xamarin.WebTests.Console/bin/Debug/Xamarin.WebTests.Console.exe --debug --log-level=8

There is also a very simple `Makefile`, which will build and run all the tests.


Last changed July 29th, 2015
Martin Baulig <martin.baulig@xamarin.com>



Xamarin.AsyncTests
==================

The new Mac GUI has two main modes of operation: it can either listen for incoming connections or
run tests locally by forking a test runner.

Test Suite and Dependencies
---------------------------

To ease testing on Mobile, a test suite typically lives in a PCL (though it does not have to
be a PCL).  Such PCLs usually require some platform-specific code, which can be used via the
`DependencyInjector.Get<T> ()` API.  You need to provide a platform-specific implementation
and register it via either one or more `DependencyInjector.RegisterDependency<T> (Func<T>)` or
`DependencyInjector.RegisterAssembly(Assembly)`.

These platform-specific implementations need to be loaded into the test runner process at
runtime.  To automate this, a platform-specific implementation assembly may use
`[assembly: AsyncTestSuite (typeof (provider), true)]` and you pass it to the command-line
tool instead of the actual test suite.  The framework then automatically registers all the
dependencies.

Each platform-specific implementation should be an executable, which references `Xamarin.AsyncTests.Console` and calls [`Xamarin.AsyncTests.Console.Program.Run(Assembly,string[])`](https://github.com/xamarin/web-tests/blob/martin-newtls/Xamarin.AsyncTests.Console/Program.cs#L95). 

For an example, see `Xamarin.WebTests.Console.exe`.  The main test suite is `Xamarin.WebTests.dll`,
which is a PCL, so it needs a platform-specific implementation.  Yon can either use

    $ mono --debug Xamarin.AsyncTests.Console.exe --dependency=Xamarin.WebTests.Console.exe Xamartin.WebTests.dll

or

    $ mono --debug Xamarin.AsyncTests.Console.exe Xamartin.WebTests.Console.exe

or simply run it directly

    $ mono --debug Xamarin.WebTests.Console.exe

Unfortunately, this technique does not work on Mobile, so a custom test app is required for
each test suite on each platform.

Settings Dialog
---------------

When starting the GUI for the first time, you need to open the Settings Dialog and configure a few things in there.

At the top, you can select one of the 4 currently supported modes of operation:

* You can wait for incoming connections (from `Xamarin.AsyncTests.Console.exe` or a platform-specific test implementation projects which includes it).  This requires the "Listen Address" to be set (must be an actual IP address, can be `127.0.0.1`, but not `0.0.0.0`).

* You can launch the tests from the GUI.  This will fork an external test runner.  Specify the platform-specific test implementation assembly (`Xamarin.WebTests.Console.exe` or `Mono.Security.NewTls.TestProvider.exe`), the Mono runtime prefix (such as `/Workspace/INSTALL` or `/Library/Frameworks/Mono.framework/Versions/Current`) and a listen address (will be used internally for the external test runner to connect).

* You can connect to Android or iOS.  Each needs its corresponding endpoint to be set (the Android / iOS test app will display the correct address on startup).

When done, use "TestSession / Start" from the main menu.

Listening for Connections
-------------------------

Select "Wait for connection" as "Server Mode" in the settings dialog.

Then connect from the Xamarin.AsyncTests.Console.exe command-line tool:

    $ mono --debug Xamarin.AsyncTests.Console.exe --gui=127.0.0.1:8888 Xamarin.WebTests.Console.exe

This tool understands some additional command-line options:

* `--result=FILE`
  Dump full test result in XML form into that file.  The default is `TestResult.xml`.
  
* `--no-result`
  Do not dump XML results.
  
* `--debug`
  Set log-level to a -1 (maximum logging) and disable any timeouts.  This is intended for
  single-stepping in the debugger.

* `--log-level=LEVEL`
  Modify local log level.
  
* `--optional-gui`
  Fall back to running tests locally if GUI connection fails with `SocketError.ConnectionRefused`.
  
* `--settings=FILE`
  Load settings from that file.  The GUI will override those.

* `--connect=ENDPOINT`
  Connect to a remote server.  Cannot be used together with the GUI.  Used for Mobile.
  
* `--gui=ENDPOINT`
  Connect to the GUI and wait for commands.
  
* `--dependency=ASSEMBLY`
  Loads the specified assembly as a dependency.  Can be used multiple times.
  
* `--category=CATEGORY`
  Select test category to run.
  
This is the recommended mode of operation for debugging and fixing bugs.  You launch the
GUI from a terminal, then run `Xamarin.AsyncTests.Console` from within Xamarin Studio.  This
gives you the full Xamarin Studio debugging functionality while still being able to use the
GUI to view test results, select which tests to run, etc.

The external process is required because the TLS tests need to be run with a custom Mono
runtime which has the new TLS changes.  Without an external process, you would have to either
install this custom runtime as the default `/Library/Frameworks/Mono.framework` or build
Xamarin.Mac for your custom prefix.

The inter-process communication layer only uses Sockets and XML, so it won't interfer with
the testing.

Running from the GUI
--------------------

The tests can also be run directly from the GUI, in which case the GUI will launch the external
`Xamarin.WebTests.Console` process.

Before you can do that for the first time, you need to open the settings dialog and configure
some values:

* "Mono Runtime"
  The Mono Runtime prefix, for instance `/Workspace/INSTALL`.

* "Launcher Path"
  Full path name of the `Xamarin.AsyncTests.Console.exe` assembly.
  
* "Test Suite"
  Full path name of the platform-specific test suite (for instance `Xamarin.WebTests.Console.exe`).
  
* "Arguments"
  Optional arguments to be passed to the launcher.
  
Running on Mobile
-----------------

For mobile, a custom platform-specific test app is required for each platform and each test suite.  You can use the `Xamarin.AsyncTests.Mobile` PCL to get a simple Xamarin Forms based UI.  The `MobileTestApp` will also take care of the server stuff for you.

To run the tests, first launch the `Xamarin.WebTests.Android` / `Xamarin.WebTests.iOS` project.  Then start the GUI, select the correct server mode and start.


Last changed March 13th, 2015
Martin Baulig <martin.baulig@xamarin.com>

