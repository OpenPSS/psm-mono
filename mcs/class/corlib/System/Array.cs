//
// System.Array.cs
//
// Authors:
//   Joe Shaw (joe@ximian.com)
//   Martin Baulig (martin@gnome.org)
//   Dietmar Maurer (dietmar@ximian.com)
//   Gonzalo Paniagua Javier (gonzalo@ximian.com)
//   Jeffrey Stedfast (fejj@novell.com)
//
// (C) 2001-2003 Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004-2011 Novell, Inc (http://www.novell.com)
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

using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.ConstrainedExecution;
using System.Reflection.Emit;

namespace System
{
	[Serializable]
	[ComVisible (true)]
	// FIXME: We are doing way to many double/triple exception checks for the overloaded functions"
	public abstract class Array : ICloneable, ICollection, IList, IEnumerable
#if NET_4_0 || MOONLIGHT || MOBILE
		, IStructuralComparable, IStructuralEquatable
#endif
	{
		// Constructor
		private Array ()
		{
		}

		/*
		 * These methods are used to implement the implicit generic interfaces 
		 * implemented by arrays in NET 2.0.
		 * Only make those methods generic which really need it, to avoid
		 * creating useless instantiations.
		 */
		internal int InternalArray__ICollection_get_Count ()
		{
			return Length;
		}

		internal bool InternalArray__ICollection_get_IsReadOnly ()
		{
			return true;
		}

		internal IEnumerator<T> InternalArray__IEnumerable_GetEnumerator<T> ()
		{
			return new InternalEnumerator<T> (this);
		}

		internal void InternalArray__ICollection_Clear ()
		{
			throw new NotSupportedException ("Collection is read-only");
		}

		internal void InternalArray__ICollection_Add<T> (T item)
		{
			throw new NotSupportedException ("Collection is of a fixed size");
		}

		internal bool InternalArray__ICollection_Remove<T> (T item)
		{
			throw new NotSupportedException ("Collection is of a fixed size");
		}

		internal bool InternalArray__ICollection_Contains<T> (T item)
		{
			if (this.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));

			int length = this.Length;
			for (int i = 0; i < length; i++) {
				T value;
				GetGenericValueImpl (i, out value);
				if (item == null){
					if (value == null)
						return true;
					else
						return false;
				}
				
				if (item.Equals (value))
					return true;
			}

			return false;
		}

		internal void InternalArray__ICollection_CopyTo<T> (T[] array, int index)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			// The order of these exception checks may look strange,
			// but that's how the microsoft runtime does it.
			if (this.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));
			if (index + this.GetLength (0) > array.GetLowerBound (0) + array.GetLength (0))
				throw new ArgumentException ("Destination array was not long " +
					"enough. Check destIndex and length, and the array's " +
					"lower bounds.");
			if (array.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));
			if (index < 0)
				throw new ArgumentOutOfRangeException (
					"index", Locale.GetText ("Value has to be >= 0."));

			Copy (this, this.GetLowerBound (0), array, index, this.GetLength (0));
		}

		internal void InternalArray__Insert<T> (int index, T item)
		{
			throw new NotSupportedException ("Collection is of a fixed size");
		}

		internal void InternalArray__RemoveAt (int index)
		{
			throw new NotSupportedException ("Collection is of a fixed size");
		}

		internal int InternalArray__IndexOf<T> (T item)
		{
			if (this.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));

			int length = this.Length;
			for (int i = 0; i < length; i++) {
				T value;
				GetGenericValueImpl (i, out value);
				if (item == null){
					if (value == null)
						return i + this.GetLowerBound (0);
					else {
						unchecked {
							return this.GetLowerBound (0) - 1;
						}
					}
				}
				if (value.Equals (item))
					// array index may not be zero-based.
					// use lower bound
					return i + this.GetLowerBound (0);
			}

			unchecked {
				// lower bound may be MinValue
				return this.GetLowerBound (0) - 1;
			}
		}

		internal T InternalArray__get_Item<T> (int index)
		{
			if (unchecked ((uint) index) >= unchecked ((uint) Length))
				throw new ArgumentOutOfRangeException ("index");

			T value;
			GetGenericValueImpl (index, out value);
			return value;
		}

		internal void InternalArray__set_Item<T> (int index, T item)
		{
			if (unchecked ((uint) index) >= unchecked ((uint) Length))
				throw new ArgumentOutOfRangeException ("index");

			object[] oarray = this as object [];
			if (oarray != null) {
				oarray [index] = (object)item;
				return;
			}
			SetGenericValueImpl (index, ref item);
		}

		// CAUTION! No bounds checking!
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern void GetGenericValueImpl<T> (int pos, out T value);

		// CAUTION! No bounds checking!
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern void SetGenericValueImpl<T> (int pos, ref T value);

		internal struct InternalEnumerator<T> : IEnumerator<T>
		{
			const int NOT_STARTED = -2;
			
			// this MUST be -1, because we depend on it in move next.
			// we just decr the size, so, 0 - 1 == FINISHED
			const int FINISHED = -1;
			
			Array array;
			int idx;

			internal InternalEnumerator (Array array)
			{
				this.array = array;
				idx = NOT_STARTED;
			}

			public void Dispose ()
			{
				idx = NOT_STARTED;
			}

			public bool MoveNext ()
			{
				if (idx == NOT_STARTED)
					idx = array.Length;

				return idx != FINISHED && -- idx != FINISHED;
			}

			public T Current {
				get {
					if (idx == NOT_STARTED)
						throw new InvalidOperationException ("Enumeration has not started. Call MoveNext");
					if (idx == FINISHED)
						throw new InvalidOperationException ("Enumeration already finished");

					return array.InternalArray__get_Item<T> (array.Length - 1 - idx);
				}
			}

			void IEnumerator.Reset ()
			{
				idx = NOT_STARTED;
			}

			object IEnumerator.Current {
				get {
					return Current;
				}
			}
		}

		// Properties
		public int Length {
			[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.Success)]
			get {
				int length = this.GetLength (0);

				for (int i = 1; i < this.Rank; i++) {
					length *= this.GetLength (i); 
				}
				return length;
			}
		}

		[ComVisible (false)]
		public long LongLength {
			[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.Success)]
			get { return Length; }
		}

		public int Rank {
			[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.Success)]
			get {
				return this.GetRank ();
			}
		}

		// IList interface
		object IList.this [int index] {
			get {
				if (unchecked ((uint) index) >= unchecked ((uint) Length))
					throw new IndexOutOfRangeException ("index");
				if (this.Rank > 1)
					throw new ArgumentException (Locale.GetText ("Only single dimension arrays are supported."));
				return GetValueImpl (index);
			} 
			set {
				if (unchecked ((uint) index) >= unchecked ((uint) Length))
					throw new IndexOutOfRangeException ("index");
				if (this.Rank > 1)
					throw new ArgumentException (Locale.GetText ("Only single dimension arrays are supported."));
				SetValueImpl (value, index);
			}
		}

		int IList.Add (object value)
		{
			throw new NotSupportedException ();
		}

		void IList.Clear ()
		{
			Array.Clear (this, this.GetLowerBound (0), this.Length);
		}

		bool IList.Contains (object value)
		{
			if (this.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));

			int length = this.Length;
			for (int i = 0; i < length; i++) {
				if (Object.Equals (this.GetValueImpl (i), value))
					return true;
			}
			return false;
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		int IList.IndexOf (object value)
		{
			if (this.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));

			int length = this.Length;
			for (int i = 0; i < length; i++) {
				if (Object.Equals (this.GetValueImpl (i), value))
					// array index may not be zero-based.
					// use lower bound
					return i + this.GetLowerBound (0);
			}

			unchecked {
				// lower bound may be MinValue
				return this.GetLowerBound (0) - 1;
			}
		}

		void IList.Insert (int index, object value)
		{
			throw new NotSupportedException ();
		}

		void IList.Remove (object value)
		{
			throw new NotSupportedException ();
		}

		void IList.RemoveAt (int index)
		{
			throw new NotSupportedException ();
		}

		// InternalCall Methods
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern int GetRank ();

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern int GetLength (int dimension);

		[ComVisible (false)]
		public long GetLongLength (int dimension)
		{
			return GetLength (dimension);
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.Success)]
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern int GetLowerBound (int dimension);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern object GetValue (params int[] indices);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern void SetValue (object value, params int[] indices);

		// CAUTION! No bounds checking!
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern object GetValueImpl (int pos);

		// CAUTION! No bounds checking!
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern void SetValueImpl (object value, int pos);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static bool FastCopy (Array source, int source_idx, Array dest, int dest_idx, int length);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static Array CreateInstanceImpl (Type elementType, int[] lengths, int[] bounds);

		// Properties
		int ICollection.Count {
			get {
				return Length;
			}
		}

		public bool IsSynchronized {
			get {
				return false;
			}
		}

		public object SyncRoot {
			get {
				return this;
			}
		}

		public bool IsFixedSize {
			get {
				return true;
			}
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}

		public IEnumerator GetEnumerator ()
		{
			return new SimpleEnumerator (this);
		}

