/////////////////////////////////////////////////////////////////
//						SCE CONFIDENTIAL
//
//
//      Copyright (C) 2010 Sony Computer Entertainment Inc.
//                        All Rights Reserved.
//
//
/////////////////////////////////////////////////////////////////

#ifndef _PSS_DEF_H_
#define _PSS_DEF_H_

/// API version number (big).
#define PSS_PLATFORM_VERSION_BIG		0
/// API version number (small).
#define PSS_PLATFORM_VERSION_SMALL		1
/// API version number (revision).
#define PSS_PLATFORM_VERSION_REVISION	0

/// PSS platform definition.
#ifndef _PSS_PLATFORM
#define _PSS_PLATFORM	1
#endif // _PSS_PLATFORM

/////////////////////////////////////////////////////////////////
// Compiler defines
/// Compiler-specific define indicating the GCC Compiler.
#define PSS_COMPILER_GNUC 0
/// Compiler-specific define indicating the Microsoft Visual Studio C/C++ Compiler.
#define PSS_COMPILER_MSVC 1
/// SNC C/C++ Compiler
#define PSS_COMPILER_SNC	2

#if defined(__SNC__)
	/// Compiler specific define. Can be used to check which compiler that's being used
	#define PSS_COMPILER		PSS_COMPILER_SNC
	#define __PSS_FUNCTION__	__FUNCTION__
#elif defined( __GNUC__ ) || defined( __GNUG__ )
	/// Compiler-specific define. Test this define to check which compiler is in use.
	#define PSS_COMPILER		PSS_COMPILER_GNUC
	#define __PSS_FUNCTION__	__PRETTY_FUNCTION__
#elif defined( _MSC_VER )
	/// Compiler-specific define. Test this define to check which compiler is in use.
	#define PSS_COMPILER		PSS_COMPILER_MSVC
	#define __PSS_FUNCTION__	__FUNCDNAME__
	#ifndef _WIN32_WINNT
		#define _WIN32_WINNT	0x0502
	#endif
#else
	#error Unsupported compiler and platform!
#endif //


/// Standard define prefix for all functions
#if PSS_COMPILER == PSS_COMPILER_MSVC
	#ifdef __cplusplus
	#define PSS_PUBLIC_API extern "C"
	#define PSS_PRIVATE_API extern "C"
	#else // __cplusplus
	#define PSS_PUBLIC_API 
	#define PSS_PRIVATE_API 
	#endif // __cplusplus
	#define PSS_INLINE static
#elif PSS_COMPILER == PSS_COMPILER_SNC
	#ifdef __cplusplus
	#define PSS_PUBLIC_API extern "C"
	#define PSS_PRIVATE_API extern "C"
	#else // __cplusplus
	#define PSS_PUBLIC_API 
	#define PSS_PRIVATE_API 
	#endif // __cplusplus
	#define PSS_INLINE static inline
#else
	#ifdef __cplusplus
	#define PSS_PUBLIC_API extern "C" __attribute__((visibility("default")))
	#define PSS_PRIVATE_API extern "C" __attribute__((visibility("default")))
	#else // __cplusplus
	#define PSS_PUBLIC_API __attribute__((visibility("default")))
	#define PSS_PRIVATE_API __attribute__((visibility("default")))
	#endif // __cplusplus
	#define PSS_INLINE static inline
#endif

/////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////
// Typedefs
/// Standard return value data type.
typedef int								ScePssResult;
/// Unsigned character data type.
typedef unsigned char					ScePssUInt8;
/// short data type.
typedef unsigned short					ScePssUInt16;
/// Unsigned integer data type.
typedef unsigned int					ScePssUInt32;
/// Signed character data type.
typedef signed char						ScePssInt8;
/// Signed short data type.
typedef signed short					ScePssInt16;
/// Signed integer data type.
typedef int								ScePssInt32;
/// 64-bit integer data type.
typedef long long						ScePssInt64;
/// Unsigned 64-bit integer data type.
typedef unsigned long long				ScePssUInt64;
/// Floating point data type.
typedef float							ScePssFloat;
/// Double-precision floating point data type.
typedef double							ScePssDouble;
/// Handle data type.
typedef ScePssUInt64					ScePssHandle;
/// Wide character data type.
typedef ScePssUInt16					ScePssWChar;
/// Character data type.
typedef char							ScePssChar;
/// Size defined as an unsigned integer data type.
typedef ScePssUInt32					ScePssSize;
/// Boolean data type.
typedef ScePssInt32						ScePssBool;
/// 64-bit integer data type.
typedef ScePssInt64						ScePssLongLong;
/// Byte data type as an unsigned integer.
typedef ScePssUInt8						ScePssByte;

