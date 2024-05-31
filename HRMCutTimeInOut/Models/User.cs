using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
namespace HRMCutTimeInOut.Models;

    public class Checks
    {
	    public string Ecode { get; set; }
	    public DateTime _DateTime { get; set; }
        public string SensorId { get; set;}
    }
    public class Timeinout
    {
	    public int No { get; set; }
        public DateTime _DateTime { get; set; }
        public string FormattedDateTime => _DateTime.ToString("yyyy-MM-dd HH:mm");
        public string Code { get; set; }
        public string Name { get; set; }
        public string DpName { get; set; }
        public string Shift { get; set; }
    }
    public class setTimeModel
    {
       public string startDate { get; set; }
       public string endDate { get; set; }
    }

    public class TimeCutData
{

    private DateTime _dateData;

    public string dateData
    {
        get { return _dateData.ToString("dd-MM-yyyy"); }
        set { _dateData = DateTime.ParseExact(value, "dd-MM-yyyy", CultureInfo.InvariantCulture); }
    }
    public string timeIn {  get; set; }
    public string timeOut { get; set; }
    public string code {  get; set; } 
    public string name { get; set; }    
    public string department { get; set; }
    public string shift { get; set; }   
    public string masterShift { get; set; }
    public string workShift { get; set; }
    public string totalHour { get; set;}
    public string checkResult { get; set;}  
}

public class LoginModel
{
    [Required]
    public string Username { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}