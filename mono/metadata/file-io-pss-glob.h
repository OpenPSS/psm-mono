/*	$OpenBSD: glob.h,v 1.10 2005/12/13 00:35:22 millert Exp $	*/
/*	$NetBSD: glob.h,v 1.5 1994/10/26 00:55:56 cgd Exp $	*/

/*
 * Copyright (c) 1989, 1993
 *	The Regents of the University of California.  All rights reserved.
 *
 * This code is derived from software contributed to Berkeley by
 * Guido van Rossum.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. Neither the name of the University nor the names of its contributors
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
 *
 *	@(#)glob.h	8.1 (Berkeley) 6/2/93
 */

#ifndef _PSS_GLOB_H_
#define	_PSS_GLOB_H_

#include <glib.h>

#include "pssdef.h"

struct stat;
typedef struct {
	uint32_t gl_pathc;		/* Count of total paths so far. */
	uint32_t gl_offs;		/* Reserved at beginning of gl_pathv. */
	int gl_flags;		/* Copy of flags parameter to glob. */
	char **gl_pathv;	/* List of paths matching pattern. */
} pss_glob_t;

#define PSS_GLOB_APPEND	0x0001	/* Append to output from previous call. */
#define PSS_GLOB_UNIQUE	0x0040	/* When appending only add items that aren't already in the list */
#define	PSS_GLOB_NOSPACE	(-1)	/* Malloc call failed. */
#define	PSS_GLOB_ABORTED	(-2)	/* Unignored error. */
#define	PSS_GLOB_NOMATCH	(-3)	/* No match and PSS_GLOB_NOCHECK not set. */
#define	PSS_GLOB_NOSYS	(-4)	/* Function not supported. */

#define	PSS_GLOB_MAGCHAR	0x0100	/* Pattern had globbing characters. */
#define PSS_GLOB_LIMIT	0x2000	/* Limit pattern match output to ARG_MAX */
#define PSS_GLOB_IGNORECASE 0x4000	/* Ignore case when matching */
#define PSS_GLOB_ABEND	PSS_GLOB_ABORTED /* backward compatibility */

G_BEGIN_DECLS
int	_pss_glob(ScePssHandle, const char *, int, pss_glob_t *, scePssDirectoryReadFunc);
void	_pss_globfree(pss_glob_t *);
G_END_DECLS

#endif /* !_PSS_GLOB_H_ */