// Integer object with same size as pointer.
#if defined(__x86_64__) || defined(_M_X64) || defined(__powerpc64__) || defined(__alpha__) || defined(__ia64__) || defined(__s390__) || defined(__s390x__)
typedef unsigned long long	pss_uintptr_t;
typedef long long			pss_intptr_t;
#else
typedef unsigned int		pss_uintptr_t;
typedef int					pss_intptr_t;
#endif

/////////////////////////////////////////////////////////////////
// Defines

/// Indicator that a parameter is optional.
#define SCE_PSS_OPTIONAL
/// Indicator that a parameter is an input parameter.
#define SCE_PSS_IN
/// Indicator that a parameter is an output parameter.
#define SCE_PSS_OUT
/// Parameter is an input and output parameter.
#define SCE_PSS_INOUT

/// Make sure NULL is defined.
#ifndef NULL
#ifdef __cplusplus
#define NULL 0
#else
#define NULL ((void *)0)
#endif
#endif

/// Boolean TRUE define.
#define SCE_PSS_TRUE 1
/// Boolean FALSE define.
#define SCE_PSS_FALSE 0

/// Invalid handle.
#define SCE_PSS_INVALID_HANDLE_VALUE ((ScePssHandle)NULL)

/// Helper macro to limit x to a range.
#define SCE_PSS_LIMIT(x,min,max)	((x)<(min)?(min):((x)>(max)?(max):(x)))

/// Helper macro to test for largest value.
#define SCE_PSS_MAX(__l, __r) ((__l)>(__r)?(__l):(__r))

/// Helper macro to test for smallest value.
#define SCE_PSS_MIN(__l, __r) ((__l)<(__r)?(__l):(__r))

/// Helper macro to swap two bytes.
#define SCE_PSS_SWAB16(_x)	((ScePssUInt16)((((ScePssUInt16)(_x) & (ScePssUInt16)0x00ffU) << 8)		\
								| (((ScePssUInt16)(_x) & (ScePssUInt16)0xff00U) >> 8)))

/// Helper macro to swap four bytes.
#define SCE_PSS_SWAB32(_x)   ((ScePssUInt32)((((ScePssUInt32)(_x) & (ScePssUInt32)0x000000ffUL) << 24)	\
								| (((ScePssUInt32)(_x) & (ScePssUInt32)0x0000ff00UL) << 8)	\
								| (((ScePssUInt32)(_x) & (ScePssUInt32)0x00ff0000UL) >> 8)	\
								| (((ScePssUInt32)(_x) & (ScePssUInt32)0xff000000UL) >> 24)))

#ifndef SCE_PSS_ARRAY_SIZE
/*!
* \brief Helper macro to calculate the size of an array.
* \param obj Target array.
*/
#define SCE_PSS_ARRAY_SIZE(obj)		( sizeof( obj ) / sizeof( obj[ 0 ]))
#endif // SCE_PSS_ARRAY_SIZE

/*!
* \brief Helper macro to create string from object.
* \param x Target object.
*/
#define __STR2__(x) #x

/*!
* \brief Helper macro to create string from object.
* \param x Target object.
*/
#define __STR1__(x) __STR2__(x)

/*!
* \brief Helper macro to concatenate file name with line number.
*/
#define __LOC__ __FILE__ "("__STR1__(__LINE__)") : "


#ifndef SCE_PSS_SUCCEEDED
/// Macro to check whether a return value is a success code.
#define SCE_PSS_SUCCEEDED(__err)  ((ScePssResult)(__err) >= 0)
#endif

#ifndef SCE_PSS_FAILED
/// Macro to check whether a return value is a failure code.
#define SCE_PSS_FAILED(__err)  ((ScePssResult)(__err) < 0)
#endif

// ASSERT 
#ifndef SCE_PSS_ASSERT
#ifdef _DEBUG
#define SCE_PSS_ASSERT(cond)	scePssAssert(cond)
#else // _DEBUG
#define SCE_PSS_ASSERT(cond)
#endif // _DEBUG
#endif // SCE_PSS_ASSERT

