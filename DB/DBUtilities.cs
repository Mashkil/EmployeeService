using Microsoft.Data.Sqlite;
using Dapper;

namespace EmployeeService.DB
{
    public static class DBUtilities
    {
        public static async Task<bool> InitializeDBAsync(this IApplicationBuilder app)
        {
            var connectionString = app.ApplicationServices.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection");

            var createSQL = @"
            CREATE TABLE IF NOT EXISTS Companies (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT
            );

            CREATE TABLE IF NOT EXISTS Departments (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT,
            Phone TEXT
            );

            CREATE TABLE IF NOT EXISTS Passports (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Type TEXT,
            Number TEXT
            );

            CREATE TABLE IF NOT EXISTS Employees (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT,
            Surname TEXT,
            Phone TEXT,
            CompanyId INTEGER,
            PassportId INTEGER,
            DepartmentId INTEGER,
            FOREIGN KEY (CompanyId) REFERENCES Companies(Id),
            FOREIGN KEY (PassportId) REFERENCES Passports(Id),
            FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
            );";

            var insertCompanySQL = @"
           INSERT INTO Companies (Name)
           VALUES 
                ('First'),
                ('Second'),
                ('Third')";

            var insertDepartmentSQL = @"
           INSERT INTO Departments (Name, Phone)
           VALUES 
                ('First', '+777777'),
                ('Second', '+88888'),
                ('Third', '+99999'),
                ('Fourth', '+44444')";

            var insertPassportSQL = @"
           INSERT INTO Passports (Type, Number)
           VALUES 
                ('main passport', '312343'),
                ('main passport', '456221'),
                ('main passport', '42134'),
                ('main passport', '656467'),
                ('international passport', '96758')";

            var insertEmployeesSQL = @"
           INSERT INTO Employees (Name, Surname, Phone, CompanyId, PassportId, DepartmentId)
           VALUES 
                ('Tony', 'Stark', '1111111', 1, 1, 1),
                ('Bruce', 'Wayne', '2222222', 1, 2, 1),
                ('Peter', 'Parker', '33333333', 1, 3, 2),
                ('Diana', 'Prince', '4444444', 2, 4, 3),
                ('Clark', 'Kent', '5555555', 3, 5, 4)";

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {

                // Check if the Customer table exists
                var tableExists = await connection.QueryFirstOrDefaultAsync<int>(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table';", transaction: transaction);

                if (tableExists > 4)
                    return true;

                await connection.ExecuteAsync(createSQL, transaction: transaction);
                await connection.ExecuteAsync(insertCompanySQL, transaction: transaction);
                await connection.ExecuteAsync(insertDepartmentSQL, transaction: transaction);
                await connection.ExecuteAsync(insertPassportSQL, transaction: transaction);
                await connection.ExecuteAsync(insertEmployeesSQL, transaction: transaction);

                transaction.Commit();
                connection.Close();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                transaction.Rollback();
                connection.Close();
                return false;
            }
        }
    }
}
