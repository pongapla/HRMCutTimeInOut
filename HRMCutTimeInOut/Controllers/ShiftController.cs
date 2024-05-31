
using Microsoft.AspNetCore.Mvc;
using HRMCutTimeInOut.Models;
using System.Collections.Generic;
using HRMCutTimeInOut.Data.SQLSERVER;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Linq;
using System;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Data;
using Microsoft.AspNetCore.Authorization;
namespace HRMCutTimeInOut.Controllers;

public class ShiftController : Controller
{
    private readonly ApplicationDBContext _context;

    public ShiftController(ApplicationDBContext context)
    {
        _context = context;
    }
    [Authorize]
    public IActionResult index()
    {
        try
        {
            IConfiguration configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .Build();
            string connectionString = configuration.GetConnectionString("DefaultConnection");
            List<Shift> shiftList = new List<Shift>();
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                // สร้าง SqlCommand โดยระบุชื่อของ stored procedure
                using (SqlCommand command = new SqlCommand("GetEmployeeData", sqlConnection))
                {
                    // กำหนดประเภทของคำสั่งเป็น stored procedure
                    command.CommandType = CommandType.StoredProcedure;

                    // สร้าง SqlDataReader เพื่ออ่านผลลัพธ์ที่ได้จากการเรียก stored procedure
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // วนลูปอ่านข้อมูลจาก SqlDataReader และเพิ่มข้อมูลลงใน sortedData
                        while (reader.Read())
                        {
                            // อ่านข้อมูลจาก SqlDataReader และทำการเพิ่มลงใน sortedData
                            Shift data = new Shift();
                            data.code = reader["code"].ToString();
                            data.name = reader["name"].ToString();
                            data.department = reader["department"].ToString();
                            data.nameShift = reader["shift"].ToString();

                            shiftList.Add(data);
                        }
                    }
                }

                sqlConnection.Close();
            }
            var dataModel = shiftList.OrderBy(x => x.department).ToList();
            return View(dataModel);
        }
        catch (Exception ex)
        {
            // กรณีเกิดข้อผิดพลาด
            // แสดงข้อความข้อผิดพลาดในคอนโทรลเลอร์
            ViewBag.ErrorMessage = "An error occurred while processing the request: " + ex.Message;
            return View("Error");
        }
        
    }
    
    
    [HttpPost]
    public IActionResult AddShift(AddShift modelData)
    {
        try
        {

            IConfiguration configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .Build();

            string connectionString = configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                
                    // ตรวจสอบว่าข้อมูลนี้มีอยู่ในตารางหรือไม่
                    string checkQuery = "IF NOT EXISTS (SELECT 1 FROM TLM_Application.dbo.TLM_Employee_Shift WHERE EmpCode = @EmpCode AND Shift = @Shift) BEGIN INSERT INTO TLM_Application.dbo.TLM_Employee_Shift (EmpCode, Shift) VALUES (@EmpCode, @Shift) END";
                    SqlCommand checkCommand = new SqlCommand(checkQuery, sqlConnection);
                    checkCommand.Parameters.AddWithValue("@EmpCode", modelData.code);
                    checkCommand.Parameters.AddWithValue("@Shift", modelData.shift);
                    checkCommand.ExecuteNonQuery();
                
                sqlConnection.Close();
            }

        } catch (Exception ex)
        {
            ViewBag.ErrorMessage = "An error occurred while processing the request: " + ex.Message;
            return View("Error");
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult EditShift(AddShift modelData)
    {
        try
        {

            IConfiguration configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .Build();

            string connectionString = configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();


                // ตรวจสอบว่าข้อมูลนี้มีอยู่ในตารางหรือไม่

                string updateQuery = @"IF EXISTS (SELECT 1 FROM TLM_Application.dbo.TLM_Employee_Shift WHERE EmpCode = @EmpCode)
                                        BEGIN
                                            UPDATE TLM_Application.dbo.TLM_Employee_Shift
                                            SET Shift = @Shift
                                            WHERE EmpCode = @EmpCode
                                        END";
                SqlCommand checkCommand = new SqlCommand(updateQuery, sqlConnection);
                checkCommand.Parameters.AddWithValue("@EmpCode", modelData.code);
                checkCommand.Parameters.AddWithValue("@Shift", modelData.shift);
                checkCommand.ExecuteNonQuery();

                sqlConnection.Close();
            }

        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = "An error occurred while processing the request: " + ex.Message;
            return View("Error");
        }

        return RedirectToAction("Index");
    }

    public IActionResult ConnectCMIF5S()
    {
        string ipAddress = "192.168.23.189";
        int port = 5005;

        // เชื่อมต่อเครื่อง
        using (TcpClient client = new TcpClient(ipAddress, port))
        {
            // สร้าง NetworkStream เพื่ออ่านและเขียนข้อมูล
            NetworkStream stream = client.GetStream();

            // ส่งข้อมูลไปยังเครื่อง
            string message = "Hello, CMIF65S!";
            byte[] data = Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);
            Console.WriteLine($"Sent: {message}");
              
            // อ่านข้อมูลที่เครื่องตอบกลับ
            data = new byte[256];
            StringBuilder response = new StringBuilder();
            int bytesRead;
            do
            {
                bytesRead = stream.Read(data, 0, data.Length);
                response.Append(Encoding.ASCII.GetString(data, 0, bytesRead));
            }
            while (stream.DataAvailable);

            // แสดงข้อมูลที่ได้รับ
            Console.WriteLine($"Received: {response}");
        }
        return View();
    }
}
