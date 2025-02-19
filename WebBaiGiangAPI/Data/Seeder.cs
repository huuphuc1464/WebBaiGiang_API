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
                context.NguoiDungs.AddRange(
                    new NguoiDung
                    {
                        MaNguoiDung = "1",
                        MaQuyen = "1",
                        MaKhoa = "030",
                        MaBoMon = "6",
                        Email = "example1@mail.com",
                        Password = "password1",
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
                        MaNguoiDung = "2",
                        MaQuyen = "2",
                        MaKhoa = "030",
                        MaBoMon = "6",
                        Email = "example2@mail.com",
                        Password = "password2",
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
