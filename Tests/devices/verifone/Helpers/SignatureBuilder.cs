using Devices.Verifone.VIPA;
using XO.Requests.DAL;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using XO.Private;

namespace Devices.Verifone.Tests.Helpers
{
    internal static class SignatureBuilder
    {
        public static (LinkDALRequestIPA5Object, int) GetSampleSignatureData()
        {
            // Some fake image data generated
            var sig0 = Encoding.Default.GetBytes("72 32");
            var sig1 = Encoding.Default.GetBytes("[{\"t\":1542,\"x\":120,\"y\":57},{\"t\":1593,\"x\":120,\"y\":68},{\"t\":1597,\"x\":121,\"y\":69},{\"t\":1601,\"x\":121,\"y\":71},{\"t\":1613,\"x\":123,\"y\":76},{\"t\":1617,\"x\":123,\"y\":78},{\"t\":1626,\"x\":125,\"y\":81},{\"t\":1630,\"x\":125,\"y\":83},{\"t\":1634,\"x\":126,\"y\":85},{\"t\":1638,\"x\":126,\"y\":87},{\"t\":1646,\"x\":128,\"y\":91},{\"t\":1650,\"x\":129,\"y\":92},{\"t\":0,\"x\":-1,\"y\":-1},{\"t\":1822,\"x\":205,\"y\":49},{\"t\":1834,\"x\":204,\"y\":49},{\"t\":1860,\"x\":204,\"y\":54},{\"t\":1864,\"x\":203,\"y\":57},{\"t\":1872,\"x\":203,\"y\":64},{\"t\":1876,\"x\":202,\"y\":69},{\"t\":1884,\"x\":202,\"y\":77},{\"t\":1892,\"x\":204,\"y\":86},{\"t\":1909,\"x\":208,\"y\":99},{\"t\":0,\"x\":-1,\"y\":-1},{\"t\":2270,\"x\":98,\"y\":128},{\"t\":2290,\"x\":100,\"y\":131},{\"t\":2294,\"x\":101,\"y\":131},{\"t\":2298,\"x\":101,\"y\":132},{\"t\":2312,\"x\":106,\"y\":137},{\"t\":2316,\"x\":107,\"y\":139},{\"t\":2320,\"x\":110,\"y\":141},{\"t\":2324,\"x\":112,\"y\":143},{\"t\":2328,\"x\":115,\"y\":145},{\"t\":2332,\"x\":118,\"y\":148},{\"t\":2340,\"x\":124,\"y\":152},{\"t\":2357,\"x\":140,\"y\":160},{\"t\":2365,\"x\":149,\"y\":162},{\"t\":2377,\"x\":164,\"y\":162},{\"t\":2381,\"x\":170,\"y\":160},{\"t\":2385,\"x\":175,\"y\":159},{\"t\":2394,\"x\":187,\"y\":153},{\"t\":2398,\"x\":192,\"y\":151},{\"t\":2402,\"x\":196,\"y\":148},{\"t\":2406,\"x\":200,\"y\":146},{\"t\":2414,\"x\":206,\"y\":142},{\"t\":2432,\"x\":214,\"y\":138},{\"t\":0,\"x\":-1,\"y\":-1}]");
            var rentSig0 = ArrayPool<byte>.Shared.Rent(sig0.Length);
            var rentSig1 = ArrayPool<byte>.Shared.Rent(sig1.Length);
            sig0.CopyTo(rentSig0, 0);
            sig1.CopyTo(rentSig1, 0);

            return (new LinkDALRequestIPA5Object()
            {
                SignatureData = new List<byte[]>
                {
                    rentSig0,
                    rentSig1
                },
                SignatureName = "name"
            }, (int)VipaSW1SW2Codes.Success);
        }
    }
}
