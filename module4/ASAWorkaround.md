
# Temporary ASA workaround

This file documents the temporary ASA work around for stream analytics

At this point, you should have just imported the ASA job.

Before we continue, go to your command prompt and stop iotedge with:

```
iotedgectl stop
```

we will be pulling a "private" build of the ASA module from a container repository in azure.  To do so, we need to provide the credentials to the repository so the Edge Agent can pull it.  To do so, run the following command

```
iotedgectl login --address asaedgeregistry.azurecr.io --username asaedgeregistry --password <get password from your lab instructor>
```

You will need the get the password for this repo from your lab instructor.

Once done, we will delete the edgeAgent container and restart edge with

```
docker container rm -f edgeAgent
iotedgectl start
```

Back on the Set Modules page, click on the ASA module you just imported.  That should show the properties of the module, including among other things, the image URI  (which will be something that looks like ````microsoft/azureiotedge-azure-stream-analytics:1.0.0-preview004````).

update the image URI to be:

````asaedgeregistry.azurecr.io/asamodule:1.0.0-linux-amd64````

and click the Save button.  

Return to the preview instructions and continue with setting routes

