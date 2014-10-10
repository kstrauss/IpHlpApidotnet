IpHlpApidotnet
=================

This is a wrapper to IpHelperAPI.dll.
Original lib taking from [here](http://www.codeproject.com/Articles/14423/Getting-the-active-TCP-UDP-connections-using-the-G).
This library provides many things that are available from netstat, but callable by c# code.

An example of usage:
```c#
var listeners = IpHlpApidotnet.NetStatFunctions.GetTcpConnections();
listeners.Where (t => t.ProcessName.Contains("Event")).Dump("Eventstore processes");
listeners.Where (l => l.Local.Port == 2113 && l.State != "TIME_WAIT").Dump("Eventstore Http Port");
```
