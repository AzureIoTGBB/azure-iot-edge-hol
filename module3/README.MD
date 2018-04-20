# Azure IoT Edge Hands On Labs - Module 3

Created and maintained by the Microsoft Azure IoT Global Black Belts

## Introduction

For this step of the lab, we are going to create an IoT Edge module that take the raw CSV format provided by our IoT Device, converts it to JSON, and then puts the messages back on the edge Hub for further processing.

We will develop our module in C# using .NET Core.   .NET Core is the cross-platform version of Microsoft's .NET framework.  The module and image we develop in this section can be used, unchanged, on x86/x64 based Linux distributions, and can be cross compiled for ARM distributions as well.

We are primarily using C# and .NET Core for this lab because it is the most completely 'ready' language in terms of tooling at the time the labs are being written (during public preview).  However, by General Availability time for IoT Edge, you will be able to develop IoT Edge modules in any supported Azure IoT language, including C, C#, Java, Node, and Python.

## Developing our module

### Install the following VS Code Extensions

Click on the "extensions" tab in VS Code and install the following:

#### Azure IoT Edge extention

Search for the "Azure IoT Edge" extension by Microsoft.  Install it.

#### C# for Visual Studio Code 

Search for "C# for Visual Studio Code (powered by OmniSharp)" extension by Microsoft.  Install it.

#### Docker

Search for "Docker" extension by Microsoft.  Install it.

#### Restart

Reload VS Code.

### Creating the module "skeleton"

We will use VS Code to develop our module.  

* in VS Code, click on File -> Open Folder and navigate to the c:\edge folder we created before
* hit CTRL and + ` to open the Integrated Terminal window in VS Code.
* We will create the "scaffolding" for our IoT Edge module using the IoT Edge module template provided by the IoT Edge product team.  To install it, in the integrated terminal, run the following command:

```
dotnet new -i Microsoft.Azure.IoT.Edge.Module
```
* now we are ready to create our project.  in the integrated terminal, run the following command  (where \<docker_user_name> is the user name for Docker Hub that you created earlier)
```
dotnet new aziotedgemodule -n FormatterModule -r <docker_user_name>/formattermodule
```
* Open the Program.cs file to view the code.  Review the boiler-plate code to understand what it does.  A few notes:
    * The main funtion retrieves the 'connection string' for the module from an environment variable passed into it's docker container by the edge Agent when it creates the container.  This connection string is for the module to connect to the local edge Hub instance.  Main called "Init" to initiate the connection to the local Hub and set up any callbacks.
    * The edge Agent also passes in the necessary cert for a connection to the edge Hub that the module must 'trust' before it can connect (for TLS).  This cert is retreived and installed by the InstallCerts method
    * the Init method creates an instance of the Device Client class (which is the exact same one you would use to connect to IoT Hub in the cloud) and connects using the connection string passed in from the egde Agent.  The Init method then sets any callbacks that will be raised whenever a message is routed to this module (on a named "input") or when module-twin changes happen
    * The PipeMessage function is an example implementation of a "listener" for messages to be routed to this module.  In the sample implementation, we are basically getting the content of the message and any user-defined meta-data properties, and echoing them back on a new message.  We are then marking the current message as "complete" so the edge runtime knows we are done with it.  We will be modifying this sample method to implement our message 'formatter'

### Modify the sample implementation

Now lets modify the sample code to implement our Formatter module.  

* in Program.cs, in the "using" section above the Program class, add the two following 'using' statements

```CSharp
using Newtonsoft.Json;
```

* above the "Program" class, add the C# class that will represent our message we want to publish to the Hub

```CSharp
class Telemetry
{
    public string deviceID;
    public float temperature;
    public float humidity;
}
```

> Note:  there is a temporary bug in the certificate validation code for IoT Edge.  The below code change is necessary to work around that bug

* Temporarily we will be bypassing certicate validation in our module.  So do so, locate this line of code at the top of the "Main" function

```CSharp
bool bypassCertVerification = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
```

and replace it with

```CSharp
bool bypassCertVerification = true;
```

* the balance of our work will be in the PipeMessage function.  the top part of the function, which gets the "Device Client" instance that is stored in Context and makes sure it's valid, and that opens the message and gets it's content as a string, is boiler-plate and is fine for our purposes.  The work we will be doing is inside the "if" block below (which checks to make sure we don't have an empty message):

```CSharp
if (!string.IsNullOrEmpty(messageString))
{
    .....
}
```

* replace the code within the ````if```` block above, with the below code

```CSharp
// if our message isn't 11 characters long (xx.xx,yy.yy) or a comma in the 6th position
// then it's not a message we are interested in.
if((!(messageString.Length == 11)) || !(messageString.Substring(5,1) == ","))
{
    Console.WriteLine("Not a message that interests this module.");
    return MessageResponse.Completed;
}

// split the CSV message into its two parts
// humidity is first, temp second
string[] parts = messageString.Split(",");

// create and populate our new message content
Telemetry t = new Telemetry();
t.deviceID = message.ConnectionDeviceId;
t.humidity = float.Parse(parts[0]);
t.temperature = float.Parse(parts[1]);

