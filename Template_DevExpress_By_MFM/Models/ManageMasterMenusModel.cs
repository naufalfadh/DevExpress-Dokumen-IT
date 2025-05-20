using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Template_DevExpress_By_MFM.Models
{
    [Table("MasterMenus")]
    public class MasterMenu
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int? ParentId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(255)]
        public string Url { get; set; }

        [StringLength(50)]
        public string Icon { get; set; }

        [Column("[Order]")]
        public int Order { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // Navigation property: Parent Menu
        [ForeignKey("ParentId")]
        public virtual MasterMenu ParentMenu { get; set; }

        // Navigation property: Child Menus
        public virtual ICollection<MasterMenu> ChildMenus { get; set; }

        public MasterMenu()
        {
            ChildMenus = new HashSet<MasterMenu>();
        }

        public override string ToString()
        {
            return $"Id: {Id}, Title: {Title}, Url: {Url}, ParentId: {ParentId}, IsActive: {IsActive}";
        }
    }
}
