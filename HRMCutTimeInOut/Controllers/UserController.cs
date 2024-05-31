using Microsoft.AspNetCore.Mvc;
using HRMCutTimeInOut.Models;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Data.OleDb;
using System.Linq;
using ClosedXML.Excel;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Authorization;



namespace HRMCutTimeInOut.Controllers;

public class UserController : Controller
{
    private readonly string _accessConnectionString;
    
    private object jsonString;

    public object JsonConvert { get; private set; }

    public UserController(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _accessConnectionString = configuration.GetConnectionString("AccessConnection");
    }

    [Authorize]
    public IActionResult Index(string startDate, string endDate)

    {

        if (startDate == null || endDate == null)
        {
            DateTime currentDate = DateTime.ParseExact(DateTime.Now.ToString("yyyy-MM-dd"), "yyyy-MM-dd", CultureInfo.InvariantCulture); // กำหนดค่า default เริ่มต้นเป็นวันที่ปัจจุบัน
            DateTime newDate = currentDate.AddDays(-1);
            //startDate = newDate.ToString("yyyy-MM-dd");
            //endDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"); // กำหนดค่า default เริ่มต้นเป็นวันที่ปัจจุบัน 
            startDate = "";
            endDate = "";
        }

        try
        {
            IConfiguration configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .Build();

            string connectionString = configuration.GetConnectionString("DefaultConnection");

            List<Timeinout> dataTimeInOut = new List<Timeinout>();

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                string sql = @"SELECT ROW_NUMBER() OVER(ORDER BY ct.dateTime) AS No, 
                ct.dateTime AS _DateTime, te.EmpCode AS Code, 
                te.prefixthai + ' ' + te.EmpFNameThai + ' ' + te.EmpLNameThai AS Name, 
                te.OrgTDesc AS Dpname, ts.Shift 
                FROM TestScheduler.dbo.cuttime ct 
                INNER JOIN TLM_Application.dbo.TLM_Employee te ON ct.code = te.EmpCode 
                INNER JOIN TLM_Application.dbo.TLM_Employee_Shift ts ON ts.EmpCode = te.EmpCode 
                WHERE ct.dateTime >= @startDate AND ct.dateTime < @endDate";

                using (SqlCommand sqlCommand = new SqlCommand(sql, sqlConnection))
                {
                    sqlCommand.Parameters.AddWithValue("@startDate", startDate);
                    sqlCommand.Parameters.AddWithValue("@endDate", endDate);

                    using (SqlDataReader dataReader = sqlCommand.ExecuteReader())
                    {
                        if (dataReader.HasRows)
                        {
                            while (dataReader.Read())
                            {
                                Timeinout timeInOut = new Timeinout();
                                timeInOut.No = (int)dataReader.GetInt64(dataReader.GetOrdinal("No"));
                                timeInOut._DateTime = dataReader.GetDateTime(dataReader.GetOrdinal("_DateTime"));
                                if (!dataReader.IsDBNull(dataReader.GetOrdinal("Code")))
                                {
                                    timeInOut.Code = dataReader.GetString(dataReader.GetOrdinal("Code"));
                                }
                                timeInOut.Name = dataReader.GetString(dataReader.GetOrdinal("Name"));
                                timeInOut.DpName = dataReader.GetString(dataReader.GetOrdinal("Dpname"));
                                timeInOut.Shift = dataReader.GetString(dataReader.GetOrdinal("Shift"));

                                dataTimeInOut.Add(timeInOut);
                            }
                        }
                    }
                }
            }

            var result = dataTimeInOut.OrderBy(x => x.Code).ThenBy(x => x._DateTime).ToList();

            return View(result);
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = "เกิดข้อผิดพลาดในการดึงข้อมูล: " + ex.Message;
            return View("Error");
        }
    }

    public IActionResult Search(string startDate, string endDate)
    {
        return RedirectToAction("Index", "User", new { startDate, endDate });
    }

    [HttpPost]
    public IActionResult TimeOverview([FromBody] setTimeModel requestModel)
    {
        try
        {
            IConfiguration configuration = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
              .Build();
            string connectionString = configuration.GetConnectionString("DefaultConnection");

            var stDate = requestModel.startDate;
            var enDate = requestModel.endDate;

            List<TimeCutData> itmeData = new List<TimeCutData>(); // ประกาศตัวแปรเพื่อเก็บข้อมูลที่ดึงมาจากฐานข้อมูล

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();

                // สร้าง SqlCommand โดยระบุชื่อของ stored procedure
                using (SqlCommand command = new SqlCommand("TLM_HR_GetTimeInOut_V1", sqlConnection))
                {
                    // กำหนดประเภทของคำสั่งเป็น stored procedure
                    command.CommandType = CommandType.StoredProcedure;

                    // เพิ่มพารามิเตอร์ของ stored procedure (หากมี)
                    command.Parameters.AddWithValue("@StartDate", stDate); // ใส่วันที่เริ่มต้นที่คุณต้องการ
                    command.Parameters.AddWithValue("@EndDate", enDate); // ใส่วันที่สิ้นสุดที่คุณต้องการ
                    command.Parameters.AddWithValue("@EmpID", "");
                    command.Parameters.AddWithValue("@DeptID", "");
                    // สร้าง SqlDataReader เพื่ออ่านผลลัพธ์ที่ได้จากการเรียก stored procedure
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        // วนลูปอ่านข้อมูลจาก SqlDataReader และเพิ่มข้อมูลลงใน sortedData
                        while (reader.Read())
                        {
                            // อ่านข้อมูลจาก SqlDataReader และทำการเพิ่มลงใน sortedData
                            TimeCutData data = new TimeCutData();
                            data.dateData = ((DateTime)reader["MDate"]).ToString("dd-MM-yyyy");
                            data.timeIn = reader["TimeIn"].ToString();
                            data.timeOut = reader["TimeOut"].ToString();
                            data.code = reader["Code"].ToString();
                            data.name = reader["Name"].ToString();
                            data.department = reader["Department"].ToString() ;
                            data.shift = reader["_Shift"].ToString();
                            data.masterShift = reader["MS"].ToString();
                            data.workShift = reader["WS"].ToString();
                            data.totalHour = reader["THr"].ToString();
                            data.checkResult = reader["CheckResult"].ToString();
                            itmeData.Add(data);
                        }
                    }
                }

                sqlConnection.Close();
            }

            // ส่งข้อมูลกลับไปยัง view ในรูปแบบ JSON
            return Json(new { ok = true, data = itmeData });
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
    public IActionResult Overview([FromBody] List<TimeCutData> modelData)
    {
        var sortedModel = modelData.OrderBy(x => x.department)
                    .ThenBy(x => x.code)
                    .ThenBy(x => x.dateData)
                    .ToList();
        var model = sortedModel;
        return View(model);
    }

    public async Task<IActionResult> ExportExcel([FromBody] List<Timeinout> modelData)
    {
        try
        {
            if (modelData == null || modelData.Count == 0)
            {
                return BadRequest("No data provided.");
            }

            // สร้างไฟล์ Excel
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Data");

            // ตั้งค่าหัวข้อคอลัมน์
            worksheet.Cell(1, 1).Value = "No";
            worksheet.Cell(1, 2).Value = "Date";
            worksheet.Cell(1, 3).Value = "Code";
            worksheet.Cell(1, 4).Value = "Name";
            worksheet.Cell(1, 5).Value = "Department";

            // เขียนข้อมูลลงในแต่ละแถว
            int row = 2;
            foreach (var item in modelData)
            {
                worksheet.Cell(row, 1).Value = item.No;
                worksheet.Cell(row, 2).Value = item._DateTime;
                worksheet.Cell(row, 3).Value = item.Code;
                worksheet.Cell(row, 4).Value = item.Name;
                worksheet.Cell(row, 5).Value = item.DpName;
                row++;
            }

            // บันทึกไฟล์ Excel ลง MemoryStream
            var stream = new MemoryStream();


            workbook.SaveAs(stream);
            stream.Position = 0;



            // ส่งไฟล์ Excel กลับไปยังไคลเอ็นต์
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "data.xlsx");

        }
        catch (Exception ex)
        {
            // กรณีเกิดข้อผิดพลาด
            return StatusCode(500, "An error occurred while processing the request.");
        }
    }

    public async Task<IActionResult> ExportExcelOverview([FromBody] List<TimeCutData> modelData)
    {
        try
        {
            if (modelData == null || modelData.Count == 0)
            {
                return BadRequest("No data provided.");
            }

            // สร้างไฟล์ Excel
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Data");

            // ตั้งค่าหัวข้อคอลัมน์
            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "TimeIn";
            worksheet.Cell(1, 3).Value = "TimeOut";
            worksheet.Cell(1, 4).Value = "Code";
            worksheet.Cell(1, 5).Value = "Name";
            worksheet.Cell(1, 6).Value = "Department";
            worksheet.Cell(1, 7).Value = "Groups";

            // เขียนข้อมูลลงในแต่ละแถว
            int row = 2;
            foreach (var item in modelData)
            {
                worksheet.Cell(row, 1).Value = item.dateData;
                worksheet.Cell(row, 2).Value = item.timeIn;
                worksheet.Cell(row, 3).Value = item.timeOut;
                worksheet.Cell(row, 4).Value = item.code;
                worksheet.Cell(row, 5).Value = item.name;
                worksheet.Cell(row, 6).Value = item.department;
                worksheet.Cell(row, 7).Value = item.shift;
                row++;
            }

            // บันทึกไฟล์ Excel ลง MemoryStream
            var stream = new MemoryStream();


            workbook.SaveAs(stream);
            stream.Position = 0;



            // ส่งไฟล์ Excel กลับไปยังไคลเอ็นต์
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "dataOverview.xlsx");

        }
        catch (Exception ex)
        {
            // กรณีเกิดข้อผิดพลาด
            return StatusCode(500, "An error occurred while processing the request.");
        }
    }
    public async Task<IActionResult> ExportText([FromBody] List<TimeCutData> modelData)
    {
        try
        {
            if (modelData == null || modelData.Count == 0)
            {
                return BadRequest("No data provided.");
            }

            // สร้างข้อความจากข้อมูล
            StringBuilder textBuilder = new StringBuilder();
            var r = modelData.OrderBy(x => x.code)
                            .ThenBy(x => x.dateData)
                            .ToList();

            // เขียนข้อมูลลงในแต่ละแถว
            foreach (var item in r)
            {
                string formattedDate = item.dateData;
                if (!string.IsNullOrEmpty(item.timeIn)) 
                { 
                    textBuilder.AppendLine($"            {formattedDate} {item.timeIn}      {item.code}");
                }
                if (!string.IsNullOrEmpty(item.timeOut))
                {
                    textBuilder.AppendLine($"            {formattedDate} {item.timeOut}      {item.code}");
                }
    
            }

            // แปลง StringBuilder เป็น byte array
            byte[] byteArray = Encoding.UTF8.GetBytes(textBuilder.ToString());

            // ส่งไฟล์ข้อความกลับไปยังไคลเอ็นต์
            return File(byteArray, "text/plain", "data.txt");
        }
        catch (Exception ex)
        {
            // กรณีเกิดข้อผิดพลาด
            return StatusCode(500, "An error occurred while processing the request.");
        }
    }

    [HttpGet]
    public IActionResult DownloadHIP()
    {
        string dateEnd = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        try
        {
            // หาข้อมูลล่า sql server 
            IConfiguration configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
               .Build();

            string connectionString = configuration.GetConnectionString("DefaultConnection");

            string[] sensorIds = { "2", "3", "4", "5", "6" };
            Dictionary<string, string> dateStartFormatted = new Dictionary<string, string>();

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                foreach (var sensorId in sensorIds)
                {
                    string sql = $"SELECT TOP 1 dateTime FROM dbo.cuttime WHERE sensorID = {sensorId} ORDER BY dateTime DESC";
                    using (SqlCommand startDateCommand = new SqlCommand(sql, sqlConnection))
                    {
                        var result = startDateCommand.ExecuteScalar();
                        if (result != null && result is DateTime dateStart)
                        {
                            dateStartFormatted[sensorId] = dateStart.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else
                        {
                            // Handle case where there is no data for this sensorID
                            dateStartFormatted[sensorId] = DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }
                }
                sqlConnection.Close();
            }

            List<Checks> checkList = new List<Checks>();

            using (OleDbConnection connection = new OleDbConnection(_accessConnectionString))
            {
                connection.Open();
                foreach (var sensorId in sensorIds)
                {
                    string sqlDT = @"SELECT Badgenumber AS nCode, checktime AS _time, sensorid 
                                     FROM CHECKINOUT 
                                     WHERE sensorid = ? AND CHECKTIME BETWEEN ? AND ?";

                    using (OleDbCommand command = new OleDbCommand(sqlDT, connection))
                    {
                        //command.Parameters.AddWithValue("?", sensorId);
                        //command.Parameters.AddWithValue("?", dateStartFormatted[sensorId]);
                        //command.Parameters.AddWithValue("?", dateEnd);
                        command.Parameters.Add(new OleDbParameter("?", sensorId));
                        command.Parameters.Add(new OleDbParameter("?", dateStartFormatted[sensorId]));
                        command.Parameters.Add(new OleDbParameter("?", dateEnd));

                        using (OleDbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var check = new Checks()
                                {
                                    Ecode = reader["nCode"].ToString(),
                                    _DateTime = reader.GetDateTime(reader.GetOrdinal("_time")),
                                    SensorId = reader["sensorid"].ToString(),
                                };
                                checkList.Add(check);
                            }
                        }
                    }
                }
                connection.Close();
            }

            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                foreach (var item in checkList)
                {
                    if (item.SensorId == "1")
                    {
                        continue; // ข้ามการบันทึกข้อมูลนี้และไปที่ item ถัดไป
                    }

                    string checkQuery = @"IF NOT EXISTS (SELECT 1 FROM dbo.cuttime WHERE dateTime = @DateTime AND code = @Code) 
                                      BEGIN 
                                          INSERT INTO dbo.cuttime (dateTime, code, sensorID) 
                                          VALUES (@DateTime, @Code, @SensorId) 
                                      END";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, sqlConnection))
                    {
                        checkCommand.Parameters.AddWithValue("@DateTime", item._DateTime);
                        checkCommand.Parameters.AddWithValue("@SensorId", item.SensorId);

                        if (item.Ecode.Length < 4)
                        {
                            item.Ecode = item.Ecode.PadLeft(4, '0');
                        }

                        checkCommand.Parameters.AddWithValue("@Code", item.Ecode);
                        checkCommand.ExecuteNonQuery();
                    }
                }
                sqlConnection.Close();
            }
            return Json(new { ok = true, data = "OK" });
        }
        catch (Exception ex)
        {
            // กรณีเกิดข้อผิดพลาด
            return StatusCode(500, "An error occurred while processing the request.");
        }
    }




}

