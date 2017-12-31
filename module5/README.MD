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

* in AlertModule.cs, above the "Program" class, add the C# class that will represent our message we want to publish to the Hub

```CSharp
    class Alert
    {
        public string deviceID;
        public string tempState;
        public float temperature;
    }
```

* the balance of our work will be in the PipeMessage function.  the top part of the function, which gets the "Device Client" instance that is stored in Context and makes sure it's valid, and that opens the message and gets it's content as a string, is boiler-plate and is fine for our purposes.  The work we will be doing is inside the "if" block below (which checks to make sure we don't have an empty message):

```CSharp
    if (!string.IsNullOrEmpty(messageString))
    {
    }
```

* replace the code within the if block above, with the below code

```CSharp
                // todo:  alert module code goes here
```

* the code above does the following things
    * todo....

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
docker login -u <username> -P <password>
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

```
{
    "routes": {
        todo....
    }
}
```
* replace "\<module name>" above (in two places) with the name of your module, case-sensitive, that you used above

    * the first route above, takes any message that does not come from a "module" and routes it into the input of our Formatter Module.  In other words, it takes messages that comes from downstream IoT devices and routes them into our formatter module.  It also keeps messages that comes from our formatter module from being routed back into itself.
    * the second route takes the output from our Formatter Module and routes it up to IoT Hub in the cloud

* Click "next" and then "finish" in the Azure portal

### Test our module

After a few seconds, the module should be downloaded and deployed to our IoT Edge runtime.  You can confirm this by opening a command prompt and typing "docker ps".  You should see all of the previous modules running, the edgeAgent, edgeHub, the 
ASA module and our new AlertModule.  You can view the logs of any of them by running "docker logs -f \<module name>" to make sure they are working.

As in the previous module, start the python script that represents our IoT Device to get messages flowing into the system. 

Once that is running, you can use "docker logs -f alertModule" (or whatever you named your module) to see its logs.  You won't see anything yet.

todo:  hold finger on the temp sensor to drive temp above 80, blah blah blah

In VS Code, under the "IOT HUB DEVICES" section, like before, you should be able to click on your __**IoT Edge device**__ (note, this is different than before), and choose "Start Monitoring D2C Messages" and see our new JSON formatted messages flowing.
