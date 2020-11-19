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

        public void LoadJson(string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] resources = assembly.GetManifestResourceNames();
            Stream stream = assembly.GetManifestResourceStream(filename);

            using (StreamReader r = new StreamReader(stream))
            {
                string json = r.ReadToEnd();
                signaturePoints = JsonConvert.DeserializeObject<List<SignatureObject>>(json);
            }
        }

        public PointCollection GetSignaturePoints()
        {
            PointCollection points = new PointCollection();
            foreach (var point in signaturePoints)
            {
                points.Add(new System.Windows.Point(point.x, point.y));
            }
            return points;
        }
    }
}
