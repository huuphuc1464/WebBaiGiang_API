using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebBaiGiangAPI.Models;

namespace WebBaiGiangAPI.Data
{
    public static class Seeder
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new AppDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>()))
            {
                // Kiểm tra nếu dữ liệu đã tồn tại
                if (context.NguoiDungs.Any())
                {
                    return; // Dữ liệu đã tồn tại, không làm gì cả
                }

                // Thêm dữ liệu mẫu
                context.Khoas.AddRange(
                    new Khoa
                    {
                        MaKhoa = "K1",
                        TenKhoa = "CNTT",
                        TrangThai = "1"
                    },
                    new Khoa
                    {
                        MaKhoa = "K2",
                        TenKhoa = "KTDN",
                        TrangThai = "1"
                    }
                );
                context.BoMons.AddRange(
                    new BoMon
                    {
                        MaBoMon = "BM1",
                        MaKhoa = "K1",
                        TenBoMon = "Lập trình web",
                        TrangThai = "1"
                    },
                    new BoMon
                    {
                        MaBoMon = "BM2",
                        MaKhoa = "K1",
                        TenBoMon = "Lập trình di động", 
                        TrangThai = "1"
                    },
                    new BoMon
                    { 
                        MaBoMon = "BM3",
                        MaKhoa = "K2",
                        TenBoMon = "Kế toán",
                        TrangThai = "1"
                    }
                );
                context.Quyens.AddRange(
                    new Quyen
                    {
                        MaQuyen = "Q1",
                        TenQuyen = "Giảng viên",
                        TrangThai = "1"
                    },
                    new Quyen
                    {
                        MaQuyen = "Q2",
                        TenQuyen = "Sinh viên",
                        TrangThai = "2"
                    }
                    );

                var passwordHasher = new PasswordHasher<NguoiDung>();
                context.NguoiDungs.AddRange(
                    new NguoiDung
                    {
                        MaNguoiDung = "ND1",
                        MaQuyen = "Q1",
                        MaKhoa = "K1",
                        MaBoMon = "BM1",
                        Email = "example1@mail.com",
                        Password = passwordHasher.HashPassword(null, "password1"),
                        HoTen = "Tran Huu Phuc",
                        Lop = "CDTH22WEBC ",
                        DiaChi = "123 Street",
                        AnhDaiDien = "default.jpg",
                        MSSV = "0306221464",
                        SDT = "0123456789",
                        GioiTinh = "Nam",
                        NgaySinh = new DateOnly(2004, 02, 17),
                        TrangThai = "1"
                    },
                    new NguoiDung
                    {
                        MaNguoiDung = "ND2",
                        MaQuyen = "Q2",
                        MaKhoa = "K2",
                        MaBoMon = "BM2",
                        Email = "example2@mail.com",
                        Password = passwordHasher.HashPassword(null, "password2"),
                        HoTen = "Nguyen Van A",
                        Lop = "CNTT01",
                        DiaChi = "456 Street",
                        AnhDaiDien = "default.jpg",
                        MSSV = "MSSV002",
                        SDT = "0987654321",
                        GioiTinh = "Nữ",
                        NgaySinh = new DateOnly(1999, 5, 20),
                        TrangThai = "1"
                    }
                );
                
                context.SaveChanges();
            }
        }
    }
}
