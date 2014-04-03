/*
 * Copyright (c) 2008-2012 Apple Inc. All rights reserved.
 *
 * @APPLE_OSREFERENCE_LICENSE_HEADER_START@
 *
 * This file contains Original Code and/or Modifications of Original Code
 * as defined in and that are subject to the Apple Public Source License
 * Version 2.0 (the 'License'). You may not use this file except in
 * compliance with the License. The rights granted to you under the License
 * may not be used to create, or enable the creation or redistribution of,
 * unlawful or unlicensed copies of an Apple operating system, or to
 * circumvent, violate, or enable the circumvention or violation of, any
 * terms of an Apple operating system software license agreement.
 *
 * Please obtain a copy of the License at
 * http://www.opensource.apple.com/apsl/ and read it before using this file.
 *
 * The Original Code and all software distributed under the License are
 * distributed on an 'AS IS' basis, WITHOUT WARRANTY OF ANY KIND, EITHER
 * EXPRESS OR IMPLIED, AND APPLE HEREBY DISCLAIMS ALL SUCH WARRANTIES,
 * INCLUDING WITHOUT LIMITATION, ANY WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE, QUIET ENJOYMENT OR NON-INFRINGEMENT.
 * Please see the License for the specific language governing rights and
 * limitations under the License.
 ** @APPLE_OSREFERENCE_LICENSE_HEADER_END@
 */
/*
 * Copyright (c) 1983, 1988, 1993, 1995
 *	The Regents of the University of California.  All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. All advertising materials mentioning features or use of this software
 *    must display the following acknowledgement:
 *	This product includes software developed by the University of
 *	California, Berkeley and its contributors.
 * 4. Neither the name of the University nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE REGENTS AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
 */

#ifndef lint
/*
static char sccsid[] = "@(#)inet.c	8.5 (Berkeley) 5/24/95";
*/
static const char rcsid[] =
	"$Id: inet.c,v 1.9 2006/04/04 04:36:27 lindak Exp $";
#endif /* not lint */

/*
 * Modified 01/27/2014 by Martin Baulig <martin.baulig@xamarin.com>.
 *
 * The new API is closely based on the original protopr() function, using
 * an enumerator-like model.
 */

#define PRIVATE 1

#include <sys/param.h>
#include <sys/queue.h>
#include <sys/sysctl.h>

#include <netinet/in.h>
#include <netinet/in_systm.h>
#include <netinet/ip.h>
#ifdef INET6
#include <netinet/ip6.h>
#endif /* INET6 */

#include <arpa/inet.h>
#include <err.h>
#include <errno.h>
#include <netdb.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <unistd.h>
#include "netstat.h"

#include "native-netstat.h"

typedef struct {
	char *buf, *next;
	size_t len;
	boolean_t udp;
	struct xinpgen *oxig;
	int which;
} NativeNetstatData;

void *
native_netstat_open (boolean_t udp)
{
	NativeNetstatData *data;
	char *buf;
	const char *mibvar;
	size_t len;
	
	mibvar = udp ? "net.inet.udp.pcblist_n" : "net.inet.tcp.pcblist_n";
	len = 0;

	if (sysctlbyname (mibvar, 0, &len, 0, 0) < 0) {
		if (errno != ENOENT)
			warn ("sysctl: %s", mibvar);
		return NULL;
	}
	if ((buf = malloc (len)) == 0) {
		warn ("malloc %lu bytes", (u_long)len);
		return NULL;
	}
	if (sysctlbyname (mibvar, buf, &len, 0, 0) < 0) {
		warn ("sysctl: %s", mibvar);
		free (buf);
		return NULL;
	}
	
	if (len <= sizeof (struct xinpgen)) {
		free (buf);
		return NULL;
	}
	
	data = malloc (sizeof (NativeNetstatData));
	data->buf = buf;
	data->len = len;
	data->oxig = (struct xinpgen *)buf;
	
	data->next = buf + ROUNDUP64 (data->oxig->xig_len);
	data->which = 0;
	data->udp = udp;
	
	return data;
}

void
native_netstat_close (void *ptr)
{
	NativeNetstatData *data = (NativeNetstatData*)ptr;

	if (!data)
		return;
	
	free (data->buf);
	free (data);
}

boolean_t
native_netstat_move_next (void *ptr, NativeNetstatEntry *entry)
{
	NativeNetstatData *data = (NativeNetstatData*)ptr;
	struct xgen_n *xgn;
	struct xtcpcb_n *tp = NULL;
	struct xinpcb_n *inp = NULL;
	struct xsocket_n *so = NULL;
	struct xsockbuf_n *so_rcv = NULL;
	struct xsockbuf_n *so_snd = NULL;
	struct xsockstat_n *so_stat = NULL;
	
again:
	if (!data->next)
		return FALSE;
	if (data->next >= data->buf + data->len)
		return FALSE;
	
	xgn = (struct xgen_n *)data->next;
	if (xgn->xgn_len <= sizeof (struct xinpgen)) {
		data->next = NULL;
		return FALSE;
	}

	data->next += ROUNDUP64 (xgn->xgn_len);
	
	if ((data->which & xgn->xgn_kind) == 0) {
		data->which |= xgn->xgn_kind;
		switch (xgn->xgn_kind) {
		case XSO_SOCKET:
			so = (struct xsocket_n *)xgn;
			break;
		case XSO_RCVBUF:
			so_rcv = (struct xsockbuf_n *)xgn;
			break;
		case XSO_SNDBUF:
			so_snd = (struct xsockbuf_n *)xgn;
			break;
		case XSO_STATS:
			so_stat = (struct xsockstat_n *)xgn;
			break;
		case XSO_INPCB:
			inp = (struct xinpcb_n *)xgn;
			break;
		case XSO_TCPCB:
			tp = (struct xtcpcb_n *)xgn;
			break;
		default:
			fprintf (stderr, "unexpected kind %d\n", xgn->xgn_kind);
			break;
		}
	} else {
		fprintf (stderr, "got %d twice\n", xgn->xgn_kind);
	}
		
	if ((!data->udp && data->which != ALL_XGN_KIND_TCP) || (data->udp && data->which != ALL_XGN_KIND_INP))
		goto again;
	data->which = 0;
		
	/* Ignore sockets for protocols other than the desired one. */
	if (data->udp) {
		if (so->xso_protocol != IPPROTO_UDP)
			goto again;
	} else {
		if (so->xso_protocol != IPPROTO_TCP)
			goto again;
	}
		
	/* Ignore PCBs which were freed during copyout. */
	if (inp->inp_gencnt > data->oxig->xig_gen)
		goto again;
	
	// Only IPv4
	if ((inp->inp_vflag & INP_IPV4) == 0)
		goto again;
	
	entry->laddr = inp->inp_laddr.s_addr;
	entry->raddr = inp->inp_faddr.s_addr;
	entry->lport = ntohs (inp->inp_lport);
	entry->rport = ntohs (inp->inp_fport);
	entry->flags = inp->inp_flags;
	
	if (!data->udp)
		entry->state = tp->t_state;
		
	return TRUE;
}