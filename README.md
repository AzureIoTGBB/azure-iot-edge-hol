# Azure IoT Edge Hands On Labs

Created and maintained by the Microsoft Azure IoT Global Black Belts

# NOTE - We are in the process of updating the labs to leverage the new GA bits and process for IoT Edge... We hope to have the updates soon.  Currently, consider these labs "closed for construction"  :-)

## Overview

This hands-on lab demonstrates setting up, configuring, and developing modules for [Azure IoT Edge](https://azure.microsoft.com/en-us/services/iot-edge/).  The intent of these labs is not to cover exhaustively every IoT Edge topic, but rather cover a scenario that allows the student to learn and understand the basics of IoT Edge, develop modules and Edge ASA jobs, and perform Edge configuration, all in a pseudo-realistic use case.

These labs were originally developed to be delivered in-person by the Azure IoT GBBs to customers, however, they are available for any customers or partners to leverage, to play, or to learn.  Over time, they will evolve past this original use to incorporate other use cases.

In this workshop you will:

* Setup and configure a simple IoT Device, based on an Arduino Uno connected to a DHT22 temperature sensor, to simply (and dumbly) send temperature over the serial port every 3 seconds
* create an "IoT Device" that reads the data from the serial port and connects to IoT Hub __**through**__ IoT Edge
* create an IoT Edge module that reads the simple CSV temp/humidity data from the device and converts to JSON and passes the message along
* create an Azure Stream Analytics module that a) aggregates the "every 3 seconds" data to a 30 second frequency to send to IoT Hub in the cloud and b) looks for temperatures above a certain threshold.  When a threshold violation occurs, the module will drop an "alert" message on Edge
* create an IoT Edge module that reads the "alert" message from ASA and sends a Direct Method call to the IoT Device to light up an "alert" LED

The labs are broken up into the following modules:

* [Module 1](module1) - Prerequisites and IoT Edge setup
* [Module 2](module2) - Setup and program the "IoT Device"
* [Module 3](module3) - Develop "Formatter" module
* [Module 4](module4) - Azure Stream Analytics Edge job
* [Module 5](module5) - Develop "Alert" module

If you have questions or issues for the lab, check out our [troubleshooting](troubleshooting.md) guide!

Below is a conceptual flow for the labs to help visualize what is taking place and how the data is flowing through the system  ("T/H" is short for "temperature and humidity)

![conceptual drawing](/images/IoT-Edge-Labs-Conceptual-Design.png)

## Hardware

For this lab, for simplicity of setup, we are using our Windows 10 desktops (running Docker and Linux containers) as the Edge device.  For the Arduino device, we leverage an Arduino Uno (you can feel free to use any device that can send data over serial). For the labs we deliver directly to customers, we leverge the following kits:
* Arduino Uno R3 “kit”:   [kit](https://www.adafruit.com/product/193)   ~$35
* DHT22 temp/humidity sensor:  [kit](https://www.adafruit.com/product/385)   ~10

## Prerequisites

>Note: For the lab exercises, we need an IoT Hub created in an Azure Subscription for which you have administrative access.

In order to execute the hands-on labs, there are a number of pre-requisites that need to be installed and configured.  Unless otherwise noted, the default installation of the items below are fine

>Note: For in-person deliveries by the IoT GBBs, some of this may have been done for you.  Please check with your instructor

* Windows 10 Fall Creators Update (build 16299)
* [Docker for Windows](https://docs.docker.com/docker-for-windows/install/)   ** the "community edition" is fine.  Make sure you install the STABLE version.  A reboot may be required to enable Hyper-V

>NOTE:  because of some issues with Window containers, the labs are intended to be run with Linux containers (on a Windows host).  Please ensure that you are running Linux containers.  The best way to tell is to right-click on the "whale" in your notification bar and make sure you see "Switch to Windows containers" in the context menu.  That shows that you are currently running Linux containers.  If not, please make the switch.

* [Visual Studio Code](https://code.visualstudio.com/)
* [.NET Core SDK](https://www.microsoft.com/net/core#windowscmd)
* [Arduino IDE](http://www.arduino.cc/)
* [Open SSL](https://sourceforge.net/projects/openssl/)
    * for the lab instructions later, create a c:\utils folder and unzip the downloaded OpenSSL zip to c:\utils\ 
    (so you should a folder structure that looks like this->    c:\utils\OpenSSL)
* [git](https://git-scm.com/downloads/)   ** installation of the default components and default configurations are fine

* [Python 2.7 for Windows](https://www.python.org/downloads/)  -- __**make sure it's 2.7.x, NOT 3.x.x**__
    * during setup, elect to "add python 2.7 folder to the path"  (see screenshot below -- You will need to SCROLL DOWN to see it)

![python_install](/images/python_install.png)
