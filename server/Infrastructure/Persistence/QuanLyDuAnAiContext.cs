using System;
using System.Collections.Generic;
using Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public partial class QuanLyDuAnAiContext : DbContext
{
    public QuanLyDuAnAiContext(DbContextOptions<QuanLyDuAnAiContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BaoCaoTienDoDuAn> BaoCaoTienDoDuAns { get; set; }

    public virtual DbSet<ChecklistCongViec> ChecklistCongViecs { get; set; }

    public virtual DbSet<ChiSoLichSuDuAn> ChiSoLichSuDuAns { get; set; }

    public virtual DbSet<ChiTietCongViec> ChiTietCongViecs { get; set; }

    public virtual DbSet<CongViec> CongViecs { get; set; }

    public virtual DbSet<DanhGiaHieuSuatCongViec> DanhGiaHieuSuatCongViecs { get; set; }

    public virtual DbSet<DanhGiaMoHinhAI> DanhGiaMoHinhAIs { get; set; }

    public virtual DbSet<DeXuatGiaoViecAI> DeXuatGiaoViecAIs { get; set; }

    public virtual DbSet<DiemNghenAI> DiemNghenAIs { get; set; }

    public virtual DbSet<DuAn> DuAns { get; set; }

    public virtual DbSet<DuAn_Sprint> DuAn_Sprints { get; set; }

    public virtual DbSet<DuBaoRuiRoAI> DuBaoRuiRoAIs { get; set; }

    public virtual DbSet<DuyetCongViec> DuyetCongViecs { get; set; }

    public virtual DbSet<KyNang> KyNangs { get; set; }

    public virtual DbSet<KyNangCongViec> KyNangCongViecs { get; set; }

    public virtual DbSet<KyNangNguoiDung> KyNangNguoiDungs { get; set; }

    public virtual DbSet<LichSuHoatDong> LichSuHoatDongs { get; set; }

    public virtual DbSet<LoaiRuiRo> LoaiRuiRos { get; set; }

    public virtual DbSet<NangLucThanhVien> NangLucThanhViens { get; set; }

    public virtual DbSet<NguoiDung> NguoiDungs { get; set; }

    public virtual DbSet<NhatKyLamViec> NhatKyLamViecs { get; set; }

    public virtual DbSet<PhuThuocCongViec> PhuThuocCongViecs { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<RuiRoDuAn> RuiRoDuAns { get; set; }

    public virtual DbSet<RuiRoNghiemTrong> RuiRoNghiemTrongs { get; set; }

    public virtual DbSet<SnapshotTienDoDuAn> SnapshotTienDoDuAns { get; set; }

    public virtual DbSet<Sprint> Sprints { get; set; }

    public virtual DbSet<TaiLieu> TaiLieus { get; set; }

    public virtual DbSet<ThanhVienDuAn> ThanhVienDuAns { get; set; }

    public virtual DbSet<ThanhVienQuaTai> ThanhVienQuaTais { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    public virtual DbSet<v_Dataset_DeXuatGiaoViec> v_Dataset_DeXuatGiaoViecs { get; set; }

    public virtual DbSet<v_Dataset_DuBaoTreHan> v_Dataset_DuBaoTreHans { get; set; }

    public virtual DbSet<v_Dataset_PhatHienDiemNghen> v_Dataset_PhatHienDiemNghens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BaoCaoTienDoDuAn>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("BaoCaoTienDoDuAn");

            entity.Property(e => e.PhanTramHoanThanh).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TenDuAn).HasMaxLength(150);
        });

        modelBuilder.Entity<ChecklistCongViec>(entity =>
        {
            entity.HasKey(e => e.MaChecklist).HasName("PK__Checklis__DFD8218ED3D11C3D");

            entity.ToTable("ChecklistCongViec");

            entity.Property(e => e.DaHoanThanh).HasDefaultValue(false);
            entity.Property(e => e.NgayHoanThanh).HasColumnType("datetime");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.NoiDung).HasMaxLength(200);

            entity.HasOne(d => d.MaCongViecNavigation).WithMany(p => p.ChecklistCongViecs)
                .HasForeignKey(d => d.MaCongViec)
                .HasConstraintName("FK__Checklist__MaCon__7E37BEF6");

            entity.HasOne(d => d.MaNguoiHoanThanhNavigation).WithMany(p => p.ChecklistCongViecs)
                .HasForeignKey(d => d.MaNguoiHoanThanh)
                .HasConstraintName("FK__Checklist__MaNgu__7F2BE32F");
        });

        modelBuilder.Entity<ChiSoLichSuDuAn>(entity =>
        {
            entity.HasKey(e => e.MaChiSo).HasName("PK__ChiSoLic__EBA18E15F75AA9BB");

            entity.ToTable("ChiSoLichSuDuAn");

            entity.Property(e => e.NganSachDaDung).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.NgayGhiNhan)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.QuyMoNhom).HasDefaultValue(0);
            entity.Property(e => e.SoCongViecBiChan).HasDefaultValue(0);
            entity.Property(e => e.SoCongViecHoanThanh).HasDefaultValue(0);
            entity.Property(e => e.SoCongViecTreHan).HasDefaultValue(0);
            entity.Property(e => e.SoLoiPhatSinh).HasDefaultValue(0);
            entity.Property(e => e.SoYeuCauThayDoi).HasDefaultValue(0);
            entity.Property(e => e.TongGioThucTe).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TongGioUocTinh).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TongSoCongViec).HasDefaultValue(0);

            entity.HasOne(d => d.MaDuAnNavigation).WithMany(p => p.ChiSoLichSuDuAns)
                .HasForeignKey(d => d.MaDuAn)
                .HasConstraintName("FK__ChiSoLich__MaDuA__2A164134");
        });

        modelBuilder.Entity<ChiTietCongViec>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("ChiTietCongViec");

            entity.Property(e => e.DoUuTien).HasMaxLength(20);
            entity.Property(e => e.MaCongViecCode).HasMaxLength(50);
            entity.Property(e => e.NguoiPhuTrach).HasMaxLength(100);
            entity.Property(e => e.NguoiTao).HasMaxLength(100);
            entity.Property(e => e.SoGioThucTe).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.SoGioUocTinh).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.TenCongViec).HasMaxLength(200);
            entity.Property(e => e.TenDuAn).HasMaxLength(150);
            entity.Property(e => e.TrangThai).HasMaxLength(30);
            entity.Property(e => e.VaiTroNguoiPhuTrach).HasMaxLength(100);
        });

        modelBuilder.Entity<CongViec>(entity =>
        {
            entity.HasKey(e => e.MaCongViec).HasName("PK__CongViec__41B7DD18737B1297");

            entity.ToTable("CongViec");

            entity.HasIndex(e => e.DoUuTien, "IDX_CongViec_DoUuTien");

            entity.HasIndex(e => e.HanChot, "IDX_CongViec_HanChot");

            entity.HasIndex(e => e.MaDuAn, "IDX_CongViec_MaDuAn");

            entity.HasIndex(e => e.MaNguoiPhuTrach, "IDX_CongViec_MaNguoiPhuTrach");

            entity.HasIndex(e => e.MaSprint, "IDX_CongViec_MaSprint");

            entity.HasIndex(e => e.TrangThai, "IDX_CongViec_TrangThai");

            entity.HasIndex(e => e.MaCongViecCode, "UQ__CongViec__F8C0C5AF526E075C").IsUnique();

            entity.Property(e => e.DoUuTien)
                .HasMaxLength(20)
                .HasDefaultValue("Trung binh");
            entity.Property(e => e.MaCongViecCode).HasMaxLength(50);
            entity.Property(e => e.NgayCapNhat).HasColumnType("datetime");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoGioThucTe)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(8, 2)");
            entity.Property(e => e.SoGioUocTinh)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(8, 2)");
            entity.Property(e => e.TenCongViec).HasMaxLength(200);
            entity.Property(e => e.TienDo).HasDefaultValue(0);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("Moi tao");

            entity.HasOne(d => d.MaDuAnNavigation).WithMany(p => p.CongViecs)
                .HasForeignKey(d => d.MaDuAn)
                .HasConstraintName("FK__CongViec__MaDuAn__693CA210");

            entity.HasOne(d => d.MaNguoiPhuTrachNavigation).WithMany(p => p.CongViecMaNguoiPhuTrachNavigations)
                .HasForeignKey(d => d.MaNguoiPhuTrach)
                .HasConstraintName("FK__CongViec__MaNguo__6B24EA82");

            entity.HasOne(d => d.MaNguoiTaoNavigation).WithMany(p => p.CongViecMaNguoiTaoNavigations)
                .HasForeignKey(d => d.MaNguoiTao)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CongViec__MaNguo__6A30C649");

            entity.HasOne(d => d.MaSprintNavigation).WithMany(p => p.CongViecs)
                .HasForeignKey(d => d.MaSprint)
                .HasConstraintName("FK_CongViec_Sprint");
        });

        modelBuilder.Entity<DanhGiaHieuSuatCongViec>(entity =>
        {
            entity.HasKey(e => e.MaDanhGia).HasName("PK__DanhGiaH__AA9515BFE636FEC7");

            entity.ToTable("DanhGiaHieuSuatCongViec");

            entity.HasIndex(e => e.MaCongViec, "IDX_DanhGiaHieuSuat_MaCongViec");

            entity.HasIndex(e => e.MaNguoiDuocDanhGia, "IDX_DanhGiaHieuSuat_MaNguoiDuocDanhGia");

            entity.HasIndex(e => e.MaCongViec, "UQ__DanhGiaH__41B7DD19AF3A49EA").IsUnique();

            entity.Property(e => e.NgayDanhGia)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.MaCongViecNavigation).WithOne(p => p.DanhGiaHieuSuatCongViec)
                .HasForeignKey<DanhGiaHieuSuatCongViec>(d => d.MaCongViec)
                .HasConstraintName("FK__DanhGiaHi__MaCon__4A8310C6");

            entity.HasOne(d => d.MaNguoiDanhGiaNavigation).WithMany(p => p.DanhGiaHieuSuatCongViecMaNguoiDanhGiaNavigations)
                .HasForeignKey(d => d.MaNguoiDanhGia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DanhGiaHi__MaNgu__4C6B5938");

            entity.HasOne(d => d.MaNguoiDuocDanhGiaNavigation).WithMany(p => p.DanhGiaHieuSuatCongViecMaNguoiDuocDanhGiaNavigations)
                .HasForeignKey(d => d.MaNguoiDuocDanhGia)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DanhGiaHi__MaNgu__4B7734FF");
        });

        modelBuilder.Entity<DanhGiaMoHinhAI>(entity =>
        {
            entity.HasKey(e => e.MaDanhGiaMoHinh).HasName("PK__DanhGiaM__006F0EA27AA24294");

            entity.ToTable("DanhGiaMoHinhAI");

            entity.Property(e => e.DoChinhXac).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.F1Score).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.LoaiDuBao).HasMaxLength(50);
            entity.Property(e => e.NgayDanhGia)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PhienBanMoHinh).HasMaxLength(50);
            entity.Property(e => e.PrecisionScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.RecallScore).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TenMoHinh).HasMaxLength(100);
        });

        modelBuilder.Entity<DeXuatGiaoViecAI>(entity =>
        {
            entity.HasKey(e => e.MaDeXuat).HasName("PK__DeXuatGi__22244765EC39FFF0");

            entity.ToTable("DeXuatGiaoViecAI");

            entity.HasIndex(e => e.MaCongViec, "IDX_DeXuatGiaoViecAI_MaCongViec");

            entity.Property(e => e.DaChapNhan).HasDefaultValue(false);
            entity.Property(e => e.DiemKhoiLuong).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.DiemKinhNghiem).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.DiemKyNang).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.DiemPhuHop).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TenMoHinh).HasMaxLength(100);

            entity.HasOne(d => d.MaCongViecNavigation).WithMany(p => p.DeXuatGiaoViecAIs)
                .HasForeignKey(d => d.MaCongViec)
                .HasConstraintName("FK__DeXuatGia__MaCon__40F9A68C");

            entity.HasOne(d => d.MaNguoiDuocDeXuatNavigation).WithMany(p => p.DeXuatGiaoViecAIs)
                .HasForeignKey(d => d.MaNguoiDuocDeXuat)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DeXuatGia__MaNgu__41EDCAC5");
        });

        modelBuilder.Entity<DiemNghenAI>(entity =>
        {
            entity.HasKey(e => e.MaDiemNghen).HasName("PK__DiemNghe__668CA1DE7FC7DD66");

            entity.ToTable("DiemNghenAI");

            entity.Property(e => e.KhuVucPhatHien).HasMaxLength(100);
            entity.Property(e => e.MucDoNghiemTrong).HasMaxLength(20);
            entity.Property(e => e.NgayPhatHien)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoNgayTreDuBao).HasDefaultValue(0);

            entity.HasOne(d => d.MaCongViecNavigation).WithMany(p => p.DiemNghenAIs)
                .HasForeignKey(d => d.MaCongViec)
                .HasConstraintName("FK__DiemNghen__MaCon__3864608B");

            entity.HasOne(d => d.MaDuAnNavigation).WithMany(p => p.DiemNghenAIs)
                .HasForeignKey(d => d.MaDuAn)
                .HasConstraintName("FK__DiemNghen__MaDuA__37703C52");
        });

        modelBuilder.Entity<DuAn>(entity =>
        {
            entity.HasKey(e => e.MaDuAn).HasName("PK__DuAn__EFD751E47531E687");

            entity.ToTable("DuAn");

            entity.HasIndex(e => e.TrangThai, "IDX_DuAn_TrangThai");

            entity.Property(e => e.NganSach).HasColumnType("decimal(15, 2)");
            entity.Property(e => e.NgayCapNhat).HasColumnType("datetime");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TenDuAn).HasMaxLength(150);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("Moi tao");

            entity.HasOne(d => d.MaQuanLyDuAnNavigation).WithMany(p => p.DuAns)
                .HasForeignKey(d => d.MaQuanLyDuAn)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DuAn__MaQuanLyDu__534D60F1");
        });

        modelBuilder.Entity<DuAn_Sprint>(entity =>
        {
            entity.HasKey(e => new { e.MaDuAn, e.MaSprint }).HasName("PK__DuAn_Spr__2035303857FD5C2E");

            entity.ToTable("DuAn_Sprint");
        });

        modelBuilder.Entity<DuBaoRuiRoAI>(entity =>
        {
            entity.HasKey(e => e.MaDuBao).HasName("PK__DuBaoRui__03C13C22F458AFDD");

            entity.ToTable("DuBaoRuiRoAI");

            entity.HasIndex(e => e.MaDuAn, "IDX_DuBaoRuiRoAI_MaDuAn");

            entity.Property(e => e.MucDoRuiRo).HasMaxLength(20);
            entity.Property(e => e.NgayDuBao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PhienBanMoHinh).HasMaxLength(50);
            entity.Property(e => e.TenMoHinh).HasMaxLength(100);
            entity.Property(e => e.XacSuatRuiRo).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.MaCongViecNavigation).WithMany(p => p.DuBaoRuiRoAIs)
                .HasForeignKey(d => d.MaCongViec)
                .HasConstraintName("FK__DuBaoRuiR__MaCon__30C33EC3");

            entity.HasOne(d => d.MaDuAnNavigation).WithMany(p => p.DuBaoRuiRoAIs)
                .HasForeignKey(d => d.MaDuAn)
                .HasConstraintName("FK__DuBaoRuiR__MaDuA__2FCF1A8A");

            entity.HasOne(d => d.MaLoaiRuiRoNavigation).WithMany(p => p.DuBaoRuiRoAIs)
                .HasForeignKey(d => d.MaLoaiRuiRo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DuBaoRuiR__MaLoa__31B762FC");
        });

        modelBuilder.Entity<DuyetCongViec>(entity =>
        {
            entity.HasKey(e => e.MaDuyet).HasName("PK__DuyetCon__34A9E4898878942C");

            entity.ToTable("DuyetCongViec");

            entity.Property(e => e.NgayDuyet).HasColumnType("datetime");
            entity.Property(e => e.NgayYeuCau)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TrangThaiDuyet)
                .HasMaxLength(30)
                .HasDefaultValue("Cho duyet");

            entity.HasOne(d => d.MaCongViecNavigation).WithMany(p => p.DuyetCongViecs)
                .HasForeignKey(d => d.MaCongViec)
                .HasConstraintName("FK__DuyetCong__MaCon__778AC167");

            entity.HasOne(d => d.MaNguoiDuyetNavigation).WithMany(p => p.DuyetCongViecMaNguoiDuyetNavigations)
                .HasForeignKey(d => d.MaNguoiDuyet)
                .HasConstraintName("FK__DuyetCong__MaNgu__797309D9");

            entity.HasOne(d => d.MaNguoiYeuCauNavigation).WithMany(p => p.DuyetCongViecMaNguoiYeuCauNavigations)
                .HasForeignKey(d => d.MaNguoiYeuCau)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DuyetCong__MaNgu__787EE5A0");
        });

        modelBuilder.Entity<KyNang>(entity =>
        {
            entity.HasKey(e => e.MaKyNang).HasName("PK__KyNang__796CFDAF0A6D4228");

            entity.ToTable("KyNang");

            entity.HasIndex(e => e.TenKyNang, "UQ__KyNang__89D6F06D164D9E2F").IsUnique();

            entity.Property(e => e.TenKyNang).HasMaxLength(100);
        });

        modelBuilder.Entity<KyNangCongViec>(entity =>
        {
            entity.HasKey(e => new { e.MaCongViec, e.MaKyNang }).HasName("PK__KyNangCo__A62112C2CFB91C2D");

            entity.ToTable("KyNangCongViec");

            entity.HasOne(d => d.MaCongViecNavigation).WithMany(p => p.KyNangCongViecs)
                .HasForeignKey(d => d.MaCongViec)
                .HasConstraintName("FK__KyNangCon__MaCon__70DDC3D8");

            entity.HasOne(d => d.MaKyNangNavigation).WithMany(p => p.KyNangCongViecs)
                .HasForeignKey(d => d.MaKyNang)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__KyNangCon__MaKyN__71D1E811");
        });

        modelBuilder.Entity<KyNangNguoiDung>(entity =>
        {
            entity.HasKey(e => new { e.MaNguoiDung, e.MaKyNang }).HasName("PK__KyNangNg__22AF18B8C23767D1");

            entity.ToTable("KyNangNguoiDung");

            entity.Property(e => e.SoNamKinhNghiem)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(4, 1)");

            entity.HasOne(d => d.MaKyNangNavigation).WithMany(p => p.KyNangNguoiDungs)
                .HasForeignKey(d => d.MaKyNang)
                .HasConstraintName("FK__KyNangNgu__MaKyN__4CA06362");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.KyNangNguoiDungs)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK__KyNangNgu__MaNgu__4BAC3F29");
        });

        modelBuilder.Entity<LichSuHoatDong>(entity =>
        {
            entity.HasKey(e => e.MaHoatDong).HasName("PK__LichSuHo__BD808BE734012662");

            entity.ToTable("LichSuHoatDong");

            entity.HasIndex(e => e.MaCongViec, "IDX_LichSuHoatDong_MaCongViec");

            entity.Property(e => e.LoaiHanhDong).HasMaxLength(100);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.MaCongViecNavigation).WithMany(p => p.LichSuHoatDongs)
                .HasForeignKey(d => d.MaCongViec)
                .HasConstraintName("FK__LichSuHoa__MaCon__123EB7A3");

            entity.HasOne(d => d.MaDuAnNavigation).WithMany(p => p.LichSuHoatDongs)
                .HasForeignKey(d => d.MaDuAn)
                .HasConstraintName("FK__LichSuHoa__MaDuA__114A936A");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.LichSuHoatDongs)
                .HasForeignKey(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LichSuHoa__MaNgu__1332DBDC");
        });

        modelBuilder.Entity<LoaiRuiRo>(entity =>
        {
            entity.HasKey(e => e.MaLoaiRuiRo).HasName("PK__LoaiRuiR__A6FED164A042D519");

            entity.ToTable("LoaiRuiRo");

            entity.HasIndex(e => e.TenLoaiRuiRo, "UQ__LoaiRuiR__F9A1C76740912CFA").IsUnique();

            entity.Property(e => e.TenLoaiRuiRo).HasMaxLength(100);
        });

        modelBuilder.Entity<NangLucThanhVien>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("NangLucThanhVien");

            entity.Property(e => e.DiemChatLuongTrungBinh).HasColumnType("decimal(38, 6)");
            entity.Property(e => e.DiemTocDoTrungBinh).HasColumnType("decimal(38, 6)");
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.TenVaiTro).HasMaxLength(100);
            entity.Property(e => e.TongGioThucTe).HasColumnType("decimal(38, 2)");
            entity.Property(e => e.TongGioUocTinh).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<NguoiDung>(entity =>
        {
            entity.HasKey(e => e.MaNguoiDung).HasName("PK__NguoiDun__C539D76267C5D3F9");

            entity.ToTable("NguoiDung");

            entity.HasIndex(e => e.MaVaiTro, "IDX_NguoiDung_MaVaiTro");

            entity.HasIndex(e => e.Email, "UQ__NguoiDun__A9D10534750D8C22").IsUnique();

            entity.Property(e => e.DangHoatDong).HasDefaultValue(true);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.KhoiLuongHienTai)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(8, 2)");
            entity.Property(e => e.KhoiLuongToiDa)
                .HasDefaultValue(40m)
                .HasColumnType("decimal(8, 2)");
            entity.Property(e => e.MucLuongTheoGio)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.SoDienThoai).HasMaxLength(20);
            entity.Property(e => e.SoNamKinhNghiem).HasDefaultValue(0);

            entity.HasOne(d => d.MaVaiTroNavigation).WithMany(p => p.NguoiDungs)
                .HasForeignKey(d => d.MaVaiTro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NguoiDung__MaVai__440B1D61");
        });

        modelBuilder.Entity<NhatKyLamViec>(entity =>
        {
            entity.HasKey(e => e.MaNhatKy).HasName("PK__NhatKyLa__E42EF42ECEB483E4");

            entity.ToTable("NhatKyLamViec");

            entity.HasIndex(e => e.MaCongViec, "IDX_NhatKyLamViec_MaCongViec");

            entity.HasIndex(e => e.MaNguoiDung, "IDX_NhatKyLamViec_MaNguoiDung");

            entity.HasIndex(e => e.NgayLamViec, "IDX_NhatKyLamViec_NgayLamViec");

            entity.Property(e => e.NgayLamViec).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoGioLam).HasColumnType("decimal(6, 2)");

            entity.HasOne(d => d.MaCongViecNavigation).WithMany(p => p.NhatKyLamViecs)
                .HasForeignKey(d => d.MaCongViec)
                .HasConstraintName("FK__NhatKyLam__MaCon__0C85DE4D");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.NhatKyLamViecs)
                .HasForeignKey(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__NhatKyLam__MaNgu__0D7A0286");
        });

        modelBuilder.Entity<PhuThuocCongViec>(entity =>
        {
            entity.HasKey(e => new { e.MaCongViecTruoc, e.MaCongViecSau }).HasName("PK__PhuThuoc__6B33129863F1E20D");

            entity.ToTable("PhuThuocCongViec");

            entity.HasIndex(e => e.MaCongViecSau, "IDX_PhuThuoc_CongViecSau");

            entity.HasIndex(e => e.MaCongViecTruoc, "IDX_PhuThuoc_CongViecTruoc");

            entity.Property(e => e.LoaiPhuThuoc)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Finish-to-Start");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.MaCongViecSauNavigation).WithMany(p => p.PhuThuocCongViecMaCongViecSauNavigations)
                .HasForeignKey(d => d.MaCongViecSau)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PhuThuocC__MaCon__6BE40491");

            entity.HasOne(d => d.MaCongViecTruocNavigation).WithMany(p => p.PhuThuocCongViecMaCongViecTruocNavigations)
                .HasForeignKey(d => d.MaCongViecTruoc)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PhuThuocC__MaCon__6AEFE058");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshToken");

            entity.Property(e => e.RefreshToken1)
                .HasMaxLength(255)
                .HasColumnName("RefreshToken");
            entity.Property(e => e.ThoiGianHetHan).HasPrecision(0);

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.MaNguoiDung)
                .HasConstraintName("FK_RefreshToken_NguoiDung");
        });

        modelBuilder.Entity<RuiRoDuAn>(entity =>
        {
            entity.HasKey(e => e.MaRuiRo).HasName("PK__RuiRoDuA__EE6C9EA5173A7958");

            entity.ToTable("RuiRoDuAn");

            entity.Property(e => e.MucDoAnhHuong).HasMaxLength(20);
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TieuDeRuiRo).HasMaxLength(200);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("Dang mo");
            entity.Property(e => e.XacSuat).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.MaCongViecNavigation).WithMany(p => p.RuiRoDuAns)
                .HasForeignKey(d => d.MaCongViec)
                .HasConstraintName("FK__RuiRoDuAn__MaCon__1EA48E88");

            entity.HasOne(d => d.MaDuAnNavigation).WithMany(p => p.RuiRoDuAns)
                .HasForeignKey(d => d.MaDuAn)
                .HasConstraintName("FK__RuiRoDuAn__MaDuA__1DB06A4F");

            entity.HasOne(d => d.MaLoaiRuiRoNavigation).WithMany(p => p.RuiRoDuAns)
                .HasForeignKey(d => d.MaLoaiRuiRo)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RuiRoDuAn__MaLoa__1F98B2C1");
        });

        modelBuilder.Entity<RuiRoNghiemTrong>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("RuiRoNghiemTrong");

            entity.Property(e => e.MucDoRuiRo).HasMaxLength(20);
            entity.Property(e => e.NgayDuBao).HasColumnType("datetime");
            entity.Property(e => e.TenCongViec).HasMaxLength(200);
            entity.Property(e => e.TenDuAn).HasMaxLength(150);
            entity.Property(e => e.TenLoaiRuiRo).HasMaxLength(100);
            entity.Property(e => e.XacSuatRuiRo).HasColumnType("decimal(5, 2)");
        });

        modelBuilder.Entity<SnapshotTienDoDuAn>(entity =>
        {
            entity.HasKey(e => e.MaSnapshot).HasName("PK__Snapshot__920B1A3F85EBB00B");

            entity.ToTable("SnapshotTienDoDuAn");

            entity.HasIndex(e => new { e.MaDuAn, e.NgayGhiNhan }, "IDX_Snapshot_MaDuAn_Ngay");

            entity.HasIndex(e => new { e.MaDuAn, e.NgayGhiNhan }, "UQ_Snapshot_DuAn_Ngay").IsUnique();

            entity.Property(e => e.NgayGhiNhan).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SoNhanSuDangHoatDong).HasDefaultValue(0);
            entity.Property(e => e.TongChiPhiThucTe)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(15, 2)");
            entity.Property(e => e.TongGioUocTinhConLai)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TongTaskBiChan).HasDefaultValue(0);
            entity.Property(e => e.TongTaskDangMo).HasDefaultValue(0);
            entity.Property(e => e.TongTaskHoanThanh).HasDefaultValue(0);
            entity.Property(e => e.TongTaskTreHan).HasDefaultValue(0);

            entity.HasOne(d => d.MaDuAnNavigation).WithMany(p => p.SnapshotTienDoDuAns)
                .HasForeignKey(d => d.MaDuAn)
                .HasConstraintName("FK__SnapshotT__MaDuA__7A3223E8");
        });

        modelBuilder.Entity<Sprint>(entity =>
        {
            entity.HasKey(e => e.MaSprint).HasName("PK__Sprint__FE261DCDB1587062");

            entity.ToTable("Sprint");

            entity.HasIndex(e => e.MaDuAn, "IDX_Sprint_MaDuAn");

            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TenSprint).HasMaxLength(100);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(30)
                .HasDefaultValue("Chua bat dau");

            entity.HasOne(d => d.MaDuAnNavigation).WithMany(p => p.Sprints)
                .HasForeignKey(d => d.MaDuAn)
                .HasConstraintName("FK__Sprint__MaDuAn__6442E2C9");
        });

        modelBuilder.Entity<TaiLieu>(entity =>
        {
            entity.HasKey(e => e.MaTaiLieu).HasName("PK__TaiLieu__FD18A6576E75FFB9");

            entity.ToTable("TaiLieu");

            entity.Property(e => e.DuongDan).HasMaxLength(500);
            entity.Property(e => e.LoaiTaiLieu).HasMaxLength(50);
            entity.Property(e => e.NgayTaiLen)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TenTaiLieu).HasMaxLength(200);

            entity.HasOne(d => d.MaCongViecNavigation).WithMany(p => p.TaiLieus)
                .HasForeignKey(d => d.MaCongViec)
                .HasConstraintName("FK__TaiLieu__MaCongV__03F0984C");

            entity.HasOne(d => d.MaDuAnNavigation).WithMany(p => p.TaiLieus)
                .HasForeignKey(d => d.MaDuAn)
                .HasConstraintName("FK__TaiLieu__MaDuAn__02FC7413");

            entity.HasOne(d => d.MaNguoiTaiLenNavigation).WithMany(p => p.TaiLieus)
                .HasForeignKey(d => d.MaNguoiTaiLen)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__TaiLieu__MaNguoi__04E4BC85");
        });

        modelBuilder.Entity<ThanhVienDuAn>(entity =>
        {
            entity.HasKey(e => e.MaThanhVienDuAn).HasName("PK__ThanhVie__F1A27A3104E5F6D9");

            entity.ToTable("ThanhVienDuAn");

            entity.HasIndex(e => new { e.MaDuAn, e.MaNguoiDung }, "UQ__ThanhVie__D384CC931CA5327C").IsUnique();

            entity.Property(e => e.NgayThamGia).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TyLePhanBo).HasDefaultValue(100);
            entity.Property(e => e.VaiTroTrongDuAn).HasMaxLength(50);

            entity.HasOne(d => d.MaDuAnNavigation).WithMany(p => p.ThanhVienDuAns)
                .HasForeignKey(d => d.MaDuAn)
                .HasConstraintName("FK__ThanhVien__MaDuA__59FA5E80");

            entity.HasOne(d => d.MaNguoiDungNavigation).WithMany(p => p.ThanhVienDuAns)
                .HasForeignKey(d => d.MaNguoiDung)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ThanhVien__MaNgu__5AEE82B9");
        });

        modelBuilder.Entity<ThanhVienQuaTai>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("ThanhVienQuaTai");

            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.KhoiLuongHienTai).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.KhoiLuongToiDa).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.PhanTramTai).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.TenVaiTro).HasMaxLength(100);
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.MaVaiTro).HasName("PK__VaiTro__C24C41CFD879962F");

            entity.ToTable("VaiTro");

            entity.HasIndex(e => e.TenVaiTro, "UQ__VaiTro__1DA558144F5419AE").IsUnique();

            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TenVaiTro).HasMaxLength(100);
        });

        modelBuilder.Entity<v_Dataset_DeXuatGiaoViec>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Dataset_DeXuatGiaoViec");

            entity.Property(e => e.DiemChatLuongTrungBinhLichSu).HasColumnType("decimal(38, 6)");
            entity.Property(e => e.PhanTramTaiNhanSu).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.SoGioUocTinh).HasColumnType("decimal(8, 2)");
        });

        modelBuilder.Entity<v_Dataset_DuBaoTreHan>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Dataset_DuBaoTreHan");

            entity.Property(e => e.DoUuTien).HasMaxLength(20);
            entity.Property(e => e.KhoiLuongHienTaiNhanSu).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.SoGioUocTinh).HasColumnType("decimal(8, 2)");
        });

        modelBuilder.Entity<v_Dataset_PhatHienDiemNghen>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_Dataset_PhatHienDiemNghen");

            entity.Property(e => e.MaCongViec).ValueGeneratedOnAdd();
            entity.Property(e => e.SoGioUocTinh).HasColumnType("decimal(8, 2)");
            entity.Property(e => e.TrangThaiHienTai).HasMaxLength(30);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
