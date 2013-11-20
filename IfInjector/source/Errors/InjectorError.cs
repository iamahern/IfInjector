using System;

namespace IfInjector.Errors
{
	/// <summary>
	/// Represents an error code constant.
	/// </summary>
	public class InjectorError {
		internal InjectorError(int messageCode, string messageTemplate) {
			MessageCode = string.Format ("IF{0:D4}", messageCode);
			MessageTemplate = messageTemplate;
		}

		/// <summary>
		/// Gets the message code.
		/// </summary>
		/// <value>The message code.</value>
		public string MessageCode { get; private set; }

		/// <summary>
		/// Gets the message template.
		/// </summary>
		/// <value>The message template.</value>
		public string MessageTemplate { get; private set; }

		public InjectorException FormatEx(params object[] args) {
			var msgFormatted = string.Format (MessageTemplate, args);
			return new InjectorException (this, msgFormatted);
		}

		public InjectorException FormatEx(Exception innerException, params object[] args) {
			var msgFormatted = string.Format (MessageTemplate, args);
			return new InjectorException (this, msgFormatted, innerException);
		}
	}
}

