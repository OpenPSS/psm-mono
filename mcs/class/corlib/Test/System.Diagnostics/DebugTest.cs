//
// MonoTests.System.Diagnostics.DebugTest.cs
//
// Author:
//	John R. Hicks (angryjohn69@nc.rr.com)
//
// (C) 2002
using System;
using System.Diagnostics;
using NUnit.Framework;

namespace MonoTests.System.Diagnostics
{
		[TestFixture]
		public class DebugTest2
		{
			[SetUp]
			protected void SetUp()
			{
#if !NET_2_1
				Debug.Listeners.Add(new TextWriterTraceListener(Console.Error));
#endif
			}
			
			[TearDown]
			protected void TearDown()
			{
				
			}

			[Test]
			public void TestAssert()
			{
				Debug.Assert(false, "Testing Assertions");
			}

#if !NET_2_1
			[Test]			
			public void TestFail ()
			{
				Debug.Fail("Testing Fail method");
			}
#endif

#if !NET_2_1
			[Test]			
			public void TestWrite()
			{
				Debug.Write("Testing Write", "Testing the output of the Write method");
			}
#endif

#if !NET_2_1
			[Test]			
			public void TestWriteIf()
			{
				Debug.WriteIf(true, "Testing WriteIf");
				Debug.WriteIf(false, "Testing WriteIf", "Passed false");
			}
#endif

			[Test]			
			public void TestWriteLine()
			{
				Debug.WriteLine("Testing WriteLine method");
			}

#if !NET_2_1
			[Test]			
			public void TestWriteLineIf()
			{
				Debug.WriteLineIf(true, "Testing WriteLineIf");
				Debug.WriteLineIf(false, "Testing WriteLineIf", "Passed false");
			}
#endif
		}
}
