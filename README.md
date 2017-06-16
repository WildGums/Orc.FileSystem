Orc.FileSystem
==============

Name|Badge
---|---
Chat|[![Join the chat at https://gitter.im/WildGums/Orc.FileSystem](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/WildGums/Orc.FileSystem?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
Downloads|![NuGet downloads](https://img.shields.io/nuget/dt/orc.filesystem.svg)
Stable version|![Version](https://img.shields.io/nuget/v/orc.filesystem.svg)
Unstable version|![Pre-release version](https://img.shields.io/nuget/vpre/orc.filesystem.svg)

This library wraps file system methods inside services. The advantages are:

- All operations are being logged and can easily be accessed (even in production scenarios)
- All operations are wrapped inside try/catch so all failures are logged as well
- Services allow easier mocking for unit tests

For documentation, please visit the [documentation portal](http://opensource.wildgums.com)