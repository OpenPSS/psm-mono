#ifndef __UTILS_MONO_COMPILER_H__
#define __UTILS_MONO_COMPILER_H__

/*
 * This file includes macros used in the runtime to encapsulate different
 * compiler behaviours.
 */
#include <config.h>

#ifdef __GNUC__
#define MONO_ATTR_USED __attribute__ ((used))
#else
#define MONO_ATTR_USED
#endif

#ifdef HAVE_KW_THREAD

#define MONO_HAVE_FAST_TLS
#define MONO_FAST_TLS_SET(x,y) x = y
#define MONO_FAST_TLS_GET(x) x
#define MONO_FAST_TLS_INIT(x)
#define MONO_FAST_TLS_DECLARE(x) static __thread gpointer x MONO_TLS_FAST MONO_ATTR_USED;

#if HAVE_TLS_MODEL_ATTR

#if defined(__PIC__) && !defined(PIC)
/*
 * Must be compiling -fPIE, for executables.  Build PIC
 * but with initial-exec.
 * http://bugs.gentoo.org/show_bug.cgi?id=165547
 */
#define PIC
#define PIC_INITIAL_EXEC
#endif

/* 
 * Define this if you want a faster libmono, which cannot be loaded dynamically as a 
 * module.
 */
//#define PIC_INITIAL_EXEC

#if defined(PIC)

#ifdef PIC_INITIAL_EXEC
#define MONO_TLS_FAST __attribute__((tls_model("initial-exec")))
#else
#if defined (__powerpc__)
/* local dynamic requires a call to __tls_get_addr to look up the
   TLS block address via the Dynamic Thread Vector. In this case Thread
   Pointer relative offsets can't be used as this modules TLS was
   allocated separately (none contiguoiusly) from the initial TLS
   block.

   For now we will disable this. */
#define MONO_TLS_FAST
#else
#define MONO_TLS_FAST __attribute__((tls_model("local-dynamic")))
#endif
#endif

#else

#define MONO_TLS_FAST __attribute__((tls_model("local-exec")))

#endif

#else
#define MONO_TLS_FAST 
#endif

#if defined(__GNUC__) && defined(__i386__)
#if defined(PIC)
#define MONO_THREAD_VAR_OFFSET(var,offset) do { int tmp; __asm ("call 1f; 1: popl %0; addl $_GLOBAL_OFFSET_TABLE_+[.-1b], %0; movl " #var "@gotntpoff(%0), %1" : "=r" (tmp), "=r" (offset)); } while (0)
#else
#define MONO_THREAD_VAR_OFFSET(var,offset) __asm ("movl $" #var "@ntpoff, %0" : "=r" (offset))
#endif
#elif defined(__x86_64__)
#if defined(PIC)
// This only works if libmono is linked into the application
#define MONO_THREAD_VAR_OFFSET(var,offset) do { guint64 foo;  __asm ("movq " #var "@GOTTPOFF(%%rip), %0" : "=r" (foo)); offset = foo; } while (0)
#else
#define MONO_THREAD_VAR_OFFSET(var,offset) do { guint64 foo;  __asm ("movq $" #var "@TPOFF, %0" : "=r" (foo)); offset = foo; } while (0)
#endif
#elif defined(__ia64__) && !defined(__INTEL_COMPILER)
#if defined(PIC)
#define MONO_THREAD_VAR_OFFSET(var,offset) __asm ("addl %0 = @ltoff(@tprel(" #var "#)), gp ;; ld8 %0 = [%0]\n" : "=r" (offset))
#else
#define MONO_THREAD_VAR_OFFSET(var,offset) __asm ("addl %0 = @tprel(" #var "#), r0 ;;\n" : "=r" (offset))
#endif
#elif defined(__arm__) && defined(__ARM_EABI__) && !defined(PIC)
#define MONO_THREAD_VAR_OFFSET(var,offset) __asm ("	ldr	%0, 1f; b 2f; 1: .word " #var "(tpoff); 2:" : "=r" (offset))
#elif defined(__mono_ppc__) && defined(__GNUC__)
#if defined(PIC)
#ifdef PIC_INITIAL_EXEC

#if defined(__mono_ppc64__)
#define MONO_THREAD_VAR_OFFSET(var,offset) \
	do { long off; \
	__asm (	"ld	%0," #var "@got@tprel(2)\n" \
	: "=r" (off)); \
	(offset) = off; } while (0)
#else
/* must be powerpc32 */
#define MONO_THREAD_VAR_OFFSET(var,offset) \
	__asm (	"lwz	%0," #var "@got@tprel(30)\n" \
	: "=r" (offset))
#endif

#else

/* local dynamic requires a call to __tls_get_addr to look up the
   TLS block address via the Dynamic Thread Vector. In this case Thread
   Pointer relative offsets can't be used as this modules TLS was
   allocated separately (none contiguoiusly) from the initial TLS
   block.

   For now we will disable this. */
#define MONO_THREAD_VAR_OFFSET(var,offset) (offset) = -1

#endif
#else
/* Must be local-exec TLS */
#define MONO_THREAD_VAR_OFFSET(var,offset) \
	__asm (	"lis	%0," #var "@tprel@ha\n" \
		"addi	%0,%0, " #var "@tprel@l\n" \
	: "=r" (offset))
