# CuratorIkvm

[Apache Curator](http://curator.apache.org/ "Apache Curator") is an excellent client library for Zookeeper, however it is written in Java for the JVM.

Fortunately, by virtue of the miraculous [IKVM project](http://www.ikvm.net/ "The IKVM Project") the Curator library can be made available to .NET projects. This repository contains the code necessary to create Curator.dll and package it for Nuget.

#### Curator Recipes
This DLL includes the [Curator Recipes](http://curator.apache.org/curator-recipes/index.html "Curator Recipes"), e.g. Leader Election, Shared Lock, NodeCache. It also includes the curator-testing module.

#### To Build
In order to start the build and packaging:

    $ build.cmd // on windows    
    $ build.sh  // on mono
    
#### Background
We wanted to automate Blue/Green deployment and failover of our services, so leader election was required. The LeaderLatch recipe has been working very well for us, but be advised that we haven't used all the Curator features or its recipes.

This project uses the [F# Project Scaffold](http://fsprojects.github.io/ProjectScaffold/release-process.html "F# Project Scaffold").

** The [NuGet package](https://www.nuget.org/packages/CuratorIkvm/ "CuratorIkvm NuGet package") is here.
