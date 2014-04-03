//
//  ManagedNetstat.h
//  ManagedNetstat
//
//  Created by Martin Baulig on 27/01/14.
//  Copyright (c) 2014 Xamarin. All rights reserved.
//

#import <Foundation/Foundation.h>

#include "native-netstat.h"

@interface ManagedNetstat : NSObject

- (void *)open: (BOOL)udp;
- (void)close: (void*)ptr;
- (BOOL)move_next: (void*)ptr entry:(NativeNetstatEntry*)entry;

@end
