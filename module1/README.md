# Azure IoT Edge Hands On Labs - Module 1

Created and maintained by the Microsoft Azure IoT Global Black Belts

## Azure Portal Access

Because the Azure Streaming Analytics on Edge feature is in preview, to access the Azure Portal, use this specially formatted URL throughout the labs:

https://portal.azure.com/?Microsoft_Azure_StreamAnalytics_onedge=true

## Create an IoT Hub and an "Edge Device"


You can create an IoT hub using the following methods:

* The + New option opens the blade shown in the following screen shot. The steps for creating the IoT hub through this method and through the marketplace are identical.

* In the Marketplace, choose Create to open the blade shown in the following screen shot.

* Provide a name and a resource group. You can use a free tier but only one free tier is available per subscription.

![Create IoT Hub](/images/create-iothub.png)

While you are in the Azure portal, let's go ahead and grab a couple of important connection parameters and create an IoT Edge Device

In the IoT Hub blade of the Azure portal for your created IoT Hub, do the following:
* In the left-hand nav bar, click on "Shared Access Policies" and then click on "iothubowner", copy the "Connection String - Primary Key" string and paste it into Notepad.  We'll need it later.  This is your "IoTHub Owner Connection string" (keep that in mind, or make a note next to it in Notepad, we will use it in subsequent labs).  
* Close the "Shared Access Policy" blade

Now let's create the "edge device"
* In the left-hand nav bar, click on "IoT Edge (preview)"
* click "Add Edge Device"
* Give your IoT Edge Device a name and click "Create"
* once created, find the IoT Edge Device connection string (primary key) and copy/paste this into Notepad.  This is the "IoT Edge Device" connection string

## Create docker hub account