#if NET_4_0 || MOONLIGHT || MOBILE
		int IStructuralComparable.CompareTo (object other, IComparer comparer)
		{
			if (other == null)
				return 1;

			Array arr = other as Array;
			if (arr == null)
				throw new ArgumentException ("Not an array", "other");

			int len = GetLength (0);
			if (len != arr.GetLength (0))
				throw new ArgumentException ("Not of the same length", "other");

			if (Rank > 1)
				throw new ArgumentException ("Array must be single dimensional");

			if (arr.Rank > 1)
				throw new ArgumentException ("Array must be single dimensional", "other");

			for (int i = 0; i < len; ++i) {
				object a = GetValue (i);
				object b = arr.GetValue (i);
				int r = comparer.Compare (a, b);
				if (r != 0)
					return r;
			}
			return 0;
		}

		bool IStructuralEquatable.Equals (object other, IEqualityComparer comparer)
		{
			Array o = other as Array;
			if (o == null || o.Length != Length)
				return false;

			if (Object.ReferenceEquals (other, this))
				return true;

			for (int i = 0; i < Length; i++) {
				object this_item = this.GetValue (i);
				object other_item = o.GetValue (i);
				if (!comparer.Equals (this_item, other_item))
					return false;
			}
			return true;
		}

 
		int IStructuralEquatable.GetHashCode (IEqualityComparer comparer)
		{
			if (comparer == null)
				throw new ArgumentNullException ("comparer");

			int hash = 0;
			for (int i = 0; i < Length; i++)
				hash = ((hash << 7) + hash) ^ GetValue (i).GetHashCode ();
			return hash;
		}
