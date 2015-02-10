//
// BindableObject.cs
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
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Xamarin.AsyncTests.UI.Binding
{
	public abstract class BindableObject : INotifyPropertyChanged
	{
		Dictionary<string,object> properties = new Dictionary<string,object> ();

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged ([CallerMemberName] string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler (this, new PropertyChangedEventArgs (propertyName));
		}

		public object GetValue (BindableProperty property)
		{
			if (properties.ContainsKey (property.PropertyName))
				return properties [property.PropertyName];
			return property.DefaultValue;
		}

		public void SetValue (BindableProperty property, object value)
		{
			object oldValue = null;
			if (properties.ContainsKey (property.PropertyName)) {
				oldValue = properties [property.PropertyName];
				if (object.Equals (oldValue, value))
					return;
			}

			if (property.PropertyChanging != null)
				property.PropertyChanging (this, oldValue, value);
			properties [property.PropertyName] = value;
			if (property.PropertyChanged != null)
				property.PropertyChanged (this, oldValue, value);
			OnPropertyChanged (property.PropertyName);
		}
	}
}

