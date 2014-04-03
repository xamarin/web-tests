//
//  ManagedNetstat.m
//  ManagedNetstat
//
//  Created by Martin Baulig on 27/01/14.
//  Copyright (c) 2014 Xamarin. All rights reserved.
//

#import "ManagedNetstat.h"

@implementation ManagedNetstat

- (void *)open: (BOOL)udp {
	return native_netstat_open (udp);
}

- (void)close: (void*)ptr {
	native_netstat_close (ptr);
}

- (BOOL)move_next: (void*)ptr entry:(NativeNetstatEntry*)entry {
	return native_netstat_move_next (ptr, entry);
}

@end