#endif

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.Success)]
		public int GetUpperBound (int dimension)
		{
			return GetLowerBound (dimension) + GetLength (dimension) - 1;
		}

		public object GetValue (int index)
		{
			if (Rank != 1)
				throw new ArgumentException (Locale.GetText ("Array was not a one-dimensional array."));
			if (index < GetLowerBound (0) || index > GetUpperBound (0))
				throw new IndexOutOfRangeException (Locale.GetText (
					"Index has to be between upper and lower bound of the array."));

			return GetValueImpl (index - GetLowerBound (0));
		}

		public object GetValue (int index1, int index2)
		{
			int[] ind = {index1, index2};
			return GetValue (ind);
		}

		public object GetValue (int index1, int index2, int index3)
		{
			int[] ind = {index1, index2, index3};
			return GetValue (ind);
		}

		[ComVisible (false)]
		public object GetValue (long index)
		{
			if (index < 0 || index > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("index", Locale.GetText (
					"Value must be >= 0 and <= Int32.MaxValue."));

			return GetValue ((int) index);
		}

		[ComVisible (false)]
		public object GetValue (long index1, long index2)
		{
			if (index1 < 0 || index1 > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("index1", Locale.GetText (
					"Value must be >= 0 and <= Int32.MaxValue."));

			if (index2 < 0 || index2 > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("index2", Locale.GetText (
					"Value must be >= 0 and <= Int32.MaxValue."));

			return GetValue ((int) index1, (int) index2);
		}

		[ComVisible (false)]
		public object GetValue (long index1, long index2, long index3)
		{
			if (index1 < 0 || index1 > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("index1", Locale.GetText (
					"Value must be >= 0 and <= Int32.MaxValue."));

			if (index2 < 0 || index2 > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("index2", Locale.GetText (
					"Value must be >= 0 and <= Int32.MaxValue."));

			if (index3 < 0 || index3 > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("index3", Locale.GetText (
					"Value must be >= 0 and <= Int32.MaxValue."));

			return GetValue ((int) index1, (int) index2, (int) index3);
		}

		[ComVisible (false)]
		public void SetValue (object value, long index)
		{
			if (index < 0 || index > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("index", Locale.GetText (
					"Value must be >= 0 and <= Int32.MaxValue."));

			SetValue (value, (int) index);
		}

		[ComVisible (false)]
		public void SetValue (object value, long index1, long index2)
		{
			if (index1 < 0 || index1 > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("index1", Locale.GetText (
					"Value must be >= 0 and <= Int32.MaxValue."));

			if (index2 < 0 || index2 > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("index2", Locale.GetText (
					"Value must be >= 0 and <= Int32.MaxValue."));

			int[] ind = {(int) index1, (int) index2};
			SetValue (value, ind);
		}

		[ComVisible (false)]
		public void SetValue (object value, long index1, long index2, long index3)
		{
			if (index1 < 0 || index1 > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("index1", Locale.GetText (
					"Value must be >= 0 and <= Int32.MaxValue."));

			if (index2 < 0 || index2 > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("index2", Locale.GetText (
					"Value must be >= 0 and <= Int32.MaxValue."));

			if (index3 < 0 || index3 > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("index3", Locale.GetText (
					"Value must be >= 0 and <= Int32.MaxValue."));

			int[] ind = {(int) index1, (int) index2, (int) index3};
			SetValue (value, ind);
		}

		public void SetValue (object value, int index)
		{
			if (Rank != 1)
				throw new ArgumentException (Locale.GetText ("Array was not a one-dimensional array."));
			if (index < GetLowerBound (0) || index > GetUpperBound (0))
				throw new IndexOutOfRangeException (Locale.GetText (
					"Index has to be >= lower bound and <= upper bound of the array."));

			SetValueImpl (value, index - GetLowerBound (0));
		}

		public void SetValue (object value, int index1, int index2)
		{
			int[] ind = {index1, index2};
			SetValue (value, ind);
		}

		public void SetValue (object value, int index1, int index2, int index3)
		{
			int[] ind = {index1, index2, index3};
			SetValue (value, ind);
		}

		public static Array CreateInstance (Type elementType, int length)
		{
			int[] lengths = {length};

			return CreateInstance (elementType, lengths);
		}

		public static Array CreateInstance (Type elementType, int length1, int length2)
		{
			int[] lengths = {length1, length2};

			return CreateInstance (elementType, lengths);
		}

		public static Array CreateInstance (Type elementType, int length1, int length2, int length3)
		{
			int[] lengths = {length1, length2, length3};

			return CreateInstance (elementType, lengths);
		}

		public static Array CreateInstance (Type elementType, params int[] lengths)
		{
			if (elementType == null)
				throw new ArgumentNullException ("elementType");
			if (lengths == null)
				throw new ArgumentNullException ("lengths");

			if (lengths.Length > 255)
				throw new TypeLoadException ();

			int[] bounds = null;

			elementType = elementType.UnderlyingSystemType;
			if (!elementType.IsSystemType)
				throw new ArgumentException ("Type must be a type provided by the runtime.", "elementType");
			if (elementType.Equals (typeof (void)))
				throw new NotSupportedException ("Array type can not be void");
			if (elementType.ContainsGenericParameters)
				throw new NotSupportedException ("Array type can not be an open generic type");
			if ((elementType is TypeBuilder) && !(elementType as TypeBuilder).IsCreated ())
				throw new NotSupportedException ("Can't create an array of the unfinished type '" + elementType + "'.");
			
			return CreateInstanceImpl (elementType, lengths, bounds);
		}

		public static Array CreateInstance (Type elementType, int[] lengths, int [] lowerBounds)
		{
			if (elementType == null)
				throw new ArgumentNullException ("elementType");
			if (lengths == null)
				throw new ArgumentNullException ("lengths");
			if (lowerBounds == null)
				throw new ArgumentNullException ("lowerBounds");

			elementType = elementType.UnderlyingSystemType;
			if (!elementType.IsSystemType)
				throw new ArgumentException ("Type must be a type provided by the runtime.", "elementType");
			if (elementType.Equals (typeof (void)))
				throw new NotSupportedException ("Array type can not be void");
			if (elementType.ContainsGenericParameters)
				throw new NotSupportedException ("Array type can not be an open generic type");

			if (lengths.Length < 1)
				throw new ArgumentException (Locale.GetText ("Arrays must contain >= 1 elements."));

			if (lengths.Length != lowerBounds.Length)
				throw new ArgumentException (Locale.GetText ("Arrays must be of same size."));

			for (int j = 0; j < lowerBounds.Length; j ++) {
				if (lengths [j] < 0)
					throw new ArgumentOutOfRangeException ("lengths", Locale.GetText (
						"Each value has to be >= 0."));
				if ((long)lowerBounds [j] + (long)lengths [j] > (long)Int32.MaxValue)
					throw new ArgumentOutOfRangeException ("lengths", Locale.GetText (
						"Length + bound must not exceed Int32.MaxValue."));
			}

			if (lengths.Length > 255)
				throw new TypeLoadException ();

			return CreateInstanceImpl (elementType, lengths, lowerBounds);
		}

		static int [] GetIntArray (long [] values)
		{
			int len = values.Length;
			int [] ints = new int [len];
			for (int i = 0; i < len; i++) {
				long current = values [i];
				if (current < 0 || current > (long) Int32.MaxValue)
					throw new ArgumentOutOfRangeException ("values", Locale.GetText (
						"Each value has to be >= 0 and <= Int32.MaxValue."));

				ints [i] = (int) current;
			}
			return ints;
		}

		public static Array CreateInstance (Type elementType, params long [] lengths)
		{
			if (lengths == null)
				throw new ArgumentNullException ("lengths");
			return CreateInstance (elementType, GetIntArray (lengths));
		}

		[ComVisible (false)]
		public object GetValue (params long [] indices)
		{
			if (indices == null)
				throw new ArgumentNullException ("indices");
			return GetValue (GetIntArray (indices));
		}

		[ComVisible (false)]
		public void SetValue (object value, params long [] indices)
		{
			if (indices == null)
				throw new ArgumentNullException ("indices");
			SetValue (value, GetIntArray (indices));
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int BinarySearch (Array array, object value)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (value == null)
				return -1;

			if (array.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));

			if (array.Length == 0)
				return -1;

			if (!(value is IComparable))
				throw new ArgumentException (Locale.GetText ("value does not support IComparable."));

			return DoBinarySearch (array, array.GetLowerBound (0), array.GetLength (0), value, null);
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int BinarySearch (Array array, object value, IComparer comparer)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (array.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));

			if (array.Length == 0)
				return -1;

			if ((comparer == null) && (value != null) && !(value is IComparable))
				throw new ArgumentException (Locale.GetText (
					"comparer is null and value does not support IComparable."));

			return DoBinarySearch (array, array.GetLowerBound (0), array.GetLength (0), value, comparer);
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int BinarySearch (Array array, int index, int length, object value)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (array.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));

			if (index < array.GetLowerBound (0))
				throw new ArgumentOutOfRangeException ("index", Locale.GetText (
					"index is less than the lower bound of array."));
			if (length < 0)
				throw new ArgumentOutOfRangeException ("length", Locale.GetText (
					"Value has to be >= 0."));
			// re-ordered to avoid possible integer overflow
			if (index > array.GetLowerBound (0) + array.GetLength (0) - length)
				throw new ArgumentException (Locale.GetText (
					"index and length do not specify a valid range in array."));

			if (array.Length == 0)
				return -1;

			if ((value != null) && (!(value is IComparable)))
				throw new ArgumentException (Locale.GetText (
					"value does not support IComparable"));

			return DoBinarySearch (array, index, length, value, null);
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int BinarySearch (Array array, int index, int length, object value, IComparer comparer)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (array.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));

			if (index < array.GetLowerBound (0))
				throw new ArgumentOutOfRangeException ("index", Locale.GetText (
					"index is less than the lower bound of array."));
			if (length < 0)
				throw new ArgumentOutOfRangeException ("length", Locale.GetText (
					"Value has to be >= 0."));
			// re-ordered to avoid possible integer overflow
			if (index > array.GetLowerBound (0) + array.GetLength (0) - length)
				throw new ArgumentException (Locale.GetText (
					"index and length do not specify a valid range in array."));

			if (array.Length == 0)
				return -1;

			if ((comparer == null) && (value != null) && !(value is IComparable))
				throw new ArgumentException (Locale.GetText (
					"comparer is null and value does not support IComparable."));

			return DoBinarySearch (array, index, length, value, comparer);
		}

		static int DoBinarySearch (Array array, int index, int length, object value, IComparer comparer)
		{
			// cache this in case we need it
			if (comparer == null)
				comparer = Comparer.Default;

			int iMin = index;
			// Comment from Tum (tum@veridicus.com):
			// *Must* start at index + length - 1 to pass rotor test co2460binarysearch_iioi
			int iMax = index + length - 1;
			int iCmp = 0;
			try {
				while (iMin <= iMax) {
					// Be careful with overflow
					// http://googleresearch.blogspot.com/2006/06/extra-extra-read-all-about-it-nearly.html
					int iMid = iMin + ((iMax - iMin) / 2);
					object elt = array.GetValueImpl (iMid);

					iCmp = comparer.Compare (elt, value);

					if (iCmp == 0)
						return iMid;
					else if (iCmp > 0)
						iMax = iMid - 1;
					else
						iMin = iMid + 1; // compensate for the rounding down
				}
			}
			catch (Exception e) {
				throw new InvalidOperationException (Locale.GetText ("Comparer threw an exception."), e);
			}

			return ~iMin;
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.Success)]
		public static void Clear (Array array, int index, int length)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			if (length < 0)
				throw new IndexOutOfRangeException ("length < 0");

			int low = array.GetLowerBound (0);
			if (index < low)
				throw new IndexOutOfRangeException ("index < lower bound");
			index = index - low;

			// re-ordered to avoid possible integer overflow
			if (index > array.Length - length)
				throw new IndexOutOfRangeException ("index + length > size");

			ClearInternal (array, index, length);
		}
		
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void ClearInternal (Array a, int index, int count);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern object Clone ();

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Copy (Array sourceArray, Array destinationArray, int length)
		{
			// need these checks here because we are going to use
			// GetLowerBound() on source and dest.
			if (sourceArray == null)
				throw new ArgumentNullException ("sourceArray");

			if (destinationArray == null)
				throw new ArgumentNullException ("destinationArray");

			Copy (sourceArray, sourceArray.GetLowerBound (0), destinationArray,
				destinationArray.GetLowerBound (0), length);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Copy (Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
		{
			if (sourceArray == null)
				throw new ArgumentNullException ("sourceArray");

			if (destinationArray == null)
				throw new ArgumentNullException ("destinationArray");

			if (length < 0)
				throw new ArgumentOutOfRangeException ("length", Locale.GetText (
					"Value has to be >= 0."));;

			if (sourceIndex < sourceArray.GetLowerBound (0))
				throw new ArgumentOutOfRangeException ("sourceIndex", Locale.GetText (
					"Number was less than the array's lower bound in the first dimension."));

			if (destinationIndex < destinationArray.GetLowerBound (0))
				throw new ArgumentOutOfRangeException ("destinationIndex", Locale.GetText (
                    "Number was less than the array's lower bound in the first dimension."));

			if (FastCopy (sourceArray, sourceIndex, destinationArray, destinationIndex, length))
				return;

			int source_pos = sourceIndex - sourceArray.GetLowerBound (0);
			int dest_pos = destinationIndex - destinationArray.GetLowerBound (0);

			// re-ordered to avoid possible integer overflow
            if (source_pos > sourceArray.Length - length) {
                string msg = "Source array was not long enough. Check " +
                    "sourceIndex and length, and the array's lower bounds";
                throw new ArgumentException(msg, string.Empty);
            }

			if (dest_pos > destinationArray.Length - length) {
				string msg = "Destination array was not long enough. Check " +
					"destinationIndex and length, and the array's lower bounds";
				throw new ArgumentException (msg, string.Empty);
			}

			if (sourceArray.Rank != destinationArray.Rank)
				throw new RankException (Locale.GetText ("Arrays must be of the same number of dimensions."));

			Type src_type = sourceArray.GetType ().GetElementType ();
			Type dst_type = destinationArray.GetType ().GetElementType ();

			if (!Object.ReferenceEquals (sourceArray, destinationArray) || source_pos > dest_pos) {
				for (int i = 0; i < length; i++) {
					Object srcval = sourceArray.GetValueImpl (source_pos + i);

					try {
						destinationArray.SetValueImpl (srcval, dest_pos + i);
					} catch {
						if (src_type.Equals (typeof (Object)))
							throw new InvalidCastException ();
						else
							throw new ArrayTypeMismatchException (String.Format (Locale.GetText (
								"(Types: source={0};  target={1})"), src_type.FullName, dst_type.FullName));
					}
				}
			}
			else {
				for (int i = length - 1; i >= 0; i--) {
					Object srcval = sourceArray.GetValueImpl (source_pos + i);

					try {
						destinationArray.SetValueImpl (srcval, dest_pos + i);
					} catch {
						if (src_type.Equals (typeof (Object)))
							throw new InvalidCastException ();
						else
							throw new ArrayTypeMismatchException (String.Format (Locale.GetText (
								"(Types: source={0};  target={1})"), src_type.FullName, dst_type.FullName));
					}
				}
			}
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Copy (Array sourceArray, long sourceIndex, Array destinationArray,
		                         long destinationIndex, long length)
		{
			if (sourceArray == null)
				throw new ArgumentNullException ("sourceArray");

			if (destinationArray == null)
				throw new ArgumentNullException ("destinationArray");

			if (sourceIndex < Int32.MinValue || sourceIndex > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("sourceIndex",
					Locale.GetText ("Must be in the Int32 range."));

			if (destinationIndex < Int32.MinValue || destinationIndex > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("destinationIndex",
					Locale.GetText ("Must be in the Int32 range."));

			if (length < 0 || length > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("length", Locale.GetText (
					"Value must be >= 0 and <= Int32.MaxValue."));

			Copy (sourceArray, (int) sourceIndex, destinationArray, (int) destinationIndex, (int) length);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Copy (Array sourceArray, Array destinationArray, long length)
		{
			if (length < 0 || length > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("length", Locale.GetText (
					"Value must be >= 0 and <= Int32.MaxValue."));

			Copy (sourceArray, destinationArray, (int) length);
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int IndexOf (Array array, object value)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
	
			return IndexOf (array, value, 0, array.Length);
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int IndexOf (Array array, object value, int startIndex)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			return IndexOf (array, value, startIndex, array.Length - startIndex);
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int IndexOf (Array array, object value, int startIndex, int count)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (array.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));

			// re-ordered to avoid possible integer overflow
			if (count < 0 || startIndex < array.GetLowerBound (0) || startIndex - 1 > array.GetUpperBound (0) - count)
				throw new ArgumentOutOfRangeException ();

			int max = startIndex + count;
			for (int i = startIndex; i < max; i++) {
				if (Object.Equals (array.GetValueImpl (i), value))
					return i;
			}

			return array.GetLowerBound (0) - 1;
		}

		public void Initialize()
		{
			//FIXME: We would like to find a compiler that uses
			// this method. It looks like this method do nothing
			// in C# so no exception is trown by the moment.
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int LastIndexOf (Array array, object value)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (array.Length == 0)
				return array.GetLowerBound (0) - 1;
			return LastIndexOf (array, value, array.Length - 1);
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int LastIndexOf (Array array, object value, int startIndex)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			return LastIndexOf (array, value, startIndex, startIndex - array.GetLowerBound (0) + 1);
		}
		
		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int LastIndexOf (Array array, object value, int startIndex, int count)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
	
			if (array.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));

			int lb = array.GetLowerBound (0);
			// Empty arrays do not throw ArgumentOutOfRangeException
			if (array.Length == 0)
				return lb - 1;

			if (count < 0 || startIndex < lb ||
				startIndex > array.GetUpperBound (0) ||	startIndex - count + 1 < lb)
				throw new ArgumentOutOfRangeException ();

			for (int i = startIndex; i >= startIndex - count + 1; i--) {
				if (Object.Equals (array.GetValueImpl (i), value))
					return i;
			}

			return lb - 1;
		}

		/* delegate used to swap array elements */
		delegate void Swapper (int i, int j);

		static Swapper get_swapper (Array array)
		{
			if (array is int[])
				return new Swapper (array.int_swapper);
			if (array is double[])
				return new Swapper (array.double_swapper);
			if (array is object[]) {
				return new Swapper (array.obj_swapper);
			}
			return new Swapper (array.slow_swapper);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Reverse (Array array)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			Reverse (array, array.GetLowerBound (0), array.GetLength (0));
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Reverse (Array array, int index, int length)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (array.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));

			if (index < array.GetLowerBound (0) || length < 0)
				throw new ArgumentOutOfRangeException ();

			// re-ordered to avoid possible integer overflow
			if (index > array.GetUpperBound (0) + 1 - length)
				throw new ArgumentException ();

			int end = index + length - 1;
			object[] oarray = array as object[];
			if (oarray != null) {
				while (index < end) {
					object tmp = oarray [index];
					oarray [index] = oarray [end];
					oarray [end] = tmp;
					++index;
					--end;
				}
				return;
			}
			int[] iarray = array as int[];
			if (iarray != null) {
				while (index < end) {
					int tmp = iarray [index];
					iarray [index] = iarray [end];
					iarray [end] = tmp;
					++index;
					--end;
				}
				return;
			}
			double[] darray = array as double[];
			if (darray != null) {
				while (index < end) {
					double tmp = darray [index];
					darray [index] = darray [end];
					darray [end] = tmp;
					++index;
					--end;
				}
				return;
			}
			// fallback
			Swapper swapper = get_swapper (array);
			while (index < end) {
				swapper (index, end);
				++index;
				--end;
			}
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort (Array array)
		{
			Sort (array, (IComparer)null);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort (Array keys, Array items)
		{
			Sort (keys, items, (IComparer)null);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort (Array array, IComparer comparer)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (array.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));

			SortImpl (array, null, array.GetLowerBound (0), array.GetLength (0), comparer);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort (Array array, int index, int length)
		{
			Sort (array, index, length, (IComparer)null);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort (Array keys, Array items, IComparer comparer)
		{
			if (items == null) {
				Sort (keys, comparer);
				return;
			}		
		
			if (keys == null)
				throw new ArgumentNullException ("keys");

			if (keys.Rank > 1 || items.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));

			SortImpl (keys, items, keys.GetLowerBound (0), keys.GetLength (0), comparer);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort (Array keys, Array items, int index, int length)
		{
			Sort (keys, items, index, length, (IComparer)null);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort (Array array, int index, int length, IComparer comparer)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
				
			if (array.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));

			if (index < array.GetLowerBound (0))
				throw new ArgumentOutOfRangeException ("index");

			if (length < 0)
				throw new ArgumentOutOfRangeException ("length", Locale.GetText (
					"Value has to be >= 0."));

			if (array.Length - (array.GetLowerBound (0) + index) < length)
				throw new ArgumentException ();
				
			SortImpl (array, null, index, length, comparer);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort (Array keys, Array items, int index, int length, IComparer comparer)
		{
			if (items == null) {
				Sort (keys, index, length, comparer);
				return;
			}

			if (keys == null)
				throw new ArgumentNullException ("keys");

			if (keys.Rank > 1 || items.Rank > 1)
				throw new RankException ();

			if (keys.GetLowerBound (0) != items.GetLowerBound (0))
				throw new ArgumentException ();

			if (index < keys.GetLowerBound (0))
				throw new ArgumentOutOfRangeException ("index");

			if (length < 0)
				throw new ArgumentOutOfRangeException ("length", Locale.GetText (
					"Value has to be >= 0."));

			if (items.Length - (index + items.GetLowerBound (0)) < length || keys.Length - (index + keys.GetLowerBound (0)) < length)
				throw new ArgumentException ();

			SortImpl (keys, items, index, length, comparer);
		}

		private static void SortImpl (Array keys, Array items, int index, int length, IComparer comparer)
		{
			if (length <= 1)
				return;

			int low = index;
			int high = index + length - 1;

#if !BOOTSTRAP_BASIC			
			if (comparer == null && items is object[]) {
				/* Its better to compare typecodes as casts treat long/ulong/long based enums the same */
				switch (Type.GetTypeCode (keys.GetType ().GetElementType ())) {
				case TypeCode.Int32:
					qsort (keys as Int32[], items as object[], low, high);
					return;
				case TypeCode.Int64:
					qsort (keys as Int64[], items as object[], low, high);
					return;
				case TypeCode.Byte:
					qsort (keys as byte[], items as object[], low, high);
					return;
				case TypeCode.Char:
					qsort (keys as char[], items as object[], low, high);
					return;
				case TypeCode.DateTime:
					qsort (keys as DateTime[], items as object[], low, high);
					return;
				case TypeCode.Decimal:
					qsort (keys as decimal[], items as object[], low, high);
					return;
				case TypeCode.Double:
					qsort (keys as double[], items as object[], low, high);
					return;
				case TypeCode.Int16:
					qsort (keys as Int16[], items as object[], low, high);
					return;
				case TypeCode.SByte:
					qsort (keys as SByte[], items as object[], low, high);
					return;
				case TypeCode.Single:
					qsort (keys as Single[], items as object[], low, high);
					return;
				case TypeCode.UInt16:
					qsort (keys as UInt16[], items as object[], low, high);
					return;	
				case TypeCode.UInt32:
					qsort (keys as UInt32[], items as object[], low, high);
					return;
				case TypeCode.UInt64:
					qsort (keys as UInt64[], items as object[], low, high);
					return;
				default:
					break;
				}				
			}
#endif

			if (comparer == null)
				CheckComparerAvailable (keys, low, high);
 
			try {
				qsort (keys, items, low, high, comparer);
			} catch (Exception e) {
				throw new InvalidOperationException (Locale.GetText ("The comparer threw an exception."), e);
			}
		}

		/* note, these are instance methods */
		void int_swapper (int i, int j) {
			int[] array = this as int[];
			int val = array [i];
			array [i] = array [j];
			array [j] = val;
		}

		void obj_swapper (int i, int j) {
			object[] array = this as object[];
			object val = array [i];
			array [i] = array [j];
			array [j] = val;
		}

		void slow_swapper (int i, int j) {
			object val = GetValueImpl (i);
			SetValueImpl (GetValue (j), i);
			SetValueImpl (val, j);
		}

		void double_swapper (int i, int j) {
			double[] array = this as double[];
			double val = array [i];
			array [i] = array [j];
			array [j] = val;
		}
		
		static bool QSortArrange (Array keys, Array items, int lo, ref object v0, int hi, ref object v1, IComparer comparer)
		{
			IComparable cmp;
			object tmp;
			
			if (comparer != null) {
				if (comparer.Compare (v1, v0) < 0) {
					swap (keys, items, lo, hi);
					tmp = v0;
					v0 = v1;
					v1 = tmp;
					
					return true;
				}
			} else if (v0 != null) {
				cmp = v1 as IComparable;
				
				if (v1 == null || cmp.CompareTo (v0) < 0) {
					swap (keys, items, lo, hi);
					tmp = v0;
					v0 = v1;
					v1 = tmp;
					
					return true;
				}
			}
			
			return false;
		}
		
		private static void qsort (Array keys, Array items, int low, int high, IComparer comparer)
		{
			//const int QSORT_THRESHOLD = 7;
			object key, hi, lo;
			IComparable cmp;
			int mid, i, k;
			
			// TODO: implement InsertionSort when QSORT_THRESHOLD reached
			
			// calculate the middle element
			mid = low + ((high - low) / 2);
			
			// get the 3 keys
			key = keys.GetValueImpl (mid);
			hi = keys.GetValueImpl (high);
			lo = keys.GetValueImpl (low);
			
			// once we re-order the low, mid, and high elements to be in
			// ascending order, we'll use mid as our pivot.
			QSortArrange (keys, items, low, ref lo, mid, ref key, comparer);
			if (QSortArrange (keys, items, mid, ref key, high, ref hi, comparer))
				QSortArrange (keys, items, low, ref lo, mid, ref key, comparer);
			
			cmp = key as IComparable;
			
			// since we've already guaranteed that lo <= mid and mid <= hi,
			// we can skip comparing them again.
			k = high - 1;
			i = low + 1;
			
			do {
				// Move the walls in
				if (comparer != null) {
					while (i < k && comparer.Compare (key, keys.GetValueImpl (i)) >= 0)
						i++;
					
					while (k >= i && comparer.Compare (key, keys.GetValueImpl (k)) < 0)
						k--;
				} else if (cmp != null) {
					while (i < k && cmp.CompareTo (keys.GetValueImpl (i)) >= 0)
						i++;
					
					while (k >= i && cmp.CompareTo (keys.GetValueImpl (k)) < 0)
						k--;
				} else {
					// This has the effect of moving the null values to the front if comparer is null
					while (i < k && keys.GetValueImpl (i) == null)
						i++;
					
					while (k >= i && keys.GetValueImpl (k) != null)
						k--;
				}
				
				if (k <= i)
					break;
				
				swap (keys, items, i, k);
				
				// make sure we keep track of our pivot element
				if (mid == i)
					mid = k;
				else if (mid == k)
					mid = i;
				
				i++;
				k--;
			} while (true);
			
			if (k != mid) {
				// swap the pivot with the last element in the first partition
				swap (keys, items, mid, k);
			}
			
			// recursively sort each partition
			if ((k + 1) < high)
				qsort (keys, items, k + 1, high, comparer);
			if ((k - 1) > low)
				qsort (keys, items, low, k - 1, comparer);
		}

		private static void CheckComparerAvailable (Array keys, int low, int high)
		{
			// move null keys to beginning of array,
			// ensure that non-null keys implement IComparable
			for (int i = 0; i < high; i++) {
				object obj = keys.GetValueImpl (i);
				if (obj == null)
					continue;
				if (!(obj is IComparable)) {
					string msg = Locale.GetText ("No IComparable interface found for type '{0}'.");
					throw new InvalidOperationException (String.Format (msg, obj.GetType ()));
				}  
			}
		}

		private static void swap (Array keys, Array items, int i, int j)
		{
			object tmp = keys.GetValueImpl (i);
			keys.SetValueImpl (keys.GetValueImpl (j), i);
			keys.SetValueImpl (tmp, j);

			if (items != null) {
				tmp = items.GetValueImpl (i);
				items.SetValueImpl (items.GetValueImpl (j), i);
				items.SetValueImpl (tmp, j);
			}
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort<T> (T [] array)
		{
			Sort<T> (array, (IComparer<T>)null);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort<TKey, TValue> (TKey [] keys, TValue [] items)
		{
			Sort<TKey, TValue> (keys, items, (IComparer<TKey>)null);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort<T> (T [] array, IComparer<T> comparer)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			SortImpl<T, T> (array, null, 0, array.Length, comparer);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort<TKey, TValue> (TKey [] keys, TValue [] items, IComparer<TKey> comparer)
		{
			if (items == null) {
				Sort<TKey> (keys, comparer);
				return;
			}		
		
			if (keys == null)
				throw new ArgumentNullException ("keys");
				
			if (keys.Length != items.Length)
				throw new ArgumentException ("Length of keys and items does not match.");
			
			SortImpl<TKey, TValue> (keys, items, 0, keys.Length, comparer);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort<T> (T [] array, int index, int length)
		{
			Sort<T> (array, index, length, (IComparer<T>)null);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort<TKey, TValue> (TKey [] keys, TValue [] items, int index, int length)
		{
			Sort<TKey, TValue> (keys, items, index, length, (IComparer<TKey>)null);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort<T> (T [] array, int index, int length, IComparer<T> comparer)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (index < 0)
				throw new ArgumentOutOfRangeException ("index");

			if (length < 0)
				throw new ArgumentOutOfRangeException ("length", Locale.GetText (
					"Value has to be >= 0."));

			if (index + length > array.Length)
				throw new ArgumentException ();
				
			SortImpl<T, T> (array, null, index, length, comparer);
		}

		[ReliabilityContractAttribute (Consistency.MayCorruptInstance, Cer.MayFail)]
		public static void Sort<TKey, TValue> (TKey [] keys, TValue [] items, int index, int length, IComparer<TKey> comparer)
		{
			if (items == null) {
				Sort<TKey> (keys, index, length, comparer);
				return;
			}

			if (keys == null)
				throw new ArgumentNullException ("keys");

			if (index < 0)
				throw new ArgumentOutOfRangeException ("index");

			if (length < 0)
				throw new ArgumentOutOfRangeException ("length");

			if (items.Length - index < length || keys.Length - index < length)
				throw new ArgumentException ();

			SortImpl<TKey, TValue> (keys, items, index, length, comparer);
		}

		private static void SortImpl<TKey, TValue> (TKey [] keys, TValue [] items, int index, int length, IComparer<TKey> comparer)
		{
			if (keys.Length <= 1)
				return;

			int low = index;
			int high = index + length - 1;
			
			//
			// Check for value types which can be sorted without Compare () method
			//
			if (comparer == null) {
#if !BOOTSTRAP_BASIC				
				switch (Type.GetTypeCode (typeof (TKey))) {
				case TypeCode.Int32:
					qsort (keys as Int32[], items, low, high);
					return;
				case TypeCode.Int64:
					qsort (keys as Int64[], items, low, high);
					return;
				case TypeCode.Byte:
					qsort (keys as byte[], items, low, high);
					return;
				case TypeCode.Char:
					qsort (keys as char[], items, low, high);
					return;
				case TypeCode.DateTime:
					qsort (keys as DateTime[], items, low, high);
					return;
				case TypeCode.Decimal:
					qsort (keys as decimal[], items, low, high);
					return;
				case TypeCode.Double:
					qsort (keys as double[], items, low, high);
					return;
				case TypeCode.Int16:
					qsort (keys as Int16[], items, low, high);
					return;
				case TypeCode.SByte:
					qsort (keys as SByte[], items, low, high);
					return;
				case TypeCode.Single:
					qsort (keys as Single[], items, low, high);
					return;
				case TypeCode.UInt16:
					qsort (keys as UInt16[], items, low, high);
					return;	
				case TypeCode.UInt32:
					qsort (keys as UInt32[], items, low, high);
					return;
				case TypeCode.UInt64:
					qsort (keys as UInt64[], items, low, high);
					return;
				}
#endif
				// Using Comparer<TKey> adds a small overload, but with value types it
				// helps us to not box them.
				if (typeof (IComparable<TKey>).IsAssignableFrom (typeof (TKey)) &&
						typeof (TKey).IsValueType)
					comparer = Comparer<TKey>.Default;
			}

			if (comparer == null)
				CheckComparerAvailable<TKey> (keys, low, high);
 
			//try {
				qsort (keys, items, low, high, comparer);
				//} catch (Exception e) {
				//throw new InvalidOperationException (Locale.GetText ("The comparer threw an exception."), e);
				//}
		}
		
		public static void Sort<T> (T [] array, Comparison<T> comparison)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (comparison == null)
				throw new ArgumentNullException ("comparison");

			SortImpl<T> (array, array.Length, comparison);
		}

		// used by List<T>.Sort (Comparison <T>)
		internal static void SortImpl<T> (T [] array, int length, Comparison<T> comparison)
		{
			if (length <= 1)
				return;
			
			try {
				int low0 = 0;
				int high0 = length - 1;
				qsort<T> (array, low0, high0, comparison);
			} catch (InvalidOperationException) {
				throw;
			} catch (Exception e) {
				throw new InvalidOperationException (Locale.GetText ("Comparison threw an exception."), e);
			}
		}
		
		static bool QSortArrange<T, U> (T [] keys, U [] items, int lo, int hi) where T : IComparable<T>
		{
			if (keys[lo] != null) {
				if (keys[hi] == null || keys[hi].CompareTo (keys[lo]) < 0) {
					swap (keys, items, lo, hi);
					return true;
				}
			}
			
			return false;
		}
		
		private static void qsort<T, U> (T[] keys, U[] items, int low, int high) where T : IComparable<T>
		{
			const int QSORT_THRESHOLD = 7;
			int mid, i, k;
			T key;
			
			if ((low + QSORT_THRESHOLD) > high) {
				// switch to insertion sort
				for (i = low + 1; i <= high; i++) {
					for (k = i; k > low; k--) {
						// if keys[k] >= keys[k-1], break
						if (keys[k-1] == null)
							break;
						
						if (keys[k] != null && keys[k].CompareTo (keys[k-1]) >= 0)
							break;
						
						swap (keys, items, k - 1, k);
					}
				}
				
				return;
			}
			
			// calculate the middle element
			mid = low + ((high - low) / 2);
			
			// once we re-order the lo, mid, and hi elements to be in
			// ascending order, we'll use mid as our pivot.
			QSortArrange<T, U> (keys, items, low, mid);
			if (QSortArrange<T, U> (keys, items, mid, high))
				QSortArrange<T, U> (keys, items, low, mid);
			
			key = keys[mid];
			
			// since we've already guaranteed that lo <= mid and mid <= hi,
			// we can skip comparing them again
			k = high - 1;
			i = low + 1;
			
			do {
				if (key != null) {
					// find the first element with a value > pivot value
					while (i < k && key.CompareTo (keys[i]) >= 0)
						i++;
					
					// find the last element with a value <= pivot value
					while (k >= i && key.CompareTo (keys[k]) < 0)
						k--;
				} else {
					while (i < k && keys[i] == null)
						i++;
					
					while (k >= i && keys[k] != null)
						k--;
				}
				
				if (k <= i)
					break;
				
				swap (keys, items, i, k);
				
				// make sure we keep track of our pivot element
				if (mid == i)
					mid = k;
				else if (mid == k)
					mid = i;
				
				i++;
				k--;
			} while (true);
			
			if (k != mid) {
				// swap the pivot with the last element in the first partition
				swap (keys, items, mid, k);
			}
			
			// recursively sort each partition
			if ((k + 1) < high)
				qsort (keys, items, k + 1, high);
			
			if ((k - 1) > low)
				qsort (keys, items, low, k - 1);
		}		
		
		static bool QSortArrange<K, V> (K [] keys, V [] items, int lo, int hi, IComparer<K> comparer)
		{
			IComparable<K> gcmp;
			IComparable cmp;
			
			if (comparer != null) {
				if (comparer.Compare (keys[hi], keys[lo]) < 0) {
					swap<K, V> (keys, items, lo, hi);
					return true;
				}
			} else if (keys[lo] != null) {
				if (keys[hi] == null) {
					swap<K, V> (keys, items, lo, hi);
					return true;
				}
				
				gcmp = keys[hi] as IComparable<K>;
				if (gcmp != null) {
					if (gcmp.CompareTo (keys[lo]) < 0) {
						swap<K, V> (keys, items, lo, hi);
						return true;
					}
					
					return false;
				}
				
				cmp = keys[hi] as IComparable;
				if (cmp != null) {
					if (cmp.CompareTo (keys[lo]) < 0) {
						swap<K, V> (keys, items, lo, hi);
						return true;
					}
					
					return false;
				}
			}
			
			return false;
		}
		
		private static void qsort<K, V> (K [] keys, V [] items, int low, int high, IComparer<K> comparer)
		{
			const int QSORT_THRESHOLD = 7;
			IComparable<K> gcmp;
			IComparable cmp;
			int mid, i, k;
			K key;
			
			if ((low + QSORT_THRESHOLD) > high) {
				// switch to insertion sort
				for (i = low + 1; i <= high; i++) {
					for (k = i; k > low; k--) {
						// if keys[k] >= keys[k-1], break
						if (comparer != null) {
							if (comparer.Compare (keys[k], keys[k-1]) >= 0)
								break;
						} else {
							if (keys[k-1] == null)
								break;
							
							if (keys[k] != null) {
								gcmp = keys[k] as IComparable<K>;
								cmp = keys[k] as IComparable;
								if (gcmp != null) {
									if (gcmp.CompareTo (keys[k-1]) >= 0)
										break;
								} else {
									if (cmp.CompareTo (keys[k-1]) >= 0)
										break;
								}
							}
						}
						
						swap<K, V> (keys, items, k - 1, k);
					}
				}
				
				return;
			}
			
			// calculate the middle element
			mid = low + ((high - low) / 2);
			
			// once we re-order the low, mid, and high elements to be in
			// ascending order, we'll use mid as our pivot.
			QSortArrange<K, V> (keys, items, low, mid, comparer);
			if (QSortArrange<K, V> (keys, items, mid, high, comparer))
				QSortArrange<K, V> (keys, items, low, mid, comparer);
			
			key = keys[mid];
			gcmp = key as IComparable<K>;
			cmp = key as IComparable;
			
			// since we've already guaranteed that lo <= mid and mid <= hi,
			// we can skip comparing them again.
			k = high - 1;
			i = low + 1;
			
			do {
				// Move the walls in
				if (comparer != null) {
					while (i < k && comparer.Compare (key, keys[i]) >= 0)
						i++;
					
					while (k >= i && comparer.Compare (key, keys[k]) < 0)
						k--;
				} else {
					if (gcmp != null) {
						while (i < k && gcmp.CompareTo (keys[i]) >= 0)
							i++;
						
						while (k >= i && gcmp.CompareTo (keys[k]) < 0)
							k--;
					} else if (cmp != null) {
						while (i < k && cmp.CompareTo (keys[i]) >= 0)
							i++;
						
						while (k >= i && cmp.CompareTo (keys[k]) < 0)
							k--;
					} else {
						while (i < k && keys[i] == null)
							i++;
						
						while (k >= i && keys[k] != null)
							k--;
					}
				}
				
				if (k <= i)
					break;
				
				swap<K, V> (keys, items, i, k);
				
				// make sure we keep track of our pivot element
				if (mid == i)
					mid = k;
				else if (mid == k)
					mid = i;
				
				i++;
				k--;
			} while (true);
			
			if (k != mid) {
				// swap the pivot with the last element in the first partition
				swap<K, V> (keys, items, mid, k);
			}
			
			// recursively sort each partition
			if ((k + 1) < high)
				qsort<K, V> (keys, items, k + 1, high, comparer);
			if ((k - 1) > low)
				qsort<K, V> (keys, items, low, k - 1, comparer);
		}
		
		static bool QSortArrange<T> (T [] array, int lo, int hi, Comparison<T> compare)
		{
			if (array[lo] != null) {
				if (array[hi] == null || compare (array[hi], array[lo]) < 0) {
					swap<T> (array, lo, hi);
					return true;
				}
			}
			
			return false;
		}
		
		private static void qsort<T> (T [] array, int low, int high, Comparison<T> compare)
		{
			const int QSORT_THRESHOLD = 7;
			int mid, i, k;
			T key;
			
			if ((low + QSORT_THRESHOLD) > high) {
				// switch to insertion sort
				for (i = low + 1; i <= high; i++) {
					for (k = i; k > low; k--) {
						// if keys[k] >= keys[k-1], break
						if (array[k-1] == null)
							break;
						
						if (array[k] != null && compare (array[k], array[k-1]) >= 0)
							break;
						
						swap<T> (array, k - 1, k);
					}
				}
				
				return;
			}
			
			// calculate the middle element
			mid = low + ((high - low) / 2);
			
			// once we re-order the lo, mid, and hi elements to be in
			// ascending order, we'll use mid as our pivot.
			QSortArrange<T> (array, low, mid, compare);
			if (QSortArrange<T> (array, mid, high, compare))
				QSortArrange<T> (array, low, mid, compare);
			
			key = array[mid];
			
			// since we've already guaranteed that lo <= mid and mid <= hi,
			// we can skip comparing them again
			k = high - 1;
			i = low + 1;
			
			do {
				// Move the walls in
				if (key != null) {
					// find the first element with a value > pivot value
					while (i < k && compare (key, array[i]) >= 0)
						i++;
					
					// find the last element with a value <= pivot value
					while (k >= i && compare (key, array[k]) < 0)
						k--;
				} else {
					while (i < k && array[i] == null)
						i++;
					
					while (k >= i && array[k] != null)
						k--;
				}
				
				if (k <= i)
					break;
				
				swap<T> (array, i, k);
				
				// make sure we keep track of our pivot element
				if (mid == i)
					mid = k;
				else if (mid == k)
					mid = i;
				
				i++;
				k--;
			} while (true);
			
			if (k != mid) {
				// swap the pivot with the last element in the first partition
				swap<T> (array, mid, k);
			}
			
			// recursively sort each partition
			if ((k + 1) < high)
				qsort<T> (array, k + 1, high, compare);
			
			if ((k - 1) > low)
				qsort<T> (array, low, k - 1, compare);
		}

		private static void CheckComparerAvailable<K> (K [] keys, int low, int high)
		{
			// move null keys to beginning of array,
			// ensure that non-null keys implement IComparable
			for (int i = low; i < high; i++) {
				K key = keys [i];
				if (key != null) {
					if (!(key is IComparable<K>) && !(key is IComparable)) {
						string msg = Locale.GetText ("No IComparable<T> or IComparable interface found for type '{0}'.");
						throw new InvalidOperationException (String.Format (msg, key.GetType ()));
					}  
				}
			}
		}

		private static void swap<K, V> (K [] keys, V [] items, int i, int j)
		{
			K tmp;

			tmp = keys [i];
			keys [i] = keys [j];
			keys [j] = tmp;

			if (items != null) {
				V itmp;
				itmp = items [i];
				items [i] = items [j];
				items [j] = itmp;
			}
		}

		private static void swap<T> (T [] array, int i, int j)
		{
			T tmp = array [i];
			array [i] = array [j];
			array [j] = tmp;
		}
		
		public void CopyTo (Array array, int index)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			// The order of these exception checks may look strange,
			// but that's how the microsoft runtime does it.
			if (this.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));
			if (index + this.GetLength (0) > array.GetLowerBound (0) + array.GetLength (0))
				throw new ArgumentException ("Destination array was not long " +
					"enough. Check destIndex and length, and the array's " +
					"lower bounds.");
			if (array.Rank > 1)
				throw new RankException (Locale.GetText ("Only single dimension arrays are supported."));
			if (index < 0)
				throw new ArgumentOutOfRangeException ("index", Locale.GetText (
					"Value has to be >= 0."));

			Copy (this, this.GetLowerBound (0), array, index, this.GetLength (0));
		}

		[ComVisible (false)]
		public void CopyTo (Array array, long index)
		{
			if (index < 0 || index > Int32.MaxValue)
				throw new ArgumentOutOfRangeException ("index", Locale.GetText (
					"Value must be >= 0 and <= Int32.MaxValue."));

			CopyTo (array, (int) index);
		}

		internal class SimpleEnumerator : IEnumerator, ICloneable
		{
			Array enumeratee;
			int currentpos;
			int length;

			public SimpleEnumerator (Array arrayToEnumerate)
			{
				this.enumeratee = arrayToEnumerate;
				this.currentpos = -1;
				this.length = arrayToEnumerate.Length;
			}

			public object Current {
				get {
			 		// Exception messages based on MS implementation
					if (currentpos < 0 )
						throw new InvalidOperationException (Locale.GetText (
							"Enumeration has not started."));
					if  (currentpos >= length)
						throw new InvalidOperationException (Locale.GetText (
							"Enumeration has already ended"));
					// Current should not increase the position. So no ++ over here.
					return enumeratee.GetValueImpl (currentpos);
				}
			}

			public bool MoveNext()
			{
				//The docs say Current should throw an exception if last
				//call to MoveNext returned false. This means currentpos
				//should be set to length when returning false.
				if (currentpos < length)
					currentpos++;
				if(currentpos < length)
					return true;
				else
					return false;
			}

			public void Reset()
			{
				currentpos = -1;
			}

			public object Clone ()
			{
				return MemberwiseClone ();
			}
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public static void Resize<T> (ref T [] array, int newSize)
		{
			if (newSize < 0)
				throw new ArgumentOutOfRangeException ();
			
			if (array == null) {
				array = new T [newSize];
				return;
			}

			int length = array.Length;
			if (length == newSize)
				return;
			
			T [] a = new T [newSize];
			if (length != 0)
				FastCopy (array, 0, a, 0, Math.Min (newSize, length));
			array = a;
		}
		
		public static bool TrueForAll <T> (T [] array, Predicate <T> match)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			if (match == null)
				throw new ArgumentNullException ("match");
			
			foreach (T t in array)
				if (! match (t))
					return false;
				
			return true;
		}
		
		public static void ForEach<T> (T [] array, Action <T> action)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			if (action == null)
				throw new ArgumentNullException ("action");
			
			foreach (T t in array)
				action (t);
		}
		
		public static TOutput[] ConvertAll<TInput, TOutput> (TInput [] array, Converter<TInput, TOutput> converter)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			if (converter == null)
				throw new ArgumentNullException ("converter");
			
			TOutput [] output = new TOutput [array.Length];
			for (int i = 0; i < array.Length; i ++)
				output [i] = converter (array [i]);
			
			return output;
		}
		
		public static int FindLastIndex<T> (T [] array, Predicate <T> match)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			
			return FindLastIndex<T> (array, 0, array.Length, match);
		}
		
		public static int FindLastIndex<T> (T [] array, int startIndex, Predicate<T> match)
		{
			if (array == null)
				throw new ArgumentNullException ();
			
			return FindLastIndex<T> (array, startIndex, array.Length - startIndex, match);
		}
		
		public static int FindLastIndex<T> (T [] array, int startIndex, int count, Predicate<T> match)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			if (match == null)
				throw new ArgumentNullException ("match");
			
			if (startIndex > array.Length || startIndex + count > array.Length)
				throw new ArgumentOutOfRangeException ();
			
			for (int i = startIndex + count - 1; i >= startIndex; i--)
				if (match (array [i]))
					return i;
				
			return -1;
		}
		
		public static int FindIndex<T> (T [] array, Predicate<T> match)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			
			return FindIndex<T> (array, 0, array.Length, match);
		}
		
		public static int FindIndex<T> (T [] array, int startIndex, Predicate<T> match)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			
			return FindIndex<T> (array, startIndex, array.Length - startIndex, match);
		}
		
		public static int FindIndex<T> (T [] array, int startIndex, int count, Predicate<T> match)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			
			if (match == null)
				throw new ArgumentNullException ("match");
			
			if (startIndex > array.Length || startIndex + count > array.Length)
				throw new ArgumentOutOfRangeException ();
			
			for (int i = startIndex; i < startIndex + count; i ++)
				if (match (array [i]))
					return i;
				
			return -1;
		}
		
		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int BinarySearch<T> (T [] array, T value)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			
			return BinarySearch<T> (array, 0, array.Length, value, null);
		}
		
		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int BinarySearch<T> (T [] array, T value, IComparer<T> comparer)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			
			return BinarySearch<T> (array, 0, array.Length, value, comparer);
		}
		
		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int BinarySearch<T> (T [] array, int index, int length, T value)
		{
			return BinarySearch<T> (array, index, length, value, null);
		}
		
		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.MayFail)]
		public static int BinarySearch<T> (T [] array, int index, int length, T value, IComparer<T> comparer)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			if (index < 0)
				throw new ArgumentOutOfRangeException ("index", Locale.GetText (
					"index is less than the lower bound of array."));
			if (length < 0)
				throw new ArgumentOutOfRangeException ("length", Locale.GetText (
					"Value has to be >= 0."));
			// re-ordered to avoid possible integer overflow
			if (index > array.Length - length)
				throw new ArgumentException (Locale.GetText (
					"index and length do not specify a valid range in array."));
			if (comparer == null)
				comparer = Comparer <T>.Default;
			
			int iMin = index;
			int iMax = index + length - 1;
			int iCmp = 0;
			try {
				while (iMin <= iMax) {
					// Be careful with overflows
					int iMid = iMin + ((iMax - iMin) / 2);
					iCmp = comparer.Compare (value, array [iMid]);

					if (iCmp == 0)
						return iMid;
					else if (iCmp < 0)
						iMax = iMid - 1;
					else
						iMin = iMid + 1; // compensate for the rounding down
				}
			} catch (Exception e) {
				throw new InvalidOperationException (Locale.GetText ("Comparer threw an exception."), e);
			}

			return ~iMin;
		}
		
		public static int IndexOf<T> (T [] array, T value)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
	
			return IndexOf<T> (array, value, 0, array.Length);
		}

		public static int IndexOf<T> (T [] array, T value, int startIndex)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			return IndexOf<T> (array, value, startIndex, array.Length - startIndex);
		}

		public static int IndexOf<T> (T [] array, T value, int startIndex, int count)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			
			// re-ordered to avoid possible integer overflow
			if (count < 0 || startIndex < array.GetLowerBound (0) || startIndex - 1 > array.GetUpperBound (0) - count)
				throw new ArgumentOutOfRangeException ();

			int max = startIndex + count;
			EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
			for (int i = startIndex; i < max; i++) {
				if (equalityComparer.Equals (array [i], value))
					return i;
			}

			return -1;
		}
		
		public static int LastIndexOf<T> (T [] array, T value)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (array.Length == 0)
				return -1;
			return LastIndexOf<T> (array, value, array.Length - 1);
		}

		public static int LastIndexOf<T> (T [] array, T value, int startIndex)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			return LastIndexOf<T> (array, value, startIndex, startIndex + 1);
		}

		public static int LastIndexOf<T> (T [] array, T value, int startIndex, int count)
		{
			if (array == null)
				throw new ArgumentNullException ("array");
			
			if (count < 0 || startIndex < array.GetLowerBound (0) ||
				startIndex > array.GetUpperBound (0) ||	startIndex - count + 1 < array.GetLowerBound (0))
				throw new ArgumentOutOfRangeException ();
			
			EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
			for (int i = startIndex; i >= startIndex - count + 1; i--) {
				if (equalityComparer.Equals (array [i], value))
					return i;
			}

			return -1;
		}
		
		public static T [] FindAll<T> (T [] array, Predicate <T> match)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (match == null)
				throw new ArgumentNullException ("match");
			
			int pos = 0;
			T [] d = new T [array.Length];
			foreach (T t in array)
				if (match (t))
					d [pos++] = t;
			
			Resize <T> (ref d, pos);
			return d;
		}

		public static bool Exists<T> (T [] array, Predicate <T> match)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (match == null)
				throw new ArgumentNullException ("match");
			
			foreach (T t in array)
				if (match (t))
					return true;
			return false;
		}

		public static ReadOnlyCollection<T> AsReadOnly<T> (T[] array)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			return new ReadOnlyCollection<T> (array);
		}

		public static T Find<T> (T [] array, Predicate<T> match)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (match == null)
				throw new ArgumentNullException ("match");
			
			foreach (T t in array)
				if (match (t))
					return t;
				
			return default (T);
		}
		
		public static T FindLast<T> (T [] array, Predicate <T> match)
		{
			if (array == null)
				throw new ArgumentNullException ("array");

			if (match == null)
				throw new ArgumentNullException ("match");
			
			for (int i = array.Length - 1; i >= 0; i--)
				if (match (array [i]))
					return array [i];
				
			return default (T);
		}

		[ReliabilityContractAttribute (Consistency.WillNotCorruptState, Cer.Success)]		
		//
		// The constrained copy should guarantee that if there is an exception thrown
		// during the copy, the destination array remains unchanged.
		// This is related to System.Runtime.Reliability.CER
		public static void ConstrainedCopy (Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
		{
			Copy (sourceArray, sourceIndex, destinationArray, destinationIndex, length);
		}
	}
}
