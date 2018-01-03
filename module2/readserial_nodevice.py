#import serial
import time

import iothub_client
from iothub_client import *
from iothub_client_args import *

i = 0

#set min and maxes for temperature and humidity
min_temp = 70.0
max_temp = 90.0
min_humidity = 50.0
max_humidity = 60.0
valley_to_peak_samples = 30
 
#change this -- don't forget the "GatewayHostName" param at the end
connection_string = "<connection string here>"

# directions for simulated data
temp_going_up = True
humidity_going_up = True
last_temp = min_temp
last_humidity = min_humidity
temp_range = (max_temp - min_temp) / valley_to_peak_samples
humidity_range = (max_humidity - min_humidity) / valley_to_peak_samples

def receive_message_callback(message, counter):
    buffer = message.get_bytearray()
    size = len(buffer)
    msg = buffer[:size].decode('utf-8')
    print("received C2D command {%s}" % (msg))
    return IoTHubMessageDispositionResult.ACCEPTED

def device_method_callback(method_name, payload, user_context):
    print("received DM {%s}, payload: %s" % (method_name, payload))

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
    print("         message number %d sent " % (user_context))
    return

def GetNextTemp():
    global min_temp
    global max_temp
    global last_temp
    global temp_going_up
    global temp_range

    if(temp_going_up):
        curr_temp = last_temp + temp_range
        if(curr_temp > max_temp):
            curr_temp = max_temp
            temp_going_up = False
    else:
        curr_temp = last_temp - temp_range
        if(curr_temp < min_temp):
            curr_temp = min_temp
            temp_going_up = True

    last_temp = curr_temp
    return curr_temp

def GetNextHumidity():
    global min_humidity
    global max_humidity
    global last_humidity
    global humidity_going_up
    global humidity_range

    if(humidity_going_up):
        curr_humidity = last_humidity + humidity_range
        if(curr_humidity > max_humidity):
            curr_humidity = max_humidity
            humidity_going_up = False
    else:
        curr_humidity = last_humidity - humidity_range
        if(curr_humidity < min_humidity):
            curr_humidity = min_humidity
            humidity_going_up = True

    last_humidity = curr_humidity
    return curr_humidity


while 1:
#    serial_line = ser.readline()
#    print(serial_line)

    curr_temp = GetNextTemp()
    curr_humidity = GetNextHumidity()

    serial_line_template = "%2.2f,%2.2f"
    serial_line= serial_line_template % (curr_humidity, curr_temp)

    message = IoTHubMessage(bytearray(serial_line, 'utf8'))
    iotHubClient.send_event_async(message, send_confirmation_callback, i)

    print("sending message: [%s]" % (serial_line))

    i=i+1

    time.sleep(2)

#ser.close() # Only executes once the loop exits

