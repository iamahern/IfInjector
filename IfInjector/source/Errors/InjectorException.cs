using System;

namespace IfInjector.Errors
{
	/// <summary>
	/// If fast injector exception.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
	public class InjectorException : Exception
	{
		internal InjectorException (InjectorError errorType, string message) : base(message) {
			ErrorType = errorType;
		}

		internal InjectorException (InjectorError errorType, string message, Exception innerException) : base(message, innerException) {
			ErrorType = errorType;
		}

		/// <summary>
		/// Gets the type of the error.
		/// </summary>
		/// <value>The type of the error.</value>
		public InjectorError ErrorType { get; private set; }
	}
}

