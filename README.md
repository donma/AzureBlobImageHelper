# AzureBlobImageHelper

Tutorial
----

#### Init Azure Blob ContainerInfo
```C#

            var containerInfo = new AzureBlobImageHelper.ContainerInfo("your_azure_storage_connectionstring", "blob_container_name");

```

#### Upload Image From Local File.
```C#
            var agent = new AzureBlobImageHelper.Agent(containerInfo);
            Console.WriteLine("Test Upload Image From File.");
            agent.UpoloadImageToSource("sample4", AppDomain.CurrentDomain.BaseDirectory + "sample.png", "PROJECT1");
            
```
