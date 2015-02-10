//
// BindableProperty.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using System;

namespace Xamarin.AsyncTests.UI.Binding
{
	public class BindableProperty
	{
		public delegate bool ValidateValueDelegate (BindableObject bindable, object value);

		public delegate void BindingPropertyChangedDelegate (BindableObject bindable, object oldValue, object newValue);

		public delegate object CoerceValueDelegate (BindableObject bindable, object value);

		public delegate void BindingPropertyChangingDelegate (BindableObject bindable, object oldValue, object newValue);

		public static BindableProperty Create (
			string propertyName, Type returnType, Type declaringType, object defaultValue,
			BindingMode defaultBindingMode = BindingMode.OneWay,
			ValidateValueDelegate validateValue = null, BindingPropertyChangedDelegate propertyChanged = null,
			BindingPropertyChangingDelegate propertyChanging = null, CoerceValueDelegate coerceValue = null)
		{
			return new BindableProperty (
				propertyName, returnType, declaringType, defaultValue, defaultBindingMode,
				validateValue, propertyChanged, propertyChanging, coerceValue);
		}

		public string PropertyName {
			get;
			private set;
		}

		public Type ReturnType {
			get;
			private set;
		}

		public Type DeclaringType {
			get;
			private set;
		}

		public object DefaultValue {
			get;
			private set;
		}

		public BindingPropertyChangingDelegate PropertyChanging {
			get;
			private set;
		}

		public BindingPropertyChangedDelegate PropertyChanged {
			get;
			private set;
		}

		BindableProperty (
			string propertyName, Type returnType, Type declaringType, object defaultValue,
			BindingMode defaultBindingMode = BindingMode.OneWay,
			ValidateValueDelegate validateValue = null, BindingPropertyChangedDelegate propertyChanged = null,
			BindingPropertyChangingDelegate propertyChanging = null, CoerceValueDelegate coerceValue = null)
		{
			PropertyName = propertyName;
			ReturnType = returnType;
			DeclaringType = declaringType;
			DefaultValue = defaultValue;
			PropertyChanged = propertyChanged;
			PropertyChanging = propertyChanging;
		}

	}
}

