//
// AssemblyInfo.cs
//
// Author:
//   Joel W. Reed (joelwreed@gmail.com)
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
using System.Reflection;
using System.Resources;
using System.Security;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about the System.ServiceModel.Discovery assembly

[assembly: AssemblyVersion (Consts.FxVersion)]
[assembly: SatelliteContractVersion (Consts.FxVersion)]

[assembly: AssemblyTitle ("System.ServiceModel.Discovery.dll")]
[assembly: AssemblyDescription ("System.ServiceModel.Discovery.dll")]
[assembly: AssemblyConfiguration ("Development version")]
[assembly: AssemblyCompany ("MONO development team")]
[assembly: AssemblyProduct ("MONO CLI")]
[assembly: AssemblyCopyright ("(c) 2003 Various Authors")]

[assembly: CLSCompliant (true)]
[assembly: AssemblyDefaultAlias ("System.ServiceModel.Discovery.dll")]
[assembly: AssemblyFileVersion (Consts.FxFileVersion)]
[assembly: AssemblyInformationalVersion (Consts.FxFileVersion)]
[assembly: NeutralResourcesLanguage ("en-US")]

[assembly: ComVisible (false)]

[assembly: AssemblyDelaySign (true)]
#if NET_2_1
[assembly: AssemblyKeyFile ("../psm.pub")]
#else
[assembly: AssemblyKeyFile("../winfx.pub")]
#endif
