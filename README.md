# Azure IoT Edge Hands On Labs

Created and maintained by the Microsoft Azure IoT Global Black Belts

## Overview

This hands-on lab demonstrates setting up, configuring, and developing modules for [Azure IoT Edge](https://azure.microsoft.com/en-us/services/iot-edge/).  The intent of these labs is not to cover exhaustively every IoT Edge topic, but rather cover a scenario that allows the student to learn and understand the basics of IoT Edge, develop modules and Edge ASA jobs, and perform Edge configuration, all in a pseudo-realistic use case.

In this workshop you will:

* Setup and configure a simple IoT Device, based on an Arduino Uno connected to a DHT22 temperature sensor, to simply (and dumbly) send temperture over the serial port every 3 seconds
* create an "IoT Device" that reads the data from the serial port and connects to IoT Hub __**through**__ IoT Edge
* create an IoT Edge module that read the simple CSV temp/humidity data from the device and converts to JSON and passes the message along
* create an Azure Stream Analytics module that a) aggregates the "every 3 seconds" data to a 30 second frequency to send to IoT Hub in the cloud and b) looks for temperatures above a certain threshold.  Then a threshold violation occurs, the module will drop an "alert" message on Edge
* create an IoT Edge module that reads the "alert" message from ASA and sends a Direct Method call to the IoT Device to light up an "alert" LED

The labs are broken up into the following modules:
* __**Module 1**__ - Prerequisites and IoT Edge setup
* __**Module 2**__ - Setup and program the "IoT Device"
* __**Module 3**__ - Develop "Formatter" module
* __**Module 4**__ - Azure Stream Analytics Edge job
* __**Module 5**__ - Develop "Alert" module





