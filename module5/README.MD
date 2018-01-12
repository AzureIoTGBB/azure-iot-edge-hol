# Azure IoT Edge Hands On Labs - Module 5

Created and maintained by the Microsoft Azure IoT Global Black Belts

## Introduction

For this step of the lab, we are going to create an IoT Edge module that will respond to the "High Temperature Alert" message generated from our Azure Stream Analytics on the Edge module in module 4. This module will take that alert, and send a Direct Method call to our device to tell it to turn on or off a "high temperature" alert LED.  This is a simple example, but demonstrates communicating back to a device from a module.  More importantly, along with module 4, it shows an example of how to take local "action" on the Edge in situations where you need low latency processing and action on the edge.

We will develop our module in C# using .NET Core.   .NET Core is the cross-platform version of Microsoft's .NET framework.  The module and image we develop in this section can be used, unchanged, on x86/x64 based Linux distributions, and can be cross compiled for ARM distributions as well.

We are primarily using C# and .NET Core for this lab because it is the most completely 'ready' language in terms of tooling at the time the labs are being written (during public preview).  However, by General Availability time for IoT Edge, you will be able to develop IoT Edge modules in any supported Azure IoT language, including C, C#, Java, Node, and Python.

## Developing our module

### Creating the module "skeleton"

We will use VS Code to develop our module.  This process will be very similar to the actions taken in Module 3

* in VS Code, click on File -> Open Folder and navigate to the c:\edge folder we created before
* hit CTRL and + to open the Integrated Terminal window in VS Code.
* We will create the "scaffolding" for our IoT Edge module using the IoT Edge module template provided by the IoT Edge product team and previously installed in module 3.  In the integrated terminal, run the following command
```
dotnet new aziotedgemodule -n AlertModule
```

### Modify the sample implementation

Now lets modify the sample code to implement our Alert module.  

* in addition to the "Device Client" that other modules use, the Alerts sample also needs to use the "Service Client" to send Direct Method calls.  To add the service client, open up the alertModule.csproj file in VS Code and add the following line in the \<ItemGroup> section of the file and save the changes.

```
    <PackageReference Include="Microsoft.Azure.Devices" Version="1.5.0-preview-001" />
```

* In the Integrated Terminal window, run the command 'dotnet restore' to download and install the new Nuget package

* in the "using" section above the Program class, add the two following 'using' statements

```CSharp
using Newtonsoft.Json;
using Microsoft.Azure.Devices;
```

* in Program.cs, above the "Program" class, add the C# class that will represent our message we want to publish to the Hub

```CSharp
    class Alert
    {
        public string deviceID { get; set; }
        public string tempState { get; set; }
        public float temperature { get; set; }
        public DateTime eventdatetime { get; set; }
    }
```

* __**Temporary workaround**__:   As of the public preview timeframe, there is a bug in the authentication logic of the edgeHub that doesn't allow us to send Direct Method calls using the module-specific connection string that is passed into the module in the EdgeHubConnectionString environment variable.  Therefore, we temporarily need to leverage the "iothubowner" level connection string when we create the ServiceClient from which we'll make the Direct Method call.  This bug will be fixed before general availability of the product.  To implement this temporary fix:
    * at the top of the "Program" class (just below the line that reads "static int counter"), add the following code

    ```CSharp
    static ServiceClient ioTServiceClient;
    static string serviceConnStr = "<iothubowner connection string>";
    ```

    where \<iothubowner connection string> is the IoT Hub level connection string you copied earlier
    * In the Init function, make the two following changes:

        * in the line that creates the MqttTransportSettings object, add Microsoft.Azure.Devices.Client in front of TransportType.Mqtt_Tcp_Only, as shown here

        ```CSharp
            MqttTransportSettings mqttSetting = new MqttTransportSettings(Microsoft.Azure.Devices.Client.TransportType.Mqtt_Tcp_Only);

        ```
    
        * right below the call to DeviceClient.CreateFromConnectionString, add this call

        ```CSharp
        ioTServiceClient = ServiceClient.CreateFromConnectionString(serviceConnStr);
        ```

* the balance of our work will be in the PipeMessage function.  the top part of the function, which gets the "Device Client" instance that is stored in Context and makes sure it's valid, and that opens the message and gets it's content as a string, is boiler-plate and is fine for our purposes.  

* In the definition of the PipeMessage function, we need to add Microsoft.Azure.Devices.Client to the definition of the message parameter, like this:

    ```CSharp
    static async Task<MessageResponse> PipeMessage(Microsoft.Azure.Devices.Client.Message message, object userContext)

    ```

