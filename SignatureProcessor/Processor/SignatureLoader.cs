using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Media;

namespace SignatureProcessor.Processor
{
    public class SignatureLoader
    {
        private List<SignatureObject> signaturePoints;

        public void LoadJsonFromResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();
            Stream stream = assembly.GetManifestResourceStream(resourceName);

            using (StreamReader r = new StreamReader(stream))
            {
                string json = r.ReadToEnd();
                signaturePoints = JsonConvert.DeserializeObject<List<SignatureObject>>(json);
            }
        }

        public void LoadJsonFromStream(Stream stream)
        {
            using (StreamReader r = new StreamReader(stream))
            {
                string json = r.ReadToEnd();
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
