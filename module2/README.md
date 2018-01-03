# Azure IoT Edge Hands On Labs - Module 2

Created and maintained by the Microsoft Azure IoT Global Black Belts

## Introduction

For this step of the lab, we are going to create our "IoT Device".  For the labs, we wanted to leverage a physical device to make the scenario slightly more realistic (and fun!).

__**NOTE:  Because of easier logistics of setup given a large number of students, we are leveraging Windows desktops for our 'Edge devices' in this lab. If we were doing this on a physical Linux device (like a Raspberr Pi), we would develop an Edge "module" to talk to the USB port containing our Arduino device and just map that port into the docker container running that module via the --devices paramter.  However, there is a current limitation in Docker for Windows that doesn't let us do that on a Windows host.  So we use an intermediate dumb IoT "device" that reads the serial/USB port and connects and sends that data to IoT Edge.**__.  The Docker issue is documented [here](https://github.com/docker/for-win/issues/1018).  Feel free to go weigh in on the importance of this issue if you think it will affect you.

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

Run VS Code and click on File - Open Folder.  Open the c:\azure-iot-edge-hol folder.  Under the /module2 folder, open the readserial.py script.

* In the line of code below

```Python
ser = serial.Serial('<serial port>', 9600)
```

replace "serial port" with the serial port your arduino device is plugged into (e.g "COM3")

* In the line below

```Python
connection_string = "<connection string here>"
```
put your connection string in the quotes.  Onto the end of your connection string, append ";GatewayHostName=mygateway.local".  This tells our Python script/device to connect to the specified IoTHub in it's connection string, but to do so __**through the specified Edge gateway**__

Ok, we now have our device ready, so let's get it connected to the Hub

## Start IoT Edge, connect device, and see data flowing through

In this section, we will get the device created above connected to IoT Edge and see the data flowing though it.

* in the Azure portal, navigate to your IoT Hub, click on IoT Edge Devices (preview) on the left nav, click on your IoT Edge device
* click on "Set Modules" in the top menu bar.  Later, we will add a customer module here, but for now, we are just going to set a route to route all data to IoT Hub, so click "Next"
* on the 'routes' page, make sure the following route is shown, if not, enter it.

```json
{
    "routes": {
        "route":{"FROM /* to $upstream"}
    }
}
```
click 'next', and click 'finish'

* $upstream is a special route destination that means "send to IoT Hub in the cloud".  So this route takes all messages (/*) and sends to the cloud.  This lets us, at thsi stage in the lab, confirm that Edge is working end-to-end before we move onto subsequent modules.

### confirm IoT Edge

The running instance of IoT Edge should have gotten a notification that it's configuration has changed.

If you run 'docker ps' again, you should see a new module/container called "edgeHub' running.  This is the local IoT Hub-like engine that will store and forward our messages and act like a local IoTHub to our downstream devices

if you run 'docker logs -f edgeHub', you should see that the Hub has successfully created a route called "route' and is up and listening on port 8883. (the TLS port for MQTT locally)

The edge device is now ready for our device to connect.

### Monitor our IoT Hub

In VS Code, click on the 'Extensions' tab on the left nav.  Search for an install the "Azure IoT Toolkit" by Microsoft.  Once installed (reload VS Code, if necessary), click back on the folder view and you should see a new section called "IOT HUB DEVICES".  Hover over it and you should see three dots "...".  Click on that and click "Set IoT Hub Connection String".  You should see an Edit box appear for you to enter a connection string.  Go back to notepad where we copied the connection strings earlier, and copy/paste the "IoT Hub level" (the 'iothubowner') connection string from earlier into the VS Code edit box and hit ok.  

After a few seconds, a list of IoT Device should appear in that section.  Once it does, find the IoT Device (not the edge device) that is tied to your python script.  Right click on it and select "Start monitoring D2C messages".  This should open an output window in VS Code and show that it is listening for messages.

### start the local IoT device

open a new command prompt and CD to the module2 folder.  Run the following command to 'run' our IoT device

```
python -u readserial.py
```

You should see debug output indicating that the device was connected to the "IoT Hub" (in actuality it is connected to the edge device) and see it starting reading and sending humidity and temperature messages.

### Observe D2C messages

In the VS Code output window opened earlier, you should see messages flowing thought the hub.  These messages have come from the device, to the local Edge Hub and been forwarded to the cloud based IoT Hub in a store-and-forward fashion (i.e. transparent gateway).

In VS Code, right click on your IoT Device and click on "Stop Monitoring D2C Messages".


### test Direct Method call

Finally, we also want to test making a Direct Method call to our IoT Device.  Later, this functionality will allow us to respond to "high temperature" alerts by taking action on the device.  For now, we just want to test the connectivity to make sure that edgeHub is routing Direct Method calls propery to our device.  To test:

* in VS Code, in the "IOT HUB DEVICES" section, right click on your IoT Device and click "Invoke Direct Method".
* in the edit box at the top for the method to call type "ON" (without the quotes) and hit \<enter>
* in the edit box for the payload, just hit \<enter>>, as we don't need a payload for our method

You should see debug output in the python script that is our IoT Device indicating that a DM call was made, and after a few seconds, the onboard LED on the device should light up.  This is a stand-in for whatever action we would want to take on our real device in the event of an "emergency" high temp alert.

* repeat the process above, sending "OFF" as the command to toggle the LED back off.


in the command prompt runing your python script, hit CTRL-C to stop the script.

## Summary 

The output of module is still the raw output of the device (in CSV format).  We've shown that we can connect a device through the edgeHub to IoT Hub in the cloud.  In the next labs, we will add modules to re-format the data as JSON, as well as aggregate the data and identify and take local action on "high temperature" alerts.