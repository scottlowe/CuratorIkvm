# CuratorIkvm

[Apache Curator](http://curator.apache.org/ "Apache Curator") is an excellent client library for Zookeeper, however it is written in Java for the JVM.

Fortunately, by virtue of the miraculous [IKVM project](http://www.ikvm.net/ "The IKVM Project") the Curator library can be made available to .NET projects. This repository contains the code necessary to create Curator.dll and package it for Nuget.

#### Curator Recipes
This DLL includes the [Curator Recipes](http://curator.apache.org/curator-recipes/index.html "Curator Recipes"), e.g. Leader Election, Shared Lock, NodeCache. It also includes the curator-testing module.

#### To Build
In order to start the build and packaging:

    $ build.cmd // on windows    
    $ build.sh  // on mono

This project uses the [F# Project Scaffold](http://fsprojects.github.io/ProjectScaffold/release-process.html "F# Project Scaffold").
