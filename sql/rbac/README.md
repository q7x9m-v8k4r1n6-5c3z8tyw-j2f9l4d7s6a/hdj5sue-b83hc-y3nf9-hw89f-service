# RBAC SQL scripts

Run the scripts in this order:
1. `001_CreateRbacSchema.sql`
2. `002_SeedRbacData.sql`

Notes:
- `001_CreateRbacSchema.sql` creates `Roles`, `Permissions`, `UserRoles`, and `RolePermissions`.
- `002_SeedRbacData.sql` seeds baseline system roles and permissions, then backfills `UserRoles` from the legacy `Users.Role` column.
- Permission code format follows `Controller.Action`, for example `Race.Create` and `RbacAssignment.AssignRoleToUser`.
- API authorization now checks `permission` claims from JWTs, while login and `me` responses return the resolved roles, permissions, and effective access list.
