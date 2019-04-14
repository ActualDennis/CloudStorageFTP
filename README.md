# "DenCloud" Library
This is simple "Cloud-storage" implementation using File Transfer Protocol, which is meant to be run on <code>one</code> machine.

<strong>Warning: </strong>Current version doesn't support SSL certificates passwords. To use SSL, just make sure your certificate does not have a password and is installed on your machine.

<strong>Tip:</strong> If you're planning on handling big amount of users using this program, you should make sure that port range difference(<code>MaxPort - MinPort</code>) is more than <code><strong>10</strong></code>

<strong>API Usage:</strong>
As a rule, you must use DI pattern in this API(<code>DenInject</code>), which leads us to:
1. Registering dependencies:
  ```csharp
var configBuilder = new DiConfigBuilder();

configBuilder.UseNeccessaryClasses();

configBuilder.UseFileSystem();

//using default implementation.
configBuilder.UseAuthentication();

//custom logger
configBuilder.UseLogger<InterfaceLogger>();

//Dependency provider is ready.
DiContainer.Construct(configBuilder);
```
 2.Validation of your provider before starting the server as:
  ```csharp
   DiContainer.ValidateProvider();
 ```
 3. Resolving Ftp Server dependency, i.e:
   ```csharp
    var server = DiContainer.Provider.Resolve<FtpServer>();
   ```
 4. Starting the server:
   ```csharp
    Task.Run(() => server.Start(IsEncryptionUsed));
   ```
 For any other questions, you can browse [this](https://github.com/ActualDennis/DenCloud.WPF) repo, which itself is an example of API usage.

    
