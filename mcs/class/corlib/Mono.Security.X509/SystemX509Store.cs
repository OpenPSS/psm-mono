//
// X509Store.cs: Handles a X.509 certificates/CRLs store
//
// Author:
//	Sebastien Pouliot  <sebastien@ximian.com>
//	Pablo Ruiz <pruiz@netway.org>
//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
// (C) 2010 Pablo Ruiz.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

using Mono.Security.Cryptography;
using Mono.Security.X509.Extensions;

namespace Mono.Security.X509 {

	internal class SystemX509Store : X509Store {

		bool empty;

		internal SystemX509Store (bool empty) : base (X509StoreManager.LocalMachinePath, false)
		{
			this.empty = empty;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct MonoCert {
			public IntPtr cert; // a char*
			public IntPtr next; // a MonoCert*
		};

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		private unsafe extern static IntPtr LoadSystemCerts(out MonoCert* pemList);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		private extern static void FreeSystemCerts(IntPtr certPool);

		protected override X509CertificateCollection BuildCertificatesCollection (string storeName) 
		{
			X509CertificateCollection coll = new X509CertificateCollection ();
			if (empty)
				return coll;
			unsafe {
				// we ignore the storeName and load certs using our internal libssl apis
				MonoCert* pem;
				IntPtr certPool = LoadSystemCerts(out pem);

				while (pem != null) {
					string cert_string = Marshal.PtrToStringAuto(pem->cert);
					byte[] cert_data = Encoding.ASCII.GetBytes(cert_string);
					try {
						var cert = CertificateFromData (cert_data);
						coll.Add (cert);
					}
					catch (Exception e) {
						Console.Error.WriteLine (e);
					}
					pem = (MonoCert*)pem->next;
				}
				FreeSystemCerts(certPool);
			}
			return coll;
		}
	}
}
