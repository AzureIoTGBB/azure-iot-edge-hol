using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;

namespace readserial_nodevice
{
    class Program
    {
        static string ConnectionString = "<IoT Device connection string>";  //don't forget the ;GatewayHostName=mygateway.local
        static bool _continue;
        static DeviceClient _deviceClient;

        static int counter = 0;
        const int timeBetweenMessages = 3000;

        //set min and maxes for temperature and humidity
        static double minTemp = 70.0;
        static double maxTemp = 90.0;
        static double minHumidity = 50.0;
        static double maxHumidity = 60.0;
        static int numSamples = 30;

        // reference variables
        static bool tempGoingUp = true;
        static bool humidityGoingUp = true;
        static double lastTemp = minTemp;
        static double lastHumidity = minHumidity;

        static double tempRange = (maxTemp - minTemp) / numSamples;
        static double humidityRange = (maxHumidity - minHumidity) / numSamples;

        static void Main(string[] args)
        {
            Thread readThread = new Thread(Read);

            InitDeviceClient().Wait();

            _continue = true;
            readThread.Start();

            Console.WriteLine("press <enter> to exit");
            Console.ReadLine();

            _continue = false;

            readThread.Join();

        }

        static Task<MethodResponse> HandleDirectMethod(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"Direct Method ({methodRequest.Name}) invoked...  ");
            Console.WriteLine("Returning response for method {0}", methodRequest.Name);

            string result = "'DM call successful'";
            return Task.FromResult(new MethodResponse(System.Text.Encoding.UTF8.GetBytes(result), 200));
        }

        static public async Task InitDeviceClient()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            // During dev you might want to bypass the cert verification. It is highly recommended to verify certs systematically in production
            mqttSetting.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            ITransportSettings[] settings = { mqttSetting };

            _deviceClient = DeviceClient.CreateFromConnectionString(ConnectionString, settings);
            await _deviceClient.OpenAsync();
            Console.WriteLine($"Connected to IoT Edge with connection string [{ConnectionString}]");

            _deviceClient.SetMethodHandlerAsync("ON", HandleDirectMethod, null).Wait();
            _deviceClient.SetMethodHandlerAsync("OFF", HandleDirectMethod, null).Wait();

        }

        public static void Read()
        {
            while (_continue)
            {
                try
                {
                    double temp = 0.0;
                    double humidity = 0.0;           

                    if(tempGoingUp)
                    {
                        temp = lastTemp + tempRange;
                        if(temp > maxTemp)
                        {
                            temp = maxTemp;
                            tempGoingUp = false;
                        }
                    }
                    else
                    {
                        temp = lastTemp - tempRange;
                        if(temp < minTemp)
                        {
                            temp = minTemp;
                            tempGoingUp = true;
                        }
                    }

                    lastTemp = temp;

                    if(humidityGoingUp)
                    {
                        humidity = lastHumidity + humidityRange;
                        if(humidity > maxHumidity)
                        {
                            humidity = maxHumidity;
                            humidityGoingUp = false;
                        }
                    }
                    else
                    {
                        humidity = lastHumidity - humidityRange;
                        if(humidity < minHumidity)
                        {
                            humidity = minHumidity;
                            humidityGoingUp = true;
                        }
                    }

                    lastHumidity = humidity;

                    string message = String.Format("{0:0.00},{1:0.00}", humidity, temp);

                    var mess = new Message(System.Text.Encoding.ASCII.GetBytes(message));

                    Task t =  _deviceClient.SendEventAsync(mess);
                    Console.WriteLine($"Message sent({counter++}) {message}");
                }
                catch (TimeoutException) { }

                Thread.Sleep(timeBetweenMessages);
            }
        }
    }
}
