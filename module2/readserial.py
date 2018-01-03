import serial
import time

import iothub_client
from iothub_client import *
from iothub_client_args import *

i = 0

#change this to your specifics...  (e.g. "COM3")
ser = serial.Serial('<serial port>', 9600)
 
#change this -- don't forget the "GatewayHostName" param at the end
connection_string = "<connection string here>"

def receive_message_callback(message, counter):
    buffer = message.get_bytearray()
    size = len(buffer)
    msg = buffer[:size].decode('utf-8')
    print("received C2D command {%s}" % (msg))
    return IoTHubMessageDispositionResult.ACCEPTED

def device_method_callback(method_name, payload, user_context):
    print("received DM {%s}, payload: %s" % (method_name, payload))

    if("ON" in str(method_name)):
        print("ON method called")
        ser.write('ON\n')
        ser.flush()

    if("OFF" in str(method_name)):
        print("OFF method called")
        ser.write('OFF\n')
        ser.flush()

    device_method_return_value = DeviceMethodReturnValue()
    device_method_return_value.response = "{ \"Response\": \"This is the response from the device\" }"
    device_method_return_value.status = 200
    return device_method_return_value

def device_twin_callback(update_state, payload, user_context):
    print("Device Twin update %s...   %s" % (update_state, payload))

protocol = IoTHubTransportProvider.MQTT

iotHubClient = IoTHubClient(connection_string, protocol)
print("Connected to IoTHub gateway")

iotHubClient.set_device_twin_callback(device_twin_callback, 0)
iotHubClient.set_device_method_callback(device_method_callback, 0)
iotHubClient.set_message_callback(receive_message_callback, 0)

def send_confirmation_callback(message, result, user_context):
    global i
    print("message number %d sent " % (user_context))
    return

while 1:
    serial_line = ser.readline().strip()
    print(serial_line)

#    serial_line="22.22,33.33"

    message = IoTHubMessage(bytearray(serial_line, 'utf8'))
    iotHubClient.send_event_async(message, send_confirmation_callback, i)    

    i=i+1

#    time.sleep(2)

ser.close() # Only executes once the loop exits