#ifndef SCE_PSS_SPAM
#define SCE_PSS_SPAM(message, ...)	
#endif // SCE_PSS_SPAM

#ifndef SCE_PSS_DEBUG
#ifdef _DEBUG
#define SCE_PSS_DEBUG(message, ...)	scePssOutputDebugStringFormat(message, ##__VA_ARGS__)
#else // _DEBUG
#define SCE_PSS_DEBUG(message, ...)	
#endif // _DEBUG
#endif // SCE_PSS_DEBUG

#ifndef SCE_PSS_WARNING
#ifdef _DEBUG
#define SCE_PSS_WARNING(message, ...)	scePssOutputDebugStringFormat(message, ##__VA_ARGS__)
#else // _DEBUG
#define SCE_PSS_WARNING(message, ...)
#endif // _DEBUG
#endif // SCE_PSS_WARNING

#ifndef SCE_PSS_CRITICAL
#define SCE_PSS_CRITICAL(message, ...)	scePssOutputDebugStringFormat(message, ##__VA_ARGS__)
#endif // SCE_PSS_CRITICAL

/////////////////////////////////////////////////////////////////
// Error codes
typedef enum ScePssErrorCode
{
	SCE_PSS_ERROR_NO_ERROR = 0,
	SCE_PSS_ERROR_BASE_ERROR = 0x80010000,
	SCE_PSS_ERROR_ERROR = 0x80010001,
	SCE_PSS_ERROR_OUT_OF_MEMORY = 0x80010002,
	SCE_PSS_ERROR_INVALID_PARAMETER = 0x80010003,
	SCE_PSS_ERROR_PATH_NOT_FOUND = 0x80010004,
	SCE_PSS_ERROR_PATH_ALREADY_EXISTS = 0x80010005,
	
	SCE_PSS_ERROR_FILE_NOT_FOUND = 0x80010006,

	SCE_PSS_ERROR_TIMEOUT = 0x80010007,
	SCE_PSS_ERROR_OUT_OF_RANGE = 0x80010008,
	SCE_PSS_ERROR_END_OF_STREAM = 0x80010009,
	SCE_PSS_ERROR_BUFFER_FULL = 0x80010010,
	SCE_PSS_ERROR_INVALID_BUFFER = 0x80010011,
	SCE_PSS_ERROR_NOT_INITIALIZED = 0x80010012,
	SCE_PSS_ERROR_ALREADY_INITIALIZED = 0x80010013,
	SCE_PSS_ERROR_NOT_AVAILABLE = 0x80010014,
	SCE_PSS_ERROR_NOT_FOUND = 0x80010015,
	SCE_PSS_ERROR_PENDING = 0x80010016,
	SCE_PSS_ERROR_ALREADY_EXISTS = 0x80010017,
	SCE_PSS_ERROR_INVALID_HANDLE = 0x80010018,
	SCE_PSS_ERROR_OUT_OF_RESOURCES = 0x80010019,
	SCE_PSS_ERROR_ACCESS_DENIED = 0x80010020,
	SCE_PSS_ERROR_NOT_SUPPORTED = 0x80010021,
	SCE_PSS_ERROR_DISK_FULL = 0x80010022,
	SCE_PSS_ERROR_SEEK_FAILED = 0x80010023,
	SCE_PSS_ERROR_INVALID_DATA = 0x80010024,
	SCE_PSS_ERROR_INVALID_CALL = 0x80010025,
	SCE_PSS_ERROR_NOT_IMPLEMENTED = 0x80010026,
	SCE_PSS_ERROR_READ_FAILED = 0x80010027,
	SCE_PSS_ERROR_WRITE_FAILED = 0x80010028,
	SCE_PSS_ERROR_BUSY = 0x80010029
} ScePssErrorCode_t;

/// GUID type.
typedef struct ScePssGuid
{
	/// Data1.
    ScePssUInt32 uData1;
	/// Data2.
    ScePssUInt16  uData2;
	/// Data3.
    ScePssUInt16  uData3;
	/// Data4.
    ScePssUInt8   uData4[ 8 ];
} ScePssGuid_t;

/// Length of a GUID when converted to a string.
#define SCE_PSS_GUID_STRING_LENGTH		(36+1)	// +1 = \0

// Application entry point definition
PSS_PUBLIC_API ScePssInt32 scePssMain( ScePssInt32 argc, char *argv[] );

#endif // _PSS_DEF_H_
