using Devices.Common.Helpers;
using Devices.SignatureProcessor;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using Xunit;

namespace Devices.Common.SignatureProcessor.Tests
{
    public class SignaturePointsConverterTests
    {
        private readonly string[] JsonPayloads =
        {
            "5B7B2274223A3935352C2278223A3331392C2279223A3130377D2C7B2274223A3936312C2278223A3331352C2279223A3130337D2C7B2274223A3936382C2278223A3331332C2279223A3130327D2C7B2274223A3937342C2278223A3331312C2279223A3130307D2C7B2274223A3938382C2278223A3330362C2279223A39387D2C7B2274223A3939342C2278223A3330332C2279223A39367D2C7B2274223A313030312C2278223A3330302C2279223A39367D2C7B2274223A313030372C2278223A3239352C2279223A39357D2C7B2274223A313031342C2278223A3239302C2279223A39357D2C7B2274223A313032302C2278223A3238342C2279223A39367D2C7B2274223A313032372C2278223A3237362C2279223A39387D2C7B2274223A313033332C2278223A3236372C2279223A3130317D2C7B2274223A313034302C2278223A3235372C2279223A3130347D2C7B2274223A313034362C2278223A3234362C2279223A3130377D2C7B2274223A313035332C2278223A3233332C2279223A3131317D2C7B2274223A313035392C2278223A3232312C2279223A3131347D2C7B2274223A313036352C2278223A3230382C2279223A3131377D2C7B2274223A313037322C2278223A3139362C2279223A3132307D2C7B2274223A313037382C2278223A3138332C2279223A3132337D2C7B2274223A313038352C2278223A3137312C2279223A3132347D2C7B2274223A313039312C2278223A3136312C2279223A3132367D2C7B2274223A313131302C2278223A3133322C2279223A3132367D2C7B2274223A313131362C2278223A3132352C2279223A3132357D2C7B2274223A313132322C2278223A3131392C2279223A3132337D2C7B2274223A313133352C2278223A3130392C2279223A3131397D2C7B2274223A313134312C2278223A3130362C2279223A3131367D2C7B2274223A313134382C2278223A3130342C2279223A3131327D2C7B2274223A313135342C2278223A3130332C2279223A3130387D2C7B2274223A313136302C2278223A3130332C2279223A3130327D2C7B2274223A313136372C2278223A3130352C2279223A39367D2C7B2274223A313137332C2278223A3130382C2279223A38397D2C7B2274223A313137392C2278223A3131332C2279223A38317D2C7B2274223A313138362C2278223A3131392C2279223A37337D2C7B2274223A313139322C2278223A3132362C2279223A36357D2C7B2274223A313139382C2278223A3133352C2279223A35377D2C7B2274223A313230352C2278223A3134352C2279223A34397D2C7B2274223A313231312C2278223A3135362C2279223A34317D2C7B2274223A313231382C2278223A3136392C2279223A33347D2C7B2274223A313232342C2278223A3138332C2279223A32377D2C7B2274223A313233302C2278223A3139362C2279223A32327D2C7B2274223A313233372C2278223A3231302C2279223A31387D2C7B2274223A313234332C2278223A3232342C2279223A31357D2C7B2274223A313235362C2278223A3235312C2279223A31337D2C7B2274223A313236332C2278223A3236332C2279223A31347D2C7B2274223A313236392C2278223A3237362C2279223A31367D2C7B2274223A313237362C2278223A3238362C2279223A31397D2C7B2274223A313238332C2278223A3239362C2279223A32347D2C7B2274223A313239362C2278223A3331322C2279223A33347D2C7B2274223A313330332C2278223A3331382C2279223A34327D2C7B2274223A313330392C2278223A3332332C2279223A34397D2C7B2274223A313331362C2278223A3332362C2279223A35387D2C7B2274223A313332322C2278223A3332372C2279223A36397D2C7B2274223A313332392C2278223A3332352C2279223A38317D2C7B2274223A313333362C2278223A3332312C2279223A39357D2C7B2274223A313334322C2278223A3331342C2279223A3131307D2C7B2274223A313334392C2278223A3330352C2279223A3132357D2C7B2274223A313335362C2278223A3239352C2279223A3133397D2C7B2274223A313336322C2278223A3238352C2279223A3135317D2C7B2274223A313336392C2278223A3237342C2279223A3136327D2C7B2274223A313337352C2278223A3236322C2279223A3137317D2C7B2274223A313338322C2278223A3235322C2279223A3137387D2C7B2274223A313338382C2278223A3234312C2279223A3138357D2C7B2274223A313339352C2278223A3233302C2279223A3139317D2C7B2274223A313430382C2278223A3231322C2279223A3139397D2C7B2274223A313431342C2278223A3230352C2279223A3230327D2C7B2274223A313432372C2278223A3139342C2279223A3230347D2C7B2274223A313433332C2278223A3138392C2279223A3230347D2C7B2274223A313434362C2278223A3138332C2279223A3230327D2C7B2274223A313435322C2278223A3138312C2279223A3230317D2C7B2274223A313435392C2278223A3137392C2279223A3139397D2C7B2274223A313436352C2278223A3137392C2279223A3139367D2C7B2274223A313437322C2278223A3138312C2279223A3139317D2C7B2274223A313437382C2278223A3138342C2279223A3138357D2C7B2274223A313438352C2278223A3138392C2279223A3137387D2C7B2274223A313439312C2278223A3139372C2279223A3136397D2C7B2274223A313439372C2278223A3230362C2279223A3136307D2C7B2274223A313530342C2278223A3231372C2279223A3135307D2C7B2274223A313531302C2278223A3232382C2279223A3134317D2C7B2274223A313532332C2278223A3235322C2279223A3132337D2C7B2274223A313533302C2278223A3236332C2279223A3131367D2C7B2274223A313533372C2278223A3237332C2279223A3130397D2C7B2274223A313534332C2278223A3238342C2279223A3130327D2C7B2274223A313535362C2278223A3330342C2279223A39337D2C7B2274223A313536332C2278223A3331342C2279223A38397D2C7B2274223A313537302C2278223A3332332C2279223A38377D2C7B2274223A313537362C2278223A3333312C2279223A38367D2C7B2274223A313538332C2278223A3333392C2279223A38367D2C7B2274223A313539302C2278223A3334362C2279223A38377D2C7B2274223A313539362C2278223A3335342C2279223A38397D2C7B2274223A313630332C2278223A3336312C2279223A39327D2C7B2274223A313631302C2278223A3336372C2279223A39367D2C7B2274223A313632332C2278223A3338312C2279223A3130347D2C7B2274223A313633302C2278223A3338372C2279223A3130397D2C7B2274223A313634322C2278223A3339392C2279223A3131377D2C7B2274223A313635362C2278223A3431332C2279223A3132357D2C7B2274223A313636322C2278223A3431392C2279223A3132387D2C7B2274223A313636392C2278223A3432372C2279223A3133307D2C7B2274223A313638322C2278223A3434322C2279223A3133327D2C7B2274223A313638392C2278223A3435312C2279223A3133327D2C7B2274223A313730322C2278223A3436392C2279223A3132387D2C7B2274223A313730382C2278223A3437382C2279223A3132357D2C7B2274223A313731352C2278223A3438372C2279223A3132317D2C7B2274223A313732382C2278223A3530332C2279223A3131327D2C7B2274223A313734322C2278223A3531372C2279223A3130327D2C7B2274223A313735352C2278223A3532372C2279223A39327D2C7B2274223A313736382C2278223A3533342C2279223A38337D2C7B2274223A313737342C2278223A3533362C2279223A37397D2C7B2274223A313738312C2278223A3533372C2279223A37357D2C7B2274223A313738382C2278223A3533372C2279223A37317D2C7B2274223A313739342C2278223A3533362C2279223A36377D2C7B2274223A313830312C2278223A3533332C2279223A36337D2C7B2274223A313830382C2278223A3532392C2279223A36307D2C7B2274223A313831342C2278223A3532332C2279223A35367D2C7B2274223A313832312C2278223A3531362C2279223A35347D2C7B2274223A313832382C2278223A3530382C2279223A35327D2C7B2274223A313833342C2278223A3439382C2279223A35317D2C7B2274223A313834312C2278223A3438382C2279223A35317D2C7B2274223A313834372C2278223A3437372C2279223A35327D2C7B2274223A313835342C2278223A3436362C2279223A35357D2C7B2274223A313836312C2278223A3435362C2279223A35397D2C7B2274223A313836372C2278223A3434352C2279223A36337D2C7B2274223A313837342C2278223A3433352C2279223A36397D2C7B2274223A313838312C2278223A3432362C2279223A37357D2C7B2274223A313838372C2278223A3431382C2279223A38317D2C7B2274223A313930312C2278223A3430352C2279223A39347D2C7B2274223A313930372C2278223A3430302C2279223A3130307D2C7B2274223A313931342C2278223A3339362C2279223A3130367D2C7B2274223A313932302C2278223A3339342C2279223A3131327D2C7B2274223A313932372C2278223A3339322C2279223A3131377D2C7B2274223A313933342C2278223A3339312C2279223A3132317D2C7B2274223A313934312C2278223A3339312C2279223A3132357D2C7B2274223A313934372C2278223A3339332C2279223A3132397D2C7B2274223A313935342C2278223A3339362C2279223A3133327D2C7B2274223A313936312C2278223A3430332C2279223A3133357D2C7B2274223A313936372C2278223A3431342C2279223A3133377D2C7B2274223A313937342C2278223A3433312C2279223A3133377D2C7B2274223A313938302C2278223A3435352C2279223A3133357D2C7B2274223A313938372C2278223A3438322C2279223A3133327D2C7B2274223A313939332C2278223A3530382C2279223A3132387D2C7B2274223A323030302C2278223A3533332C2279223A3132337D2C7B2274223A323030372C2278223A3535372C2279223A3131397D2C7B2274223A323031332C2278223A3537392C2279223A3131367D2C7B2274223A323032302C2278223A3539392C2279223A3131347D2C7B2274223A323032362C2278223A3631372C2279223A3131337D2C7B2274223A323033332C2278223A3633332C2279223A3131327D2C7B2274223A323033392C2278223A3634372C2279223A3131337D2C7B2274223A323034352C2278223A3635382C2279223A3131347D2C7B2274223A323035322C2278223A3636382C2279223A3131367D2C7B2274223A323035382C2278223A3637362C2279223A3131397D2C7B2274223A323036352C2278223A3638332C2279223A3132327D2C7B2274223A323037312C2278223A3638392C2279223A3132357D2C7B2274223A323037382C2278223A3639342C2279223A3132397D2C7B2274223A323039302C2278223A3730302C2279223A3133377D2C7B2274223A323039372C2278223A3730322C2279223A3134327D2C7B2274223A323130392C2278223A3730342C2279223A3135337D2C7B2274223A323132392C2278223A3730342C2279223A3136397D2C7B2274223A323133352C2278223A3730352C2279223A3137337D2C7B2274223A323134322C2278223A3730372C2279223A3137387D2C7B2274223A323134382C2278223A3731302C2279223A3138327D2C7B2274223A323135342C2278223A3731372C2279223A3138377D2C7B2274223A323136312C2278223A3732382C2279223A3139317D2C7B2274223A323136372C2278223A3734362C2279223A3139367D2C7B2274223A323137332C2278223A3737312C2279223A3139397D2C7B2274223A323138302C2278223A3739362C2279223A3230317D2C7B2274223A323139322C2278223A3832382C2279223A3230317D2C7B2274223A323139382C2278223A3833362C2279223A3230327D2C7B2274223A323230342C2278223A3834322C2279223A3230337D2C7B2274223A302C2278223A2D312C2279223A2D317D5D90002C7B2274223A323031342C2278223A3638342C2279223A3134357D2C7B2274223A323032362C2278223A3730302C2279223A3133367D2C7B2274223A323033332C2278223A3730372C2279223A3133337D2C7B2274223A323033392C2278223A3731332C2279223A3133317D2C7B2274223A323034352C2278223A3731392C2279223A3133307D2C7B2274223A323035382C2278223A3732392C2279223A3133307D2C7B2274223A323036342C2278223A3733352C2279223A3133327D2C7B2274223A323037312C2278223A3734302C2279223A3133357D2C7B2274223A323037372C2278223A3734362C2279223A3133397D2C7B2274223A323038332C2278223A3735312C2279223A3134347D2C7B2274223A323039302C2278223A3735352C2279223A3134397D2C7B2274223A323039362C2278223A3735392C2279223A3135357D2C7B2274223A323130322C2278223A3736332C2279223A3136307D2C7B2274223A323130382C2278223A3736372C2279223A3136367D2C7B2274223A323131342C2278223A3737322C2279223A3137317D2C7B2274223A323132312C2278223A3737382C2279223A3137367D2C7B2274223A323132372C2278223A3738362C2279223A3138307D2C7B2274223A323133332C2278223A3830302C2279223A3138337D2C7B2274223A323133392C2278223A3831382C2279223A3138347D2C7B2274223A302C2278223A2D312C2279223A2D317D5D",
            "5B7B2274223A3736372C2278223A3431362C2279223A3133307D2C7B2274223A3832302C2278223A3432302C2279223A3132357D2C7B2274223A3833322C2278223A3432322C2279223A3132347D2C7B2274223A3833382C2278223A3432342C2279223A3132347D2C7B2274223A3835362C2278223A3432382C2279223A3132327D2C7B2274223A3836382C2278223A3433322C2279223A3132327D2C7B2274223A3838312C2278223A3433372C2279223A3132307D2C7B2274223A3838372C2278223A3434302C2279223A3132307D2C7B2274223A3931312C2278223A3435382C2279223A3131337D2C7B2274223A3931372C2278223A3436342C2279223A3131317D2C7B2274223A3932332C2278223A3437312C2279223A3130397D2C7B2274223A3932392C2278223A3437372C2279223A3130377D2C7B2274223A3933352C2278223A3438342C2279223A3130347D2C7B2274223A3934312C2278223A3439312C2279223A3130327D2C7B2274223A3934372C2278223A3439372C2279223A39397D2C7B2274223A3935332C2278223A3530342C2279223A39377D2C7B2274223A3935392C2278223A3531312C2279223A39347D2C7B2274223A3936352C2278223A3531372C2279223A39327D2C7B2274223A3937312C2278223A3532342C2279223A39307D2C7B2274223A3937362C2278223A3533312C2279223A38377D2C7B2274223A3938332C2278223A3533382C2279223A38357D2C7B2274223A3938392C2278223A3534342C2279223A38337D2C7B2274223A313031322C2278223A3537322C2279223A37357D2C7B2274223A313031382C2278223A3537392C2279223A37347D2C7B2274223A313032342C2278223A3538352C2279223A37327D2C7B2274223A313033302C2278223A3539322C2279223A37307D2C7B2274223A313033362C2278223A3539382C2279223A36387D2C7B2274223A313034322C2278223A3630342C2279223A36377D2C7B2274223A313034382C2278223A3631302C2279223A36357D2C7B2274223A313036362C2278223A3632362C2279223A36327D2C7B2274223A313037372C2278223A3633342C2279223A36307D2C7B2274223A313038332C2278223A3633382C2279223A36307D2C7B2274223A313038392C2278223A3634322C2279223A35397D2C7B2274223A313130302C2278223A3634372C2279223A35397D2C7B2274223A313130362C2278223A3635302C2279223A36307D2C7B2274223A313131322C2278223A3635322C2279223A36307D2C7B2274223A313131382C2278223A3635342C2279223A36317D2C7B2274223A302C2278223A2D312C2279223A2D317D5D",
        };
        const int jsonPayloadToTest = 1;

