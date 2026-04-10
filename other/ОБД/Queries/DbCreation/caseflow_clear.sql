DO $$ DECLARE
    table_name TEXT;
BEGIN
    -- Loop through each table in the current schema
    FOR table_name IN
        SELECT tablename
        FROM pg_tables
        WHERE schemaname = 'public'
    LOOP
        EXECUTE format('DROP TABLE IF EXISTS %I CASCADE;', table_name);
    END LOOP;
END $$;
