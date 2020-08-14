//
// CompletionContainer.cs
//
// Authors:
//    Jérémie Laval <jeremie dot laval at xamarin dot com>
//
// Copyright 2011 Xamarin Inc (http://www.xamarin.com).
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

#if NET_4_0 || MOBILE

using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace System.Threading.Tasks
{
	internal struct CompletionContainer
	{
		Task single;
		ConcurrentQueue<Task> completed;

		public void Add (Task continuation)
		{
			if (single == null && Interlocked.CompareExchange (ref single, continuation, null) == null)
				return;
			if (completed == null)
				Interlocked.CompareExchange (ref completed, new ConcurrentQueue<Task> (), null);
			completed.Enqueue (continuation);
		}

		public bool HasElements {
			get {
				return single != null;
			}
		}

		public bool TryGetNextCompletion (out Task continuation)
		{
			continuation = null;

			if (single != null && (continuation = Interlocked.Exchange (ref single, null)) != null)
				return true;

			return completed != null && completed.TryDequeue (out continuation);
		}
	}
}

#endif
