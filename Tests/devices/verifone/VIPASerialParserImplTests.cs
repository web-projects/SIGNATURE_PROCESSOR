﻿using Devices.Common;
using Devices.Common.Helpers;
using Devices.Verifone.Connection;
using Moq;
using System;
using TestHelper;
using Xunit;

namespace Devices.Verifone.Tests
{
    public class VIPASerialParserImplTests : IDisposable
    {
        private readonly string ComPort = "COM9";
        private readonly Mock<DeviceLogHandler> mockDeviceLogHandler;

        private readonly string[] SignatureInput = 
        { 
            // CapturedSignaturePayload.001
            "0101FEDFAA050400000000DFAA020963616E63656C42746EDFAA0300DFAA02026F6BDFAA0300DFAA020C7369676E617475726554776FDFAA0382116E5B7B2274223A313233362C2278223A3333312C2279223A3131397D2C7B2274223A313235362C2278223A3332302C2279223A3131327D2C7B2274223A313236332C2278223A3331352C2279223A3131307D2C7B2274223A313237302C2278223A3330392C2279223A3130387D2C7B2274223A313238342C2278223A3239352C2279223A3130347D2C7B2274223A313239382C2278223A3237392C2279223A3130307D2C7B2274223A313330352C2278223A3236392C2279223A39387D2C7B2274223A313331000101FE322C2278223A3236302C2279223A39367D2C7B2274223A313332362C2278223A3234302C2279223A39327D2C7B2274223A313333332C2278223A3233312C2279223A39307D2C7B2274223A313334302C2278223A3232332C2279223A38377D2C7B2274223A313334372C2278223A3231362C2279223A38357D2C7B2274223A313336312C2278223A3230342C2279223A37397D2C7B2274223A313336392C2278223A3230302C2279223A37357D2C7B2274223A313337362C2278223A3139372C2279223A37317D2C7B2274223A313338332C2278223A3139352C2279223A36367D2C7B2274223A313339302C2278223A3139352C2279223A36317D2C7B22A70101FE74223A313339372C2278223A3139362C2279223A35367D2C7B2274223A313430342C2278223A3139392C2279223A34397D2C7B2274223A313431312C2278223A3230342C2279223A34337D2C7B2274223A313431382C2278223A3231312C2279223A33367D2C7B2274223A313432352C2278223A3232302C2279223A32397D2C7B2274223A313433322C2278223A3233302C2279223A32337D2C7B2274223A313433392C2278223A3234302C2279223A31387D2C7B2274223A313434362C2278223A3235332C2279223A31347D2C7B2274223A313435332C2278223A3236352C2279223A31307D2C7B2274223A313436372C2278223A3239302C2279223AF20101FE387D2C7B2274223A313437342C2278223A3330332C2279223A387D2C7B2274223A313438312C2278223A3331362C2279223A31307D2C7B2274223A313439352C2278223A3334302C2279223A31367D2C7B2274223A313530322C2278223A3335312C2279223A32317D2C7B2274223A313530392C2278223A3336312C2279223A32367D2C7B2274223A313531362C2278223A3337302C2279223A33327D2C7B2274223A313532332C2278223A3337382C2279223A33397D2C7B2274223A313533302C2278223A3338352C2279223A34367D2C7B2274223A313534342C2278223A3339352C2279223A36327D2C7B2274223A313535312C2278223A3339382CB70101FE2279223A37327D2C7B2274223A313535382C2278223A3430302C2279223A38317D2C7B2274223A313536352C2278223A3430302C2279223A39327D2C7B2274223A313537322C2278223A3339382C2279223A3130337D2C7B2274223A313537392C2278223A3339352C2279223A3131347D2C7B2274223A313538362C2278223A3339312C2279223A3132357D2C7B2274223A313539332C2278223A3338362C2279223A3133357D2C7B2274223A313630302C2278223A3338302C2279223A3134357D2C7B2274223A313630372C2278223A3337332C2279223A3135347D2C7B2274223A313631342C2278223A3336362C2279223A3136327D2C7B2274223AB90101FE313632382C2278223A3335322C2279223A3137347D2C7B2274223A313633352C2278223A3334352C2279223A3137387D2C7B2274223A313634322C2278223A3333382C2279223A3138317D2C7B2274223A313634392C2278223A3333322C2279223A3138337D2C7B2274223A313635362C2278223A3332352C2279223A3138347D2C7B2274223A313636332C2278223A3332302C2279223A3138347D2C7B2274223A313637302C2278223A3331352C2279223A3138337D2C7B2274223A313637372C2278223A3331302C2279223A3138307D2C7B2274223A313638342C2278223A3330362C2279223A3137377D2C7B2274223A313639312C2278223A3330F10101FE332C2279223A3137347D2C7B2274223A313730352C2278223A3239392C2279223A3136347D2C7B2274223A313731322C2278223A3239382C2279223A3135377D2C7B2274223A313732302C2278223A3239392C2279223A3135307D2C7B2274223A313732372C2278223A3330302C2279223A3134317D2C7B2274223A313733342C2278223A3330322C2279223A3133327D2C7B2274223A313735352C2278223A3331342C2279223A3130327D2C7B2274223A313736322C2278223A3331392C2279223A39347D2C7B2274223A313736392C2278223A3332342C2279223A38357D2C7B2274223A313737362C2278223A3333302C2279223A37377D2C7B2274B90101FE223A313738332C2278223A3333352C2279223A37307D2C7B2274223A313739302C2278223A3334312C2279223A36347D2C7B2274223A313739372C2278223A3334372C2279223A35397D2C7B2274223A313830342C2278223A3335342C2279223A35357D2C7B2274223A313831312C2278223A3336302C2279223A35327D2C7B2274223A313831392C2278223A3336372C2279223A34397D2C7B2274223A313832352C2278223A3337352C2279223A34387D2C7B2274223A313833392C2278223A3339312C2279223A35307D2C7B2274223A313834362C2278223A3430302C2279223A35337D2C7B2274223A313835332C2278223A3430392C2279223A35B80101FE377D2C7B2274223A313836302C2278223A3431382C2279223A36327D2C7B2274223A313836372C2278223A3432382C2279223A36387D2C7B2274223A313837342C2278223A3433382C2279223A37367D2C7B2274223A313838312C2278223A3434382C2279223A38337D2C7B2274223A313839352C2278223A3436362C2279223A39377D2C7B2274223A313930322C2278223A3437352C2279223A3130337D2C7B2274223A313930392C2278223A3438332C2279223A3130397D2C7B2274223A313931362C2278223A3439312C2279223A3131347D2C7B2274223A313932322C2278223A3530302C2279223A3131397D2C7B2274223A313933302C227822AA0101FE3A3530382C2279223A3132327D2C7B2274223A313933372C2278223A3531372C2279223A3132357D2C7B2274223A313934342C2278223A3532382C2279223A3132367D2C7B2274223A313935312C2278223A3533382C2279223A3132367D2C7B2274223A313935382C2278223A3534392C2279223A3132357D2C7B2274223A313936352C2278223A3536312C2279223A3132327D2C7B2274223A313937322C2278223A3537332C2279223A3131377D2C7B2274223A313937392C2278223A3538342C2279223A3131317D2C7B2274223A313938362C2278223A3539342C2279223A3130357D2C7B2274223A313939332C2278223A3630332C2279223A3938F40101FE7D2C7B2274223A323030332C2278223A3631312C2279223A39307D2C7B2274223A323030372C2278223A3631382C2279223A38327D2C7B2274223A323031342C2278223A3632342C2279223A37347D2C7B2274223A323032312C2278223A3632382C2279223A36367D2C7B2274223A323032382C2278223A3633312C2279223A35397D2C7B2274223A323033352C2278223A3633342C2279223A35317D2C7B2274223A323034322C2278223A3633352C2279223A34347D2C7B2274223A323034392C2278223A3633352C2279223A33377D2C7B2274223A323036332C2278223A3633332C2279223A32357D2C7B2274223A323037302C2278223A3633302CB20101FE2279223A32307D2C7B2274223A323037372C2278223A3632362C2279223A31347D2C7B2274223A323038342C2278223A3632332C2279223A397D2C7B2274223A323039312C2278223A3631382C2279223A357D2C7B2274223A323039382C2278223A3631322C2279223A327D2C7B2274223A323130352C2278223A3630372C2279223A307D2C7B2274223A323132362C2278223A3538332C2279223A307D2C7B2274223A323133332C2278223A3537322C2279223A347D2C7B2274223A323134302C2278223A3536302C2279223A397D2C7B2274223A323135342C2278223A3533362C2279223A32357D2C7B2274223A323136312C2278223A3532352C22E80101FE79223A33347D2C7B2274223A323137352C2278223A3530362C2279223A35357D2C7B2274223A323138322C2278223A3439382C2279223A36367D2C7B2274223A323138392C2278223A3439312C2279223A37377D2C7B2274223A323139362C2278223A3438362C2279223A38377D2C7B2274223A323230332C2278223A3438332C2279223A39377D2C7B2274223A323231302C2278223A3438302C2279223A3130367D2C7B2274223A323231372C2278223A3437392C2279223A3131337D2C7B2274223A323233312C2278223A3438312C2279223A3132357D2C7B2274223A323233372C2278223A3438332C2279223A3133307D2C7B2274223A32323434A70101FE2C2278223A3438362C2279223A3133347D2C7B2274223A323235302C2278223A3439302C2279223A3133387D2C7B2274223A323235372C2278223A3439372C2279223A3134307D2C7B2274223A323236342C2278223A3530362C2279223A3134317D2C7B2274223A323237302C2278223A3532302C2279223A3133397D2C7B2274223A323237372C2278223A3533362C2279223A3133357D2C7B2274223A323238342C2278223A3535342C2279223A3132397D2C7B2274223A323239302C2278223A3537302C2279223A3132337D2C7B2274223A323239372C2278223A3538362C2279223A3131367D2C7B2274223A323330332C2278223A3539392C2279B50101FE223A3131307D2C7B2274223A323331302C2278223A3631302C2279223A3130347D2C7B2274223A323331362C2278223A3632302C2279223A39387D2C7B2274223A323332322C2278223A3632392C2279223A39337D2C7B2274223A323332392C2278223A3633372C2279223A38397D2C7B2274223A323334322C2278223A3634372C2279223A38337D2C7B2274223A323335352C2278223A3635332C2279223A38317D2C7B2274223A323336312C2278223A3635352C2279223A38307D2C7B2274223A323336382C2278223A3635382C2279223A38307D2C7B2274223A323338302C2278223A3636312C2279223A38327D2C7B2274223A323338372C2278A60101FE223A3636322C2279223A38347D2C7B2274223A323430302C2278223A3636342C2279223A39327D2C7B2274223A323430362C2278223A3636342C2279223A39387D2C7B2274223A323431332C2278223A3636332C2279223A3130367D2C7B2274223A323431392C2278223A3636332C2279223A3131337D2C7B2274223A323433322C2278223A3636312C2279223A3132397D2C7B2274223A323434352C2278223A3636312C2279223A3134357D2C7B2274223A323435312C2278223A3636332C2279223A3135317D2C7B2274223A323435382C2278223A3636342C2279223A3135377D2C7B2274223A323436342C2278223A3636372C2279223A3136337DA80101FE2C7B2274223A323437312C2278223A3637302C2279223A3136387D2C7B2274223A323437372C2278223A3637352C2279223A3137327D2C7B2274223A323438342C2278223A3638312C2279223A3137367D2C7B2274223A323439302C2278223A3639302C2279223A3137387D2C7B2274223A323439362C2278223A3730322C2279223A3137397D2C7B2274223A323530332C2278223A3731372C2279223A3137387D2C7B2274223A323530392C2278223A3733342C2279223A3137367D2C7B2274223A323532322C2278223A3736382C2279223A3136387D2C7B2274223A323532382C2278223A3738332C2279223A3136337D2C7B2274223A323533342CAF0100CB2278223A3739372C2279223A3135397D2C7B2274223A323534362C2278223A3831382C2279223A3135327D2C7B2274223A323535392C2278223A3833322C2279223A3134367D2C7B2274223A323536352C2278223A3833372C2279223A3134337D2C7B2274223A323537312C2278223A3834322C2279223A3134317D2C7B2274223A323537372C2278223A3834362C2279223A3133397D2C7B2274223A323538332C2278223A3834392C2279223A3133387D2C7B2274223A302C2278223A2D312C2279223A2D317D5D900005",
            // CapturedSignaturePayload.002
            "0101FEDFAA050400000000DFAA020963616E63656C42746EDFAA0300DFAA02026F6BDFAA0300DFAA020C7369676E617475726554776FDFAA038211705B7B2274223A3935352C2278223A3331392C2279223A3130377D2C7B2274223A3936312C2278223A3331352C2279223A3130337D2C7B2274223A3936382C2278223A3331332C2279223A3130327D2C7B2274223A3937342C2278223A3331312C2279223A3130307D2C7B2274223A3938382C2278223A3330362C2279223A39387D2C7B2274223A3939342C2278223A3330332C2279223A39367D2C7B2274223A313030312C2278223A3330302C2279223A39367D2C7B2274223A313030372C2278223A3239352C2279223A39357D2C7B2274223A313031342C2278223A3239302C2279223A39357D2C7B2274223A313032302C2278223A3238342C2279223A39367D2C7B2274223A313032372C2278223A3237362C2279223A39387D2C7B2274223A313033332C2278223A3236372C2279223A3130317D2C7B2274223A313034302C2278223A3235372C2279223A3130347D2C7B2274223A313034362C2278223A3234362C2279223A3130377D2C7B2274223A313035332C2278223A3233332C2279223A3131317D2C7B2274223A313035392C2278223A3232312C2279223A3131347D2C7B2274223A313036352C2278223A3230382C2279223A3131377D2C7B2274223A313037322C2278223A3139362C2279223A3132307D2C7B2274223A313037382C2278223A3138332C2279223A3132337D2C7B2274223A313038352C2278223A3137312C2279223A3132347D2C7B2274223A313039312C2278223A3136312C2279223A3132367D2C7B2274223A313131302C2278223A3133322C2279223A3132367D2C7B2274223A313131362C2278223A3132352C2279223A3132357D2C7B2274223A313132322C2278223A3131392C2279223A3132337D2C7B2274223A313133352C2278223A3130392C2279223A3131397D2C7B2274223A313134312C2278223A3130362C2279223A3131367D2C7B2274223A313134382C2278223A3130342C2279223A3131327D2C7B2274223A313135342C2278223A3130332C2279223A3130387D2C7B2274223A313136302C2278223A3130332C2279223A3130327D2C7B2274223A313136372C2278223A3130352C2279223A39367D2C7B2274223A313137332C2278223A3130382C2279223A38397D2C7B2274223A313137392C2278223A3131332C2279223A38317D2C7B2274223A313138362C2278223A3131392C2279223A37337D2C7B2274223A313139322C2278223A3132362C2279223A36357D2C7B2274223A313139382C2278223A3133352C2279223A35377D2C7B2274223A313230352C2278223A3134352C2279223A34397D2C7B2274223A313231312C2278223A3135362C2279223A34317D2C7B2274223A313231382C2278223A3136392C2279223A33347D2C7B2274223A313232342C2278223A3138332C2279223A32377D2C7B2274223A313233302C2278223A3139362C2279223A32327D2C7B2274223A313233372C2278223A3231302C2279223A31387D2C7B2274223A313234332C2278223A3232342C2279223A31357D2C7B2274223A313235362C2278223A3235312C2279223A31337D2C7B2274223A313236332C2278223A3236332C2279223A31347D2C7B2274223A313236392C2278223A3237362C2279223A31367D2C7B2274223A313237362C2278223A3238362C2279223A31397D2C7B2274223A313238332C2278223A3239362C2279223A32347D2C7B2274223A313239362C2278223A3331322C2279223A33347D2C7B2274223A313330332C2278223A3331382C2279223A34327D2C7B2274223A313330392C2278223A3332332C2279223A34397D2C7B2274223A313331362C2278223A3332362C2279223A35387D2C7B2274223A313332322C2278223A3332372C2279223A36397D2C7B2274223A313332392C2278223A3332352C2279223A38317D2C7B2274223A313333362C2278223A3332312C2279223A39357D2C7B2274223A313334322C2278223A3331342C2279223A3131307D2C7B2274223A313334392C2278223A3330352C2279223A3132357D2C7B2274223A313335362C2278223A3239352C2279223A3133397D2C7B2274223A313336322C2278223A3238352C2279223A3135317D2C7B2274223A313336392C2278223A3237342C2279223A3136327D2C7B2274223A313337352C2278223A3236322C2279223A3137317D2C7B2274223A313338322C2278223A3235322C2279223A3137387D2C7B2274223A313338382C2278223A3234312C2279223A3138357D2C7B2274223A313339352C2278223A3233302C2279223A3139317D2C7B2274223A313430382C2278223A3231322C2279223A3139397D2C7B2274223A313431342C2278223A3230352C2279223A3230327D2C7B2274223A313432372C2278223A3139342C2279223A3230347D2C7B2274223A313433332C2278223A3138392C2279223A3230347D2C7B2274223A313434362C2278223A3138332C2279223A3230327D2C7B2274223A313435322C2278223A3138312C2279223A3230317D2C7B2274223A313435392C2278223A3137392C2279223A3139397D2C7B2274223A313436352C2278223A3137392C2279223A3139367D2C7B2274223A313437322C2278223A3138312C2279223A3139317D2C7B2274223A313437382C2278223A3138342C2279223A3138357D2C7B2274223A313438352C2278223A3138392C2279223A3137387D2C7B2274223A313439312C2278223A3139372C2279223A3136397D2C7B2274223A313439372C2278223A3230362C2279223A3136307D2C7B2274223A313530342C2278223A3231372C2279223A3135307D2C7B2274223A313531302C2278223A3232382C2279223A3134317D2C7B2274223A313532332C2278223A3235322C2279223A3132337D2C7B2274223A313533302C2278223A3236332C2279223A3131367D2C7B2274223A313533372C2278223A3237332C2279223A3130397D2C7B2274223A313534332C2278223A3238342C2279223A3130327D2C7B2274223A313535362C2278223A3330342C2279223A39337D2C7B2274223A313536332C2278223A3331342C2279223A38397D2C7B2274223A313537302C2278223A3332332C2279223A38377D2C7B2274223A313537362C2278223A3333312C2279223A38367D2C7B2274223A313538332C2278223A3333392C2279223A38367D2C7B2274223A313539302C2278223A3334362C2279223A38377D2C7B2274223A313539362C2278223A3335342C2279223A38397D2C7B2274223A313630332C2278223A3336312C2279223A39327D2C7B2274223A313631302C2278223A3336372C2279223A39367D2C7B2274223A313632332C2278223A3338312C2279223A3130347D2C7B2274223A313633302C2278223A3338372C2279223A3130397D2C7B2274223A313634322C2278223A3339392C2279223A3131377D2C7B2274223A313635362C2278223A3431332C2279223A3132357D2C7B2274223A313636322C2278223A3431392C2279223A3132387D2C7B2274223A313636392C2278223A3432372C2279223A3133307D2C7B2274223A313638322C2278223A3434322C2279223A3133327D2C7B2274223A313638392C2278223A3435312C2279223A3133327D2C7B2274223A313730322C2278223A3436392C2279223A3132387D2C7B2274223A313730382C2278223A3437382C2279223A3132357D2C7B2274223A313731352C2278223A3438372C2279223A3132317D2C7B2274223A313732382C2278223A3530332C2279223A3131327D2C7B2274223A313734322C2278223A3531372C2279223A3130327D2C7B2274223A313735352C2278223A3532372C2279223A39327D2C7B2274223A313736382C2278223A3533342C2279223A38337D2C7B2274223A313737342C2278223A3533362C2279223A37397D2C7B2274223A313738312C2278223A3533372C2279223A37357D2C7B2274223A313738382C2278223A3533372C2279223A37317D2C7B2274223A313739342C2278223A3533362C2279223A36377D2C7B2274223A313830312C2278223A3533332C2279223A36337D2C7B2274223A313830382C2278223A3532392C2279223A36307D2C7B2274223A313831342C2278223A3532332C2279223A35367D2C7B2274223A313832312C2278223A3531362C2279223A35347D2C7B2274223A313832382C2278223A3530382C2279223A35327D2C7B2274223A313833342C2278223A3439382C2279223A35317D2C7B2274223A313834312C2278223A3438382C2279223A35317D2C7B2274223A313834372C2278223A3437372C2279223A35327D2C7B2274223A313835342C2278223A3436362C2279223A35357D2C7B2274223A313836312C2278223A3435362C2279223A35397D2C7B2274223A313836372C2278223A3434352C2279223A36337D2C7B2274223A313837342C2278223A3433352C2279223A36397D2C7B2274223A313838312C2278223A3432362C2279223A37357D2C7B2274223A313838372C2278223A3431382C2279223A38317D2C7B2274223A313930312C2278223A3430352C2279223A39347D2C7B2274223A313930372C2278223A3430302C2279223A3130307D2C7B2274223A313931342C2278223A3339362C2279223A3130367D2C7B2274223A313932302C2278223A3339342C2279223A3131327D2C7B2274223A313932372C2278223A3339322C2279223A3131377D2C7B2274223A313933342C2278223A3339312C2279223A3132317D2C7B2274223A313934312C2278223A3339312C2279223A3132357D2C7B2274223A313934372C2278223A3339332C2279223A3132397D2C7B2274223A313935342C2278223A3339362C2279223A3133327D2C7B2274223A313936312C2278223A3430332C2279223A3133357D2C7B2274223A313936372C2278223A3431342C2279223A3133377D2C7B2274223A313937342C2278223A3433312C2279223A3133377D2C7B2274223A313938302C2278223A3435352C2279223A3133357D2C7B2274223A313938372C2278223A3438322C2279223A3133327D2C7B2274223A313939332C2278223A3530382C2279223A3132387D2C7B2274223A323030302C2278223A3533332C2279223A3132337D2C7B2274223A323030372C2278223A3535372C2279223A3131397D2C7B2274223A323031332C2278223A3537392C2279223A3131367D2C7B2274223A323032302C2278223A3539392C2279223A3131347D2C7B2274223A323032362C2278223A3631372C2279223A3131337D2C7B2274223A323033332C2278223A3633332C2279223A3131327D2C7B2274223A323033392C2278223A3634372C2279223A3131337D2C7B2274223A323034352C2278223A3635382C2279223A3131347D2C7B2274223A323035322C2278223A3636382C2279223A3131367D2C7B2274223A323035382C2278223A3637362C2279223A3131397D2C7B2274223A323036352C2278223A3638332C2279223A3132327D2C7B2274223A323037312C2278223A3638392C2279223A3132357D2C7B2274223A323037382C2278223A3639342C2279223A3132397D2C7B2274223A323039302C2278223A3730302C2279223A3133377D2C7B2274223A323039372C2278223A3730322C2279223A3134327D2C7B2274223A323130392C2278223A3730342C2279223A3135337D2C7B2274223A323132392C2278223A3730342C2279223A3136397D2C7B2274223A323133352C2278223A3730352C2279223A3137337D2C7B2274223A323134322C2278223A3730372C2279223A3137387D2C7B2274223A323134382C2278223A3731302C2279223A3138327D2C7B2274223A323135342C2278223A3731372C2279223A3138377D2C7B2274223A323136312C2278223A3732382C2279223A3139317D2C7B2274223A323136372C2278223A3734362C2279223A3139367D2C7B2274223A323137332C2278223A3737312C2279223A3139397D2C7B2274223A323138302C2278223A3739362C2279223A3230317D2C7B2274223A323139322C2278223A3832382C2279223A3230317D2C7B2274223A323139382C2278223A3833362C2279223A3230327D2C7B2274223A323230342C2278223A3834322C2279223A3230337D2C7B2274223A302C2278223A2D312C2279223A2D317D5D90002C7B2274223A323031342C2278223A3638342C2279223A3134357D2C7B2274223A323032362C2278223A3730302C2279223A3133367D2C7B2274223A323033332C2278223A3730372C2279223A3133337D2C7B2274223A323033392C2278223A3731332C2279223A3133317D2C7B2274223A323034352C2278223A3731392C2279223A3133307D2C7B2274223A323035382C2278223A3732392C2279223A3133307D2C7B2274223A323036342C2278223A3733352C2279223A3133327D2C7B2274223A323037312C2278223A3734302C2279223A3133357D2C7B2274223A323037372C2278223A3734362C2279223A3133397D2C7B2274223A323038332C2278223A3735312C2279223A3134347D2C7B2274223A323039302C2278223A3735352C2279223A3134397D2C7B2274223A323039362C2278223A3735392C2279223A3135357D2C7B2274223A323130322C2278223A3736332C2279223A3136307D2C7B2274223A323130382C2278223A3736372C2279223A3136367D2C7B2274223A323131342C2278223A3737322C2279223A3137317D2C7B2274223A323132312C2278223A3737382C2279223A3137367D2C7B2274223A323132372C2278223A3738362C2279223A3138307D2C7B2274223A323133332C2278223A3830302C2279223A3138337D2C7B2274223A323133392C2278223A3831382C2279223A3138347D2C7B2274223A302C2278223A2D312C2279223A2D317D5D",
        };
        private readonly int SignaturePayloadTest = 0;

