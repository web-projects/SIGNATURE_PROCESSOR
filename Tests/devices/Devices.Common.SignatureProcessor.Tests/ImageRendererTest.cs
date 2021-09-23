using Devices.SignatureProcessor;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Xunit;

namespace Devices.Common.SignatureProcessor.Tests
{
    public class ImageRendererTests
    {
        const string skipfact = "Device Only";  // set this to null to run skipable tests

        /// <summary>
        /// Given a set of points, test that an image is constructed from the point collection
        /// </summary>
        [Fact]
        public void CreateBitmapFromPoints_ReturnsBitmap_WhenCalled()
        {
            string json = "[{\"t\":6153,\"x\":373,\"y\":131},{\"t\":6174,\"x\":371,\"y\":129},{ \"t\":6202,\"x\":366,\"y\":127},{ \"t\":6225,\"x\":361,\"y\":124},{ \"t\":6241,\"x\":357,\"y\":120},{ \"t\":6247,\"x\":355,\"y\":119},{ \"t\":6253,\"x\":353,\"y\":117},{ \"t\":6258,\"x\":351,\"y\":116},{ \"t\":6264,\"x\":349,\"y\":114},{ \"t\":6269,\"x\":346,\"y\":112},{ \"t\":6275,\"x\":343,\"y\":111},{ \"t\":6281,\"x\":340,\"y\":109},{ \"t\":6292,\"x\":333,\"y\":107},{ \"t\":6298,\"x\":330,\"y\":105},{ \"t\":6315,\"x\":319,\"y\":102},{ \"t\":6320,\"x\":315,\"y\":100},{ \"t\":6337,\"x\":304,\"y\":97},{ \"t\":6354,\"x\":295,\"y\":94},{ \"t\":6370,\"x\":288,\"y\":90},{ \"t\":6376,\"x\":286,\"y\":88},{ \"t\":6382,\"x\":284,\"y\":87},{ \"t\":6398,\"x\":280,\"y\":83},{ \"t\":6404,\"x\":279,\"y\":81},{ \"t\":6409,\"x\":279,\"y\":79},{ \"t\":6426,\"x\":283,\"y\":73},{ \"t\":6437,\"x\":290,\"y\":68},{ \"t\":6443,\"x\":295,\"y\":64},{ \"t\":6454,\"x\":307,\"y\":58},{ \"t\":6466,\"x\":323,\"y\":52},{ \"t\":6471,\"x\":330,\"y\":50},{ \"t\":6477,\"x\":338,\"y\":48},{ \"t\":6489,\"x\":355,\"y\":46},{ \"t\":6505,\"x\":380,\"y\":46},{ \"t\":6511,\"x\":388,\"y\":47},{ \"t\":6517,\"x\":395,\"y\":49},{ \"t\":6522,\"x\":403,\"y\":51},{ \"t\":6528,\"x\":410,\"y\":53},{ \"t\":6533,\"x\":417,\"y\":56},{ \"t\":6539,\"x\":423,\"y\":59},{ \"t\":6545,\"x\":429,\"y\":63},{ \"t\":6551,\"x\":434,\"y\":67},{ \"t\":6556,\"x\":440,\"y\":73},{ \"t\":6562,\"x\":444,\"y\":78},{ \"t\":6567,\"x\":449,\"y\":83},{ \"t\":6573,\"x\":452,\"y\":89},{ \"t\":6578,\"x\":455,\"y\":96},{ \"t\":6584,\"x\":458,\"y\":102},{ \"t\":6590,\"x\":459,\"y\":108},{ \"t\":6595,\"x\":461,\"y\":114},{ \"t\":6607,\"x\":461,\"y\":129},{ \"t\":6624,\"x\":455,\"y\":147},{ \"t\":6629,\"x\":452,\"y\":153},{ \"t\":6640,\"x\":444,\"y\":165},{ \"t\":6652,\"x\":435,\"y\":174},{ \"t\":6657,\"x\":430,\"y\":178},{ \"t\":6669,\"x\":420,\"y\":184},{ \"t\":6675,\"x\":414,\"y\":186},{ \"t\":6680,\"x\":409,\"y\":188},{ \"t\":6686,\"x\":404,\"y\":189},{ \"t\":6691,\"x\":399,\"y\":189},{ \"t\":6697,\"x\":395,\"y\":190},{ \"t\":6708,\"x\":387,\"y\":190},{ \"t\":6719,\"x\":380,\"y\":188},{ \"t\":6731,\"x\":374,\"y\":184},{ \"t\":6736,\"x\":372,\"y\":181},{ \"t\":6742,\"x\":371,\"y\":178},{ \"t\":6753,\"x\":371,\"y\":170},{ \"t\":6759,\"x\":373,\"y\":165},{ \"t\":6764,\"x\":375,\"y\":159},{ \"t\":6770,\"x\":378,\"y\":153},{ \"t\":6776,\"x\":382,\"y\":147},{ \"t\":6787,\"x\":393,\"y\":134},{ \"t\":6798,\"x\":405,\"y\":122},{ \"t\":6804,\"x\":411,\"y\":117},{ \"t\":6809,\"x\":418,\"y\":113},{ \"t\":6821,\"x\":430,\"y\":105},{ \"t\":6827,\"x\":437,\"y\":101},{ \"t\":6832,\"x\":444,\"y\":98},{ \"t\":6838,\"x\":451,\"y\":94},{ \"t\":6843,\"x\":459,\"y\":91},{ \"t\":6849,\"x\":468,\"y\":88},{ \"t\":6855,\"x\":476,\"y\":86},{ \"t\":6860,\"x\":486,\"y\":83},{ \"t\":6866,\"x\":496,\"y\":81},{ \"t\":6872,\"x\":506,\"y\":80},{ \"t\":6877,\"x\":518,\"y\":79},{ \"t\":6883,\"x\":528,\"y\":78},{ \"t\":6889,\"x\":539,\"y\":78},{ \"t\":6894,\"x\":548,\"y\":79},{ \"t\":6900,\"x\":558,\"y\":79},{ \"t\":6905,\"x\":566,\"y\":81},{ \"t\":6911,\"x\":574,\"y\":82},{ \"t\":6917,\"x\":582,\"y\":82},{ \"t\":0,\"x\":-1,\"y\":-1}]";
            List<SignatureObject> signaturePoints = JsonConvert.DeserializeObject<List<SignatureObject>>(json);
            List<PointF[]> pointCollection = SignaturePointsConverter.FormatPointsForBitmap(signaturePoints);
            Bitmap signatureBitmap = ImageRenderer.CreateBitmapFromPoints(pointCollection);
            Assert.NotNull(signatureBitmap);
        }

        /// <summary>
        /// Run this test locally only to validate expected output
        /// </summary>
        [Fact(Skip = skipfact)]
        public void CreateImageFromStream_CreatesBitmap_WhenCalled()
        {
            Bitmap signatureBmp = new Bitmap(200, 160);

            using (var memoryStream = new MemoryStream())
            {
                signatureBmp.Save(memoryStream, ImageFormat.Png);
                byte[] imageBytes = memoryStream.ToArray();
                ImageRenderer.CreateImageFromStream(imageBytes);
                Assert.True(File.Exists("C:\\Temp\\Signature.png"));
            }
        }
    }
}
