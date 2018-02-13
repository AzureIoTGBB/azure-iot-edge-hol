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

In the Integrated Terminal Window, to add the new package reference (to the AlertModule.csproj file) and install it, type

```PowerShell
cd AlertModule

dotnet add package Microsoft.Azure.Devices --version 1.16.0-preview-001

dotnet restore
```

* Open the Program.cs file, and in the "using" section above the Program class, add the two following 'using' statements

```CSharp
using Newtonsoft.Json;
using Microsoft.Azure.Devices;
using Message = Microsoft.Azure.Devices.Client.Message;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;
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

* at the top of the "Program" class (just below the line that reads "static int counter"), add the following code to declare an instance of the ServiceClient

```CSharp
static ServiceClient iotServiceClient;
```

* in the Main function, either delete or comment out this line of code:

```CSharp
if (!bypassCertVerification) InstallCert();
```

* In the Init function, right below the call to DeviceClient.CreateFromConnectionString, add this call

```CSharp
iotServiceClient = ServiceClient.CreateFromConnectionString(connectionString);
```

* the balance of our work will be in the PipeMessage function.  the top part of the function, which gets the "Device Client" instance that is stored in Context and makes sure it's valid, and that opens the message and gets it's content as a string, is boiler-plate and is fine for our purposes.  

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
    var result = await iotServiceClient.InvokeDeviceMethodAsync(alertMsg.deviceID, new CloudToDeviceMethod(command));
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

## Customizing module startup

The way the Service Client expects to see and validate certificates is different than the Device-side client that we are used to using with the Device Client.

So as part of the 'startup' of the alertModule, we need to make sure we place the certs used by our Edge runtime in the right place to authenticate edgeHub.  We will do this by running, in our container, a shell script that does the install of the certificate information passed in via environment variables from the Edge runtime.

In VS Code, from the folder view, right click on your AlertModule folder and choose New File.  Name the file 'start.sh'.  Within the start.sh file, copy and paste the following code:

```bash
#!/bin/bash

###############################################################################
# Script to setup a edge module before executing it. The input to this script
# are driven through environament variables
#    Required Environament Variables
#        MODULE_NAME - This is the name of the dotnet DLL to execute
#    Optional Environament Variables
#        EdgeModuleCACertificateFile - This is set by the Edge Agent when
#                                      launching the module and is used for
#                                      TLS validation of the EdgeHub server
###############################################################################

###############################################################################
# Global Script Variables
###############################################################################
# Path on Debian OSes where CA certs are to be copied before they are installed
CA_CERT_PATH="/usr/local/share/ca-certificates/edge-module-ca.crt"

# Module name to be launched
LAUNCH_MODULE_NAME=

###############################################################################
# Helper function to install a CA certificate
# Input:
#   CA certificate file path
#   CA certificate install path
#
# Return Ouput: None
# Globals Updated: None
###############################################################################
function install_ca_cert()
{
    ca_cert_file_path="${1}"
    ca_cert_install_path="${2}"

    # copy the CA cert into the ca cert dir
    command="cp ${ca_cert_file_path} ${ca_cert_install_path}"
    echo "Executing Command:${command}"
    ${command}
    if [ $? -ne 0 ]; then
        echo "Failed to Copy Edge Device CA Certificate."
        exit 1
    fi

    # register the newly added CA cert
    command="update-ca-certificates"
    echo "Executing: ${command}"
    ${command}
    if [ $? -ne 0 ]; then
        echo "Failed to Update CA Certificate."
        exit 1
    fi
    echo "Certificate installed successfully!"

}
###############################################################################
# Validate Environment Variables and Install any CA certificate(s)
#    MODULE_NAME - Edge Module DLL to launch
#    EdgeModuleCACertificateFile - Edge CA Certificate To Install
#
# Return Ouput: None
# Globals Updated: None
###############################################################################
function validate_and_process_env_args()
{
    # make sure MODULE to be launched is provided
    LAUNCH_MODULE_NAME=$(printenv MODULE_NAME)
    if [[ -z "${LAUNCH_MODULE_NAME}" ]]; then
        echo "Module Name Cannot Be Empty"
        exit 1
    fi

    # check if the CA certificate is available and if so install it
    EdgeModuleCACertificateFile=$(printenv EdgeModuleCACertificateFile)
    echo "CA Certificate File: ${EdgeModuleCACertificateFile}"
    # check if the EdgeAgent supplied the module with the Edge Runtime CA cert
    if [[ ! -z "${EdgeModuleCACertificateFile}" ]]; then
        install_ca_cert ${EdgeModuleCACertificateFile} ${CA_CERT_PATH}
    else
        echo "Warning: Edge Hub CA Certificate Not Available! Edge Hub" \
             " Server TLS validation will likely fail."
    fi
}

function launch_module()
{
    local module="${1}"
    echo "Launching Edge Module: ${module}"
    exec dotnet ${module}
}

