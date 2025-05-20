using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Template_DevExpress_By_MFM.Models
{
    [Table("tb_user")]
    public class MasterUserForm
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_user { get; set; }
        public long? usr_npk { get; set; }
        public string usr_nama { get; set; }
        public string usr_plant { get; set; }
        public string usr_section { get; set; }
        public string usr_role { get; set; }    
        public string usr_pass { get; set; }
        public string usr_createBy { get; set; }
        public DateTime? usr_createDate { get; set; }
        public string usr_modifBy { get; set; }
        public DateTime? usr_modifDate { get; set; }
        public int usr_status { get; set; } 
        public string usr_email { get; set; }
        public string usr_img_ttd { get; set; }

    }


}