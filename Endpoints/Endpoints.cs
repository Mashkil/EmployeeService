using AutoMapper;
using Dapper;
using EmployeeService.DB;
using EmployeeService.Models;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeService.Endpoints
{
    public static class Endpoints
    {
        public static void MapEndpoints(this IEndpointRouteBuilder builder)
        {
            builder.MapGet("employees/{companyId}", async (int companyId, SqlConnectionFactory connectionFactory) =>
            {
                using var connection = connectionFactory.CreateConnection();
                const string request =
                "SELECT " +
                "e.Id, " +
                "e.Name, " +
                "e.Surname, " +
                "e.Phone, " +
                "e.CompanyId, " +
                "e.PassportId, " +
                "e.DepartmentId, " +
                $"d.Id AS {nameof(Department.Id)}, " +
                $"d.Name AS {nameof(Department.Name)}, " +
                $"d.Phone AS {nameof(Department.Phone)}, " +
                $"p.Id AS {nameof(Passport.Id)}, " +
                $"p.Number AS {nameof(Passport.Number)}, " +
                $"p.Type AS {nameof(Passport.Type)} " +
                "FROM Employees e " +
                "JOIN Departments d ON e.DepartmentId = d.Id " +
                "JOIN Passports p ON e.PassportId = p.Id " +
                "Where e.CompanyId = @CompanyId";

                var employees = await connection.QueryAsync<Employee, Department, Passport, Employee>(request,
                    (employee, department, passport) =>
                    {
                        employee.Department = department;
                        employee.Passport = passport;
                        return employee;
                    },
                    splitOn: $"{nameof(Department.Id)},{nameof(Passport.Id)}",
                    param: new { CompanyId = companyId }
                    );

                return Results.Ok(employees);
            });

            builder.MapGet("employees/{companyId}/{departmentId}", async (int companyId, int departmentId, SqlConnectionFactory connectionFactory) =>
            {
                using var connection = connectionFactory.CreateConnection();
                const string request =
                "SELECT " +
                "e.Id, " +
                "e.Name, " +
                "e.Surname, " +
                "e.Phone, " +
                "e.CompanyId, " +
                "e.PassportId, " +
                "e.DepartmentId, " +
                $"d.Id AS {nameof(Department.Id)}, " +
                $"d.Name AS {nameof(Department.Name)}, " +
                $"d.Phone AS {nameof(Department.Phone)}, " +
                $"p.Id AS {nameof(Passport.Id)}, " +
                $"p.Number AS {nameof(Passport.Number)}, " +
                $"p.Type AS {nameof(Passport.Type)} " +
                "FROM Employees e " +
                "JOIN Departments d ON e.DepartmentId = d.Id " +
                "JOIN Passports p ON e.PassportId = p.Id " +
                "Where e.CompanyId = @CompanyId AND e.DepartmentId = @DepartmentId";

                var employees = await connection.QueryAsync<Employee, Department, Passport, Employee>(request,
                    (employee, department, passport) =>
                    {
                        employee.Department = department;
                        employee.Passport = passport;
                        return employee;
                    },
                    splitOn: $"{nameof(Department.Id)},{nameof(Passport.Id)}",
                    param: new { CompanyId = companyId, DepartmentId = departmentId }
                    );

                return Results.Ok(employees);
            });

            builder.MapPost("employees", async ([FromBody] EmployeeDto employeeDto, SqlConnectionFactory connectionFactory) =>
            {
                if (employeeDto.Passport == null)
                    return Results.BadRequest("You must create new passport for new employee");
                else if (employeeDto.Passport.Type == null || employeeDto.Passport.Number == null)
                    return Results.BadRequest("Passport's fields cannot be null");

                if (employeeDto.DepartmentId == null && employeeDto.Department == null)
                    return Results.BadRequest("You must create new department for new employee or select available");

                using var connection = connectionFactory.CreateConnection();
                connection.Open();

                using var transaction = connection.BeginTransaction();

                try
                {
                    const string insertPassport = @"
                    INSERT INTO Passports (Type, Number)                    
                    VALUES (@Type, @Number); SELECT last_insert_rowid();";

                    var passportId = await connection.ExecuteScalarAsync<int>(insertPassport, new { employeeDto.Passport.Type, employeeDto.Passport.Number }, transaction);

                    if (employeeDto.DepartmentId == null && employeeDto.Department != null)
                    {
                        const string insertDepartment = @"
                        INSERT INTO Departments (Name, Phone)
                        VALUES (@Name, @Phone); SELECT last_insert_rowid();";

                        var departmentId = await connection.ExecuteScalarAsync<int>(insertDepartment, new { employeeDto.Department.Name, employeeDto.Department.Phone }, transaction);
                        employeeDto.DepartmentId = departmentId;
                    }

                    const string insertQuery = @"
                    INSERT INTO Employees (Name, Surname, Phone, CompanyId, PassportId, DepartmentId)
                    VALUES (@Name, @Surname, @Phone, @CompanyId, @PassportId, @DepartmentId);
                    SELECT last_insert_rowid();";

                    var config = new MapperConfiguration(
                        cfg => cfg.CreateMap<EmployeeDto, Employee>()
                        .ForMember(e => e.Department, c => c.Ignore())
                        .ForMember(e => e.Passport, c => c.Ignore()));

                    var mapper = new Mapper(config);

                    var employee = mapper.Map<EmployeeDto, Employee>(employeeDto);
                    employee.PassportId = passportId;

                    var newEmployeeId = await connection.ExecuteScalarAsync<int>(insertQuery, employee, transaction);

                    transaction.Commit();
                    connection.Close();
                    return Results.Ok(new { Id = newEmployeeId });
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    connection.Close();
                    return Results.Problem(null, null, 500, e.Message);
                }
            });

            builder.MapDelete("employees/{employeeId}", async (int employeeId, SqlConnectionFactory connectionFactory) =>
            {
                using var connection = connectionFactory.CreateConnection();
                connection.Open();

                using var transaction = connection.BeginTransaction();
                try
                {
                    const string getEmployeePassportId = "Select PassportId FROM Employees WHERE Id = @EmployeeId";

                    var passportId = await connection.ExecuteScalarAsync<int>(getEmployeePassportId, new { EmployeeId = employeeId });

                    const string deletePassportQuery = "DELETE FROM Passports WHERE Id = @PassportId";

                    const string deleteEmployeeQuery = "DELETE FROM Employees WHERE Id = @EmployeeId";

                    await connection.ExecuteAsync(deleteEmployeeQuery, new { EmployeeId = employeeId });

                    await connection.ExecuteAsync(deletePassportQuery, new { PassportId = passportId });

                    transaction.Commit();
                    connection.Close();

                    return Results.Ok();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    connection.Close();

                    return Results.Problem(null, null, 500, e.Message);
                }
            });

            builder.MapPut("employees/{employeeId}", async (int employeeId, [FromBody] UpdateEmployeeDto employee, SqlConnectionFactory connectionFactory) =>
            {
                using var connection = connectionFactory.CreateConnection();
                connection.Open();

                if (connection.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM Employees WHERE Id = @Id", new { Id = employeeId }) < 1)
                {
                    connection.Close();
                    return Results.BadRequest($"There is no employee with id {employeeId}");
                }

                using var transaction = connection.BeginTransaction();

                try
                {
                    const string baseUpdateEmployee = "UPDATE Employees SET ";
                    var setStatements = new List<string>();

                    if (!string.IsNullOrEmpty(employee.Name))
                        setStatements.Add($"Name = @{nameof(UpdateEmployeeDto.Name)}");

                    if (!string.IsNullOrEmpty(employee.Surname))
                        setStatements.Add($"Surname = @{nameof(UpdateEmployeeDto.Surname)}");

                    if (!string.IsNullOrEmpty(employee.Phone))
                        setStatements.Add($"Phone = @{nameof(UpdateEmployeeDto.Phone)}");

                    if (employee.CompanyId != null)
                    {
                        var result = connection.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM Companies WHERE Id = @Id", new { Id = employee.CompanyId });

                        if (result < 1)
                        {
                            transaction.Rollback();
                            connection.Close();
                            return Results.BadRequest($"Company with Id {employee.CompanyId} does not exist");
                        }

                        setStatements.Add($"CompanyId = @{nameof(UpdateEmployeeDto.CompanyId)}");
                    }

                    if (employee.DepartmentId != null)
                    {
                        var result = connection.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM Departments WHERE Id = @Id", new { Id = employee.DepartmentId });

                        if (result < 1)
                        {
                            transaction.Rollback();
                            connection.Close();
                            return Results.BadRequest($"Department with Id {employee.DepartmentId} does not exist");
                        }

                        setStatements.Add($"DepartmentId = @{nameof(EmployeeDto.DepartmentId)}");
                    }

                    if (employee.Passport != null)
                    {
                        var passportId = connection.QueryFirstOrDefault<int?>("SELECT PassportId FROM Employees WHERE Id = @Id", new { Id = employeeId });

                        if (passportId != null)
                        {
                            const string updatePassport = @"
                            UPDATE Passports
                            SET Type = @Type, 
                            Number = @Number
                            WHERE Id = @PassportId;";

                            await connection.ExecuteAsync(updatePassport, new
                            {
                                employee.Passport.Type,
                                employee.Passport.Number,
                                PassportId = passportId
                            }, transaction);
                        }
                    }

                    var updateEmployee = baseUpdateEmployee + string.Join(", ", setStatements) + " WHERE Id = @EmployeeId;";

                    await connection.ExecuteAsync(updateEmployee, new
                    {
                        employee.Name,
                        employee.Surname,
                        employee.Phone,
                        employee.CompanyId,
                        employee.DepartmentId,
                        EmployeeId = employeeId
                    }, transaction);

                    transaction.Commit();
                    connection.Close();

                    return Results.NoContent();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    connection.Close();

                    return Results.Problem(null, null, 500, e.Message);
                }
            });
        }
    }
}