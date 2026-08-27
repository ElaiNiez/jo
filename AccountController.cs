using Microsoft.AspNetCore.Mvc;
using NIEZ.Models;
using NIEZ.Service;

namespace NIEZ.Controllers
{
    public class AccountController : Controller
    {
        private readonly Db _db;


        // ==========================================================
        // CONSTRUCTOR
        // ==========================================================

        public AccountController(Db db)
        {
            _db = db;
        }


        // ==========================================================
        // REGISTER
        // ==========================================================

        [HttpPost]
        public IActionResult Register(
            string fullName,
            string email,
            string password)
        {
            User user = new User();

            bool success = user.Register(
                _db,
                fullName,
                email,
                password,
                out string message);

            return Json(new
            {
                success,
                message
            });
        }


        // ==========================================================
        // LOGIN
        // ==========================================================

        [HttpPost]
        public IActionResult Login(
            string email,
            string password)
        {
            User user = new User();

            bool success = user.Login(
                _db,
                email,
                password,
                out int id,
                out string fullName,
                out string message);


            if (success)
            {
                HttpContext.Session.SetInt32(
                    "UserId",
                    id
                );

                HttpContext.Session.SetString(
                    "FullName",
                    fullName
                );
            }


            return Json(new
            {
                success,
                message
            });
        }


        // ==========================================================
        // GET USERS
        // ==========================================================

        [HttpGet]
        public IActionResult GetUsers()
        {
            try
            {
                using (var con = _db.Connection())
                {
                    con.Open();


                    string query = @"
                        SELECT
                            Id,
                            FullName,
                            Email
                        FROM Users
                        ORDER BY Id ASC";


                    using (var cmd =
                        new Microsoft.Data.SqlClient.SqlCommand(
                            query,
                            con))
                    using (var reader =
                        cmd.ExecuteReader())
                    {
                        List<object> users =
                            new List<object>();


                        while (reader.Read())
                        {
                            users.Add(new
                            {
                                id = Convert.ToInt32(
                                    reader["Id"]
                                ),

                                fullName =
                                    reader["FullName"]
                                        .ToString(),

                                email =
                                    reader["Email"]
                                        .ToString()
                            });
                        }


                        return Json(new
                        {
                            success = true,
                            data = users
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        // ==========================================================
        // UPDATE USER
        // ==========================================================

        [HttpPost]
        public IActionResult UpdateUser(
            int id,
            string fullName,
            string email)
        {
            try
            {
                // VALIDATE

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Full Name is required."
                    });
                }


                if (string.IsNullOrWhiteSpace(email))
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Email is required."
                    });
                }


                User user = new User();


                string[] columns =
                {
                    "FullName",
                    "Email"
                };


                string[] values =
                {
                    fullName,
                    email
                };


                bool success =
                    user.Update(
                        _db,
                        "Users",
                        columns,
                        values,
                        "Id",
                        id.ToString(),
                        out string message
                    );


                return Json(new
                {
                    success,

                    message =
                        success
                            ? "User updated successfully."
                            : message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        // ==========================================================
        // DELETE USER
        // ==========================================================

        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                User user = new User();


                bool success =
                    user.Delete(
                        _db,
                        "Users",
                        "Id",
                        id.ToString(),
                        out string message
                    );


                return Json(new
                {
                    success,

                    message =
                        success
                            ? "User deleted successfully."
                            : message
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        // ==========================================================
        // LOGOUT
        // ==========================================================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction(
                "Login",
                "Home"
            );
        }
    }
} 