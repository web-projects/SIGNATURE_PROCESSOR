using XO.Common.DAL;
using XO.Private;

namespace XO.Requests.DAL
{
    public class LinkDALRequest
    {
        public LinkDALIdentifier DALIdentifier { get; set; }
        public LinkDALRequestIPA5Object LinkObjects { get; set; }
    }
}
