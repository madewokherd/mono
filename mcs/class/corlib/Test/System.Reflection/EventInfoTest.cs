//
// EventInfoTest
//
// Ben Maurer (bmaurer@ximian.com)
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Threading;
using System.Reflection;
using System.Runtime.InteropServices;

using NUnit.Framework;

namespace MonoTests.System.Reflection
{
	[TestFixture]
	public class EventInfoTest
	{
		[Test]
		public void IsDefined_AttributeType_Null ()
		{
			EventInfo priv = typeof (PrivateEvent).GetEvents (
				BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.Static) [0];

			try {
				priv.IsDefined ((Type) null, false);
				Assert.Fail ("#1");
			} catch (ArgumentNullException ex) {
				Assert.AreEqual (typeof (ArgumentNullException), ex.GetType (), "#2");
				Assert.IsNull (ex.InnerException, "#3");
				Assert.IsNotNull (ex.Message, "#4");
				Assert.IsNotNull (ex.ParamName, "#5");
				Assert.AreEqual ("attributeType", ex.ParamName, "#6");
			}
		}

		[Test]
		public void TestGetXXXMethod ()
		{
			EventInfo priv = typeof (PrivateEvent).GetEvents (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) [0];
			Assert.IsNull (priv.GetAddMethod (), "#A1");
			Assert.IsNull (priv.GetRaiseMethod (), "#A2");
			Assert.IsNull (priv.GetRemoveMethod (), "#A3");

			EventInfo pub = typeof (PublicEvent).GetEvents (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) [0];
			Assert.IsNotNull (pub.GetAddMethod (), "#B1");
			Assert.IsNull (pub.GetRaiseMethod (), "#B2");
			Assert.IsNotNull (pub.GetRemoveMethod (), "#B3");
		}

		[Test]
		public void AddHandlerToNullInstanceEventRaisesTargetException ()
		{
			EventInfo ev = typeof (TestClass).GetEvent ("pub");
			EventHandler dele = (a,b) => {};
			try {
				ev.AddEventHandler (null, dele);
				Assert.Fail ("#1");
			} catch (TargetException) {}
		}

		[Test]
		public void AddHandleToPrivateEventRaisesInvalidOperationException ()
		{
			EventInfo ev = typeof (TestClass).GetEvent ("priv", BindingFlags.NonPublic| BindingFlags.Instance);
			EventHandler dele = (a,b) => {};
			try {
				ev.AddEventHandler (new PrivateEvent (), dele);
				Assert.Fail ("#1");
			} catch (InvalidOperationException) {}			
		}

		[Test]
		public void AddHandlerWithIncompatibleTargetShouldRaiseTargetException ()
		{
			EventInfo ev = typeof (TestClass).GetEvent ("pub");
			EventHandler dele = (a,b) => {};
			try {
				ev.AddEventHandler (new PublicEvent (), dele);
				Assert.Fail ("#1");
			} catch (TargetException) {}			
		}

		[Test]
		public void AddHandlerShouldWrapExceptions ()
		{
			EventInfo ev = typeof (ExceptionEvent).GetEvent ("Event");
			EventHandler dele = (a,b) => {};
			try {
				ev.AddEventHandler (new ExceptionEvent (), dele);
				Assert.Fail ("#1");
			} catch (TargetInvocationException e) {
				Assert.AreEqual (typeof (Exception), e.InnerException.GetType ());
			}
		}

		[Test]
		public void RemoveHandleToPrivateEventRaisesInvalidOperationException ()
		{
			EventInfo ev = typeof (TestClass).GetEvent ("priv", BindingFlags.NonPublic| BindingFlags.Instance);
			EventHandler dele = (a,b) => {};
			try {
				ev.RemoveEventHandler (new PrivateEvent (), dele);
				Assert.Fail ("#1");
			} catch (InvalidOperationException) {}			
		}

		[Test]
		public void EventInfoModule ()
		{
			Type type = typeof (TestClass);
			EventInfo ev = type.GetEvent ("pub");

			Assert.AreEqual (type.Module, ev.Module);
		}

		[Test]
		public void MetadataToken ()
		{
			EventInfo ev = typeof (TestClass).GetEvent ("pub");
			Assert.IsTrue ((int)ev.MetadataToken > 0);
		}

        [Test]
        public void TestDerivedClassHidingEventWithPrivate ()
        {
			Type derived = typeof(Derived);
			EventInfo[] events = derived.GetEvents (BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
			Assert.AreEqual(0, events.Length, "MyEvent event is private in derived class and should be hidden.");

			Type staticDerived2 = typeof(StaticDerived2);
			EventInfo[] events2 = staticDerived2.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			Console.WriteLine(events2.Length);
		}

        [Test]
		public void TestDerivedClassOveridingEventWithStatic () {
			Type staticDerived2 = typeof(StaticDerived2);
			EventInfo[] events = staticDerived2.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			Assert.AreEqual(0, events.Length, "MyEvent event is static in parent class and should be hidden.");
		}

		// https://github.com/mono/mono/issues/13350
		[Test]
		public void ReflectedType () {
			Type type = typeof (B);
			EventInfo eventInfo = type.GetEvent (nameof (A.Event));
			MemberInfo memberInfo = eventInfo.AddMethod;
			Assert.AreEqual (type, memberInfo.ReflectedType);
		}

#pragma warning disable 67
		public class A
		{
			public event Action Event { add { } remove { } }
		}

		public class B : A
		{
		}

		public class ExceptionEvent
		{
			public event EventHandler Event {
				add {
					throw new Exception("This exception should be wrapped by AddEventHandler");
				}
				remove { }
			}
		}

		public class PrivateEvent
		{
			private static event EventHandler x;
		}

		public class PublicEvent
		{
			public static event EventHandler x;
		}

		public class TestClass
		{
			public event EventHandler pub;
			private event EventHandler priv;
		}

		private abstract class Base
		{
			public event Action MyEvent { add { } remove { } }
			public int MyProp { get; }
		}

		private abstract class Derived : Base
		{
			private new event Action MyEvent { add { } remove { } }
			private new int MyProp { get; }
		}

		private abstract class Derived2 : Derived
		{
		}

		private class StaticBase
		{
			public event Action MyEvent { add { } remove { } }
		}

		private class StaticDerived : StaticBase
		{
			public new static event Action MyEvent { add { } remove { } }
		}

		private class StaticDerived2 : StaticDerived
		{
		}
#pragma warning restore 67
	}
}
