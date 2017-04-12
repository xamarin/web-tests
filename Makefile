TOP = .
include $(TOP)/Make.config

ALL_BUILD_TARGETS = \
	Build-FastCheck Build-Console Build-Mac Build-IOS-Debug

All:: $(ALL_BUILD_TARGETS)
	@echo "Build done"

CleanAll::
	git clean -xffd

Jenkins-Build::
	which mono
	$(MONO) --version
	$(MAKE) JENKINS=1 Build-$(JENKINS_TARGET) $*

Jenkins-Install::
	$(MAKE) JENKINS=1 Install-$(JENKINS_TARGET) $*

Jenkins-Run::
	$(MAKE) JENKINS=1 Run-$(JENKINS_TARGET)-$(JENKINS_TESTS)

#
#
#

Build-FastCheck::
	$(MAKE) Build-Console

Run-FastCheck-Provision::
	$(MONO) --version
	$(MAKE) Run-Console-Martin

Run-FastCheck-%::
	$(MONO) --version
	$(MAKE) Run-Console-$*

#
#
#

Build-Console::
	$(MAKE) ASYNCTESTS_COMMAND=local TARGET_NAME=$@ CONSOLE_CONFIGURATION=Debug .Console-Internal-Build

Build-ConsoleLegacy::
	$(MAKE) ASYNCTESTS_COMMAND=local TARGET_NAME=$@ CONSOLE_CONFIGURATION=DebugLegacy .Console-Internal-Build

Build-ConsoleAppleTls::
	$(MAKE) ASYNCTESTS_COMMAND=local TARGET_NAME=$@ CONSOLE_CONFIGURATION=DebugAppleTls .Console-Internal-Build

Run-Console-%::
	$(MAKE) ASYNCTESTS_COMMAND=local TARGET_NAME=$@ CONSOLE_CONFIGURATION=Debug .Console-Run-$*

Run-ConsoleLegacy-%::
	$(MAKE) ASYNCTESTS_COMMAND=local TARGET_NAME=$@ CONSOLE_CONFIGURATION=DebugLegacy .Console-Run-$*

Run-ConsoleAppleTls-%::
	$(MAKE) ASYNCTESTS_COMMAND=local TARGET_NAME=$@ CONSOLE_CONFIGURATION=DebugAppleTls .Console-Run-$*

#
#
#

Build-IOS-Debug::
	$(MAKE) ASYNCTESTS_COMMAND=simulator TARGET_NAME=$@ IOS_TARGET=iPhoneSimulator IOS_CONFIGURATION=Debug .IOS-Internal-Build

Run-IOS-Debug-%::
	$(MAKE) ASYNCTESTS_COMMAND=simulator TARGET_NAME=$@ IOS_TARGET=iPhoneSimulator IOS_CONFIGURATION=Debug .IOS-Run-$*

#
#
#

Build-Mac::
	$(MAKE) ASYNCTESTS_COMMAND=mac TARGET_NAME=$@ MAC_CONFIGURATION=Debug .Mac-Internal-Build

Run-Mac-%::
	$(MAKE) ASYNCTESTS_COMMAND=mac TARGET_NAME=$@ MAC_CONFIGURATION=Debug .Mac-Run-$*

#
#
#

Build-Android::
	$(MAKE) ASYNCTESTS_COMMAND=android TARGET_NAME=$@ ANDROID_CONFIGURATION=Debug .Android-Internal-Build

Install-Android::
	$(MAKE) ASYNCTESTS_COMMAND=android TARGET_NAME=$@ ANDROID_CONFIGURATION=Debug .Android-Internal-Install

Run-Android-%::
	$(MAKE) ASYNCTESTS_COMMAND=android TARGET_NAME=$@ ANDROID_CONFIGURATION=Debug .Android-Run-$*

#
#
#

IOS-Sim-%::
	$(MAKE) IOS_TARGET=iPhoneSimulator ASYNCTESTS_COMMAND=simulator TARGET_NAME=$@ .IOS-$*

IOS-Dev-%::
	$(MAKE) IOS_TARGET=iPhone ASYNCTESTS_COMMAND=device TARGET_NAME=$@ .IOS-$*

TVOS-Sim-%::
	$(MAKE) TVOS_TARGET=iPhoneSimulator ASYNCTESTS_COMMAND=tvos TARGET_NAME=$@ .TVOS-$*

TVOS-Dev-%::
	$(MAKE) TVOS_TARGET=iPhone ASYNCTESTS_COMMAND=device TARGET_NAME=$@ .TVOS-$*

Console-%::
	$(MAKE) ASYNCTESTS_COMMAND=local TARGET_NAME=$@ .Console-$*

DotNet-%::
	$(MAKE) ASYNCTESTS_COMMAND=local TARGET_NAME=$* .DotNet-$*

Mac-%::
	$(MAKE) ASYNCTESTS_COMMAND=mac TARGET_NAME=$@ .Mac-$*

