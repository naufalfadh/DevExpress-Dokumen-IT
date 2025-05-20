using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Template_DevExpress_By_MFM.Models
{
    [Table("tb_dok_req_detail_p3sp")] // Tabel detail di database
    public class DetailDokumenRequestP3sp
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long detail_id { get; set; }

        [ForeignKey("MasterDokumenRequestP3sp")]
        public long dok_id { get; set; }
        public int dok_quality { get; set; }
        public int dok_cost { get; set; }
        public int dok_delivery { get; set; }
        public int dok_safety { get; set; }
        public int dok_morale { get; set; }
        public int dok_productivity { get; set; }
        public string dok_tingkat_kesulitan { get; set; }
        public int dok_score { get; set; }
        public string dok_pic { get; set; }
        public string dok_prioritas { get; set; }
        public DateTime dok_due_date { get; set; }
        public string dok_hasil_analisa { get; set; }

        [JsonIgnore]
        public MasterDokumenRequestP3sp MasterDokumenRequestP3sp { get; set; }
    }
}