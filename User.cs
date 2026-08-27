using System.Linq;
using Microsoft.Data.SqlClient;
using NIEZ.Service;

namespace NIEZ.Models
{
    public class User
    {
        //==========================================================
        // OPEN CONNECTION
        //==========================================================

        private SqlConnection OpenConnection(Db db)
        {
            SqlConnection con = db.Connection();
            con.Open();
            return con;
        }


        //==========================================================
        // ADD PARAMETERS
        //==========================================================

        private void AddParameters(
            SqlCommand cmd,
            string[] names,
            string[] values)
        {
            for (int i = 0; i < names.Length; i++)
            {
                cmd.Parameters.AddWithValue(
                    names[i],
                    values[i]);
            }
        }


        //==========================================================
        // VALIDATE FIELDS
        //==========================================================

        private bool ValidateFields(
            string[] values,
            string[] fields,
            out string message)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(values[i]))
                {
                    message = fields[i] + " is required.";
                    return false;
                }
            }

            message = "";
            return true;
        }


        //==========================================================
        // CHECK IF RECORD EXISTS
        //==========================================================

        private bool Exists(
            Db db,
            string table,
            string column,
            string value)
        {
            using (SqlConnection con = OpenConnection(db))
            {
                string query =
                    $"SELECT COUNT(*) FROM {table} WHERE {column}=@Value";

                SqlCommand cmd =
                    new SqlCommand(query, con);

                cmd.Parameters.AddWithValue(
                    "@Value",
                    value);

                return Convert.ToInt32(
                    cmd.ExecuteScalar()) > 0;
            }
        }


        //==========================================================
        // BUILD SQL STATEMENT
        //
        // Reused by:
        // INSERT
        // SELECT
        // UPDATE
        // DELETE
        //
        //==========================================================

        private string BuildStatement(
            string action,
            string table,
            string[] columns,
            string[] values,
            string whereColumn = "")
        {
            switch (action.ToUpper())
            {
                //==================================================
                // INSERT
                //==================================================

                case "INSERT":

                    return
                        $"INSERT INTO {table} " +
                        $"({string.Join(",", columns)}) " +
                        $"VALUES ({string.Join(",", values.Select((x, i) => "@value" + i))})";


                //==================================================
                // SELECT
                //==================================================

                case "SELECT":

                    string selectColumns =
                        columns.Length == 0
                            ? "*"
                            : string.Join(",", columns);

                    return
                        $"SELECT {selectColumns} FROM {table}";


                //==================================================
                // UPDATE
                //==================================================

                case "UPDATE":

                    string setColumns =
                        string.Join(
                            ",",
                            columns.Select(
                                (column, i) =>
                                    $"{column}=@value{i}"));

                    return
                        $"UPDATE {table} " +
                        $"SET {setColumns} " +
                        $"WHERE {whereColumn}=@id";


                //==================================================
                // DELETE
                //==================================================

                case "DELETE":

                    return
                        $"DELETE FROM {table} " +
                        $"WHERE {whereColumn}=@id";


                default:

                    throw new ArgumentException(
                        "Invalid SQL action.");
            }
        }


        //==========================================================
        // EXECUTE NON QUERY
        //
        // Reused by:
        // INSERT
        // UPDATE
        // DELETE
        //
        //==========================================================

        private bool ExecuteNonQuery(
            Db db,
            string query,
            string[] parameterNames,
            string[] values,
            out string message)
        {
            try
            {
                using (SqlConnection con = OpenConnection(db))
                using (SqlCommand cmd =
                    new SqlCommand(query, con))
                {
                    AddParameters(
                        cmd,
                        parameterNames,
                        values);

                    cmd.ExecuteNonQuery();

                    message = "Operation successful.";

                    return true;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }


        //==========================================================
        // INSERT
        //
        // Works for ANY TABLE.
        //
        //==========================================================

        public bool Insert(
            Db db,
            string table,
            string[] columns,
            string[] values,
            out string message)
        {
            string[] parameterNames =
                values
                    .Select(
                        (value, i) =>
                            "@value" + i)
                    .ToArray();

            string query =
                BuildStatement(
                    "INSERT",
                    table,
                    columns,
                    parameterNames);

            return ExecuteNonQuery(
                db,
                query,
                parameterNames,
                values,
                out message);
        }


        //==========================================================
        // SELECT
        //
        // Works for ANY TABLE.
        //
        //==========================================================

        public List<Dictionary<string, object>> Select(
            Db db,
            string table,
            string[] columns,
            out string message)
        {
            List<Dictionary<string, object>> rows =
                new List<Dictionary<string, object>>();

            try
            {
                using (SqlConnection con =
                    OpenConnection(db))
                using (SqlCommand cmd =
                    new SqlCommand(
                        BuildStatement(
                            "SELECT",
                            table,
                            columns,
                            Array.Empty<string>()),
                        con))
                using (SqlDataReader reader =
                    cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Dictionary<string, object> row =
                            new Dictionary<string, object>();

                        for (int i = 0;
                             i < reader.FieldCount;
                             i++)
                        {
                            row.Add(
                                reader.GetName(i),
                                reader.GetValue(i));
                        }

                        rows.Add(row);
                    }
                }

                message = "Select successful.";

                return rows;
            }
            catch (Exception ex)
            {
                message = ex.Message;

                return rows;
            }
        }


        //==========================================================
        // UPDATE
        //
        // Works for ANY TABLE.
        //
        //==========================================================

        public bool Update(
            Db db,
            string table,
            string[] columns,
            string[] values,
            string whereColumn,
            string id,
            out string message)
        {
            string[] parameterNames =
                values
                    .Select(
                        (value, i) =>
                            "@value" + i)
                    .Append("@id")
                    .ToArray();

            string[] parameterValues =
                values
                    .Append(id)
                    .ToArray();

            string query =
                BuildStatement(
                    "UPDATE",
                    table,
                    columns,
                    parameterNames,
                    whereColumn);

            return ExecuteNonQuery(
                db,
                query,
                parameterNames,
                parameterValues,
                out message);
        }


        //==========================================================
        // DELETE
        //
        // Works for ANY TABLE.
        //
        //==========================================================

        public bool Delete(
            Db db,
            string table,
            string whereColumn,
            string id,
            out string message)
        {
            string[] parameterNames =
            {
                "@id"
            };

            string[] values =
            {
                id
            };

            string query =
                BuildStatement(
                    "DELETE",
                    table,
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    whereColumn);

            return ExecuteNonQuery(
                db,
                query,
                parameterNames,
                values,
                out message);
        }


        //==========================================================
        // REGISTER
        //
        // Reuses:
        // ValidateFields()
        // Exists()
        // Insert()
        //
        //==========================================================

        public bool Register(
            Db db,
            string fullName,
            string email,
            string password,
            out string message)
        {
            string[] values =
            {
                fullName,
                email,
                password
            };

            string[] fields =
            {
                "Full Name",
                "Email",
                "Password"
            };

            // Validate fields

            if (!ValidateFields(
                values,
                fields,
                out message))
            {
                return false;
            }


            // Check duplicate email

            if (Exists(
                db,
                "Users",
                "Email",
                email))
            {
                message =
                    "Email already exists.";

                return false;
            }


            // Insert user

            string[] columns =
            {
                "FullName",
                "Email",
                "Password"
            };

            if (!Insert(
                db,
                "Users",
                columns,
                values,
                out message))
            {
                return false;
            }


            message =
                "Registration Successful!";

            return true;
        }


        //==========================================================
        // LOGIN
        //
        // Reuses:
        // ValidateFields()
        // AddParameters()
        //
        //==========================================================

        public bool Login(
            Db db,
            string email,
            string password,
            out int id,
            out string fullName,
            out string message)
        {
            id = 0;
            fullName = "";

            string[] values =
            {
                email,
                password
            };

            string[] fields =
            {
                "Email",
                "Password"
            };


            // Validate fields

            if (!ValidateFields(
                values,
                fields,
                out message))
            {
                return false;
            }


            // Login

            try
            {
                using (SqlConnection con =
                    OpenConnection(db))
                {
                    string query =
                    @"SELECT Id, FullName
                      FROM Users
                      WHERE Email=@Email
                      AND Password=@Password";

                    using (SqlCommand cmd =
                        new SqlCommand(query, con))
                    {
                        AddParameters(
                            cmd,
                            new[]
                            {
                                "@Email",
                                "@Password"
                            },
                            values);

                        using (SqlDataReader reader =
                            cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                message =
                                    "Invalid Email or Password.";

                                return false;
                            }

                            id =
                                Convert.ToInt32(
                                    reader["Id"]);

                            fullName =
                                reader["FullName"]
                                    .ToString();

                            message =
                                "Login Successful!";

                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;

                return false;
            }
        }
    }
}