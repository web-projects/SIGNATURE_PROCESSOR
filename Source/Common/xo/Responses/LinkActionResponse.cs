using System.Collections.Generic;
using XO.Responses.DAL;

namespace XO.Responses
{
    public class LinkActionResponse
    {
        public List<LinkErrorValue> Errors { get; set; }
        public LinkDALResponse DALResponse { get; set; }
    }
}
