XBUILD=/Applications/Xcode.app/Contents/Developer/usr/bin/xcodebuild
PROJECT_ROOT=../ManagedNetstat
PROJECT=$(PROJECT_ROOT)/ManagedNetstat.xcodeproj
TARGET=ManagedNetstat

all: libManagedNetstat.a

libManagedNetstat-i386.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphonesimulator -configuration Release clean build
	-mv $(PROJECT_ROOT)/Build/Release-iphonesimulator/lib$(TARGET).a $@

libManagedNetstat-armv7.a:
	$(XBUILD) -project $(PROJECT) -target $(TARGET) -sdk iphoneos -arch armv7 -configuration Release clean build
	-mv $(PROJECT_ROOT)/Build/Release-iphoneos/lib$(TARGET).a $@

libManagedNetstat.a: libManagedNetstat-i386.a libManagedNetstat-armv7.a
	lipo -create -output $@ $^

clean:
	-rm -f *.a *.dll
