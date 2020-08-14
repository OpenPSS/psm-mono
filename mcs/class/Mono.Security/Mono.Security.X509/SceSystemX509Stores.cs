//
// X509Stores.cs: Handles X.509 certificates/CRLs stores group.
//
// Author:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2004 Novell (http://www.novell.com)
//

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
using System.IO;

using Mono.Security.X509.Extensions;

namespace Mono.Security.X509 {

	internal class SceSystemX509Stores : X509Stores {

		internal SceSystemX509Stores() : base ("")
		{
		}

		// properties

		public override X509Store Personal {
			get { 
				if (_personal == null) {
					_personal = new SceSystemX509Store (empty: true);
				}
				return _personal; 
			}
		}

		public override X509Store OtherPeople {
			get { 
				if (_other == null) {
					_other = new SceSystemX509Store (empty: true);
				}
				return _other; 
			}
		}

		public override X509Store IntermediateCA {
			get { 
				if (_intermediate == null) {
					_intermediate = new SceSystemX509Store (empty: true);
				}
				return _intermediate; 
			}
		}

		public override X509Store TrustedRoot {
			get { 
				if (_trusted == null) {
					_trusted = new SceSystemX509Store (empty: false); // empty = false means this store will load the system certs
				}
				return _trusted; 
			}
		}

		public override X509Store Untrusted {
			get { 
				if (_untrusted == null) {
					_untrusted = new SceSystemX509Store (empty: true);
				}
				return _untrusted; 
			}
		}

		// the code that consumes X509Stores doesn't seem to use the above properties, but instead calls Open.  sigh.
                public override X509Store Open (string storeName, bool create)
                {
                        if (storeName == null)
                                throw new ArgumentNullException ("storeName");

			if (storeName == Names.TrustedRoot)
				return new SceSystemX509Store (empty: false); // empty == false means this store will load the system certs
			return new SceSystemX509Store (empty: true);
                }

	}
}
