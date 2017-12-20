# Azure IoT Edge Hands On Labs - Module 1

Created and maintained by the Microsoft Azure IoT Global Black Belts

## Introduction

For this step of the lab, we are going to create our "IoT Device".  For the labs, we wanted to leverage a physical device to make the scenario slightly more realistic (and fun!).

__**NOTE:  Because of easier logistics of setup given a large number of students, we are leveraging Windows desktops for our 'Edge devices' in this lab. If we were doing this on a physical Linux device (like a Raspberr Pi), we would develop an Edge "module" to talk to the USB port containing our Arduino device and just map that port into the docker container running that module via the --devices paramter.  However, there is a current limitation in Docker for Windows that doesn't let us do that on a Windows host.  So we use an intermediate dumb IoT "device" that reads the serial/USB port and connects and sends that data to IoT Edge.**__

*** todo:  link to Docker issue above

Ok, let's get started...

## Create Arduino device

### Connect device and ensure basic operation

1. Launch the Arduino Desktop App.  Upon launching you will be presented an empty project called a “sketch”.

![ArduinoIDE](/images/m2bArduino4.png)

2. Connect the Arduino device to the workstation with the USB cable.  (Note: the Arduino device can get power via either USB or an external power supply.  For the purposes of this workshop we’ll be getting power via USB)

3. In the Arduino IDE you must select your device as your deployment target.  Do this from the Tools -> Port menu:

![ArduinoIDE](/images/m2bArduino5.png)

4. Now that the device is setup in the IDE, you can open and deploy a sample sketch.  From the File -> Examples -> Basic menu open the “Blink” sketch.

![ArduinoIDE](/images/m2bArduino6.png)

5. Click the deploy button to load the sketch to the device.  After the sketch has deployed look at your Arduino to validate you have a blinking LED (once per second).

### Assemble device

In this section, we will assemble the IoT device out of the arduino and DHT22 temp/humidity sensor

1. __**Disconnect the Arduino from your workstation!!**__.  Note this step is very important to ensure there is no electric charge running through the device while we’re assembling.

2. With the provided jumper wires and breadboard assemble the Arduino using the following schematic.  ** please note the diagram is logical and not to scale.  The first and second pins cannot really be separated like shown **

![schematic](/images/m2bArduino7.png)

This diagram may seem complicated, so let’s deconstruct it a bit.

3. The black wire is the ground wire; it runs to the right most pin on the DHT sensor.
4. The red wire provides power; it runs to the left most pin on the DHT sensor.
5. The green wire is the signal wire it runs to the pin adjacent to the power lead.
6. The resistor between pins 1 and 2 of the DHT sensor is called a "pull up" resistor.  It essentially just ensures that, during times we are not actively reading the sensor, the pin is "pulled up" to 5V and not electrically "bouncing around" freely.  This helps cut down on communication errors between the device and sensor.  __**Note that resistors are not directional, so it doesn't matter which direction you plug the resistor into the breadboard**__

### Develop sketch to read sensor and send across serial

In this section, we will load and execute the arduino "code" to talk to the DHT sensor and send the temperature and humidity data across the serial port.

1. Plug your device back in to your workstation via USB.

2. Open the dhtSensorSketch.ino sketch in the Arduino IDE

3. In order to use the sensor we first need to download a library for simplifying communication with the device.  In the Arduino IDE select “Manage Libraries” from the Sketch -> Include Library menu.

![library install](/images/m2bArduino8.png)
4. From the library manager window search for “DHT”,  select the second option “DHT sensor library by Adafruit” library, and click “Install”.

![library install](/images/m2bArduino9.png)

5. When the install is complete close the Library Manager window.

6. Deploy the code to the Arduino device (second button from the left on the command bar)

![Deploy](/images/m2bArduino10.png)

7. Open the "Serial Monitor" tool and make sure that you are getting valid humidity and temperature readings.  Place your thumb and index finger aorund the sensor and ensure the values change

![Serial monitor](/images/m2bArduino11.png)

## Create "IoT Device"

Per the note about needing an intermediate IoT device to talk to the serial port and forward the messages on, we have a "dumb" IoT Device that reads the CSV-formatted data fron the serial/USB port and sends it to IoT Edge

For our device, we will leverage a python script that emulates our IoT Device.  The device leverages our python Azure IoT SDK to connect to the hub.

### setup libraries and pre-requisites

1. Because we are in public preview with IoT Edge, we need to leverage a preview version of the python SDK.  To install that preview version, open an administrator command prompt and run this command

```
pip install azure-iothub-device-client==1.2.0.0b0
```

2. Our script leverages the pyserial library for reading from the serial port, so we need to install it.  From the command prompt, run

```
pip install pyserial
```

3. To represent our device in IoT Hub, we need to create an IoT Device
    * in the Azure portal, for your IoT Hub, click on "IoT Devices" in the left-nav  (note, this is different than the "IoT Edge Devices" we used previously)
    * click on "+Add Device" to add a new device.  Give the device a name and click "create"
    * capture (in notepad) the Connection String - Primary Key for your IoT device, we will need it in a moment

4. We need to fill in a couple of pieces of information into our python script.  

* In the line of code below

```Python
ser = serial.Serial('serial port', 9600)
```
replace "serial port" with the serial port your arduino device is plugged into (e.g "COM3")

* In the line below

```Python
connection_string = "<connection string here>"
```
put your connection string in the quotes.  Onto the end of your connection string, append ";GatewayHostName=mygateway.local".  This tells our Python script/device to connect to the specified IoTHub in it's connection string, but to do so __**through the specified Edge gateway**__

Ok, we now have our device ready, so let's get it connected to the Hub

*** todo:  deploy edge hub and see our device data flow through