Android-%::
	$(MAKE) ASYNCTESTS_COMMAND=android TARGET_NAME=$@ .Android-$*
	
Check-System::
	@./system-dependencies.sh

Create-Keychain::
	-security create-keychain -p $(TEST_KEYCHAIN_PASSWORD) $(TEST_KEYCHAIN)
	security default-keychain -s $(TEST_KEYCHAIN)
	security unlock-keychain -p $(TEST_KEYCHAIN_PASSWORD) $(TEST_KEYCHAIN)

Default-Keychain::
	security default-keychain -s login.keychain
	-security delete-keychain $(TEST_KEYCHAIN)

#
# Internal IOS make targets
#

.IOS-Run-Experimental::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=All .IOS-Internal-Run

.IOS-Run-All::
	$(MAKE) TEST_CATEGORY=All .IOS-Internal-Run

.IOS-Run-Work::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Work .IOS-Internal-Run

.IOS-Run-New::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=New .IOS-Internal-Run

.IOS-Run-Martin::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Martin .IOS-Internal-Run

.IOS-Internal-Build::
	$(MONO) $(NUGET_EXE) restore $(EXTRA_NUGET_RESTORE_ARGS) Xamarin.WebTests.iOS.sln
	$(MSBUILD) /p:Configuration='$(IOS_CONFIGURATION)' /p:Platform='$(IOS_TARGET)' Xamarin.WebTests.iOS.sln

.IOS-Internal-Run::
	$(MONO) $(MONO_FLAGS) $(ASYNCTESTS_CONSOLE_EXE) $(ASYNCTESTS_ARGS) $(JENKINS_ARGS) --category=$(TEST_CATEGORY) \
		--sdkroot=$(XCODE_DEVELOPER_ROOT) --stdout=$(STDOUT) --stderr=$(STDERR) --result=$(TEST_RESULT) \
		--junit-result=$(JUNIT_TEST_RESULT) $(EXTRA_ASYNCTESTS_ARGS) $(ASYNCTESTS_COMMAND) $(WEBTESTS_IOS_APP)

#
# Internal TVOS make targets
#

.TVOS-Debug-%::
	$(MAKE) TVOS_CONFIGURATION=Debug .TVOS-Run-$*

.TVOS-DebugAppleTls-%::
	$(MAKE) TVOS_CONFIGURATION=DebugAppleTls .TVOS-Run-$*

.TVOS-Build-Debug::
	$(MAKE) TVOS_CONFIGURATION=Debug .TVOS-Internal-Build

.TVOS-Build-DebugAppleTls::
	$(MAKE) TVOS_CONFIGURATION=DebugAppleTls .TVOS-Internal-Build

.TVOS-Run-Experimental::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=All .TVOS-Internal-Run

.TVOS-Run-All::
	$(MAKE) TEST_CATEGORY=All .TVOS-Internal-Run

.TVOS-Run-Work::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Work .TVOS-Internal-Run

.TVOS-Run-New::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=New .TVOS-Internal-Run

.TVOS-Run-Martin::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Martin .TVOS-Internal-Run

.TVOS-Internal-Build::
	$(MONO) $(NUGET_EXE) restore $(EXTRA_NUGET_RESTORE_ARGS) Xamarin.WebTests.iOS.sln
	$(XBUILD) /p:Configuration='$(TVOS_CONFIGURATION)' /p:Platform='$(TVOS_TARGET)' Xamarin.WebTests.TVOS.sln

.TVOS-Internal-Run::
	$(MONO) $(ASYNCTESTS_CONSOLE_EXE) $(ASYNCTESTS_ARGS) $(JENKINS_ARGS) --category=$(TEST_CATEGORY) \
		--sdkroot=$(XCODE_DEVELOPER_ROOT) --stdout=$(STDOUT) --stderr=$(STDERR) --result=$(TEST_RESULT) \
		$(EXTRA_ASYNCTESTS_ARGS) $(ASYNCTESTS_COMMAND) $(WEBTESTS_TVOS_APP)

#
# Internal Console make targets
#

.Console-Run-Experimental::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=All .Console-Internal-Run

.Console-Run-All::
	$(MAKE) TEST_CATEGORY=All .Console-Internal-Run

.Console-Run-Work::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Work .Console-Internal-Run

.Console-Run-New::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=New .Console-Internal-Run

.Console-Run-Martin::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Martin .Console-Internal-Run

.Console-Internal-Build::
	$(MSBUILD) /p:Configuration='$(CONSOLE_CONFIGURATION)' $(WEBTESTS_CONSOLE_SLN)

.Console-Internal-Run::
	$(MONO) $(MONO_FLAGS) $(WEBTESTS_CONSOLE_EXE) $(ASYNCTESTS_ARGS) $(JENKINS_ARGS) --category=$(TEST_CATEGORY) \
		--stdout=$(STDOUT) --stderr=$(STDERR) --result=$(TEST_RESULT) --junit-result=$(JUNIT_TEST_RESULT) \
		$(EXTRA_ASYNCTESTS_ARGS) $(ASYNCTESTS_COMMAND)

