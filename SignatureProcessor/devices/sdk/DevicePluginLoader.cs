using Devices.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace DEVICE_SDK.Sdk
{
    public class DevicePluginLoader : IDevicePluginLoader
    {
        [Import]
        public IEnumerable<ICardDevice> Devices { get; set; }

        private ContainerConfiguration CreateContainerConfiguration(string pluginPath)
        {
            // Determine the current entry assembly location because we are expecting to find 
            // a drivers folder directly underneath it where our drivers are located.
            string assemblyLocation = Assembly.GetEntryAssembly().Location;

            // If the directory doesn't even exist then there is nothing for us to do.
            if (!Directory.Exists(pluginPath))
            {
                return null;
            }

            // Locate all of the DLL files in the Drivers folder at any nested level since
            // these will be examined by the composition host to discover desired types.
            var assemblies = Directory
                .GetFiles(pluginPath, "*.dll", SearchOption.AllDirectories)
                .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)
                .ToList();

            // Create the container configuration with the set of assemblies located.
            return new ContainerConfiguration().WithAssemblies(assemblies);
        }

        public List<ICardDevice> FindAvailableDevices(string pluginPath)
        {
            try
            {
                // Create the composition host and try to find exports of base or derived type ICardDevice.
                using (CompositionHost container = CreateContainerConfiguration(pluginPath).CreateContainer())
                {
                    Devices = container.GetExports<ICardDevice>();
                }
            }
            catch (CompositionFailedException ex)
            {
                Console.Error.WriteLine($"Device discovery failed on composition builder: {ex}");
            }

            return Devices.ToList();
        }

        public List<string> GetAvailableDevicePlugins()
        {
            return Devices.Select(a => a.GetType().Name).ToList();
        }

        public void LoadDevicePlugin(string deviceName)
        {
            var device = Devices.Where(a => a.GetType().Name == deviceName).FirstOrDefault();
            if (device == null)
            {
                Console.WriteLine($"Device name {deviceName} does not exist. Device plugin not loaded.");
                return;
            }

            Console.WriteLine($"Device plugin name: {device.GetType().Name}.");
        }
    }
}
