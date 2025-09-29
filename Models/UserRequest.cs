using SQLUserForge.Services;

namespace SQLUserForge.Models
{
    public class UserRequest
    {
        public string ServerInstance { get; set; } = "";
        public bool UseIntegratedSecurity { get; set; } = true;
        public string? AdminLogin { get; set; }
        public string? AdminPassword { get; set; }

        public string TargetDatabase { get; set; } = "";
        public string NewLoginName { get; set; } = "";
        public string NewLoginPassword { get; set; } = "";

        public string[] SelectedDbRoles { get; set; } = [];
        public string[] SelectedServerRoles { get; set; } = [];

        public override string ToString()
        {
            var dbRoles = SelectedDbRoles.Length > 0
                ? string.Join(", ", SelectedDbRoles)
                : TranslationProvider.T("none");

            var srvRoles = SelectedServerRoles.Length > 0
                ? string.Join(", ", SelectedServerRoles)
                : TranslationProvider.T("none");

            return
        $@"{TranslationProvider.T("confirm_instance")} : {ServerInstance}
        {TranslationProvider.T("confirm_admin_auth")} : {(UseIntegratedSecurity
                        ? TranslationProvider.T("auth_windows")
                        : $"{TranslationProvider.T("auth_sql")} ({AdminLogin})")}
        {TranslationProvider.T("confirm_login_to_create")} : {NewLoginName}
        {TranslationProvider.T("confirm_password")} : {(string.IsNullOrEmpty(NewLoginPassword) ? TranslationProvider.T("none") : "(hidden)")}
        {TranslationProvider.T("confirm_default_db")} : {TargetDatabase}
        {TranslationProvider.T("confirm_db_roles")} : {dbRoles}
        {TranslationProvider.T("confirm_srv_roles")} : {srvRoles}";
        }
    }
}
