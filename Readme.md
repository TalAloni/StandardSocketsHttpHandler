StandardSocketsHttpHandler:
===========================
StandardSocketsHttpHandler is a backport of SocketsHttpHandler to .NET Standard 2.0.

## Note:

**HTTP/2 over TLS requires the ALPN TLS extension, accessible in .NET through the SslStream.NegotiatedApplicationProtocol property, which is only available starting from .NET Standard 2.1.**

**In addition, ALPN is only available starting from Windows 8.1.**

Features added to StandardSocketsHttpHandler:
=============================================
• .NET Standard 2.0 compatibility (.NET Framework 4.7.2 is supported)  
• SocketCreated Event - allows configuration of various socket options (TCP Keepalive, etc.)  

Features missing from StandardSocketsHttpHandler:
=============================================
• Windows Authentication (NLTM & Kerberos)  
• Operating System proxy settings detection  
• Brotli compression  

Using StandardSocketsHttpHandler:
=================================
HttpClient client = new HttpClient(new StandardSocketsHttpHandler())  
 
Releases:
=========
Releases are available via [NuGet.org](https://www.nuget.org/packages/StandardSocketsHttpHandler)  

Contact:
========
If you have any question, feel free to contact me.  
Tal Aloni <tal.aloni.il@gmail.com>  
