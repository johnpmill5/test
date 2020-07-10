using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Client;
using System.Text;
using Newtonsoft.Json;
using Message = Microsoft.Azure.Devices.Client.Message;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;
using System.Collections.Generic;

namespace IoTDeviceBulkImpExp
{
    class Program
    {
        private static RegistryManager registryManager;

        const string _connectionString = "HostName=hallstattiothubdev.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=nw2rdNVLn0CGuHfW+poYapKUBKQLmOhhwFUwh1/sUuo=";

        static async Task Main(string[] args)
        {
            const int numberOfDevices = 10;

            registryManager = RegistryManager.CreateFromConnectionString(_connectionString);

            string deviceId = "device";
            var devices = new List<Device>();

            try
            {
                for (int i = 0; i < numberOfDevices; i++)
                {
                    string newDeviceId = deviceId + i;

                    var device = await AddDeviceAsync(newDeviceId);
                    devices.Add(device);
                }

                foreach (var device in devices)
                {
                    Random r = new Random();
                    int genRand = r.Next(5000,7000);
                    var deviceClient = GetDeviceClient(device);
                    await SendDeviceToCloudMessagesAsync(deviceClient);
                    System.Threading.Thread.Sleep(genRand);

                }

                Console.WriteLine("Hit any key to exit.");
                Console.ReadLine();
            }
            finally
            {                
                foreach (var device in devices)
                {
                    registryManager.RemoveDeviceAsync(device).Wait();
                }
               
            }
            //await SendDeviceToCloudMessagesAsync(newDeviceId);
        }
        private static string GetConnectionString(Device device)
        {
            return $"HostName=hallstattiothubdev.azure-devices.net;DeviceId={device.Id};SharedAccessKey={device.Authentication.SymmetricKey.PrimaryKey}";
        }

        private static DeviceClient GetDeviceClient(Device device)
        {
            return DeviceClient.CreateFromConnectionString(GetConnectionString(device), TransportType.Mqtt);
        }

        private static async Task SendDeviceToCloudMessagesAsync(DeviceClient deviceClient)
        {
            double minTemperature = 20;
            double minHumidity = 60;
            Random rand = new Random();

            double currentTemperature = minTemperature + rand.NextDouble() * 15;
            double currentHumidity = minHumidity + rand.NextDouble() * 20;

            // Create JSON message  
            var telemetryDataPoint = new
            {
                temperature = currentTemperature,
                humidity = currentHumidity
            };

            string messageString = JsonConvert.SerializeObject(telemetryDataPoint);

            var message = new Message(Encoding.ASCII.GetBytes(messageString));

            // Add a custom application property to the message.  
            // An IoT hub can filter on these properties without access to the message body.  
            //message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");  

            // Send the telemetry message  
            await deviceClient.SendEventAsync(message);
            Console.WriteLine($"{DateTime.Now} > Sending message: {messageString}");
        }

        private async static Task<Device> AddDeviceAsync(string deviceId)
        {
            Device device;
            try
            {
                Console.WriteLine($"New device: {deviceId}");

                var d = new Device(deviceId);

                device = await registryManager.AddDeviceAsync(d);

                return device;
            }
            catch (DeviceAlreadyExistsException)
            {
                Console.WriteLine($"Already existing device [{deviceId}]");
               
                throw;
            }         
            
        }
    }
}

