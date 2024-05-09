namespace EmployeeService.Models
{
    public class EmployeeDto
    {
        public string Name { get; set; }

        public string Surname { get; set; }

        public string Phone { get; set; }

        public int CompanyId { get; set; }

        public int? DepartmentId { get; set; }

        public DepartmentDto? Department { get; set; } = null;

        public PassportDto? Passport { get; set; } = null;
    }

    public class DepartmentDto
    {
        public string Name { get; set; }

        public string Phone { get; set; }
    }

    public class PassportDto
    {
        public string Type { get; set; }

        public string Number { get; set; }
    }
}