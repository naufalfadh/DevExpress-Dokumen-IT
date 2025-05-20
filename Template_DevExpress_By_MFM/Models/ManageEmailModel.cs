using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Template_DevExpress_By_MFM.Models
{
    [Table("manage_email")]
    public class ManageEmail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int64 id_recnum_email { get; set; }
        public string category { get; set; }
        public string email_from { get; set; }
        public string email_to { get; set; }
        public DateTime? insert_date { get; set; }
        public DateTime? update_date { get; set; }
    }



}