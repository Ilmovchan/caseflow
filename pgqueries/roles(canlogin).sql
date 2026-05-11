SELECT rolname, rolcanlogin
FROM pg_roles
WHERE rolname IN ('admin', 'detective', 'admin_illia', 'detective_kravchuk', 'detective_taran')
ORDER BY rolname;