###############################################################################
# Main Script Execution
###############################################################################
validate_and_process_env_args
launch_module "${LAUNCH_MODULE_NAME}"
```

Save the file (CTRL-S)

Like you saw in Module 2, the template provides a Dockerfile for generating our Docker image.  The default Dockerfile generated by the template just copies over and starts the module directly.  In our case, we need to modify the generated Dockerfile to invoke our start.sh script (which launches the module after installing the certs).  Since we are running linux containers on our Windows hosts, we need to modify the Linux version of the docker image.

In the folder view of VS Code, drill down into the AlertModule -> Docker -> linux_x64 folder and open Dockerfile.

replace the contents of the Dockerfile with the below...  (if you named your module something other than AlertModule, you need to change that name below)

```
FROM microsoft/dotnet:2.0.5-runtime

ENV MODULE_NAME "AlertModule.dll"

ARG EXE_DIR=.

WORKDIR /app

COPY $EXE_DIR/ ./

CMD ["./start.sh"]

```

Save your changes to the Dockerfile  (CTRL-S)

## Deploying our module

Now that our module is created, we need to deploy it to our Edge device and get our messages routed through it.

The first step is to create our docker image with the modified Dockerfile from above.  

* In VS Code, expand the Docker folder, and expand the Linux-x64 folder.  
* Right-click on the DockerFile and click on "Build IoT Edge module Docker image"
* in the pop-up folder dialog, navigate to the './bin/Debug/netcoreapp2.0/publish' folder under your project and click the "Select folder as EXE_DIR" button
* in the pop-up text box at the top of VS Code, enter an image name.  You should start it with the name of the docker hub user id you created earlier.  For example:

```
<docker_user_name>/alertmodule:latest
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
    * in the image URI box, put in the exact same image name you used in the previous step (e.g. <docker hub id>/alertmodule:latest)
    * leave the other defaults and click "Save"
* back on the "Set Modules" blade, click "next"
* on the "specify routes" blade, replace the default with the following:

```json
{
  "routes": {
    "toFormatterFromDevices": "FROM /messages/* WHERE NOT IS_DEFINED($connectionModuleId) INTO BrokeredEndpoint(\"/modules/formattermodule/inputs/input1\")",
    "toASAJob": "FROM /messages/modules/formattermodule/outputs/output1 INTO BrokeredEndpoint(\"/modules/edgeASAJob/inputs/inputFromHub\")",
    "toAlerts": "FROM /messages/modules/edgeASAJob/outputs/asaAlertTemp INTO BrokeredEndpoint(\"/modules/alertmodule/inputs/input1\")",
    "asaToIoTHub": "FROM /messages/modules/edgeASAJob/outputs/asaAggregatedTemp INTO $upstream",
    "asaAlertsToIoTHub": "FROM /messages/modules/edgeASAJob/outputs/asaAlertTemp INTO $upstream"
  }
}
```

* replace "alertmodule" above (in two places) with the name of your module, case-sensitive, that you used above if different from "alertmodule".  Ditto for edgeASAJob and formattermodule

* the first route above, takes any message that does not come from a "module" and routes it into the input of our Formatter Module.  In other words, it takes messages that comes from downstream IoT devices and routes them into our formatter module.  It also keeps messages that comes from our formatter module from being routed back into itself.
* the second route takes the output from our Formatter Module and routes it up to IoT Hub in the cloud

* Click "next" and then "finish" in the Azure portal

### Test our module

After a few seconds, the module should be downloaded and deployed to our IoT Edge runtime.  You can confirm this by opening a command prompt and typing "docker ps".  You should see all of the previous modules running, the edgeAgent, edgeHub, the formatterModule, the ASA module and our new AlertModule.  You can view the logs of any of them by running "docker logs -f \<module name>" to make sure they are working.

As in the previous modules, start the python script (or .NET Core app) that represents our IoT Device to get messages flowing into the system.

Once that is running, you can use "docker logs -f alertModule" (or whatever you named your module) to see its logs.  You won't see anything yet.

Now hold your finger on the DHT22 temperature sensor to drive temp above 80.  Once it crosses above 80, you should see the Alert come through the docker logs for the alertModule, the DM call come through in the debug output of the python (or dotnet) IoT Device, and the onboard LED on the Arduino, after a few seconds, light up.  If you release the temperature sensor and the temperature drops back below 80, you should see the process repeat to turn the LED back off.

In VS Code, under the "IOT HUB DEVICES" section, like before, you should be able to right click on your __**IoT Edge device**__ (note, this is different than before), and choose "Start Monitoring D2C Messages" and see the alert messages flow through to IoT Hub as well if you repeat the process of heating the temperature sensor up.

Congratulations - You are done!  You now have:

* data coming from a physical IoT device flowing into IoT Edge
* an Edge module that intercepts and reformats that data
* Streaming Analytics on the edge that pre-aggregates the data as well as generates "local" alerts when a dangerous high temperature situation occurs
* a module that takes the 'high temp' alerts and uses them to invoke an action on the IoT Device in question.