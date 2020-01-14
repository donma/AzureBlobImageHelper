# AzureBlobImageHelper

Tutorial
----

一套 library 可以讓你無 server 配合 Mircosoft Azure Storage 做出一套 Image Service ，因為這是 library 版本 ，如果你是想要直接使用 Server 版本，你可以到這裡 https://github.com/donma/N2ImageAgent.AzureBlob2020

Happy Coding :)

#### Init Azure Blob ContainerInfo
```C#

            var containerInfo = new AzureBlobImageHelper
                                .ContainerInfo("your_azure_storage_connectionstring", "blob_container_name");

```

#### Upload Image From Local File.
```C#
            var agent = new AzureBlobImageHelper.Agent(containerInfo);
            Console.WriteLine("Test Upload Image From File.");
            agent.UpoloadImageToSource("sample4", 
                                       AppDomain.CurrentDomain.BaseDirectory + "sample4.jpg", "PROJECT1");
            
```

#### Upload Image From File Byte[]

```C#
            var agent = new AzureBlobImageHelper.Agent(containerInfo);
            var byteData = System.IO.File.ReadAllBytes(AppDomain.CurrentDomain.BaseDirectory + "sample4.jpg");
            agent.UpoloadImageToSource("sample4", byteData, "PROJECT1");

```

#### Get Image Info By Utility

```C#
            var agent = new AzureBlobImageHelper.Agent(containerInfo);
            var imageInfo = AzureBlobImageHelper.Utility.
                GetImageInfo(AppDomain.CurrentDomain.BaseDirectory + "sample4.jpg", "sample4", "TAGNAME");
            Console.WriteLine(JsonConvert.SerializeObject(imageInfo));

```

#### Upload  Image Info To Blob

```C#
            var agent = new AzureBlobImageHelper.Agent(containerInfo);
            var imageInfo = AzureBlobImageHelper.Utility
                            .GetImageInfo(AppDomain.CurrentDomain.BaseDirectory + "sample4.jpg", "sample4", "TAGNAME");
            agent.UpoloadImageInfo(imageInfo, "PROJECT1");

```

#### Delete All Thumb By Id

```C#
            var agent = new AzureBlobImageHelper.Agent(containerInfo);
            agent.DeleteAllThumbById("sample4", "PROJECT1");

```

#### Delete Image Info By Id

```C#
            var agent = new AzureBlobImageHelper.Agent(containerInfo);
            agent.DeleteInfoById("sample4", "PROJECT1");

```

#### Delete Source Image

```C#
            var agent = new AzureBlobImageHelper.Agent(containerInfo);
            agent.DeleteImageFromSource("sample4", "PROJECT1");

```

#### Is Image Source Exist

```C#
            var agent = new AzureBlobImageHelper.Agent(containerInfo);
            Console.WriteLine("TEST1's sample4  is Exist : " + agent.IsImageSourceExisted("sample4", "PROJECT1"));

```

#### Is Image Info Exist

```C#
            var agent = new AzureBlobImageHelper.Agent(containerInfo);
            Console.WriteLine("TEST1's sample4 info is Exist : " + agent.IsImageInfoExisted("sample4", "PROJECT1"));

```

#### Is Image Thumb Exist

```C#
            var agent = new AzureBlobImageHelper.Agent(containerInfo);
            Console.WriteLine("TEST1's sample4  Thumb 0x0  is Existed : " + agent.IsImageThumbExisted("sample4", "PROJECT1",0,0));
            Console.WriteLine("TEST1's sample4  Thumb 0x500  is Existed : " + agent.IsImageThumbExisted("sample4", "PROJECT1", 0, 500));

```

#### Download Image Source to Image

```C#
            var agent = new AzureBlobImageHelper.Agent(containerInfo);
            //all save blob file I will rename *.gif , but the contain is origi file format.
            var imageSorce1=agent.DownloadImageFromBlob("sample4"+".gif", "PROJECT1");
            imageSorce1.Save(AppDomain.CurrentDomain.BaseDirectory + "sample4_source.gif");

```

#### Download Image Source to Image

```C#
            var agent = new AzureBlobImageHelper.Agent(containerInfo);
            //live for 20 seconds.
            var res=agent.GetImageThumbFromSource("sample4", "PROJECT1", 0, 100, DateTime.Now.AddSeconds(20));
            Console.WriteLine("the image thumb url is "+res.FullUrl);
```



