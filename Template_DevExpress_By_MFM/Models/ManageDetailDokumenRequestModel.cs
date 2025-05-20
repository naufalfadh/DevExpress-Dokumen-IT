using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Template_DevExpress_By_MFM.Models
{
    [Table("tb_dok_req_detail")] // Tabel detail di database
    public class DetailDokumenRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long detail_id { get; set; } // Primary Key

        [ForeignKey("MasterDokumenRequest")]
        public long dok_id { get; set; } // Foreign Key ke MasterDokumenRequest

        [Required]
        [MaxLength(255)]
        public string dok_menu { get; set; }

        [Required]
        [MaxLength(100)]
        public string dok_id_menu { get; set; }

        [Required]
        [MaxLength(50)]
        public string dok_access { get; set; }

        [MaxLength(500)]
        public string dok_note { get; set; }

        public MasterDokumenRequest MasterDokumenRequest { get; set; }
    }
}