IoT Edge modules are pulled by the Edge runtime from a docker containder image repository.  You can host one locally in your own network/infrastructure if you choose, Azure offers a [container service](https://azure.microsoft.com/en-us/services/container-service/)  and of course, Docker themselves offer a repository (docker hub).  For simplicity, we will run the labs based off of hosting images in docker hub.  If you feel confident in doing so, feel free to leverage other docker image respositories instead of docker hub if you wish.

For Docker Hub, you need a Docker ID.  Create one by visting www.docker.com and clicking on "Create Docker ID" and following the instructions.  Remember the docker ID you create, as we'll use it later.  If you are given a choice during sign up, choose a repository visibility of 'public'.  Generally, docker images are referred to in a three part name:  \<respository>/image:tag where "respository" (if using Docker Hub) is just your Docker ID,  image is your image name, and tag is an optional "tag" you can use to have multiple images with the same name (often used for versioning).

## Clone the lab materials locally

The first step is to clone the lab materials locally (you'll need a few components of module2 locally to run).

```cmd
cd \
git clone https://github.com/azureiotgbb/azure-iot-edge-hol
```

## Additional setup

There are a few final steps needed to set up our specific lab scenario.  We are using our Edge device "as a gateway*, so we need:

1. Our IoT Device to be able to find it
2. Have valid certificates so the IoT Device will open a successful TLS connection to the Edge

First let's add a host file entry for our Edge device. This will let our "IoT Device" resolve and find our Edge gateway.  

* Open a command prompt __*as an Administrator*__
* Type the command bellow to open the hosts file in notepad
    ```
    notepad.exe c:\windows\system32\drivers\etc\hosts
    ```
* Add a row at the bottom with the following and then save and close the file
    ```
    127.0.0.1  mygateway.local
    ```
* Confirm you can successfully "ping mygateway.local"

Now let's create the certificates needed

* Open a PowerShell session __*as an Adminstrator*__ 

>Note: Do this in a plain Powershell window.  It does not work in the PowerShell ISE for some reason.

First, we will clone the Azure IoT C sdk.  We need this to get the certificate generation scripts.  Also, while Edge is in public preview, we need the 'modules-preview' branch of the SDK.

After cloning the C sdk, we prepare the PowerShell environment to we can generate the certificates.

Run the following commands from the root of the **"C" drive**

    cd \

    git clone -b modules-preview http://github.com/azure/azure-iot-sdk-c

    mkdir c:\edge
    cd \edge
    Set-ExecutionPolicy Unrestricted
    $ENV:PATH += ";c:\utils\OpenSSL\bin"
    $ENV:OPENSSL_CONF="c:\utils\OpenSSL\bin\openssl.cnf"
    . \azure-iot-sdk-c\tools\CACertificates\ca-certs.ps1
    Test-CACertsPrerequisites

Make sure it returns the result "SUCCESS". If the Test-CACertsprequisites call fails, it means that the local machine already contains Azure IoT test certs (possibly from a previously deployment). If that happens, you need to follow Step 5 - Cleanup of the instructions [here](https://github.com/Azure/azure-iot-sdk-c/blob/CACertToolEdge/tools/CACertificates/CACertificateOverview.md) before moving on

>Note: Do not close the powershell session yet. If you do, just reopen it and re run lines 4-6

We are now ready to generate the TLS certificates for our Edge device. Make sure you are still in the __c:\edge folder__ in your PowerShell session and run the command bellow to generate our test certificates. In production, you would use a real CA.

    New-CACertsCertChain rsa

In the azure portal, navigate back to your IoT Hub and click on "Certificates" on the left-nav and click "+Add".  Give your certificate a name, and upload the c:\edge\RootCA.cer" file

Now we need to generate certs for our specific gateway. In Powershell run:
    
    New-CACertsEdgeDevice myGateway


This will generate the gateway specific certificates (MyGateway.*). When prompted to enter a password during the signing process, just enter "1234".

>Note: If anything goes wrong during this process and you need to repeat it, you'll likely need to clean up the existing certs before generating new ones.  To do so, follow Step 5 - Cleanup, of the process outlined [here](https://github.com/Azure/azure-iot-sdk-c/blob/CACertToolEdge/tools/CACertificates/CACertificateOverview.md)

## Install IoT Edge configuration tool

Microsoft provides a python-based, cross-platform configuration and setup tool for IoT Edge.  To install the tool, open an administrator command prompt and run:

```cmd
pip install -U azure-iot-edge-runtime-ctl
```

## Configure and start IoT Edge

Now that we have all the pieces in place, we are ready to start up our IoT Edge device.  We will start it by specifying the IoT Edge Device connection string capture above, as well as specifying the certificates we generated to allow downstream devices to establish valid TLS sessions with our Edge gateway.

To setup and configure our IoT Edge device, make sure  Docker is running and run the following command:  (if you used '1234' for the password above, enter it again here when prompted).

```

iotedgectl setup --connection-string "<Iot Edge Device connection string>" --edge-hostname "mygateway.local" --device-ca-cert-file c:\edge\myGateway-public.pem --device-ca-chain-cert-file c:\edge\myGateway-all.pem --device-ca-private-key-file c:\edge\myGateway-private.pem --owner-ca-cert-file c:\edge\RootCA.pem
    
```
Replace *IoT Edge Device connection string* with the Edge device connection string you captured above.  If it prompts you for a password for the edge private cert, use '12345'   (NOTE: different from the password above!)

We're ready now to start our IoT Edge device

```
iotedgectl start
```

You can see the status of the docker images by running 

```
docker ps
```

At this point, because we haven't added any modules to our Edge device yet, you should only see one container/module running called 'edgeAgent'

If you want to see if the edge Agent successfully started, run

```
docker logs -f edgeAgent
```

>Note: You may see an error in the edgeAgent logs about having an 'empty configuration'.  That's fine because we haven't set a configuration yet! 

CTRL-C to exit the logs when you are ready

__**Congratulations -- You now have an IoT Edge device up and running and ready to use**__

To continue with module 2, click [here](/module2)
