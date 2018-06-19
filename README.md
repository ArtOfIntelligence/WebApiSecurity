# WebApiSecurity 
#### For ASP.NET Web API 2
## Goal
Secure calls to ASP.NET Web API 2 controllers, using expiring tokens over HTTP `Authorization` request header. 

_Contact me [here](https://github.com/ArtOfIntelligence/WebApiSecurity/issues/1)_.

## Content
* [Introduction](#introduction)
* [How Does It Work?](#how-does-it-work) 
* [Getting Started](#getting-started)
	*  [Prerequisites](#prerequisites)
	*  [Installation](#installation)
*  [Usage](#usage)
	*  [Server Preparation](#server-preparation)
	*  [Client Authentication & Calls](#client-authentication--calls)
	*  [Using Library in .NET Client Application](#using-library-in-net-client-application)
*  [Dependencies](#dependencies)
*  [Contribution](#contribution)
*  [Contact](#contact)
*  [License](#license)
*  [References](#references)



## Introduction
This is an advanced implementation of the [Authentication Filter\Attribute](https://docs.microsoft.com/en-us/previous-versions/aspnet/web-frameworks/dn202087%28v=vs.118%29) and [Authorization Attribute](https://docs.microsoft.com/en-us/previous-versions/aspnet/web-frameworks/hh834194%28v=vs.118%29). By plugging this library into your ASP.NET Web API 2 server project you can easily control:
1. Authenticating API clients using tokens, and,
2. Authorizing API methods execution based on user **Roles**.

The library uses a stateless mechanism, so no session variables needed on the server at all.

The library uses AES 256 bit encryption our other library `ArtOfIntelligence.Cryptography`, yet you can trace and change this to any engine with some effort.

Additionally, if your API client is .NET based, you may use the wrapped client classes to speed up the authentication and authorization process.  

## How Does It Work?
After implementing this library into your project it should behave as following:
1. Server: Application start and configuration is passed to library *(explained below)*
2. Client: request a valid `Token`: 
	- Client requests a `Challenge` using provided `ClientId`
	- Server provide an encrypted `Challenge` string 
	- Client decrypts `Challenge` and generates a `Soluion`
	- Client send `Solution` to server and asks for a valid `Token`
3. Client: makes a call to any target method in any controller on your server with Authentication and Authorization attributes, and passes the HTTP **`Authorization`** request header (using `Token` data as credentials)
4. Server: Passes the request to Authentication filter:
	- If `Token` is valid, filters allows request
	- If `Token` is invalid or expired, server terminates request and sends ([401 unauthorized](https://httpstatuses.com/401))  
5. Server: If target method (or its controller) implements `Authorization` attribute with role(s), it will pass the request to the attribute to verify that client is authorized under that role:
	- If authorized, request is delivered to target method for execution
	- If not authorized, server terminates request and sends ([401 unauthorized](https://httpstatuses.com/401))  

> Contribution Needed:  A clean flowchart explaining the above.

## Getting Started
I estimate around 2 hours to implement this library if prerequisites are met. And trust me, this is way faster than the 6 days it took me to learn, build and debug.  
### Prerequisites


1. ASP.NET Web API 2 project
2. A list of client entities (records) in your database for example, with the following fields: 
	- **Client Id `string`**
	(eg: `User001`, `ABC-123`, `545A52B2E`)
	* **Client Secret `string`** (32 characters - 256 bit)
	(eg: `VtsdVxTj8LERXBzByKd178R8Af0rFyV5`)	
	* **Roles `string array`**
	(eg: `["Administrators", "Operators"]`)		
4. Good understanding of your .NET language (eg: VB.NET/C#)
5. LINQ would also help


### Installation
`ArtOfIntelligence.WebApiSecurity` library for .NET  is available on NuGet: 
```
Install-Package ArtOfIntelligence.WebApiSecurity
```
NuGet Link: [https://www.nuget.org/packages/ArtOfIntelligence.WebApiSecurity/](https://www.nuget.org/packages/ArtOfIntelligence.WebApiSecurity/)
## Usage
### Server Preparation
| Step| Purpose |
| ------------- |-------------|
| 1. Server Configuration | Provide security and authentication settings to library
| 2. Create Authentication Controller      | Expose authentication methods vie Web API  
| 3. Apply Authentication Filter Attribute(s) | Forcing authentication on your API controllers  
| 4. Apply Authorization Attribute(s) | Forcing authorization on your API controllers and/or methods  
 ### Client Authentication & Calls
 For the API client to get authenticated and be able to make calls, a number of steps need to be executed. They are explained in the table below, but before you get scared and run away, **I have encapsulated the client functionality in this library**, so if your client app is built with .NET you will need to add just 2 lines of code. 
 > Contribution Needed: Encapsulating client functionality in other languages (I will work on JavaScript/TypeScript version soon, but if you can you are welcome).

| Step| Purpose |
| ------------- |-------------|
| 1. Request Challenge | Get a challenge from server to begin authentication |
| 2. Decrypt Challenge | Provide solution to challenge and request token _(Using AES encryption)_ | 
| 3. Request Token  | Receive token to use with API calls | 
| 4. Make API Calls  |  Use server functions :) | 

#### Using Library in .NET Client Application
If your client is built with .NET, you will proceed as following:

| Step| Purpose |
| ------------- |-------------|
| 1. Client Configuration | Provide security, authentication and server URL and other settings to library|
| 2. Add Authorization Header to Requests | Provide your credentials to API server |

## Dependencies 
Nuget takes care of adding library dependencies. The following libraries are required:
* ArtOfIntelligence.Cryptography (for AES Encryption)
* ArtOfIntelligence.Util (few Helpers)
* Microsoft.AspNet.WebApi
* Microsoft.AspNet.WebApi.Client
* Microsoft.AspNet.WebApi.Core 
* Microsoft.AspNet.WebApi.WebHost
* Newtonsoft.Json (for Serialization)
* System.Web.Http.Common


## Contribution  
* This is the first time I publish open source since 1999 on planet-source-code.com, I just learned the basics of contributing to NuGet and GitHub, help me make this better
* This library is fully functioning for my needs, yet there are a lot of areas in which you can help in, if you are interested please contact me 
## Contact 
I created an issue [here: "General Discussions"](https://github.com/ArtOfIntelligence/WebApiSecurity/issues/1). (I hope that's the write way to do it in GitHub). 

## Next *(Future features)*
* Create JavaScript/TypeScript client library to encapsulate authentication process
* Compose a more detailed documentation with examples for this library (if I see any demand as you can imagine how time consuming this is)
> Contribution is welcome, really....

## Author

**Jack Alexander (*Taher*)** 
*Business Solution Architect* @ Art of Intelligence - Dubai
## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE) file for details

## Acknowledgments

* *We are standing on the shoulders of giants.*


## References
Microsoft's example
 [Authentication Filters in ASP.NET Web API 2](https://docs.microsoft.com/en-us/aspnet/web-api/overview/security/authentication-filters) **(docs.microsoft.com)**

Understand the basics of HTTP authentication 
[The general HTTP authentication framework](https://developer.mozilla.org/en-US/docs/Web/HTTP/Authentication) **(developer.mozilla.org)**
