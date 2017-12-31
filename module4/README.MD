# Azure IoT Edge Hands On Labs - Module 4

Created and maintained by the Microsoft Azure IoT Global Black Belts

## Introduction

For this step of the lab, we are going to create an Azure Stream Analytics job that will run on our IoT Edge device.  Azure Stream Analytics (ASA) is our SQL-based near real time analytics engine. One of the great value propositions of running ASA on the Edge is that you can create and manage the ASA query in the cloud, and have it pushed to be executed on the Edge. 

For our use case, we are going to have the ASA job do two things concurrently:

    1. aggregate the temperature and humidity data, which comes in at a higher rate, to a lower frequency of every 20 seconds
    2. look for cases in which the tempertaure transition above or below 80 degrees.  This will kick off an 'alert' message that we can react to downstream of the ASA job

## Creating the ASA job

Our ASA job will be set up to take the messages produced by our formatter module in module 3 of the labs.

An ASA job consists of three parts:
* one or more "inputs", or sources of either streaming or reference data
* one or more "outputs" or sinks for the query results
* the "query" that represents the processing of the job, which takes data from the inputs, processes it, and writes the results to one or more outputs

To create the ASA job:

* in the Azure portal, navigate in the left nav bar to the Azure Stream Analytics jobs blade and click Add
    * give your ASA job a unique name (e.g. edgeASAJob)
    * under resource group, choose "use existing" and pick the same resource group that contains your IoT Hub
    * for Hosting Environment, choose "Edge"
    * click "Create"

After a few moments, your ASA job will be created.  Click "Go to Resource" to navigate to the portal blade for your job

* click on the "inputs" box and click "Add"
* give your input a name (e.g. "inputFromHub") and leave the other settings as default.  Note that source is 'Edge Hub'
* click Create to create your input

Navigate back to the blade for your ASA job and click on the "outputs" box to add outputs

* Our job is going to leverage two different outputs, one for the aggregated temperature data and one for the alerts
    * click Add to create a new output
    * give your output a unique name (e.g. asaAggregatedTemp) for the aggregated temperature data
    * leave the remaining settings at default and click "Create"
    * repeat the above three steps for the alert output (e.g. asaAlertTemp)

The next step is to specify the query.  Click on the "query" box to open the query editor.  Copy/paste in this query:

```SQL
WITH
    StreamData
    AS (
        SELECT *
        FROM <input>
        WHERE temperature is not null
    ),
    TemperatureCTE
    AS (
        SELECT
            deviceID,
            temperature,
            CASE
            WHEN cast(temperature as bigint) >= 80 THEN 'HIGH'
                ELSE 'LOW'
            END as tempState,
            System.TimeStamp as eventdatetime
       FROM StreamData
       WHERE TRY_CAST(temperature AS bigint) IS NOT NULL
    )

SELECT
    deviceID,
    AVG (humidity) AS [AverageHumidity],
    AVG (temperature) AS [AverageTumidity],
    System.TimeStamp as eventdatetime
INTO
    [<asaAggregateTemp Output name>]
FROM [StreamData]
WHERE
    [humidity] IS NOT NULL
GROUP BY
    deviceID,
    SlidingWindow (s, 20)

SELECT *
INTO   <asa Alert Temp output name>
FROM   TemperatureCTE
WHERE
    LAG(tempState, 1) OVER (PARTITION BY deviceID LIMIT DURATION(minute, 10)) <> tempState

```

make sure you replace:
* \<input name> with the name of our ASA input
* \<asaAggregateTemp Output Name> with the name of the output you created for the aggregate data
* \<asa Alert Temp output name> with the name of the output you created for the temperature alerts

If we break down the query above, it really has four main parts:

*  the first Common Table Expression (subquery) just filters the data to make sure we have data with a valid temperature value
* the second CTE checks to see if the temperature is over 80, where is it considered "HIGH", otherwise it is "LOW"
* the third query takes the AVG of humidity and temp over a sliding (aka back-to-back) window of 20 seconds and sends this output to the aggregated temperature data output
* the fourth query uses the SQL LAG function to look at, and compare the "current" TempState (i.e HIGH or LOW) and the TempState of the previous row.  That's what LAG does.  If the values are different, then we have either transitioned from LOW to HIGH, or HIGH to LOW, so we generate an Alert message to our alert output

* Click Save to save your query and close the editor

## Deploy the ASA job

In order to deploy our ASA job to Edge, the first step we need to do is to create a storage account in Azure.  The storage account is used to hold the details of the job and as a place from which the ASA on Edge module can download it.

To create a storage account:

* in the Azure portal, navigate to "Storage accounts" on the left-hand nav and click "Add"
    * give the account a name (which has to be globally unique)
    * for resource group, use the same one we used for the ASA job and IoT Hub
    * leave all the other settings at default and click "Create"
    * after the account gets created, navigate to it and click "Browse Blobs"
    * Create a new container (give it a name) and for access, choose "Container" level and select "Ok"


Now we are ready to deploy our ASA job to our Edge device.

* navigate back to your IoT Hub and then to your Edge device
* click "Set Modules"
* select "Import Azure Stream Analytics IoT Edge Module"
* select your subscription, and the ASA job you created earlier
* under Storage account, select your subscription and the storage account you created earlier
* click "next" to configure our routes
* on the 'specify routes' screen, enter the following routes

```
todo:  routes including ASA job
```

click 'next' and 'finish' to publish our routes.

## Test our module

re-start the python script that represents our device.  Use "docker ps" and "docker logs -f \<module name>" to see the various debug messages from the modules

If it is not still running, restart the D2C message monitoring within VS Code.  You should see the aggregated temperature data come through every 20 seconds or so.  If you hold your finger on the temperature sensor on the Arduinio device enough to make it go above (and then release and let it fall below) 80 degrees, you should also see the Alert messages comes through to IoT Hub.

## Summary

So now we have ASA generating aggregated temperature/humidity data at a lower and less costly message rate to the cloud, and also doing local processing to generate temperture 'alerts'.  However, we are not yet taking action on those alerts.  We will do that in the next module (module 5).

