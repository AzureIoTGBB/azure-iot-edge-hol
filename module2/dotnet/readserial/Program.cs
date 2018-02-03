using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;

namespace readserial
{
    class Program
    {

        static string PortName = "<Port name>";  // e.g. "COM3"
        static string ConnectionString = "<IoT Device connection string>";  //don't forget the ;GatewayHostName=mygateway.local

        static bool _continue;
        static SerialPort _serialPort;

        static DeviceClient _deviceClient;

        static int counter = 0;

        static void Main(string[] args)
        {
            Thread readThread = new Thread(Read);

            InitDeviceClient().Wait();

            _serialPort = new SerialPort();

                    // Allow the user to set the appropriate properties.
            _serialPort.PortName = PortName;
            _serialPort.BaudRate = 9600;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;

            // Set the read/write timeouts
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;

            _serialPort.Open();
            Console.WriteLine($"Serial port [{PortName}] opened");

            _continue = true;
            readThread.Start();

            Console.WriteLine("press <enter> to exit");

            Console.ReadLine();

            _continue = false;

            readThread.Join();
            _serialPort.Close();
            //_deviceClient.Close();

        }

        static Task<MethodResponse> HandleDirectMethod(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"Direct Method ({methodRequest.Name}) invoked...  ");
//            Console.WriteLine("\t{0}", methodRequest.DataAsJson);
            Console.WriteLine("\nReturning response for method {0}", methodRequest.Name);

            writeSerial(methodRequest.Name);

            string result = "'Input was written to log.'";
            return Task.FromResult(new MethodResponse(System.Text.Encoding.UTF8.GetBytes(result), 200));
        }

        static void writeSerial(string command)
        {
            _serialPort.WriteLine(command);
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
                    string message = _serialPort.ReadLine().Trim();
//                    Console.WriteLine(message);
                    var mess = new Message(System.Text.Encoding.ASCII.GetBytes(message));

//                    await _deviceClient.SendEventAsync("output1", mess);
                    Task t =  _deviceClient.SendEventAsync(mess);
                    Console.WriteLine($"Message sent({counter++}) {message}");
                }
                catch (TimeoutException) { }
            }
        }
    }
}
