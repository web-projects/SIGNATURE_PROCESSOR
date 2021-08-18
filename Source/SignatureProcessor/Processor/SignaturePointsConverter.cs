using Newtonsoft.Json;
using SignatureProcessor.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Media;

namespace SignatureProcessor.Processor
{
    public class SignaturePointsConverter
    {
        private List<SignatureObject> signaturePoints;

        public void LoadJsonFromResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();
            Stream stream = assembly.GetManifestResourceStream(resourceName);

            if (stream != null)
            {
                using (StreamReader r = new StreamReader(stream))
                {
                    string json = r.ReadToEnd();
                    string worker = JsonConvert.SerializeObject(json, Formatting.None);
                    byte[] jsonArray = ConversionHelper.AsciiToByte(worker);
                    string convertedArray = BitConverter.ToString(jsonArray).Replace("-", "");
                    signaturePoints = JsonConvert.DeserializeObject<List<SignatureObject>>(json);
                }
            }
        }

        public void LoadJsonFromStream(Stream stream)
        {
            using (StreamReader r = new StreamReader(stream))
            {
                string json = r.ReadToEnd();
                byte[] deCypheredArray = ConversionHelper.HexToByteArray(json);
                signaturePoints = JsonConvert.DeserializeObject<List<SignatureObject>>(json);
            }
        }

        public List<PointCollection> GetSignaturePoints()
        {
            List<PointCollection> points = new List<PointCollection>();
            PointCollection collection = new PointCollection();
            if (signaturePoints != null)
            {
                foreach (var point in signaturePoints)
                {
                    // identify stroke separator
                    if (point.x == -1 && point.x == -1)
                    {
                        points.Add(collection);
                        collection = new PointCollection();
                        continue;
                    }
                    else
                    {
                        collection.Add(new System.Windows.Point(point.x, point.y));
                    }
                }
            }
            return points;
        }
    }
}
