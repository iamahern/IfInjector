# IfFastInjector v0.1

This project is a fork of the https://ffastinjector.codeplex.com/ library by David Walker (https://plus.google.com/109602553530284384616/).

The goal of this fork is to convert the libary into a proper instance-based injector object as well as to add injection annotations.

For the present time, Multi and Map binders are not a goal.

# Documentations

https://github.com/iamahern/IfFastInjector/wiki

# Example Usage and Quickstart

## Source Code

The implementation consists of two files:
IfFastInjector.cs
IfFastInjectorInternal.cs

## Example Usage

The code sample below should give you a flavor of the types of things you can do.  A deeper explanation is given in the full documentation.

```
// =================================
// Setup
// =================================
using IfFastInjector;

// You may define an interface or base type and then bind it to an implementation type
//   via a the IfImplementedBy annotation
[IfImplementedBy(typeof(MyCType))]
abstract class MyBaseType {
    // Here, the base type requests a property to be injected using the [IfInject] annotation.
    [IfInject]
    public MyOtherType OtherType { get; private set; }
}

// The MyCType concrete type defines itself as a singleton.  
//   Without an IfSingleton annotation or a binding to that effect, classes are treated as instance based
[IfSingleton]
class MyCType : MyBaseType {
    // Here, the type requests a field to be injected using the [IfInject] annotation.
    [IfInject]
    private IMyFactoryType factoryType;
}


class MyOtherType {
    public int Count { get; private set; }

    // Here the IfInject annotation is used to nominate a constructor for use during injection.
    //  By default, the 'simplest' (IE the constructor with the fewest arguments) is chosen if 
    //  no constructor has been explicitly nominated.
    [IfInject]
    public MyOtherType(IMyFactoryType factoryType) { }

    // For example: without the IfInject annotation above, the system would choose this 
    //  constructor by default.
    public MyOtherType() {}
}

interface IMyFactoryType { }

class MyFactoryType : IMyFactoryType {}

// =================================
// Usage
// =================================
var injector = IfInjector.NewInstance();

// Here we bind MyOtherType to itself
injector.Bind<MyOtherType>()
  .AddPropertyInjector((x) => x.Count, () => 5)  // Then add an injector to set the 'Count' property
  .AsSingleton();                                // finally, we flag it as a singleton

// Here, we bind an interface to a concrete type. In this case, the concrete type is provided by 
// using a closure.  You may also use a factory method as well.
injector.Bind<IMyFactoryType,MyFactoryType>(() => new MyFactoryType())
  .AsSingleton();  

// ==============

// Here we resolve an object using its 'keytype'. Due to the annotation above, this 
//  is automatically resolved to 'MyCType'. Type bindings may be overridden by 
//  explicit .Bind<> statements.
var val = injector.Resolve<MyBaseType>();

/* 
Looking at the structure of the object returned:

MyCType {
  // Attribute based binding
  [attribute] OtherType : MyOtherType {  // Type bound explicitly
    Count : int = 5          // Explicit property binding
  }

  // Attribute based binding
  [field] factoryType : IMyFactoryType = MyFactoryType {}  // Type bound explicitly using factory
}

*/

```

# LICENSE

This source is under the BSD license (http://ffastinjector.codeplex.com/license).
