# Azure IoT Edge Hands On Labs

Created and maintained by the Microsoft Azure IoT Global Black Belts


## Troubleshooting

This troubleshooting guide contains some tips for troubleshooting your labs.  As a reminder, Azure IoT Edge is in *preview*, so you should not be shocked to find that there are bugs, idiosyncracies, and stuff that just doesn't work.  With that said, the product is generally in very good shape and is of very high quality for a preview.

### General tips

Read the instructions VERY carefully.  A good number of the 'issues' found in early internal testing existed between the seat and the keyboard :-).  The instructions can get very detailed in places, so if something doesn't work, make sure to re-read the instructions.

Make sure you are always working with the latest set of instructions (i.e. refresh your browser).  We find and fix stuff regularly.

### Docker tips

For these labs, at least for now, we are using Docker for Windows with *Linux* containers.  There is no reason to believe they won't work with Windows containers, but we haven't tested them.  If you have issues, make sure Docker is running Linux containers (right click on the 'whale' in your notification bar and see if "switch to Windows containers..." is there.  If so, that means you are using Linux containers)

_*A few very useful docker commands:*_

* docker ps:    gives a list of running docker containers
* docker images : gives a list of locally caches images
* docker image rm -f \<image name> :  removes (forceably) a locally cached image (forcing a fresh re-download) where \<image name> is the image name in the format of \<repository>/\<image>:\<tag>.  You can alternately just specify the image ID (found from 'docker images') instead of the name.  Generally you want to make sure the Edge runtime is stopped when you do this (iotedgectl stop)
* docker container ls -a : gives a list of docker containers (whether running or not) 
* docker container rm -f \<container name> :  removes (forceably) a docker container and makes the edge runtime recreate it when it next starts.  Generally you want to make sure the Edge runtime is stopped when you do this (iotedgectl stop)
* docker logs -f \<container name> : shows (and follows) the logs for a running container.  CTRL-C to exit it.

Of all of these, 'docker logs -f \<container name>' is your best friend.  It shows anything written to the console (i.e. printf, Console.Writeline, etc) from within the container.  All of the MSFT provided modules, and the ones you develop in the lab, provide useful information in the logs

_*Restarting/Resetting Docker:*_

Sometimes Docker can get in a weird state.  We've seen this a few times when a machine sleeps or hibernates.  One thing, short of a reboot, that you can do (after stopping Edge!) is to right click on the whale in your notification area, click 'settings", click on the 'reset' tab, and click "Restart Docker".  If that doesn't work, "Reset to Factory Defaults" is another good option.  The need for this doesn't happen often, but has happened.

### Lab troubleshooting tips

_*when i try to install the edge runtime control script with 'pip install -U azure-iot-edge-runtime-control', it tells me pip is not found*_

Make sure that c:\python27\scripts is in your path.  If not, add it and re-open your command prompt

_*when my IoT Device (python script) tries to connect to Edge, I get a TLS error or some other error*_

* make sure you are running the pre-release version of the Python SDK.  If you run "pip list", you should see an entry for "azure-iothub-device-client (1.2.0.0b0)" 
* Make sure that your GatewayHostName parameter is spelled correctly ('mygateway.local')
* make sure you can 'ping' the hostname  (should resolve to 127.0.0.1)
* make sure that the edge runtime was started with the right certs.  Open c:\programdata\azure-iot-edge\config\config.json and make sure the "certificates" section matches this:
![edgecerts](/images/edgecerts.png)
* make sure that the edgeHub is actually running and listening on port 8883.  run 'docker ps' to confirm (as in the image)
![edgeHubrunning](/images/edgeHubrunning.png)
* finally, look in the edgeHub logs (docker logs -f edgeHub) for any errors

_*My module(s) are up and running fine (via docker ps and docker logs), but no messages are 'flowing' through them.  In other words, messages are going into edgeHub from my python script, but don't seem to get routed anywhere (including IoT Hub)*_

We've seen a few times where edgeHub seems to just kind-of get 'stuck'.  No errors in the log, and you can send data, C2D messages, DM calls, etc through it, but no messages get routed to either modules or IoT Hub.  The only fix we've found so far is to delete, and let Edge re-create, the edgeHub container.

To do so:
* stop your python script/IoT device if it is running
* stop IoT Edge (iotedgectl stop)
* delete the edgeHub container
    * docker container rm -f edgeHub
* delete the edgeHub image (probably not necessary, but why not)
    * docker image rm -f microsoft/azureiotedge-hub:1.0-preview
* restart IoT Edge (iotedgectl start)
* restart your IoT device