#
# Internal .NET make targets
#

.DotNet-Build-Debug::
	$(MAKE) DOTNET_CONFIGURATION=Debug .DotNet-Internal-Build

.DotNet-Debug-%::
	$(MAKE) DOTNET_CONFIGURATION=Debug .DotNet-Run-$*

.DotNet-Run-Experimental::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=All .DotNet-Internal-Run

.DotNet-Run-All::
	$(MAKE) TEST_CATEGORY=All .DotNet-Internal-Run

.DotNet-Run-Work::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Work .DotNet-Internal-Run

.DotNet-Run-New::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=New .DotNet-Internal-Run

.DotNet-Run-Martin::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Martin .DotNet-Internal-Run

.DotNet-Internal-Build::
	$(MSBUILD) /p:Configuration='$(DOTNET_CONFIGURATION)' Xamarin.WebTests.DotNet.sln

.DotNet-Internal-Run::
	$(MONO) $(MONO_FLAGS) $(WEBTESTS_DOTNET_EXE) $(ASYNCTESTS_ARGS) $(JENKINS_ARGS) --category=$(TEST_CATEGORY) \
		--result=$(TEST_RESULT) $(EXTRA_ASYNCTESTS_ARGS) $(ASYNCTESTS_COMMAND)

#
# Internal Mac make targets
#

.Mac-Run-Experimental::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=All .Mac-Internal-Run

.Mac-Run-All::
	$(MAKE) TEST_CATEGORY=All .Mac-Internal-Run

.Mac-Run-Work::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Work .Mac-Internal-Run

.Mac-Run-New::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=New .Mac-Internal-Run

.Mac-Run-Martin::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Martin .Mac-Internal-Run

.Mac-Internal-Build::
	$(MONO) $(NUGET_EXE) restore $(EXTRA_NUGET_RESTORE_ARGS) Xamarin.WebTests.Mac.sln
	$(XBUILD) /p:Configuration='$(MAC_CONFIGURATION)' Xamarin.WebTests.Mac.sln

# FIXME: do we still need this after using "open -F -W -n"?
# defaults write -app $(WEBTESTS_MAC_APP) NSQuitAlwaysKeepsWindows -bool false

.Mac-Internal-Run::
	$(MONO) $(MONO_FLAGS) $(ASYNCTESTS_CONSOLE_EXE) $(ASYNCTESTS_ARGS) $(JENKINS_ARGS) --category=$(TEST_CATEGORY) \
		--stdout=$(STDOUT) --stderr=$(STDERR) --result=$(TEST_RESULT) --junit-result=$(JUNIT_TEST_RESULT) \
		$(EXTRA_ASYNCTESTS_ARGS) $(ASYNCTESTS_COMMAND) $(WEBTESTS_MAC_APP)

#
# Internal Android make targets
#

.Android-Build-Debug::
	$(MAKE) ANDROID_CONFIGURATION=Debug .Android-Internal-Build

.Android-Install-Debug::
	$(MAKE) ANDROID_CONFIGURATION=Debug .Android-Internal-Install

.Android-Debug-%::
	$(MAKE) ANDROID_CONFIGURATION=Debug .Android-Run-$*

.Android-Run-All::
	$(MAKE) TEST_CATEGORY=All .Android-Internal-Run

.Android-Run-Work::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Work .Android-Internal-Run

.Android-Run-New::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=New .Android-Internal-Run

.Android-Run-Martin::
	$(MAKE) ASYNCTESTS_ARGS="--features=+Experimental --debug --log-level=5" TEST_CATEGORY=Martin .Android-Internal-Run

.Android-Internal-Build::
	$(MONO) $(NUGET_EXE) restore $(EXTRA_NUGET_RESTORE_ARGS) Xamarin.WebTests.Android.sln
	$(XBUILD) /p:Configuration='$(ANDROID_CONFIGURATION)' Xamarin.WebTests.Android.sln

.Android-Internal-Install::
	$(MONO) $(ASYNCTESTS_CONSOLE_EXE) avd
	$(MONO) $(ASYNCTESTS_CONSOLE_EXE) emulator
	$(XBUILD) /p:Configuration='$(ANDROID_CONFIGURATION)' /t:Install $(WEBTESTS_ANDROID_PROJECT)

.Android-Internal-Run::
	$(MONO) $(MONO_FLAGS) $(ASYNCTESTS_CONSOLE_EXE) $(ASYNCTESTS_ARGS) $(JENKINS_ARGS) --category=$(TEST_CATEGORY) \
		--stdout=$(STDOUT) --stderr=$(STDERR) --result=$(TEST_RESULT) $(EXTRA_ASYNCTESTS_ARGS) \
		$(ASYNCTESTS_COMMAND) $(WEBTESTS_ANDROID_MAIN_ACTIVITY)
