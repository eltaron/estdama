using Microsoft.AspNetCore.Identity;

namespace TMS.Core.Entities.Identity;

public class ApplicationRole : IdentityRole
{
    public string? NameAr { get; set; }
    public string? Description { get; set; }
}
