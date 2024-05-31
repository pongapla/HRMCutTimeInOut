namespace HRMCutTimeInOut.Models;

public class Shift
{
    public string name {  get; set; }
    public string code {  get; set; }
    public string nameShift { get; set; }
    public string shiftType { get; set; }
    public string department { get; set; }
}

public class AddShift
{
    public string code { get; set; } 
    public string shift { get; set; }
}