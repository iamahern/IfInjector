This page covers the details of why I created this project as well as the architectural differences from fFastInjector.

# Usage

IfInjector is designed to be a used in Mobile environments. As a result the code was written as a PCL library that (theoretically) be used as is in >=WP7.1, Android and iOS.

# Production Worthiness

While I have padded out the inherited code with quite a few extra unit tests, the framework has yet to be used in anger in a production project. Currently, I do not have access to a Xamarin.iOS platform so I am dubious that there are not platform idiosyncrasies that do not show up in unit tests.

# Differences From fFastInjector

The code is a fork of the [fFastInjector](http://ffastinjector.codeplex.com) library, which was noted in the [IoC framework shootout](http://www.palmmedia.de/blog/2011/8/30/ioc-container-benchmark-performance-comparison) as being 'the' fastest framework available. In forking the code, I maintained and extended the core Linq-based architecture which was the key to the original frameworks compactness and performance.

The name of the framework is 'IfFastInjector', which stands for 'instance' 'fFastInjector' to denote one of the key architectural differences.

The major divergences are:

- **IfIector is instance based rather than a static class.** You may have as many injector objects as you like and they will all operate independently of each other.
- **Advanced Features.** Such as generics and instance injection. 
- **Auto-Injection Capabilities.** There is a lot that can be said here, but the key features are:
> - Auto-binding / auto-resolve implementations to interfaces.
> - Auto-wiring private fields.
> - Auto-wiring attributes, including attributes with private 'set' methods.