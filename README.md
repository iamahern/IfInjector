# IfFastInjector v0.1

This project is a fork of the https://ffastinjector.codeplex.com/ library by David Walker (https://plus.google.com/109602553530284384616/).

The goal of this fork is to convert the libary into a proper instance-based injector object as well as to add Ninjit style injection annotations.

For the present time, Multi and Map binders are not a goal.

# Usage

Usage is similar to ffastinjector. To custruct a new injector:

var injector = IfFastInjector.IfInjector.NewInstance();

The guide on this blog post (http://coding.grax.com/2013/05/fFastInjector-Initial.html) is then valid, changing the examples from the static 'Injector' class, to instead refer to the instance object. Time permitting, better docs should follow.


# LICENSE

This source is under the BSD license (http://ffastinjector.codeplex.com/license).