        private readonly byte[] SignaturePayload;

        private readonly VIPASerialParserImpl subject;

        public VIPASerialParserImplTests()
        {
            mockDeviceLogHandler = new Mock<DeviceLogHandler>();
            SignaturePayload = ConversionHelper.HexToByteArray(SignatureInput[SignaturePayloadTest]);

            subject = new VIPASerialParserImpl(mockDeviceLogHandler.Object, ComPort);
        }

        public void Dispose()
        {
        }

        [Fact]
        public void ReadAndExecute_ValidatesBytesForSignaturePayload()
        {
            Assert.True(SignaturePayload.Length > 0);

            subject.BytesRead(SignaturePayload, SignaturePayload.Length);

            // compare bytes in private member
            byte[] combinedResponseBytes = Helper.GetFieldValueFromInstance<byte[]>("combinedResponseBytes", false, false, subject);
            Helper.CallPrivateMethod<int>("CalculateByteArrayLength", subject, out int length, new object[] { combinedResponseBytes, combinedResponseBytes.Length });
            byte[] buffer = new byte[length + 1];
            Buffer.BlockCopy(combinedResponseBytes, 0, buffer, 0, length + 1);
            Assert.Equal(SignaturePayload, buffer);

            // Process as chained-message response
            Helper.CallPrivateMethod<bool>("ProcessChainedMessageResponse", subject, out bool result);
            Assert.False(result);
        }
    }
}
