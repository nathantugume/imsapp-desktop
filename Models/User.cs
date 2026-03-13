namespace imsapp_desktop.Models;

public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Role { get; set; }
    public string Status { get; set; } = "1";
    public string? Vcode { get; set; }
    public string? Country { get; set; }

    public string StatusDisplay => Status == "1" ? "Active" : "Inactive";
}
