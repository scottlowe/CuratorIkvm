# CuratorIkvm

Apache Curator ([http://curator.apache.org/](http://curator.apache.org/ "Apache Curator")) is an excellent client library for Zookeeper, however it is written in Java for the JVM.

Fortunately, by virtue of the miraculous IKVM project ([http://www.ikvm.net/](http://www.ikvm.net/ "The IKVM Project")) the Curator library can be made available to .NET projects. This repository contains the code ncessary to create Curator.dll and package it for Nuget.

In order to start the build and packaging:

    $ build.cmd // on windows    
    $ build.sh  // on mono

## Maintainer(s)

- [@scottlowe](https://github.com/scottlowe)
