SELECT 
    r.rolname AS role_name,
    u.rolname AS member_name
FROM pg_auth_members am
JOIN pg_roles r ON am.roleid = r.oid
JOIN pg_roles u ON am.member = u.oid
WHERE r.rolname IN ('admin', 'detective')
ORDER BY r.rolname, u.rolname;