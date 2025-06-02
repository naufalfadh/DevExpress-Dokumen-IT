using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Template_DevExpress_By_MFM.Models
{
    [Table("tb_dok_req_ppi")]
    public class MasterDokumenRequestPpi
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long dok_id { get; set; }
        public string dok_refnum { get; set; }
        public long? usr_npk { get; set; }
        public string usr_nama { get; set; }
        public string dok_user_erp { get; set; }
        public string dok_plant { get; set; }
        public string dok_section { get; set; }
        public string dok_req_by { get; set; }
        public string dok_document { get; set; }
        public string dok_lampiran { get; set; }
        public string dok_tingkat { get; set; }
        public string dok_superuser { get; set; }
        public string dok_jenis_superuser { get; set; }
        public string dok_jenis_pekerjaan { get; set; }
        public string dok_judul_request { get; set; }
        public int dok_status { get; set; }
        public string dok_reason { get; set; }
        public string dok_spesifikasi { get; set; }
        public string createBy { get; set; }
        public string modifBy { get; set; }
        public DateTime? createDate { get; set; }
        public DateTime? modifDate { get; set; }
        public string dok_approve_k { get; set; }
        public DateTime? modifDate_k { get; set; }
        public string dok_approve_kadiv { get; set; }
        public DateTime? modifDate_kadiv { get; set; }
        public string dok_ttd_user { get; set; }
        public string dok_ttd_kadept { get; set; }
        public string dok_ttd_kadeptit { get; set; }
        public string dok_ttd_kadivit { get; set; }
        public DateTime? dok_tgl_penerimaan { get; set; }
        public DateTime? dok_tgl_dibutuhkan { get; set; }
        public DateTime? dok_tgl_efektif { get; set; }
        public string dok_dilaksanakan_oleh { get; set; }
        public DateTime? dok_tgl_efektif_bast { get; set; }
        public string dok_dilaksanakan_bast { get; set; }
        public string dok_ttd_user_bast { get; set; }
        public string dok_ttd_kadeptit_bast { get; set; }
        public string dok_ttd_kadept_bast { get; set; }
        public string dok_reason_reject { get; set; }
        public DateTime? dok_tgl_bast_user { get; set; }
        public DateTime? dok_tgl_bast_kadeptit { get; set; }
        public DateTime? dok_tgl_bast_kadept { get; set; }

    }
}