#endif
#elif defined(__s390x__)
# if defined(PIC)
// This only works if libmono is linked into the application
#  define MONO_THREAD_VAR_OFFSET(var,offset) do { guint64 foo;  				\
						__asm__ ("basr	%%r1,0\n\t"			\
							 "j	0f\n\t"				\
							 ".quad " #var "@INDNTPOFF\n\t"		\
							 "0:\n\t"				\
							 "lg	%%r2,4(%%r1)\n\t"		\
							 "brasl	%%r14,__tls_get_offset@PLT\n\t" \
							 "lgr	%0,%%r2\n\t"			\
							: "=r" (foo) : 				\
							: "1", "2", "14", "cc");		\
						offset = foo; } while (0)
# else
#  define MONO_THREAD_VAR_OFFSET(var,offset) do { guint64 foo;  				\
						__asm__ ("basr	%%r1,0\n\t"			\
							 "j	0f\n\t"				\
							 ".quad " #var "@NTPOFF\n"		\
							 "0:\n\t"				\
							 "lg	%0,4(%%r1)\n\t"			\
							: "=r" (foo) : : "1");			\
						offset = foo; } while (0)
# endif

#else
#define MONO_THREAD_VAR_OFFSET(var,offset) (offset) = -1
#endif

#if defined(PIC) && !defined(PIC_INITIAL_EXEC)
/* 
 * The above definitions do not seem to work if libmono is loaded dynamically as a module.
 * See bug #78767.
 */
#undef MONO_THREAD_VAR_OFFSET
#define MONO_THREAD_VAR_OFFSET(var,offset) (offset) = -1
#endif

#elif defined(TARGET_VITA)

#define MONO_HAVE_FAST_TLS 
#define MONO_FAST_TLS_SET(x,y) pthread_vita_tls_set_np(x, y)
#define MONO_FAST_TLS_GET(x) pthread_vita_tls_get_np(x)
#define MONO_FAST_TLS_INIT(x)  x = pthread_vita_tls_create_np(NULL)
#define MONO_FAST_TLS_DECLARE(x) static int x;
#undef MONO_THREAD_VAR_OFFSET
#define MONO_THREAD_VAR_OFFSET(var,offset) (offset) = var

#elif defined(__APPLE__) && defined(__i386__) 

#define MONO_HAVE_FAST_TLS
#define MONO_FAST_TLS_SET(x,y) pthread_setspecific(x, y)
#define MONO_FAST_TLS_GET(x) pthread_getspecific(x)
#define MONO_FAST_TLS_INIT(x) pthread_key_create(&x, NULL)
#define MONO_FAST_TLS_DECLARE(x) static pthread_key_t x;

#define MONO_THREAD_VAR_OFFSET(x,y) ({	\
	typeof(x) _x = (x);			\
	pthread_key_t _y;	\
	(void) (&_x == &_y);		\
	y = (gint32) x; })
#else /* no HAVE_KW_THREAD */

#define MONO_THREAD_VAR_OFFSET(var,offset) (offset) = -1

/*Macros to facilitate user code*/
#define MONO_FAST_TLS_INIT(x)
#define MONO_FAST_TLS_DECLARE(x)
#endif

/* Deal with Microsoft C compiler differences */
#ifdef _MSC_VER

#include <float.h>
#define isnan(x)	_isnan(x)
#define trunc(x)	(((x) < 0) ? ceil((x)) : floor((x)))
#define isinf(x)	(_isnan(x) ? 0 : (_fpclass(x) == _FPCLASS_NINF) ? -1 : (_fpclass(x) == _FPCLASS_PINF) ? 1 : 0)
#define isnormal(x)	_finite(x)

#define popen		_popen
#define pclose		_pclose

#include <direct.h>
#define mkdir(x)	_mkdir(x)

/* GCC specific functions aren't available */
#define __builtin_return_address(x)	NULL

#define __func__ __FUNCTION__

#endif /* _MSC_VER */

#if !defined(_MSC_VER) && !defined(PLATFORM_SOLARIS) && !defined(_WIN32) && !defined(__CYGWIN__) && HAVE_VISIBILITY_HIDDEN && !defined(__ANDROID__)
#define MONO_INTERNAL __attribute__ ((visibility ("hidden")))
#if MONO_LLVM_LOADED
#define MONO_LLVM_INTERNAL 
#else
#define MONO_LLVM_INTERNAL MONO_INTERNAL
#endif
#else
#define MONO_INTERNAL 
#define MONO_LLVM_INTERNAL 
#endif

#if HAVE_DEPRECATED
#define MONO_DEPRECATED __attribute__ ((deprecated))
#else
#define MONO_DEPRECATED 
#endif

#ifdef _MSC_VER
#define MONO_NOINLINE __declspec (noinline)
#else
#define MONO_NOINLINE __attribute__((noinline))
#endif

#ifdef _MSC_VER
#define MONO_UNUSED
#else
#define MONO_UNUSED __attribute__((unused))
#endif

#endif /* __UTILS_MONO_COMPILER_H__*/