        private readonly byte[] SignaturePayload;

        public SignaturePointsConverterTests()
        {
            SignaturePayload = ConversionHelper.HexToByteArray(JsonPayloads[jsonPayloadToTest]);
        }

        [Fact]
        public void ConvertPointsToImage_Returns_MemoryStream()
        {
            string json = "[{\"t\":7227,\"x\":88,\"y\":83},{\"t\":7247,\"x\":87,\"y\":81},{\"x\":-1,\"y\":-1}]";
            byte[] signaturePointsInBytes = ConversionHelper.AsciiToByte(json);
            byte[] signatureStream = SignaturePointsConverter.ConvertPointsToImage(signaturePointsInBytes);
            Assert.NotNull(signatureStream);
        }

        [Theory]
        [InlineData("[{\"t\":7227,\"x\":88,\"y\":83},{\"t\":7247,\"x\":87,\"y\":81},{\"t\":7257,\"x\":-1,\"y\":-1}]", 1, new int[] { 2 })]
        [InlineData("[{\"t\":1,\"x\":80,\"y\":81},{\"t\":2,\"x\":82,\"y\":83},{\"t\":3,\"x\":-1,\"y\":-1},{\"t\":4,\"x\":84,\"y\":85},{\"t\":5,\"x\":86,\"y\":87},{\"t\":6,\"x\":-1,\"y\":-1}]", 2, new int[] { 2, 2 })]
        [InlineData("[{\"t\":1,\"x\":0,\"y\":1},{\"t\":2,\"x\":2,\"y\":3},{\"t\":3,\"x\":-1,\"y\":-1},{\"t\":4,\"x\":4,\"y\":5},{\"t\":5,\"x\":6,\"y\":7},{\"t\":6,\"x\":-1,\"y\":-1},{\"t\":7,\"x\":8,\"y\":9},{\"t\":8,\"x\":-1,\"y\":-1}]", 3, new int[] { 2, 2, 1 })]
        public void FormatPointsForBitmap_Returns_AppropriatelySizedPointList_WhenCalled(string json, int arraySize, int[] fields)
        {
            List<SignatureObject> signaturePoints = JsonConvert.DeserializeObject<List<SignatureObject>>(json);
            List<PointF[]> result = SignaturePointsConverter.FormatPointsForBitmap(signaturePoints);
            Assert.Equal(arraySize, result.Count);
            int index = 0;
            foreach (var point in result)
            {
                Assert.Equal(point.Length, fields[index++]);
            }
        }

        [Fact]
        public void SignaturePayload_Convert_ToSetOfPoints_WithAnomalliedJsonPayload_Properly()
        {
            byte[] signatureStream = SignaturePointsConverter.ConvertPointsToImage(SignaturePayload);
            Assert.NotNull(signatureStream);
        }
    }
}