// serialize to a string
string newMessage = JsonConvert.SerializeObject(t);

// create a new IoT Message object and copy
// any properties from the original message
var pipeMessage = new Message(Encoding.ASCII.GetBytes(newMessage));
foreach (var prop in message.Properties)
{
    pipeMessage.Properties.Add(prop.Key, prop.Value);
}

// send the data to the edge Hub on a named output (for routing)
await deviceClient.SendEventAsync("output1", pipeMessage);
Console.WriteLine($"Converted message sent({counter}): {newMessage}");
```

* the code above does the following things
    * some rudimentary checking to make sure we have a message in the right format
    * disassembles the humidity and temp values from the CSV input
    * creates a new message using these values and serializes as JSON
    * copies any user-defined metadata properties from the original input message to the new message
    * send the message back out to the hub on named output
    * marks the original message as "complete" so that the Hub knows we successfully processed it and it does not need to be re-sent

* Save your changes to the Program.cs file

## Deploying our module

Now that our module is created, we need to deploy it to our Edge device and get our messages routed through it.

### Log into Docker

The first step is to log into Docker in the Terminal window.

````
docker login -u <Docker username> -p <Docker password>
````

### Upload the image to Docker

In VS Code explorer:

* Right-click the module.json file and click "Build and Push IoT Edge module Docker Image". 
* In the pop-up dropdown box at the top of the VS Code window, select 'amd64' (Linux container). 

VS Code then builds your code, containerize the ````FormatterModule.dll```` and push it to the container registry you specified.

Our module is now in Docker with the tag ````0.0.1-amd64````


## Deploy Edge module

In this section, we will get the module created above deployed and view the results.

* in the Azure portal, navigate to your IoT Hub, click on IoT Edge Devices (preview) on the left nav, click on your IoT Edge device
* click on "Set Modules" in the top menu bar.  
* In the Set Modules blade, click on "Add IoT Edge Module"
    * In the "IoT Edge Modules" dialog, give your module a name (for example:  formattermodule).  Remember the name you used, including the proper 'case' of the letters, as we'll need that when we set the routes in the next step.
    * in the image URI box, put in the exact same image name you used in the previous step (e.g. \<docker hub id>/formattermodule:0.0.1-amd64)
    * leave the other defaults and click "Save"
* back on the "Set Modules" blade, click "next"
* on the "specify routes" blade, replace the default with the following:

```
{
    "routes": {
        "toFormatterFromDevices": "FROM /messages/* WHERE NOT IS_DEFINED($connectionModuleId) INTO BrokeredEndpoint(\"/modules/formattermodule/inputs/input1\")",
        "toIoTHub": "FROM /messages/modules/formattermodule/outputs/output1 INTO $upstream"
    }
}
```
* replace "formattermodule" above (in two places) with the name of your module, case-sensitive, that you used above if different than 'formattermodule'

    * the first route above, takes any message that does not come from a "module" and routes it into the input of our Formatter Module.  In other words, it takes messages that comes from downstream IoT devices and routes them into our formatter module.  It also keeps messages that comes from our formatter module from being routed back into itself.
    * the second route takes the output from our Formatter Module and routes it up to IoT Hub in the cloud

* Click "next" and then "finish" in the Azure portal

### Test our module

After a few seconds, the module should be downloaded and deployed to our IoT Edge runtime.  You can confirm this by opening a command prompt and typing "docker ps".  You should see three modules running, the edgeAgent, edgeHub, and our new formatterModule.  You can view the logs of any of them by running "docker logs -f \<module name>" to make sure they are working.

As in the previous module, start the python script (or .NET Core app, if you did that option) that represents our IoT Device to get messages flowing into our formatter module.

Once that is running, you can use "docker logs -f formatterModule" (or whatever you named your module) to see its logs.  You should see debug output indicating that it is receiving the CSV input, and outputing our new JSON module.

In VS Code, under the "IOT HUB DEVICES" section, like before, you should be able to click on your __**IoT Edge device**__ (note, this is different than before), and choose "Start Monitoring D2C Messages" and see our new JSON formatted messages flowing.

__**  NOTE:  Now that we have an edge module "in between" our IoT device and IoT Hub in the cloud, the messages no longer appears as though it comes from the original IoT Device, but rather from our Edge device now. This is an important distinction.  This is one of the reasons also, that our Formatter Module snags the original deviceID and embeds it in our message (we could have also put it in the user-defined metadata for the message).  Otherwise, the original device origin would be lost.**__

Stop the python script (CTRL-C) (or hit \<enter> if you used the .NET Core app) while we work on the next module

Ok - now we have messages flowing from our IoT Device and being translated to JSON and sent to IoT Hub.  However, the messages are flowing at the original high rate of our "dumb" IoTHub device.  Now we are going to add the capability to aggregate the data at the edge, as well as determine when a "high temperature" alert condition occurs (module 4) and take local action (module 5).

To continue with module 4, click [here](/module4)
