using Microsoft.EntityFrameworkCore;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        { }

        public DbSet<Khoa> Khoas { get; set; }
        public DbSet<BoMon> BoMons { get; set; }
        public DbSet<HocPhan> HocPhans { get; set; }
        public DbSet<BaiGiang> BaiGiangs { get; set; }
        public DbSet<NguoiDung> NguoiDungs { get; set; }
        public DbSet<Quyen> Quyens { get; set; }
        public DbSet<Lop> Lops { get; set; }
        public DbSet<DanhGia> DanhGias { get; set; }
        public DbSet<BaiTap> BaiTaps { get; set; }
        public DbSet<ThongTinWeb> ThongTinWebs { get; set; }
        public DbSet<DangKyLopHoc> DangKyLopHocs { get; set; }
        public DbSet<NopBaiTap> NopBaiTaps { get; set; }
        public DbSet<DiemDanh> DiemDanhs { get; set; }
        public DbSet<BangDiem> BangDiems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<BangDiem>(entity =>
            {
                entity.Property(e => e.ChuyenCan).HasColumnType("decimal(18,2)");
                entity.Property(e => e.HeSo1).HasColumnType("decimal(18,2)");
                entity.Property(e => e.HeSo2).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ThiLan1).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ThiLan2).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TBKT).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TongKetLan1).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TongKetLan2).HasColumnType("decimal(18,2)");
            });

            // --- Khoa - BoMon ---
            modelBuilder.Entity<BoMon>()
                .HasOne(bm => bm.Khoa)
                .WithMany(k => k.BoMons)
                .HasForeignKey(bm => bm.MaKhoa)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Khoa - NguoiDung ---
            modelBuilder.Entity<NguoiDung>()
                .HasOne(nd => nd.Khoa)
                .WithMany(k => k.NguoiDungs)
                .HasForeignKey(nd => nd.MaKhoa)
                .OnDelete(DeleteBehavior.Restrict);

            // --- BoMon - HocPhan ---
            modelBuilder.Entity<HocPhan>()
                .HasOne(hp => hp.BoMon)
                .WithMany(bm => bm.HocPhans)
                .HasForeignKey(hp => hp.MaBoMon)
                .OnDelete(DeleteBehavior.Restrict);

            // --- BoMon - NguoiDung ---
            modelBuilder.Entity<NguoiDung>()
                .HasOne(nd => nd.BoMon)
                .WithMany(bm => bm.NguoiDungs)
                .HasForeignKey(nd => nd.MaBoMon)
                .OnDelete(DeleteBehavior.Restrict);

            // --- NguoiDung - Quyen ---
            modelBuilder.Entity<NguoiDung>()
                .HasOne(nd => nd.Quyen)
                .WithMany(q => q.NguoiDungs)
                .HasForeignKey(nd => nd.MaQuyen)
                .OnDelete(DeleteBehavior.Cascade);

            // --- HocPhan - GiangVien ---
            modelBuilder.Entity<HocPhan>()
                .HasOne(hp => hp.GiangVien)
                .WithMany()
                .HasForeignKey(hp => hp.MaGiangVien)
                .OnDelete(DeleteBehavior.Restrict);

            // --- HocPhan - BaiGiang ---
            modelBuilder.Entity<HocPhan>()
                .HasOne(hp => hp.BaiGiang)
                .WithMany()
                .HasForeignKey(hp => hp.MaBaiGiang)
                .OnDelete(DeleteBehavior.Restrict);

            // --- HocPhan - BaiTap ---
            modelBuilder.Entity<HocPhan>()
                .HasOne(hp => hp.BaiTap)
                .WithMany()
                .HasForeignKey(hp => hp.MaBaiTap)
                .OnDelete(DeleteBehavior.Restrict);

            // --- BaiGiang - Lop ---
            modelBuilder.Entity<BaiGiang>()
                .HasOne(bg => bg.Lop)
                .WithMany(l => l.BaiGiangs)
                .HasForeignKey(bg => bg.MaLop)
                .OnDelete(DeleteBehavior.Cascade);

            // --- Lop - HocPhan ---
            modelBuilder.Entity<Lop>()
                .HasOne(l => l.HocPhan)
                .WithMany(hp => hp.Lops)
                .HasForeignKey(l => l.MaHocPhan)
                .OnDelete(DeleteBehavior.Cascade);

            // --- DanhGia - NguoiDung & Lop ---
            modelBuilder.Entity<DanhGia>()
                .HasOne(dg => dg.SinhVien)
                .WithMany(nd => nd.DanhGias)
                .HasForeignKey(dg => dg.MaSinhVien)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DanhGia>()
                .HasOne(dg => dg.Lop)
                .WithMany(l => l.DanhGias)
                .HasForeignKey(dg => dg.MaLop)
                .OnDelete(DeleteBehavior.Cascade);

            // --- BaiTap - Lop ---
            modelBuilder.Entity<BaiTap>()
                .HasOne(bt => bt.Lop)
                .WithMany(l => l.BaiTaps)
                .HasForeignKey(bt => bt.MaLop)
                .OnDelete(DeleteBehavior.Cascade);

            // --- BaiTap - NguoiTao (NguoiDung) ---
            modelBuilder.Entity<BaiTap>()
                .HasOne(bt => bt.NguoiTao)
                .WithMany()
                .HasForeignKey(bt => bt.MaNguoiTao)
                .OnDelete(DeleteBehavior.Restrict);

            // --- DangKyLopHoc - NguoiDung & Lop ---
            modelBuilder.Entity<DangKyLopHoc>()
                .HasOne(dkl => dkl.SinhVien)
                .WithMany(nd => nd.DangKyLopHocs)
                .HasForeignKey(dkl => dkl.MaSinhVien)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DangKyLopHoc>()
                .HasOne(dkl => dkl.Lop)
                .WithMany(l => l.DangKyLopHocs)
                .HasForeignKey(dkl => dkl.MaLop)
                .OnDelete(DeleteBehavior.Cascade);

            // --- NopBaiTap - BaiTap & NguoiDung ---
            modelBuilder.Entity<NopBaiTap>()
                .HasOne(nbt => nbt.BaiTap)
                .WithMany(bt => bt.NopBaiTaps)
                .HasForeignKey(nbt => nbt.MaBaiTap)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NopBaiTap>()
                .HasOne(nbt => nbt.SinhVien)
                .WithMany(nd => nd.NopBaiTaps)
                .HasForeignKey(nbt => nbt.MaSinhVien)
                .OnDelete(DeleteBehavior.Cascade);

            // --- DiemDanh - NguoiDung & Lop ---
            modelBuilder.Entity<DiemDanh>()
                .HasOne(dd => dd.SinhVien)
                .WithMany(nd => nd.DiemDanhs)
                .HasForeignKey(dd => dd.MaSinhVien)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DiemDanh>()
                .HasOne(dd => dd.Lop)
                .WithMany(l => l.DiemDanhs)
                .HasForeignKey(dd => dd.MaLop)
                .OnDelete(DeleteBehavior.Cascade);

            // --- BangDiem - NguoiDung & Lop ---
            modelBuilder.Entity<BangDiem>()
                .HasOne(bd => bd.Lop)
                .WithMany(l => l.BangDiems)
                .HasForeignKey(bd => bd.MaLop)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BangDiem>()
                .HasOne(bd => bd.SinhVien)
                .WithMany(nd => nd.BangDiems)
                .HasForeignKey(bd => bd.MaSinhVien)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

