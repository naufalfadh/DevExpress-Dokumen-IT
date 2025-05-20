using Template_DevExpress_By_MFM.Models;
using System;
using System.Web;
using System.Data.Entity;

namespace Template_DevExpress_By_MFM
{
    public partial class GSDbContext : DbContext
    {
        public DbSet<ManageEmail> ManageEmail { get; set; }
        public DbSet<MasterDokumenRequest> MasterDokumenRequest { get; set; }
        public DbSet<DetailDokumenRequest> DetailDokumenRequest { get; set; }
        public DbSet<MasterUserForm> MasterUserForm { get; set; }
        public DbSet<MasterMenu> MasterMenu { get; set; }
        public DbSet<MasterDokumenRequestP3sp> MasterDokumenRequestP3sp { get; set; }
        public DbSet<DetailDokumenRequestP3sp> DetailDokumenRequestP3sp { get; set; }

        public GSDbContext() : base("name=GSDbContext") { }

        public GSDbContext(string dbSource, string dbName, string dbUsers, string dbPass)
            : base($"Data Source=" + dbSource + ";initial catalog=" + dbName + ";User Id=" + dbUsers + ";Password=" + dbPass + "; ") { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer<GSDbContext>(null);
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DetailDokumenRequest>()
       .HasRequired(d => d.MasterDokumenRequest) // DetailDokumenRequest harus memiliki MasterDokumenRequest
       .WithMany(m => m.DetailDokumenRequests) // MasterDokumenRequest memiliki banyak DetailDokumenRequest
       .HasForeignKey(d => d.dok_id) // Foreign Key
       .WillCascadeOnDelete(true); // Jika Master dihapus, detail juga ikut terhapus

            modelBuilder.Entity<DetailDokumenRequestP3sp>()
        .HasRequired(d => d.MasterDokumenRequestP3sp)
        .WithMany(m => m.DetailDokumenRequestP3sp)
        .HasForeignKey(d => d.dok_id)
        .WillCascadeOnDelete(true);

        }

    }

    public partial class GSDbContextGSTrack : DbContext
    {
        public GSDbContextGSTrack() : base("name=GSDbContextGSTrack") { }

        public GSDbContextGSTrack(string dbSource, string dbName, string dbUsers, string dbPass)
            : base($"Data Source=" + dbSource + ";initial catalog=" + dbName + ";User Id=" + dbUsers + ";Password=" + dbPass + "; ") { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer<GSDbContextGSTrack>(null);
            base.OnModelCreating(modelBuilder);
        }

    }
}
