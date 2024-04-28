using Microsoft.AspNetCore.Mvc;
using HRMCutTimeInOut.Models;
using System.Collections.Generic;

namespace HRMCutTimeInOut.Controllers
{
	public class UserController : Controller
	{
		public IActionResult Index()
		{
			/* การประกาศ Obj ทำได้ 3 วิธี ตย ด้าานล่าง
			User u1 = new User();
			var u2 = new User();
			User u3 = new(); */
			Users u1 = new Users();
			u1.Code = "123";
			u1.Name = "XMax";
			u1.Lname = "300";
			u1.Department = "Prod";

			var u2 = new Users();
			u2.Code = "124";
			u2.Name = "pong";
			u2.Lname = "naja";
			u2.Department = "MIS";

			Users u3 = new Users();
			u3.Code = "125";
			u3.Name = "Choojai";
			u3.Lname = "Meeta";
			u3.Department = "Prod2";
			/* การส่ง Obj รวมไป 3 ตัวจะต้องทำให้เป็น list และต้อง ใช้ using system.collections.generic */
			List<Users> allUser = new List<Users>();
			allUser.Add(u1);
			allUser.Add(u2);
			allUser.Add(u3);
			return View(allUser);
		}
		public IActionResult Access()
		{
			return RedirectToAction("Index");
		}
	}
}

