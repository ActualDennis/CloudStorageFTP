# CloudStorageFtp
This is simple "Cloud-storage" implementation using File Transfer Protocol, which is meant to be run on <code>one</code> machine.

To start <strong>using</strong> the server, do the following:
    
    1.Clone the repo.
    2.Edit Configuration.xml to meet your needs(if something's unclear, check the comments right to the tag you're stuck with).
    3.Compile it using VS2017/VS2019.
    4.Go the folder where it's compiled(usually bin/debug/*.exe).
    5.Run the exe.
<strong>Warning: </strong>Current version doesn't support SSL certificates passwords. To use SSL, just make sure your certificate does not have a password. 

<strong>Tip:</strong> If you're planning on handling big amount of users using this program, you should make sure that port range difference(<code>MaxPort - MinPort</code>) is more than <code><strong>10</strong></code>

<strong>API Usage:</strong>
As a rule, you must use DI pattern in this API(<code>DenInject</code>), which leads us to:
1. Registering dependencies:
  ```csharp
 var config = new DiConfiguration();
  
 config.RegisterSingleton<FtpServer, FtpServer>();
 config.RegisterSingleton<IAuthenticationProvider, FtpDbAuthenticationProvider>();
  ...
  
 Provider = new DependencyProvider(config);
```
Dependencies provider <strong>must</strong> be set in DiContainer.Provider class <strong>AND</strong> should be later validated as:
  ```csharp
   Provider.ValidateConfig();
 ```
 2. Resolving Ftp Server dependency, i.e:
   ```csharp
    var server = DiContainer.Provider.Resolve<FtpServer>();
   ```
 3. Starting the server:
   ```csharp
    Task.Run(() => server.Start(
        PortReadFromConfigFile, 
        File.Exists(DefaultServerValues.CertificateLocation)));//basic check to see if ssl encryption will be used.
   ```
 For any other questions, you can browse this repo, which itself is an example of API usage.

    
