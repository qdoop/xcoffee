[xcoffee](https://github.com/qdoop/xcoffee) for Visual Studio 2013
=================

[xcoffee](https://github.com/qdoop/xcoffee) extends Visual Studio adding autocomplete Intellisense support for CoffeeScript. 

If you ever write CoffeeScript, will make your life as a developer easier. 

This is for all Web developers using Visual Studio.

Inspired from [Web Essentials](http://vswebessentials.com/). Thank you so much!


##Getting started
To contribute to this project, you'll need to do a few things first:

 1. Fork the project on GitHub
 1. Clone it to your computer
 1. Install the [Visual Studio 2013 SDK](http://www.microsoft.com/visualstudio/eng/downloads#d-vs-sdk).
 1. Open the solution in VS2013.

To install your local fork into your main VS instance, you will first need to open `Source.extension.vsixmanifest` and bump the version number to make it overwrite the (presumably) already-installed production copy. (alternatively, just uninstall `xcoffee` from within VS first)

You can then build the project, then double-click the VSIX file from the bin folder to install it in Visual Studio.


##Useful Links
 - [Web Essentials on Github](https://github.com/madskristensen/WebEssentials2013)
 - [Getting started with Visual Studio extension development](http://blog.slaks.net/2013-10-18/extending-visual-studio-part-1-getting-started/)
 - [Inside the Visual Studio editor](http://msdn.microsoft.com/en-us/library/vstudio/dd885240.aspx)
 - [Extending the Visual Studio editor](http://msdn.microsoft.com/en-us/library/vstudio/dd885244.aspx)
