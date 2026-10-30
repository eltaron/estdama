namespace TMS.Core.Security;

public static class Perm
{
    public const string View = "view";
    public const string Create = "create";
    public const string Edit = "edit";
    public const string Delete = "delete";
}

public sealed class PermissionAction
{
    public PermissionAction(string key, string nameAr)
    {
        Key = key;
        NameAr = nameAr;
    }

    public string Key { get; }
    public string NameAr { get; }
}

public sealed class PermissionModule
{
    public PermissionModule(string key, string nameAr, IReadOnlyList<PermissionAction> actions)
    {
        Key = key;
        NameAr = nameAr;
        Actions = actions;
    }

    public string Key { get; }
    public string NameAr { get; }
    public IReadOnlyList<PermissionAction> Actions { get; }

    public string Permission(string actionKey) => $"{Key}.{actionKey}";
}

public static class PermissionCatalog
{
    public const string ClaimType = "Permission";
    public const string AdminRole = "Admin";

    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersEdit = "users.edit";
    public const string UsersDelete = "users.delete";

    public const string RolesView = "roles.view";
    public const string RolesCreate = "roles.create";
    public const string RolesEdit = "roles.edit";
    public const string RolesDelete = "roles.delete";

    public const string CoursesView = "courses.view";
    public const string CoursesCreate = "courses.create";
    public const string CoursesEdit = "courses.edit";
    public const string CoursesDelete = "courses.delete";

    public const string TrainingView = "training.view";
    public const string TrainingCreate = "training.create";
    public const string TrainingEdit = "training.edit";
    public const string TrainingDelete = "training.delete";

    public const string InstructorsView = "instructors.view";
    public const string InstructorsCreate = "instructors.create";
    public const string InstructorsEdit = "instructors.edit";
    public const string InstructorsDelete = "instructors.delete";

    public const string AccountsView = "accounts.view";
    public const string AccountsCreate = "accounts.create";
    public const string AccountsEdit = "accounts.edit";
    public const string AccountsDelete = "accounts.delete";

    public const string ArchiveView = "archive.view";
    public const string ArchiveCreate = "archive.create";
    public const string ArchiveEdit = "archive.edit";
    public const string ArchiveDelete = "archive.delete";

    public const string ReportsView = "reports.view";
    public const string ReportsCreate = "reports.create";
    public const string ReportsEdit = "reports.edit";
    public const string ReportsDelete = "reports.delete";

    public static readonly PermissionModule Users = new("users", "المستخدمون", new[]
    {
        new PermissionAction(Perm.View, "الاطلاع"),
        new PermissionAction(Perm.Create, "الإضافة"),
        new PermissionAction(Perm.Edit, "التعديل"),
        new PermissionAction(Perm.Delete, "الحذف")
    });

    public static readonly PermissionModule Roles = new("roles", "الأدوار والصلاحيات", new[]
    {
        new PermissionAction(Perm.View, "الاطلاع"),
        new PermissionAction(Perm.Create, "الإضافة"),
        new PermissionAction(Perm.Edit, "التعديل"),
        new PermissionAction(Perm.Delete, "الحذف")
    });

    public static readonly PermissionModule Courses = new("courses", "الدورات", new[]
    {
        new PermissionAction(Perm.View, "الاطلاع"),
        new PermissionAction(Perm.Create, "الإضافة"),
        new PermissionAction(Perm.Edit, "التعديل"),
        new PermissionAction(Perm.Delete, "الحذف")
    });

    public static readonly PermissionModule Training = new("training", "المتدربون والتدريب", new[]
    {
        new PermissionAction(Perm.View, "الاطلاع"),
        new PermissionAction(Perm.Create, "الإضافة"),
        new PermissionAction(Perm.Edit, "التعديل"),
        new PermissionAction(Perm.Delete, "الحذف")
    });

    public static readonly PermissionModule Instructors = new("instructors", "المدربون", new[]
    {
        new PermissionAction(Perm.View, "الاطلاع"),
        new PermissionAction(Perm.Create, "الإضافة"),
        new PermissionAction(Perm.Edit, "التعديل"),
        new PermissionAction(Perm.Delete, "الحذف")
    });

    public static readonly PermissionModule Accounts = new("accounts", "الحسابات والمدفوعات", new[]
    {
        new PermissionAction(Perm.View, "الاطلاع"),
        new PermissionAction(Perm.Create, "الإضافة"),
        new PermissionAction(Perm.Edit, "التعديل"),
        new PermissionAction(Perm.Delete, "الحذف")
    });

    public static readonly PermissionModule Archive = new("archive", "الأرشفة", new[]
    {
        new PermissionAction(Perm.View, "الاطلاع"),
        new PermissionAction(Perm.Create, "الإضافة"),
        new PermissionAction(Perm.Edit, "التعديل"),
        new PermissionAction(Perm.Delete, "الحذف")
    });

    public static readonly PermissionModule Reports = new("reports", "التقارير", new[]
    {
        new PermissionAction(Perm.View, "الاطلاع"),
        new PermissionAction(Perm.Create, "الإضافة"),
        new PermissionAction(Perm.Edit, "التعديل"),
        new PermissionAction(Perm.Delete, "الحذف")
    });

    public static readonly PermissionModule[] Modules =
    {
        Users, Roles, Courses, Training, Instructors, Accounts, Archive, Reports
    };

    public static string Permission(string moduleKey, string actionKey) => $"{moduleKey}.{actionKey}";

    public static IEnumerable<string> AllPermissions =>
        Modules.SelectMany(m => m.Actions.Select(a => m.Permission(a.Key)));

    public static PermissionModule? GetModule(string key) =>
        Modules.FirstOrDefault(m => m.Key == key);

    public static string GetActionNameAr(string actionKey) => actionKey switch
    {
        Perm.View => "الاطلاع",
        Perm.Create => "الإضافة",
        Perm.Edit => "التعديل",
        Perm.Delete => "الحذف",
        _ => actionKey
    };
}