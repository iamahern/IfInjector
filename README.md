# IfInjector v0.6

This project is a fork of the https://ffastinjector.codeplex.com/ library by David Walker (https://plus.google.com/109602553530284384616/).

The goal of the project is to provide a featureful high performance micro-IoC container suitable for use in mobile environments. The key distinguishing feature in relation to other Micro containers is support for annotation based configuration.

# Key Features

* Auto-Wiring
* Configuration through code
* Configuration through annotations
* Fast (top performer on [IoC Container Shootout](http://www.palmmedia.de/blog/2011/8/30/ioc-container-benchmark-performance-comparison)) 
* Small (<35 KB)

# Documentations

https://github.com/iamahern/IfInjector/wiki

# Example Usage and Quickstart

## Source Code

The implementation consists of 3 files:

* IfInjector.cs (Interfaces and API definition)
* IfInjector.Injector.cs (The injector implementation)
* IfInjector.IfCore.cs (Core binding, platform & expression builder utilities.)

Simply include these in your project or use the nuget package (https://www.nuget.org/packages/IfInjector).

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
var injector = new Injector();

IMyType obj = injector.Resolve<IMyType>();

```

# LICENSE

This source is under the BSD license (http://ffastinjector.codeplex.com/license).
