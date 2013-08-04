# IfInjector v0.3

This project is a fork of the https://ffastinjector.codeplex.com/ library by David Walker (https://plus.google.com/109602553530284384616/).

The goal of the project is to provide a featureful high performance micro-IoC container suitable for use in mobile environments.

# Key Features

* Auto-Wiring
* Auto-Registration
* Configuration through code
* Configuration through annotations
* Fast (top performer on [IoC Container Shootout](www.palmmedia.de/blog/2011/8/30/ioc-container-benchmark-performance-comparison)) 
* Small (under 1200 lines of code)

# Documentations

https://github.com/iamahern/IfFastInjector/wiki

# Example Usage and Quickstart

## Source Code

The implementation consists of two files:

IfInjector.cs
IfInjectorInternal.cs

Simply include these in your project.

## Example Usage

```
// =================================
// Setup
// =================================
using IfInjector;

[ImplementedBy(typeof(MyType))]
interface IMyType { }

[Singleton]
class MyType : IMyType {}


// =================================
// Usage
// =================================
var injector = Injector.NewInstance();

IMyType obj = injector.Resolve<IMyType>();

```

# LICENSE

This source is under the BSD license (http://ffastinjector.codeplex.com/license).