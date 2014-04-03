//
//  native-netstat.h
//  MobileLibrary
//
//  Created by Martin Baulig on 24/01/14.
//  Copyright (c) 2014 Xamarin. All rights reserved.
//

#ifndef MobileLibrary_native_netstat_h
#define MobileLibrary_native_netstat_h

#include <stdint.h>

typedef enum {
	Closed,
	Listen,
	SynSent,
	SynRecvd,
	Established,
	CloseWait,
	FinWait1,
	Closing,
	LastAck,
	FinWait2,
	TimeWait
} NativeNetstatTcpState;

typedef struct {
	uint64_t laddr, raddr;
	uint32_t lport, rport;
	uint32_t flags;
	NativeNetstatTcpState state;
} NativeNetstatEntry;

extern void *
native_netstat_open (boolean_t udp);

extern void
native_netstat_close (void *ptr);

extern boolean_t
native_netstat_move_next (void *ptr, NativeNetstatEntry *entry);

#endif
