using System;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ConnectionFramework
{
	using ConnectionFramework;

	public class CertificateValidationParameters : ICloneable
	{
		public CertificateValidator Validator {
			get; set;
		}

		public bool RequireValidatorInvocation {
			get; set;
		}

		public object Clone ()
		{
			return new CertificateValidationParameters {
				Validator = Validator, RequireValidatorInvocation = RequireValidatorInvocation
			};
		}
	}
}

