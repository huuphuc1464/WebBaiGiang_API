using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebBaiGiangAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Khoas",
                columns: table => new
                {
                    MaKhoa = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    TenKhoa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Khoas", x => x.MaKhoa);
                });

            migrationBuilder.CreateTable(
                name: "Quyens",
                columns: table => new
                {
                    MaQuyen = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    TenQuyen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quyens", x => x.MaQuyen);
                });

            migrationBuilder.CreateTable(
                name: "BoMons",
                columns: table => new
                {
                    MaBoMon = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaKhoa = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    TenBoMon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoMons", x => x.MaBoMon);
                    table.ForeignKey(
                        name: "FK_BoMons_Khoas_MaKhoa",
                        column: x => x.MaKhoa,
                        principalTable: "Khoas",
                        principalColumn: "MaKhoa",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDungs",
                columns: table => new
                {
                    MaNguoiDung = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaQuyen = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaKhoa = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaBoMon = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Lop = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AnhDaiDien = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MSSV = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SDT = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    GioiTinh = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    NgaySinh = table.Column<DateOnly>(type: "date", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDungs", x => x.MaNguoiDung);
                    table.ForeignKey(
                        name: "FK_NguoiDungs_BoMons_MaBoMon",
                        column: x => x.MaBoMon,
                        principalTable: "BoMons",
                        principalColumn: "MaBoMon",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NguoiDungs_Khoas_MaKhoa",
                        column: x => x.MaKhoa,
                        principalTable: "Khoas",
                        principalColumn: "MaKhoa",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NguoiDungs_Quyens_MaQuyen",
                        column: x => x.MaQuyen,
                        principalTable: "Quyens",
                        principalColumn: "MaQuyen",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HocPhans",
                columns: table => new
                {
                    MaHocPhan = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaBoMon = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaGiangVien = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    TenHocPhan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AnhDaiDien = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MoTaNgan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTaChiTiet = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DiemDanhGia = table.Column<int>(type: "int", nullable: false),
                    SoLuongSinhVien = table.Column<int>(type: "int", nullable: false),
                    LanCapNhatCuoi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileDeCuong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HinhThucHoc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SoTiet = table.Column<int>(type: "int", nullable: false),
                    SoTinChi = table.Column<int>(type: "int", nullable: false),
                    LoaiHocPhan = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HocPhans", x => x.MaHocPhan);
                    table.ForeignKey(
                        name: "FK_HocPhans_BoMons_MaBoMon",
                        column: x => x.MaBoMon,
                        principalTable: "BoMons",
                        principalColumn: "MaBoMon",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HocPhans_NguoiDungs_MaGiangVien",
                        column: x => x.MaGiangVien,
                        principalTable: "NguoiDungs",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThongTinWebs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenWeb = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Logo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SDT = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Facebook = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Gmail = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Fax = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaNguoiThayDoiCuoi = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    ThoiGianThayDoiCuoi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongTinWebs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThongTinWebs_NguoiDungs_MaNguoiThayDoiCuoi",
                        column: x => x.MaNguoiThayDoiCuoi,
                        principalTable: "NguoiDungs",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lops",
                columns: table => new
                {
                    MaLop = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaHocPhan = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    HocKy = table.Column<int>(type: "int", nullable: false),
                    TenLop = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhongHoc = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NamHoc = table.Column<int>(type: "int", nullable: false),
                    TrangThaiLop = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    TrangThaiHoc = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lops", x => x.MaLop);
                    table.ForeignKey(
                        name: "FK_Lops_HocPhans_MaHocPhan",
                        column: x => x.MaHocPhan,
                        principalTable: "HocPhans",
                        principalColumn: "MaHocPhan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaiGiangs",
                columns: table => new
                {
                    MaBaiGiang = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaHocPhan = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaLop = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    HinhAnh = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Chuong = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    TenBaiGiang = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Video = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaiGiangs", x => x.MaBaiGiang);
                    table.ForeignKey(
                        name: "FK_BaiGiangs_HocPhans_MaHocPhan",
                        column: x => x.MaHocPhan,
                        principalTable: "HocPhans",
                        principalColumn: "MaHocPhan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaiGiangs_Lops_MaLop",
                        column: x => x.MaLop,
                        principalTable: "Lops",
                        principalColumn: "MaLop",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaiTaps",
                columns: table => new
                {
                    MaBaiTap = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaLop = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaHocPhan = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaNguoiTao = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    TenBaiTap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LinkBaiTap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileBaiTap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HanNop = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaiTaps", x => x.MaBaiTap);
                    table.ForeignKey(
                        name: "FK_BaiTaps_HocPhans_MaHocPhan",
                        column: x => x.MaHocPhan,
                        principalTable: "HocPhans",
                        principalColumn: "MaHocPhan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaiTaps_Lops_MaLop",
                        column: x => x.MaLop,
                        principalTable: "Lops",
                        principalColumn: "MaLop",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BaiTaps_NguoiDungs_MaNguoiTao",
                        column: x => x.MaNguoiTao,
                        principalTable: "NguoiDungs",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BangDiems",
                columns: table => new
                {
                    MaBangDiem = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaLop = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaSinhVien = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    ChuyenCan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HeSo1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HeSo2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ThiLan1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ThiLan2 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TBKT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongKetLan1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongKetLan2 = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BangDiems", x => x.MaBangDiem);
                    table.ForeignKey(
                        name: "FK_BangDiems_Lops_MaLop",
                        column: x => x.MaLop,
                        principalTable: "Lops",
                        principalColumn: "MaLop",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BangDiems_NguoiDungs_MaSinhVien",
                        column: x => x.MaSinhVien,
                        principalTable: "NguoiDungs",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DangKyLopHocs",
                columns: table => new
                {
                    MaDangKy = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaSinhVien = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaLop = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    NgayDangKy = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DangKyLopHocs", x => x.MaDangKy);
                    table.ForeignKey(
                        name: "FK_DangKyLopHocs_Lops_MaLop",
                        column: x => x.MaLop,
                        principalTable: "Lops",
                        principalColumn: "MaLop",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DangKyLopHocs_NguoiDungs_MaSinhVien",
                        column: x => x.MaSinhVien,
                        principalTable: "NguoiDungs",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DanhGias",
                columns: table => new
                {
                    MaDanhGia = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaSinhVien = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaLop = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    NoiDungDanhGia = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SoDiem = table.Column<int>(type: "int", nullable: false),
                    NgayDanhGia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGias", x => x.MaDanhGia);
                    table.ForeignKey(
                        name: "FK_DanhGias_Lops_MaLop",
                        column: x => x.MaLop,
                        principalTable: "Lops",
                        principalColumn: "MaLop",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DanhGias_NguoiDungs_MaSinhVien",
                        column: x => x.MaSinhVien,
                        principalTable: "NguoiDungs",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiemDanhs",
                columns: table => new
                {
                    MaDiemDanh = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaSinhVien = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaLop = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    NgayHoc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiemDanhs", x => x.MaDiemDanh);
                    table.ForeignKey(
                        name: "FK_DiemDanhs_Lops_MaLop",
                        column: x => x.MaLop,
                        principalTable: "Lops",
                        principalColumn: "MaLop",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiemDanhs_NguoiDungs_MaSinhVien",
                        column: x => x.MaSinhVien,
                        principalTable: "NguoiDungs",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NopBaiTaps",
                columns: table => new
                {
                    MaNopBai = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaBaiTap = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    MaSinhVien = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    FileNop = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayNop = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NopBaiTaps", x => x.MaNopBai);
                    table.ForeignKey(
                        name: "FK_NopBaiTaps_BaiTaps_MaBaiTap",
                        column: x => x.MaBaiTap,
                        principalTable: "BaiTaps",
                        principalColumn: "MaBaiTap",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NopBaiTaps_NguoiDungs_MaSinhVien",
                        column: x => x.MaSinhVien,
                        principalTable: "NguoiDungs",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaiGiangs_MaHocPhan",
                table: "BaiGiangs",
                column: "MaHocPhan");

            migrationBuilder.CreateIndex(
                name: "IX_BaiGiangs_MaLop",
                table: "BaiGiangs",
                column: "MaLop");

            migrationBuilder.CreateIndex(
                name: "IX_BaiTaps_MaHocPhan",
                table: "BaiTaps",
                column: "MaHocPhan");

            migrationBuilder.CreateIndex(
                name: "IX_BaiTaps_MaLop",
                table: "BaiTaps",
                column: "MaLop");

            migrationBuilder.CreateIndex(
                name: "IX_BaiTaps_MaNguoiTao",
                table: "BaiTaps",
                column: "MaNguoiTao");

            migrationBuilder.CreateIndex(
                name: "IX_BangDiems_MaLop",
                table: "BangDiems",
                column: "MaLop");

            migrationBuilder.CreateIndex(
                name: "IX_BangDiems_MaSinhVien",
                table: "BangDiems",
                column: "MaSinhVien");

            migrationBuilder.CreateIndex(
                name: "IX_BoMons_MaKhoa",
                table: "BoMons",
                column: "MaKhoa");

            migrationBuilder.CreateIndex(
                name: "IX_DangKyLopHocs_MaLop",
                table: "DangKyLopHocs",
                column: "MaLop");

            migrationBuilder.CreateIndex(
                name: "IX_DangKyLopHocs_MaSinhVien",
                table: "DangKyLopHocs",
                column: "MaSinhVien");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGias_MaLop",
                table: "DanhGias",
                column: "MaLop");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGias_MaSinhVien",
                table: "DanhGias",
                column: "MaSinhVien");

            migrationBuilder.CreateIndex(
                name: "IX_DiemDanhs_MaLop",
                table: "DiemDanhs",
                column: "MaLop");

            migrationBuilder.CreateIndex(
                name: "IX_DiemDanhs_MaSinhVien",
                table: "DiemDanhs",
                column: "MaSinhVien");

            migrationBuilder.CreateIndex(
                name: "IX_HocPhans_MaBoMon",
                table: "HocPhans",
                column: "MaBoMon");

            migrationBuilder.CreateIndex(
                name: "IX_HocPhans_MaGiangVien",
                table: "HocPhans",
                column: "MaGiangVien");

            migrationBuilder.CreateIndex(
                name: "IX_Lops_MaHocPhan",
                table: "Lops",
                column: "MaHocPhan");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDungs_MaBoMon",
                table: "NguoiDungs",
                column: "MaBoMon");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDungs_MaKhoa",
                table: "NguoiDungs",
                column: "MaKhoa");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDungs_MaQuyen",
                table: "NguoiDungs",
                column: "MaQuyen");

            migrationBuilder.CreateIndex(
                name: "IX_NopBaiTaps_MaBaiTap",
                table: "NopBaiTaps",
                column: "MaBaiTap");

            migrationBuilder.CreateIndex(
                name: "IX_NopBaiTaps_MaSinhVien",
                table: "NopBaiTaps",
                column: "MaSinhVien");

            migrationBuilder.CreateIndex(
                name: "IX_ThongTinWebs_MaNguoiThayDoiCuoi",
                table: "ThongTinWebs",
                column: "MaNguoiThayDoiCuoi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaiGiangs");

            migrationBuilder.DropTable(
                name: "BangDiems");

            migrationBuilder.DropTable(
                name: "DangKyLopHocs");

            migrationBuilder.DropTable(
                name: "DanhGias");

            migrationBuilder.DropTable(
                name: "DiemDanhs");

            migrationBuilder.DropTable(
                name: "NopBaiTaps");

            migrationBuilder.DropTable(
                name: "ThongTinWebs");

            migrationBuilder.DropTable(
                name: "BaiTaps");

            migrationBuilder.DropTable(
                name: "Lops");

            migrationBuilder.DropTable(
                name: "HocPhans");

            migrationBuilder.DropTable(
                name: "NguoiDungs");

            migrationBuilder.DropTable(
                name: "BoMons");

            migrationBuilder.DropTable(
                name: "Quyens");

            migrationBuilder.DropTable(
                name: "Khoas");
        }
    }
}
