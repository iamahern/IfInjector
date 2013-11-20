using System;

namespace IfInjector.Errors
{
	/// <summary>
	/// If fast injector errors.
	/// </summary>
	public static class InjectorErrors
	{
		public static readonly InjectorError ErrorResolutionRecursionDetected = new InjectorError(1, "Resolution recursion detected.  Resolve<{0}> is called by a dependency of Resolve<{0}> leading to an infinite loop.");
		public static readonly InjectorError ErrorUnableToResultInterface = new InjectorError(2, "Error on {0}. Unable to resolve Interface and Abstract classes without a configuration.");
		public static readonly InjectorError ErrorMustContainMemberExpression = new InjectorError(3, "Must contain a MemberExpression");
		public static readonly InjectorError ErrorAmbiguousBinding =  new InjectorError(4, "Multiple implicit bindings exist for type: {0}. Please disambiguate by adding an explicit binding for this type.");
		public static readonly InjectorError ErrorUnableToBindNonClassFieldsProperties = new InjectorError(5, "Autoinjection is only supported on single instance 'class' fields. Please define a manual binding for the field or property '{0}' on class '{1}'.");
		public static readonly InjectorError ErrorNoAppropriateConstructor = new InjectorError (6, "No appropriate constructor for type: {0}.");
		public static readonly InjectorError ErrorMayNotBindInjector = new InjectorError (7, "Binding 'Injector' types is not permitted.");
		public static readonly InjectorError ErrorBindingRegistrationNotPermitted = new InjectorError (8, "Injector is in resolved state. Explicit binding registration is no longer permitted.");

		public static readonly InjectorError ErrorGenericsCannotResolveOpenType = new InjectorError (9, "Generic type: {0} has open parameters. Unable to resolve.");
		public static readonly InjectorError ErrorGenericsCannotCreateBindingForNonGeneric = new InjectorError(10, "Cannot create binding for non generic type: {0}.");
		public static readonly InjectorError ErrorGenericsCannotCreateBindingForClosedGeneric = new InjectorError(11, "Cannot create binding for closed generic type: {0}.");
		public static readonly InjectorError ErrorGenericsBindToTypeIsNotDerivedFromKey = new InjectorError(12, "Cannot create binding for types that are not inherited from key types. Binding type is: {0}; key type is {1}.");
		public static readonly InjectorError ErrorGenericsBindToTypeMustHaveSameTypeArgsAsKey = new InjectorError(13, "Cannot create binding for types that do not have the same generic arguments as their key type. Binding type is: {0}; key type is {1}.");
	}
}

