using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Template_DevExpress_By_MFM.Models
{
    [Table("tlkp_user")]
    public class MasterUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 id_user { get; set; }
        public Int32? ref_id_cust { get; set; }
        public string user_npk { get; set; }
        public string user_nama { get; set; }
        public string user_pass { get; set; }
        public string user_role { get; set; }
        public DateTime? user_lastLogin { get; set; }
        public string user_createBy { get; set; }
        public DateTime? user_createDate { get; set; }
        public string user_modifBy { get; set; }
        public DateTime? user_modifDate { get; set; }
        public int? user_status { get; set; }
        public string shift_plant { get; set; }
        public string email { get; set; }
    }

    public class GetLastCustID
    {
        public Int32? cust_id { get; set; }
    }


}