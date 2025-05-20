using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Template_DevExpress_By_MFM.Models
{
    public class APIModel
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
    }

    public class JsonMeta
    {
        public int code { get; set; }
        public string message { get; set; }
        public string status { get; set; }
    }

    public class JsonApi_Result
    {
        public List<JsonMeta> meta { get; set; } = new List<JsonMeta>();
        public List<string> data { get; set; } = new List<string>();
    }
    public class JSON_Token_GSVIEW
    {
        //public List<string> token { get; set; } = new List<string>();
        public string token { get; set; }
    }
}