namespace TempModule
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;

    class Program
    {
        static int counter = 0;
        static DeviceClient ioTHubModuleClient = null;
//        static Random rnd = new Random();
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
            // The Edge runtime gives us the connection string we need -- it is injected as an environment variable
            string connectionString = Environment.GetEnvironmentVariable("EdgeHubConnectionString");

            // Cert verification is not yet fully functional when using Windows OS for the container
            bool bypassCertVerification = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (!bypassCertVerification) InstallCert();
            Init(connectionString, bypassCertVerification).Wait();

            System.Timers.Timer t = new System.Timers.Timer(timeBetweenMessages);
            t.Elapsed += OnTimerEvent;
            t.AutoReset = true;
            t.Start();

            // Wait until the app unloads or is cancelled
           var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Add certificate in local cert store for use by client for secure connection to IoT Edge runtime
        /// </summary>
        static void InstallCert()
        {
            string certPath = Environment.GetEnvironmentVariable("EdgeModuleCACertificateFile");
            if (string.IsNullOrWhiteSpace(certPath))
            {
                // We cannot proceed further without a proper cert file
                Console.WriteLine($"Missing path to certificate collection file: {certPath}");
                throw new InvalidOperationException("Missing path to certificate file.");
            }
            else if (!File.Exists(certPath))
            {
                // We cannot proceed further without a proper cert file
                Console.WriteLine($"Missing path to certificate collection file: {certPath}");
                throw new InvalidOperationException("Missing certificate file.");
            }
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            store.Add(new X509Certificate2(X509Certificate2.CreateFromCertFile(certPath)));
            Console.WriteLine("Added Cert: " + certPath);
            store.Close();
        }

        /// <summary>
        /// Initializes the DeviceClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init(string connectionString, bool bypassCertVerification = false)
        {
            Console.WriteLine("Connection String {0}", connectionString);

            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            // During dev you might want to bypass the cert verification. It is highly recommended to verify certs systematically in production
            if (bypassCertVerification)
            {
                mqttSetting.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            }
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
//            DeviceClient ioTHubModuleClient = DeviceClient.CreateFromConnectionString(connectionString, settings);
            ioTHubModuleClient = DeviceClient.CreateFromConnectionString(connectionString, settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            // Register callback to be called when a message is received by the module
//            await ioTHubModuleClient.SetInputMessageHandlerAsync("input1", PipeMessage, ioTHubModuleClient);
        }

        static async void OnTimerEvent(object source, System.Timers.ElapsedEventArgs e)
        {

            //Console.WriteLine("Timer expired");

//            double temp = (rnd.NextDouble() * (90-50)) + 50;
//            double humidity = (rnd.NextDouble() * (70-40)) + 40;            
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

            string sMessage = String.Format("{0:0.00},{1:0.00}", humidity, temp);

            var mess = new Message(Encoding.ASCII.GetBytes(sMessage));

            Console.WriteLine($"Message sent({counter++}) {sMessage}");

            await ioTHubModuleClient.SendEventAsync("output1", mess);

        }
    }
}