The rest of the work we will be doing is inside the "if" block below (which checks to make sure we don't have an empty message):

```CSharp
    if (!string.IsNullOrEmpty(messageString))
    {
    }
```

* replace the code within the if block above, with the below code

```CSharp
string command = "";

Console.WriteLine($"Received message, body: [{messageString}]");

var alertMsg = JsonConvert.DeserializeObject<Alert[]>(messageString)[0];

if(alertMsg.tempState == "HIGH")
    command = "ON";
else
    command = "OFF";

Console.WriteLine($"Invoking Direct Method to {alertMsg.deviceID} for tempState={alertMsg.tempState}");

try {
    var result = await ioTServiceClient.InvokeDeviceMethodAsync(alertMsg.deviceID, new CloudToDeviceMethod(command));
    Console.WriteLine($"Result status: {result.Status}");
}
catch(Exception e)
{
    Console.WriteLine($"Exception caught while invoking DM: {e.ToString()}");
}
```

* the code above does the following things
    * Receives the "Alert" message passed to use from Stream Analytics on the Edge
    * parses the JSON message into an Alert object (technically, ASA passes us a JSON array of only one element, so we take the 'first' element)
    * pulls out the device ID of the device that caused the alert, and figures out, based on the 'tempstate' (HIGH or LOW) whether to send the "ON" or "OFF" command to the device
    * using the ServiceClient object, makes the Direct Method call.

* in VS Code, on the code tree, right click on the AlertModule.csproj file and click "Build IoT Edge Module".  This task builds the module, and then copies the executable and it's dependencies into a folder from which we can build the docker image to deploy it in the next step.

## Deploying our module

Now that our module is created, we need to deploy it to our Edge device and get our messages routed through it.

The first step is to create our docker image.  Luckily the VS Code template makes that easy for us by creating the necessary DockerFile to create our image.  Since we are running linux containers on our Windows hosts, we need to build the Linux version of the image.

* In VS Code, expand the Docker folder, and expand the Linux-x64 folder.  
* Right-click on the DockerFile and click on "Build IoT Edge module Docker image"
* in the pop-up folder dialog, navigate to the './bin/Debug/netcoreapp2.0/publish' folder under your project and click the "Select folder as EXE_DIR" button
* in the pop-up text box at the top of VS Code, enter an image name.  You should start it with the name of the docker hub user id you created earlier.  For example:

```
<docker_user_name>/alertModule:latest
```

* hit enter to build your image

Once the image is build, we want to upload it to docker so the edge Agent can pull it.   Technically, we don't have to do this, since edge is running on the same box in which we just created the image (i.e. it's already "local"), but it's a good habit to get into since you'll need to do it when Edge isn't running local to your development box

the first step is to log into the docker hub.  Docker hub will let you download images anonymously, but you need to be authenticated to upload images.

* From the Integrated Terminal in VS Code, enter the following command:

```
docker login -u <username> -p <password>
```

where "username" and "password" are the credentials you used earlier when you created your docker hub account.

* Once succesfully authenticated to docker, in VS Code, click View->Command Pallete and search for the Edge: Push IoT Edge module Docker Image command.
* In the pop-up text box at the top of VS Code, enter the same image name you used above


## Deploy Edge module

In this section, we will get the module created above deployed and view the results.

* in the Azure portal, navigate to your IoT Hub, click on IoT Edge Devices (preview) on the left nav, click on your IoT Edge device
* click on "Set Modules" in the top menu bar.  
* In the Set Modules blade, click on "Add IoT Edge Module"
    * In the "IoT Edge Modules" dialog, give your module a name (for example:  AlertModule).  Remember the name you used, including the proper 'case' of the letters, as we'll need that when we set the routes in the next step.
    * in the image URI box, put in the exact same image name you used in the previous step (e.g. <docker hub id>/alertModule:latest)
    * leave the other defaults and click "Save"
* back on the "Set Modules" blade, click "next"
* on the "specify routes" blade, replace the default with the following:

```json
{
  "routes": {
    "toFormatterFromDevices": "FROM /messages/* WHERE NOT IS_DEFINED($connectionModuleId) INTO BrokeredEndpoint(\"/modules/formattermodule/inputs/input1\")",
    "toASAJob": "FROM /messages/modules/formattermodule/outputs/output1 INTO BrokeredEndpoint(\"/modules/edgeASAJob/inputs/inputFromHub\")",
    "toAlerts": "FROM /messages/modules/edgeASAJob/outputs/asaAlertTemp INTO BrokeredEndpoint(\"/modules/alertmodule/inputs/input1\")",
    "asaToIoTHub": "FROM /messages/modules/edgeASAJob/outputs/asaAggregatedData INTO $upstream",
    "asaAlertsToIoTHub": "FROM /messages/modules/edgeASAJob/outputs/asaAlertTemp INTO $upstream"
  }
}
```
* replace "\<module name>" above (in two places) with the name of your module, case-sensitive, that you used above

    * the first route above, takes any message that does not come from a "module" and routes it into the input of our Formatter Module.  In other words, it takes messages that comes from downstream IoT devices and routes them into our formatter module.  It also keeps messages that comes from our formatter module from being routed back into itself.
    * the second route takes the output from our Formatter Module and routes it up to IoT Hub in the cloud

* Click "next" and then "finish" in the Azure portal

### Test our module

After a few seconds, the module should be downloaded and deployed to our IoT Edge runtime.  You can confirm this by opening a command prompt and typing "docker ps".  You should see all of the previous modules running, the edgeAgent, edgeHub, the formatterModule, the ASA module and our new AlertModule.  You can view the logs of any of them by running "docker logs -f \<module name>" to make sure they are working.

As in the previous module, start the python script that represents our IoT Device to get messages flowing into the system. 

Once that is running, you can use "docker logs -f alertModule" (or whatever you named your module) to see its logs.  You won't see anything yet.

Now hold your finger on the DHT22 temperature sensor to drive temp above 80.  Once it crosses above 80, you should see the Alert come through the docker logs for the alertModule, the DM call come through in the debug output of the python IoT Device, and the onboard LED on the Arduino, after a few seconds, light up.  If you release the temperature sensor and the temperature drops back below 80, you should see the process repeat to turn the LED back off.

In VS Code, under the "IOT HUB DEVICES" section, like before, you should be able to right click on your __**IoT Edge device**__ (note, this is different than before), and choose "Start Monitoring D2C Messages" and see the alert messages flow through to IoT Hub as well if you repeat the process.

Congratulations - you now have:
* data coming from a physical IoT device flowing into IoT Edge
* an Edge module that intercepts and reformats that data
* Streaming Analytics on the edge that pre-aggregates the data as well as generates "local" alerts when a dangerous high temperature situation occurs
* a module that takes the 'high temp' alerts and uses them to invoke an action on the IoT Device in question.
